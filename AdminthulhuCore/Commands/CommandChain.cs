using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Adminthulhu
{
    public static class CommandChain {

        public class Data {
            public List<CustomCommand> customCommands = new List<CustomCommand> ();
        }

        public static Data data;
        public static string dataFileName = "customcommands";

        public static void Initialize() {
            LoadData ();
        }

        public static void LoadData() {
            data = SerializationIO.LoadObjectFromFile<Data> (Program.dataPath + dataFileName + Program.gitHubIgnoreType);
            if (data == null)
                data = new Data ();

            foreach (CustomCommand cmd in data.customCommands) {
                CustomCommandSet.customSet.AddProceduralCommands (cmd);
            }
        }

        public static void SaveData() {
            SerializationIO.SaveObjectToFile (Program.dataPath + dataFileName + Program.gitHubIgnoreType, data, true, false);
        }

        public static void AddCommand(CustomCommand newCommand, CommandSet set) {
            data.customCommands.Add (newCommand);
            set.AddProceduralCommands (newCommand);
            SaveData ();
        }

        public static void DeleteCommand(CustomCommand command, CommandSet set) {
            data.customCommands.Remove (command);
            set.RemoveCommand (command);
            SaveData ();
        }

        public class CustomCommand : Command {

            public string chain;
            public ulong owner;

            public CustomCommand(string _name, string _desc, string _chain, ulong _owner) {
                command = _name;
                shortHelp = _desc;
                chain = _chain;
                owner = _owner;
                commandEnabled = true;

                AddOverload (typeof (object), shortHelp);
            }

            public async Task<Result> Execute(SocketUserMessage e, params object[] parameters) {
                string cmd;
                List<string> args = Utility.ConstructArguments (chain, out cmd);
                for (int i = 0; i < parameters.Length; i++) {
                    CommandVariables.Set (e.Id, "arg" + i, parameters [ i ], true);
                }
                Program.FoundCommandResult result = await Program.FindAndExecuteCommand (e, cmd.Substring (1), args, Program.commands, 0, false);
                return result.result;
            }

            public override string AllowExecution(SocketMessage e, List<string> args) {
                string firstPart = base.AllowExecution (e, args);
                if (owner != 0 && e.Author.Id != owner) {
                    firstPart += "Custom command not public.";
                }
                return firstPart;
            }
        }

        public class CustomCommandSet : CommandSet {
            public static CommandSet customSet;

            public CustomCommandSet() {
                command = "custom";
                shortHelp = "A collection of saved custom commands.";
                catagory = Category.Advanced;
                customSet = this;
                requiredPermission = Permissions.Type.CreateCustomCommands;

                commandsInSet = new Command [ ] {
                    new New (), new Delete (),
                };
            }

            public class New : Command {
                public New() {
                    command = "new";
                    shortHelp = "Create new custom command.";

                    AddOverload (typeof (CustomCommand), "Create a new custom command by the given variables.");
                }

                public Task<Result> Execute(SocketUserMessage e, string name, string description, string chain, bool isPublic) {
                    if (!name.Contains (' ') || !name.Contains ('(') || !name.Contains (')')) {
                        if (isPublic && !(e.Author as SocketGuildUser).GuildPermissions.ManageGuild) {
                            return TaskResult (null, "Error - Only members with Manage Guild permission can add public commands.");
                        }
                        if (chain [ 0 ] == '(') {
                            CustomCommand cmd = new CustomCommand (name, description, chain.Substring (1, chain.Length - 2), isPublic ? 0 : e.Author.Id);
                            AddCommand (cmd, customSet);
                            return TaskResult (cmd, "Succesfully added custom command: " + cmd.command);
                        } else {
                            return TaskResult (null, "Error - Parsed command not a command, command must be encapsulated in parantheses.");
                        }
                    } else {
                        return TaskResult (null, "Error - Name cannot contain spaces nor parantheses.");
                    }
                }
            }

            public class Delete : Command {
                public Delete() {
                    command = "delete";
                    shortHelp = "Delete existing custom commands.";

                    AddOverload (typeof (bool), "Delete an existing custom commands. Cannot delete one you don't own.");
                }

                public Task<Result> Execute(SocketUserMessage e, string name) {
                    IEnumerable<Command> cmd = customSet.commandsInSet.Where (x => x.command == name);
                    if (cmd.Count () > 0) {
                        CustomCommand custom = cmd.First () as CustomCommand;
                        if (custom.owner == e.Author.Id || (custom.owner == 0 && (e.Author as SocketGuildUser).GuildPermissions.ManageGuild)) {
                            DeleteCommand (custom, customSet);
                            return TaskResult (null, "Succesfully removed command " + custom.command + ".");
                        } else {
                            return TaskResult (null, "Error - Access to command denied.");
                        }
                    } else {
                        return TaskResult (null, "Error - Command " + name + " not found.");
                    }
                }
            }
        }
    }
}
