using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
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
                c.enabledSettings = enabledSettings;
            }
        }

        public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            if (arguments.Count > 0 && arguments[0] == "?") {
                // Display all commands within command.
                string commands = "Commands in the **" + command + "** command set:\n```";
                foreach (Command c in commandsInSet) {
                    commands += c.GetShortHelp () + "\n";
                }
                commands += "```";
                Program.messageControl.SendMessage (e, commands);
            } else {
                // Standard command format is !command arg1;arg2;arg3
                // Commandset format is !command secondaryCommand arg1;arg2;arg3
                // Would it be possible to have commandSets within commandSets?
                string message = this.command + " " + arguments[0];
                string secondayCommand = message.Substring (message.IndexOf (' ') + 1);
                string command = "";

                List<string> newArguments = Program.ConstructArguments (secondayCommand, out command);

                ChatLogger.DebugLog (message);
                Program.FindAndExecuteCommand (e, command, newArguments, commandsInSet);
            }
        }

        public override string GetShortHelp () {
            return helpPrefix + command + " (set)";
        }
    }
}
