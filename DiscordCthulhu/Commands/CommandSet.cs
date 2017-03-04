using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {
    public class CommandSet : Command {

        public Command[] commandsInSet = new Command[0];

        public CommandSet () {
            command = "commandset";
            name = "Default Command Set";
            help = "A placeholder, shouldn't be accessable in final version.";
        }

        public override async Task Initialize () {

            await base.Initialize ();
            foreach (Command c in commandsInSet) {
                c.helpPrefix = helpPrefix + command + " ";
                await c.Initialize ();
                c.enabledSettings = enabledSettings;
                c.isAdminOnly = isAdminOnly;
            }
        }

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            if (arguments.Count > 0 && arguments[0] == "?") {
                // Display all commands within command.
                string commands = "Commands in the **" + command + "** command set:\n```";
                foreach (Command c in commandsInSet) {
                    commands += c.GetShortHelp () + "\n";
                }
                commands += "```";
                await Program.messageControl.SendMessage (e, commands);
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

                    List<string> newArguments = Program.ConstructArguments (secondayCommand, out command);
                    await Program.FindAndExecuteCommand (e, command, newArguments, commandsInSet);
                }
            }
        }

        public override string GetShortHelp () {
            return helpPrefix + command + " (set)";
        }
    }
}
