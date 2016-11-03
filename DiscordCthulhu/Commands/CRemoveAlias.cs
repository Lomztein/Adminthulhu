using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    public class CRemoveAlias : Command {

        public CRemoveAlias () {
            command = "removealias";
            name = "Remove Alias";
            argHelp = "<alias>";
            help = "Removes the alias " + argHelp + " from your collection.";
        }

        public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                if (!Program.aliasCollection.RemoveAlias (e.User.Name, arguments[0])) {
                    Program.messageControl.SendMessage(e, "Failed to remove " + arguments[0] + " from your collection, as it doesn't seem to be there.");
                } else {
                    Program.messageControl.SendMessage(e, arguments[0] + " removed from your collection of aliasses.");
                }
            }
        }
    }
}
