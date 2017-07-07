using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Adminthulhu {
    public class CShowCredits : Command {

        public CShowCredits () {
            command = "credits";
            shortHelp = "Show butt credits.";
            longHelp = "Shows the people behind the bot.";
            argumentNumber = 0;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                Program.messageControl.SendMessage (e, string.Join ("Main Programmer: Marcus \"Lomztein\" Jensen\n",
                                                    "Additional Programming: Frederik \"Fred\" Rosenberg and Victor \"Nyx\" Koch\n",
                                                    "This bot is created using the Discord.NET Discord Bot API for C#"), false);
            }
            return Task.CompletedTask;
        }
    }
}
