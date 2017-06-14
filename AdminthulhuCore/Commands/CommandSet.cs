using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Adminthulhu {
    public class CommandSet : Command {

        public Command[] commandsInSet = new Command[0];

        public CommandSet () {
            command = "commandset";
            name = "Default Command Set";
            help = "A placeholder, shouldn't be accessable in final version.";
        }

        public override void Initialize () {

            base.Initialize ();
            foreach (Command c in commandsInSet) {
                c.helpPrefix = helpPrefix + command + " ";
                c.Initialize ();
                c.isAdminOnly = isAdminOnly;
            }
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            if (arguments.Count > 0 && arguments[0] == "?") {
                // Display all commands within command.
                string commands = "Commands in the **" + command + "** command set:\n```";
                foreach (Command c in commandsInSet) {
                    commands += Utility.FormatCommand (c) + "\n";
                }
                commands += "```";
                Program.messageControl.SendMessage (e, commands, false);
            } else {
                // Standard command format is !command arg1;arg2;arg3
                // Commandset format is !command secondaryCommand arg1;arg2;arg3
                // Would it be possible to have commandSets within commandSets?
                if (arguments.Count != 0) {
                    string combinedArgs = "";
                    for (int i = 0; i < arguments.Count; i++) {
                        combinedArgs += arguments[i];
                        if (i != arguments.Count - 1)
                            combinedArgs += ";";
                    }

                    string message = this.command + " " + combinedArgs;
                    string secondayCommand = message.Substring (message.IndexOf (' ') + 1);
                    string command = "";

                    List<string> newArguments = Utility.ConstructArguments (secondayCommand, out command);
                    Program.FindAndExecuteCommand (e, command, newArguments, commandsInSet);
                }
            }
            return Task.CompletedTask;
        }

        public override string GetCommand () {
            return helpPrefix + command + " (set)";
        }
    }
}
