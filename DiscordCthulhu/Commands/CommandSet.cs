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
            Initialize ();
            command = "commandset";
            name = "Default Command Set";
            help = "A placeholder, shouldn't be accessable in final version.";
        }

        public override void Initialize () {
            base.Initialize ();
            foreach (Command c in commandsInSet) {
                c.Initialize ();
                c.enabledSettings = enabledSettings;
            }
        }

        public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            // Standard command format is !command arg1;arg2;arg3
            // Commandset format is !command secondaryCommand arg1;arg2;arg3
            // Would it be possible to have commandSets within commandSets?
            string message = e.Message.RawText;
            string secondayCommand = message.Substring (message.IndexOf (' ') + 1);
            string command = "";

            arguments = Program.ConstructArguments (secondayCommand, out command);
            Program.FindAndExecuteCommand (e, command, arguments, commandsInSet);
        }
    }
}
