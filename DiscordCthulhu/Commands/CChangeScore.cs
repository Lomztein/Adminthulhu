using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {

    public class CChangeScore : Command {

        public CChangeScore () {
            command = "changescore";
            name = "Change Score";
            argHelp = "<user>;<amount>;<reason>";
            help = "Change the score of <user> by <amount> for <reason>.";
            argumentNumber = 3;
            isAdminOnly = true;
        }

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            await base.ExecuteCommand (e, arguments);
            if (await AllowExecution (e, arguments)) {
                int number;

                if (int.TryParse (arguments[1], out number)) {

                    await Program.scoreCollection.ChangeScore (arguments[0], number);
                    await Program.messageControl.SendMessage (e, arguments[0] + " score has been changed by " + number.ToString () + ".\n" + 
                        "Their score now totals " + Program.scoreCollection.GetScore (arguments[0]) + ".");

                    SocketGuildUser user = Program.FindUserByName ((e.Channel as SocketGuildChannel).Guild, arguments[0]);
                    if (user != null) {
                        await Program.messageControl.SendMessage (user, "Your score has been increased by " + number + " for reason: " + arguments[2]);
                    }
                }else {
                    await Program.messageControl.SendMessage (e, "Failed to parse second argument.");
                }
            }
        }
    }
}
