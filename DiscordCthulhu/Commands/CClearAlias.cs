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

        public override Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                AliasCollection.User user = Program.aliasCollection.FindUsersByAlias (e.Author.Username)[0];
                if (Program.aliasCollection.RemoveUser (user)) {
                    Program.messageControl.SendMessage(e, "All your aliasses has been removed from the collection.");
                } else {
                    Program.messageControl.SendMessage(e, "Couldn't find any aliasses in your name.");
                }
            }
            return Task.CompletedTask;
        }
    }
}
