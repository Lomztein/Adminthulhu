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

        public bool isAdminOnly = false;
        public bool availableInDM = false;
        public bool availableOnServer = true;
        public bool commandEnabled = false;
        public bool allowInMain = false;

        public List<Overload> overloads = new List<Overload> ();

        public class FindMethodResult {
            public MethodInfo method;
            public List<object> parameters;

            public FindMethodResult(MethodInfo _method, List<object> _parameters) {
                method = _method; parameters = _parameters;
            }
        }

        // Don't look at this, it became a bit fucky after command chaining was implemented.
        public async Task<FindMethodResult> FindMethod(SocketUserMessage e, params string [ ] arguments) {
            MethodInfo [ ] infos = GetType ().GetMethods ().Where (x => x.Name == "Execute").ToArray ();
            List<object> parameterList = new List<object> ();

            foreach (MethodInfo inf in infos) {
                ParameterInfo [ ] paramInfo = inf.GetParameters ();

                if (arguments.Length == 1)
                    if (arguments [ 0 ] [ 0 ] == '(')
                        arguments = Utility.SplitArgs (GetParenthesesArgs (arguments [ 0 ])).ToArray ();

                bool isMethod = paramInfo.Length - 1 == arguments.Length; // Have to off-by-one since all commands gets the SocketUserMessage parsed through.

                if (isMethod == true) {
                    for (int i = 1; i < paramInfo.Length; i++) {
                        try {
                            object arg = arguments [ i - 1 ];

                            if (arg != null) {
                                while (arg != null && arg.ToString () [ 0 ].IsTrigger ()) {
                                    string newCmd = "";
                                    List<string> newArgs = new List<string> ();

                                    newArgs = Utility.ConstructArguments (arg.ToString ().Substring (1), out newCmd);

                                    Program.FoundCommandResult fr = await Program.FindAndExecuteCommand (e, newCmd, newArgs, Program.commands);
                                    Result res = fr.result;
                                    arg = res.value;
                                }
                            }

                            try {
                                if (arg != null) {
                                    object obj = Convert.ChangeType (arg, paramInfo [ i ].ParameterType);
                                    parameterList.Add (obj);
                                } else {
                                    throw new Exception ();
                                }
                            } catch {
                                if (paramInfo [ i ].ParameterType.IsInstanceOfType (arg) || arg == null)
                                    parameterList.Add (arg);
                                else
                                    throw new Exception ();
                            }
                        } catch (Exception exc) {
                            Logging.Log (Logging.LogType.EXCEPTION, exc.Message + " - " + exc.StackTrace);
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

        public virtual async Task<Result> TryExecute(SocketUserMessage e, params string[] arguments) {
            string executionError = AllowExecution (e, arguments.ToList ());
            string executionPrefix = "Failed to execute command " + command;
            if (executionError == "") {
                FindMethodResult result = await FindMethod (e, arguments);
                if (result != null) {
                    try {
                        result.parameters.Insert (0, e);
                        Result task = await (result.method.Invoke (this, result.parameters.ToArray ()) as Task<Result>);
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

        public virtual string AllowExecution (SocketMessage e, List<string> args) {

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

            return errors;
        }

        public virtual void Initialize () {
            LoadConfiguration ();
            BotConfiguration.AddConfigurable (this);
        }

        public virtual string GetHelp (SocketMessage e) {
            string help = "";
            string executionErrors = AllowExecution (e, null);
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
    }
}
