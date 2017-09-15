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
                c.helpPrefix = helpPrefix + command + " ";
                c.Initialize ();
                c.isAdminOnly = isAdminOnly;
            }
        }

        public override async Task<Result> TryExecute (SocketUserMessage e, int depth, params string[] arguments) {
            // Standard command format is !command arg1;arg2;arg3
            // Commandset format is !command secondaryCommand arg1;arg2;arg3
            // Would it be possible to have commandSets within commandSets?
            if (arguments.Length != 0) {

                if (arguments.Length == 1) {
                    int spaceIndex = arguments [ 0 ].IndexOf (' ');
                    if (spaceIndex != -1) {
                        string beginning = arguments [ 0 ].Substring (0, spaceIndex);
                        if (arguments [ 0 ] [ spaceIndex + 1 ] == '(') {
                            arguments = Utility.SplitArgs (GetParenthesesArgs (arguments [ 0 ])).ToArray ();
                            arguments [ 0 ] = beginning + " " + arguments [ 0 ];
                        }
                    }
                }


                string combinedArgs = "";
                for (int i = 0; i < arguments.Length; i++) {
                    combinedArgs += arguments [ i ];
                    if (i != arguments.Length - 1)
                        combinedArgs += ";";
                }

                string message = this.command + " " + combinedArgs;
                string secondayCommand = message.Substring (message.IndexOf (' ') + 1);
                string command = "";

                List<string> newArguments = Utility.ConstructArguments (secondayCommand, out command);
                return (await Program.FindAndExecuteCommand (e, command, newArguments, commandsInSet, depth)).result;
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
                if (c.AllowExecution (e, null) == "") {
                    help += Utility.FormatCommand (c) + "\n";
                }
            }
            return help + "```";
        }

        // Just don't look it directly in the eye.
        public void AddProceduralCommands(params Command [ ] procCommands) {
            List<Command> curCommands = commandsInSet.ToList ();
            curCommands.AddRange (procCommands.ToList ());
            commandsInSet = curCommands.ToArray ();

            foreach (Command c in procCommands) {
                c.helpPrefix = helpPrefix + command + " ";
                c.Initialize ();
                c.isAdminOnly = isAdminOnly;
            }
        }

        public void RemoveCommand(Command cmd) {
            List<Command> curCommands = commandsInSet.ToList ();
            curCommands.Remove (cmd);
            commandsInSet = curCommands.ToArray ();
        }
    }
}
