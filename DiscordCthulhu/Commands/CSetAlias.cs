using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {
    public class CSetAlias : Command {

        public CSetAlias () {
            command = "addalias";
            name = "Add Alias";
            argHelp = "<alias>";
            help = "Adds an alias " + argHelp + " to your collection, or creates a new collection if you don't have any.";
        }

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            await base.ExecuteCommand (e, arguments);
            if (await AllowExecution (e, arguments)) {
                if (!await Program.aliasCollection.AddAlias (e.Author.Username, arguments[0])) {
                    await Program.messageControl.SendMessage(e, "Failed to add " + arguments[0] + " to your collection, as it is already there.");
                } else {
                    await Program.messageControl.SendMessage(e, arguments[0] + " added to your collection of aliasses.");
                }
            }
        }
    }
}
