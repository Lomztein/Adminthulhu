using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Adminthulhu {
    public class CommandSet : Command {

        public Command [ ] commandsInSet = new Command [ 0 ];

        public CommandSet() {
            command = "commandset";
            shortHelp = "DEFAULT_COMMAND_SET";
            catagory = Category.Set;
        }

        public override void Initialize() {
            base.Initialize ();
            InitCommands ();
        }

        public void InitCommands() {
            foreach (Command c in commandsInSet) {
                FeedRecursiveData (c);
            }
        }

        public override async Task<Result> TryExecute (SocketUserMessage e, int depth, params object[] arguments) {
            // Standard command format is !command arg1;arg2;arg3
            // Commandset format is !command secondaryCommand arg1;arg2;arg3
            // Would it be possible to have commandSets within commandSets?
            if (arguments.Length != 0) {

                string combinedArgs = "";
                for (int i = 0; i < arguments.Length; i++) {
                    combinedArgs += arguments [ i ];
                    if (i != arguments.Length - 1)
                        combinedArgs += ";";
                }

                string cmd = "";

                List<string> newArguments = Utility.ConstructArguments (combinedArgs, out cmd);
                return (await Program.FindAndExecuteCommand (e, cmd, newArguments, commandsInSet, depth, false, false)).result;
            } else {
                return new Result (this, "");
            }
        }

        public override string GetCommand() {
            return helpPrefix + command + " (set)";
        }

        public override string GetHelp(SocketMessage e) {
            // Display all commands within command.
            string help = "";
            help += ("Commands in the **" + command + "** command set:\n```");
            foreach (Command c in commandsInSet) {
                if (c.AllowExecution (e) == "") {
                    help += Utility.FormatCommand (c) + "\n";
                }
            }
            if (help == "Commands in the **" + command + "** command set:\n```") { // Ew
                return "This set contains no available commands.";
            } else {
                return help + "```";
            }
        }

        // Just don't look it directly in the eye.
        public void AddProceduralCommands(params Command [ ] procCommands) {
            List<Command> curCommands = commandsInSet.ToList ();
            curCommands.AddRange (procCommands.ToList ());
            commandsInSet = curCommands.ToArray ();

            foreach (Command c in procCommands) {
                FeedRecursiveData (c);
            }
        }

        private void FeedRecursiveData (Command c) {
            c.helpPrefix = helpPrefix + command + " ";
            c.Initialize ();
            if (!c.isAdminOnly)
                c.isAdminOnly = isAdminOnly;

            c.requiredPermission = requiredPermission;
        }

        public void RemoveCommand(Command cmd) {
            List<Command> curCommands = commandsInSet.ToList ();
            curCommands.Remove (cmd);
            commandsInSet = curCommands.ToArray ();
        }
    }
}
