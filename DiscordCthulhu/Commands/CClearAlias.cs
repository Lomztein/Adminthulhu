using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {
    public class CClearAliasses : Command {

        public CClearAliasses () {
            command = "clearalias";
            name = "Clear Aliassses";
            help = "Clears off all aliasses to your name.";
            argumentNumber = 0;
        }

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            await base.ExecuteCommand (e, arguments);
            if (await AllowExecution (e, arguments)) {
                AliasCollection.User user = Program.aliasCollection.FindUsersByAlias (e.Author.Username)[0];
                if (await Program.aliasCollection.RemoveUser (user)) {
                    await Program.messageControl.SendMessage(e, "All your aliasses has been removed from the collection.");
                } else {
                    await Program.messageControl.SendMessage(e, "Couldn't find any aliasses in your name.");
                }
            }
        }
    }
}
