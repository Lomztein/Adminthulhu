using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {
    public class CCreateInvite : Command {

        /*public CCreateInvite () {
            command = "createinvite";
            name = "Create a Single Person Invite";
            help = "Creates a single person invite to this server.";
            argumentNumber = 0;
        }

        public override async void ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                IInvite invite = await e.SocketGuild.CreateInvite (1800, 1, false, false);
                Program.messageControl.SendMessage(e, "Invite URL: " + invite.Url);
            }
        }*/
    }
}
