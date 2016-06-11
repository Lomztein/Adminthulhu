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
            help = "\"!removealias <alias>\" - Removes the alias from your collection.";
        }

        public override async void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (arguments)) {
                if (!Program.aliasCollection.RemoveAlias (e.User.Name, arguments[0])) {
                    await e.Channel.SendMessage ("Failed to remove " + arguments[0] + " from your collection, as it doesn't seem to be there.");
                } else {
                    await e.Channel.SendMessage (arguments[0] + " removed from your collection of aliasses.");
                }
            }
        }
    }
}
