using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    public class CSetAlias : Command {

        public CSetAlias () {
            command = "addalias";
            name = "Add Alias";
            argHelp = "<alias>";
            help = "Adds an alias " + argHelp + " to your collection, or creates a new collection if you don't have any.";
        }

        public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                if (!Program.aliasCollection.AddAlias (e.User.Name, arguments[0])) {
                    Program.messageControl.SendMessage(e, "Failed to add " + arguments[0] + " to your collection, as it is already there.");
                } else {
                    Program.messageControl.SendMessage(e, arguments[0] + " added to your collection of aliasses.");
                }
            }
        }
    }
}
