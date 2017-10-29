using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using System.Reflection;

namespace Adminthulhu {
    public class Command : IConfigurable {

        public enum Category {
            None, Utility, Fun, Set, Advanced, Admin 
        }

        public const int CHARS_TO_HELP = 4;

        public string command = null;
        public string shortHelp = null;
        public string helpPrefix = Program.commandTrigger;
        public Category catagory = Category.None;
        public Permissions.Type requiredPermission = Permissions.Type.Null;

        public bool isAdminOnly = false;
        public bool availableInDM = false;
        public bool availableOnServer = true;
        public bool commandEnabled = false;
        public bool allowInMain = false;

        public List<Overload> overloads = new List<Overload> ();

        public static List<Callstack> callstacks = new List<Callstack> ();
        public static int maxCallstacks = 64;

        public class FindMethodResult {
            public MethodInfo method;
            public List<object> parameters;

            public FindMethodResult(MethodInfo _method, List<object> _parameters) {
                method = _method; parameters = _parameters;
            }
        }

        // Don't look at this, it became a bit fucky after command chaining was implemented.
        public async Task<FindMethodResult> FindMethod(SocketUserMessage e, int depth, params string [ ] arguments) {
            MethodInfo [ ] infos = GetType ().GetMethods ().Where (x => x.Name == "Execute").ToArray ();
            List<object> parameterList = new List<object> ();

            foreach (MethodInfo inf in infos) {
                ParameterInfo [ ] paramInfo = inf.GetParameters ();

                if (arguments.Length == 1)
                    if (arguments [ 0 ] [ 0 ] == '(')
                        arguments = Utility.SplitArgs (GetParenthesesArgs (arguments [ 0 ])).ToArray ();

                bool containsParamArray = paramInfo.Any (x => x.IsDefined (typeof (ParamArrayAttribute), false));
                bool isMethod = paramInfo.Length - 1 == arguments.Length || (arguments.Length >= paramInfo.Length && containsParamArray); // Have to off-by-one since all commands gets the SocketUserMessage parsed through.

                if (isMethod == true) {
                    for (int i = 1; i < paramInfo.Length; i++) {
                        try {

                            object arg = null;
                            List<object> paramArray = new List<object> ();

                            if (paramInfo[i].IsDefined (typeof (ParamArrayAttribute), false)) {
                                dynamic dyn = Activator.CreateInstance (Type.GetType ($"System.Collections.Generic.List`1[{paramInfo [ i ].ParameterType.GetElementType ().FullName}]"));
                                
                                if (!arguments [ i - 1 ].GetType ().IsArray) {
                                    for (int j = i - 1; j < arguments.Length; j++) {
                                        arg = arguments [ j ];

                                        try {
                                            dynamic convert = TryConvert (paramInfo [ i ], arg, true);
                                            dyn.Add (convert);
                                        } catch { }

                                        arg = await TryExecuteChainCommand (e, arg, depth);
                                    }
                                    List<double> doub = new List<double> ();
                                    arg = dyn.ToArray ();
                                } else {
                                    arg = arguments[i];
                                }

                            } else {
                                arg = arguments [ i - 1 ];
                            }

                            arg = await TryExecuteChainCommand (e, arg, depth);

                            if (arg != null)
                                TryAddToParams (ref parameterList, paramInfo [ i ], arg, false);
                            else
                                parameterList.Add (null);
                        } catch (Exception exc) {
                            Logging.Log (Logging.LogType.EXCEPTION, exc.Message);
                            isMethod = false;
                            parameterList.Clear ();
                            break;
                        }
                    }
                }

                if (isMethod) {
                    return new FindMethodResult (inf, parameterList);
                }
            }

            return null;
        }

        private async Task<object> TryExecuteChainCommand(SocketUserMessage e, object arg, int depth) {
            if (arg != null) {
                while (arg != null && arg.ToString () [ 0 ].IsTrigger ()) {
                    string newCmd = "";
                    List<string> newArgs = new List<string> ();

                    newArgs = Utility.ConstructArguments (arg.ToString ().Substring (1), out newCmd);

                    Program.FoundCommandResult fr = await Program.FindAndExecuteCommand (e, newCmd, newArgs, Program.commands, depth);
                    Result res = fr.result;
                    arg = res.value;
                }
            }
            return arg;
        }

        private void TryAddToParams(ref List<object> paramList, ParameterInfo info, object arg, bool hasParamsAttribute) {
            Type type = hasParamsAttribute ? info.ParameterType.GetElementType () : info.ParameterType;
            paramList.Add (TryConvert (info, arg, hasParamsAttribute));
        }

        private object TryConvert(ParameterInfo info, object toConvet, bool hasParamsAttribute) {
            Type type = hasParamsAttribute ? info.ParameterType.GetElementType () : info.ParameterType;
            try {
                if (toConvet != null) {
                    object obj = Convert.ChangeType (toConvet, type);
                    return obj;
                } else {
                    throw new Exception ();
                }
            } catch {
                if (type.IsInstanceOfType (toConvet) || toConvet == null)
                    return toConvet;
                else
                    throw new Exception ();
            }
        }

        public virtual async Task<Result> TryExecute(SocketUserMessage e, int depth, params string[] arguments) {
            string executionError = AllowExecution (e);
            string executionPrefix = "Failed to execute command " + command;
            if (executionError == "") {
                FindMethodResult result = await FindMethod (e, depth, arguments);
                if (result != null) {
                    try {
                        result.parameters.Insert (0, e);
                        Result task = await (result.method.Invoke (this, result.parameters.ToArray ()) as Task<Result>);
                        AddToCallstack (e.Id, new Callstack.Item (this, result.parameters.GetRange(1, result.parameters.Count - 1).Select (x => x.ToString ()).ToList (), task.message, task.value));
                        return task;

                    } catch (Exception exc) {
                        Logging.Log (Logging.LogType.EXCEPTION, exc.Message);
                    }
                } else {
                    Program.messageControl.SendMessage (e, $"{executionPrefix}: \n\tNo suitable command overload found.", allowInMain);
                }
            } else {
                Program.messageControl.SendMessage (e, $"{executionPrefix}: Failed to execute: {executionError}", allowInMain);
            }
            return null;
        }

        public Task<Result> TaskResult(object value, string message) {
            return Task.FromResult (new Result (value, message));
        }

        public virtual string AllowExecution (SocketMessage e) {

            string errors = string.Empty;

            if (commandEnabled == false)
                errors += "\n\tNot enabled on this server.";

            if (!availableInDM && e.Channel as SocketDMChannel != null) {
                errors += "\n\tNot available in DM channels.";
            }

            if (isAdminOnly && !(e.Author as SocketGuildUser).GuildPermissions.Administrator) {
                errors += "\n\tUser is not administrator.";
            }

            if (!availableOnServer && e.Channel as SocketGuildChannel != null) {
                errors += "\n\tNot avaiable on server.";
            }

            if (requiredPermission != Permissions.Type.Null && !Permissions.HasPermission (e.Author as SocketGuildUser, requiredPermission)) {
                errors += "\n\tNot permitted to use this command.";
            }

            return errors;
        }

        public virtual void Initialize () {
            LoadConfiguration ();
            BotConfiguration.AddConfigurable (this);
        }

        public static void AddToCallstack(ulong chainID, Callstack.Item item) {
            Callstack curStack = callstacks.Find (x => x.chainID == chainID);
            if (curStack == null) {
                curStack = new Callstack (chainID);
                callstacks.Insert (0, curStack);
            }

            curStack.items.Add (item);
            if (callstacks.Count > maxCallstacks)
                callstacks.RemoveAt (maxCallstacks);
        }

        public virtual string GetHelp (SocketMessage e) {
            string help = "";
            string executionErrors = AllowExecution (e);
            if (executionErrors == "") {
                help += "**" + (helpPrefix + command + " - " + shortHelp) + "**```";
                AddArgs (ref help);
                help += "```";

                if (isAdminOnly)
                    help += "**ADMIN ONLY**";
                return help;
            } else {
                return "Failed to execute\n" + executionErrors;
            }
        }

        public virtual string GetShortHelp () {
            string text = "`" + shortHelp + helpPrefix + command + "`";
            return text;
        }

        public virtual void AddArgs(ref string description) {
            MethodInfo [ ] methods = GetType ().GetMethods ().Where (x => x.Name == "Execute").ToArray ();
            for (int i = 0; i < methods.Length; i++) {
                description += $"{overloads[i].returnType.Name} {GetCommand ()} "; // I have no idea whats going on there.
                ParameterInfo [ ] paramInfos = methods [ i].GetParameters ();
                for (int j = 1; j < paramInfos.Length; j++) {
                    description += $"<{paramInfos[j].Name} {paramInfos[j].ParameterType.Name}>";
                    if (j != paramInfos.Length - 1)
                        description += " ; ";
                    else
                        description += " ";
                }
                description += "- " + overloads[i].description + "\n";
            }
        }

        public virtual string GetCommand() {
            return helpPrefix + command;
        }

        public virtual string GetOnlyName() {
            return shortHelp; // Wrapper functions ftw
        }

        public void AddOverload(Type type, string description) {
            overloads.Add (new Overload (type, description));
        }

        public virtual void LoadConfiguration() {
            string [ ] parts = helpPrefix.Length > 1 ? helpPrefix.Substring(1).Split (' ') : new string [0];
            string path = "";
            foreach (string part in parts) {
                if (part.Length > 0)
                    path += part.Substring (0, 1).ToUpper () + part.Substring (1) + ".";
            }
            if (this as CommandChain.CustomCommand != null) {
                commandEnabled = true; // Force enable custom commands.
            } else {
                commandEnabled = BotConfiguration.GetSetting ("Command." + path + command.Substring (0, 1).ToUpper () + command.Substring (1) + "Enabled", "Command" + command.Substring (0, 1).ToUpper () + command.Substring (1) + "Enabled", false);
            }
        }

        public Command CloneCommand() {
            return this.MemberwiseClone () as Command;
        }

        public static string GetParenthesesArgs(string input) {
            int startIndex = 0, endIndex = input.Length;
            int balance = 0;

            for (int i = 0; i < input.Length; i++) {
                if (input [ i ] == '(') {
                    if (balance == 0)
                        startIndex = i;
                    balance++;
                }
                if (input [ i ] == ')') {
                    balance--;

                    if (balance == 0) {
                        endIndex = i;
                        break;
                    }
                }
            }
            return input.Substring (startIndex + 1, endIndex - startIndex - 1);
        }

        public static bool TryIsolateWrappedCommand(string input, out string cmd, out List<string> args) {
            cmd = "";
            args = new List<string> ();

            if (input.Length > 1 && input [ 1 ].IsTrigger ()) {
                args = Utility.ConstructArguments (GetParenthesesArgs (input), out cmd);
                cmd = cmd.Substring (1);
                return true;
            }
            return false;
        }

        public class Overload {
            public Type returnType;
            public string description;

            public Overload(Type _returnType, string _description) {
                returnType = _returnType;
                description = _description;
            }
        }

        public class Result {
            public object value;
            public string message;

            public Result(object _value, string _message) {
                value = _value;
                message = _message;
            }
        }

        public class Callstack {

            public ulong chainID;
            public List<Item> items;

            public Callstack(ulong _chainID) {
                chainID = _chainID;
                items = new List<Item> ();
            }

            public class Item {
                public Command command;
                public List<string> arguments;
                public string message;
                public object returnObj;

                public Item(Command _command, List<string> _arguments, string _message, object _returnObj) {
                    command = _command;
                    arguments = _arguments;
                    message = _message;
                    returnObj = _returnObj;
                }
            }
        }
    }
}
