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
            catagory = Category.Utility;

            AddOverload (typeof (object), "Shows the people behind the bot.");
        }

        public Task<Result> Execute ( SocketUserMessage e) {

                return TaskResult ("", string.Join ("Main Programmer: Marcus \"Lomztein\" Jensen\n",
                                                    "Additional Programming: Frederik \"Fred\" Rosenberg and Victor \"Nyx\" Koch\n",
                                                    "This bot is created using the Discord.NET Discord Bot API for C#, and uses the Newtonsoft.JSON library for IO."));
        }
    }
}
