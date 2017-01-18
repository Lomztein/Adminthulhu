using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    public class CShowCredits : Command {

        public CShowCredits () {
            command = "showcredits";
            name = "Show Creidts";
            help = "Shows the people behind the bot.";
            argumentNumber = 0;
        }

        public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                Program.messageControl.SendMessage (e, "Main Programmer: Marcus \"Lomztein\" Jensen\n" +
                                                    +"Additional Programming: Frederik \"Fred\" Rosenberg and Victor \"Nyx\" Koch\n" +
                                                    +"This bot is created using the Discord.NET Discord Bot API for C#");
            }
      }
}
