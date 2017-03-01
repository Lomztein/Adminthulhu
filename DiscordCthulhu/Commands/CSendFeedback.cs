using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    class CSendFeedback : Command {

        public CSendFeedback () {
            command = "sendfeedback";
            name = "Send Feedback";
            help = "Send feedback to the author of this bot.";
            argumentNumber = 0;
        }

        public override void ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                //Program.messageControl.SendMessage (Program.discordClient., "Invite URL: " + invite.Url);
            }
        }

    }
}
