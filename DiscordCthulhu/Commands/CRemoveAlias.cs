using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {
    public class CRemoveAlias : Command {

        public CRemoveAlias () {
            command = "removealias";
            name = "Remove Alias";
            argHelp = "<alias>";
            help = "Removes the alias " + argHelp + " from your collection.";
        }

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            await base.ExecuteCommand (e, arguments);
            if (await AllowExecution (e, arguments)) {
                if (!await Program.aliasCollection.RemoveAlias (e.Author.Username, arguments[0])) {
                    await Program.messageControl.SendMessage(e, "Failed to remove " + arguments[0] + " from your collection, as it doesn't seem to be there.");
                } else {
                    await Program.messageControl.SendMessage(e, arguments[0] + " removed from your collection of aliasses.");
                }
            }
        }
    }
}
