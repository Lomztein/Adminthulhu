using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {
    class CSendFeedback : Command {

        public CSendFeedback () {
            command = "sendfeedback";
            name = "Send Feedback";
            help = "Send feedback to the author of this bot.";
            argumentNumber = 0;
        }

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            await base.ExecuteCommand (e, arguments);
            if (await AllowExecution (e, arguments)) {
                //Program.messageControl.SendMessage (Program.discordClient., "Invite URL: " + invite.Url);
            }
        }

    }
}
