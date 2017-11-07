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

        public async Task<List<object>> ConvertChainCommandsToObjects(SocketUserMessage e, List<object> input, int depth) {
            List<object> converted = new List<object> ();

            foreach (object obj in input) {
                object result = obj;
                string stringObj = obj.ToString ();

                if (stringObj.Length > 0) {
                    if (stringObj [ 0 ].IsTrigger ()) {

                        string cmd;
                        List<string> args = Utility.ConstructArguments (stringObj.Substring (1), out cmd);

                        Program.FoundCommandResult foundCommandResult = await Program.FindAndExecuteCommand (e, cmd, args, Program.commands, depth + 1, false, true);
                        if (foundCommandResult.result != null) {
                            result = foundCommandResult.result.value;
                        }
                    } else if (stringObj [ 0 ] == '{') {
                        int endIndex = stringObj.IndexOf ('}');
                        if (endIndex != -1) {
                            string varName = stringObj.Substring (1, endIndex - 1);
                            result = CommandVariables.Get (e.Id, varName);
                        }
                    }
                }

                converted.Add (result);
            }

            return converted;
        }

        // Don't look at this, it became a bit fucky after command chaining was implemented.
        public async Task<FindMethodResult> FindMethod(params object [ ] arguments) {
            MethodInfo [ ] infos = GetType ().GetMethods ().Where (x => x.Name == "Execute").ToArray ();
            dynamic parameterList = new List<object> ();

            foreach (MethodInfo inf in infos) {
                ParameterInfo [ ] paramInfo = inf.GetParameters ();

                bool anyParams = paramInfo.Any (x => x.IsDefined (typeof (ParamArrayAttribute)));
                bool isMethod = paramInfo.Length - 1 == arguments.Length || (anyParams && arguments.Length >= paramInfo.Length); // Have to off-by-one since all commands gets the SocketUserMessage parsed through.

                if (isMethod == true) {
                    for (int i = 1; i < paramInfo.Length; i++) {
                        try {
                            int argIndex = i - 1;
                            object arg = arguments [ argIndex ];

                            if (paramInfo [ i ].IsDefined (typeof (ParamArrayAttribute)) && !arguments [ argIndex ].GetType ().IsArray) {
                                Type elementType = paramInfo [ i ].ParameterType.GetElementType ();

                                dynamic dyn = Activator.CreateInstance (typeof (List<>).MakeGenericType (elementType));
                                for (int j = argIndex; j < arguments.Length; j++) {
                                    TryAddToParams (ref dyn, arguments [ j ], elementType);
                                }

                                arg = dyn.ToArray ();
                            }

                            TryAddToParams (ref parameterList, arg, paramInfo [ i ].ParameterType);
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

        private void TryAddToParams(ref dynamic paramList, object arg, Type type) {
            dynamic result = TryConvert (arg, type);
            paramList.Add (result);
        }

        private object TryConvert(object toConvert, Type type) {
            try {
                if (toConvert != null) {
                    dynamic obj = Convert.ChangeType (toConvert, type);
                    return obj;
                } else {
                    throw new Exception ();
                }
            } catch {
                if (type.IsInstanceOfType (toConvert) || toConvert == null)
                    return toConvert;
                else
                    throw new Exception ();
            }
        }

        public virtual async Task<Result> TryExecute(SocketUserMessage e, int depth, params object[] arguments) {
            string executionError = AllowExecution (e);
            string executionPrefix = "Failed to execute command " + command;
            if (executionError == "") {

                if (arguments.Length == 1) {
                    string stringArg = arguments [ 0 ].ToString ();
                    if (stringArg [ 0 ] == '(')
                        arguments = Utility.SplitArgs (GetParenthesesArgs (stringArg)).ToArray ();
                }

                arguments = (await ConvertChainCommandsToObjects (e, arguments.ToList (), depth)).ToArray ();
                FindMethodResult result = await FindMethod (arguments);
                if (result != null) {
                    try {
                        result.parameters.Insert (0, e);
                        Result task = await (result.method.Invoke (this, result.parameters.ToArray ()) as Task<Result>);
                        AddToCallstack (e.Id, new Callstack.Item (this, result.parameters.GetRange(1, result.parameters.Count - 1).Select (x => x.ToString ()).ToList (), task.message, task.value));
                        return task;

                    } catch (Exception exc) {
                        Logging.Log (exc);
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

        public Embed GetHelpEmbed(SocketMessage e, bool advanced) {
            EmbedBuilder builder = new EmbedBuilder ();
            if (!commandEnabled) {
                builder.WithTitle ("Not enabled on this server.");
                return builder.Build ();
            }

            builder.WithAuthor (Utility.GetUserName (Utility.GetServer ().GetUser (Program.discordClient.CurrentUser.Id)) + " Command Help") // lolwhat.
                .WithTitle ($"Command \"{helpPrefix}{command}\"")
                .WithDescription (shortHelp);

            // This is quite similar to GetArgs and GetHelp together, and the other functions are obsolete due to this.
            MethodInfo [ ] methods = GetType ().GetMethods ().Where (x => x.Name == "Execute").ToArray ();
            for (int i = 0; i < methods.Length; i++) {
                if (overloads.Count <= i) {
                    builder.AddField ("Undefined overload", "Blame that lazy bastard of a dev.");
                } else {
                    MethodInfo info = methods [ i ];
                    Overload ol = overloads [ i ];

                    string olText = advanced ? $"{ol.returnType.Name} => " : helpPrefix + command;

                    ParameterInfo [ ] parameters = info.GetParameters ();

                    olText += " (";
                    for (int j = 1; j < parameters.Length; j++) { // Remember to ignore first parameter, it being the SocketUserMessage.
                        ParameterInfo pInfo = parameters [ j ];
                        olText += advanced ? pInfo.ParameterType.Name + " " + pInfo.Name : pInfo.Name;

                        if (j != parameters.Length - 1)
                            olText += "; ";
                    }
                    olText += ")";

                    builder.AddField (olText, ol.description);
                }
            }

            string footer = string.Empty;
            if (isAdminOnly)
                footer += " - ADMIN ONLY";
            if (allowInMain)
                footer += " - ALLOWED IN MAIN";
            if (availableInDM && !availableOnServer)
                footer += " - ONLY IN DM";
            if (availableInDM && availableOnServer)
                footer += " - AVAILABLE IN DM";
            if (requiredPermission != Permissions.Type.Null)
                footer += " - REQUIRIES PERMISSION: " + requiredPermission.ToString ().ToUpper ();

            builder.WithColor (CSetColor.GetUserColor (Program.discordClient.CurrentUser.Id).Color);
            builder.WithFooter (footer);
            return builder.Build ();
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
                commandEnabled = BotConfiguration.GetSetting ("Command." + path + command.Capitalize () + "Enabled", this, false);
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

        public static List<Command> RecursiveCacheCommands (List<Command> source) {
            List<Command> result = new List<Command> ();
            foreach (Command cmd in source) {
                if (cmd is CommandSet) {
                    CommandSet set = cmd as CommandSet;
                    result.AddRange (RecursiveCacheCommands (set.commandsInSet.ToList ()));
                } else {
                    result.Add (cmd);
                }
            }
            return result;
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

        /*public class Request {

    public Command command;
    public List<object> arguments;
    public SocketUserMessage userMessage;

    public Request(Command _command, List<object> _arguments, SocketUserMessage _userMessage) {
        command = _command;
        arguments = _arguments;
        userMessage = _userMessage;
    }

    public async Task<Result> Execute(int depth) {
        return await command.TryExecute (userMessage, depth, arguments.ToArray ());
    }
}*/
    }
}
