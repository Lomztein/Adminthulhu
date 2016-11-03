using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    public class CQuote : Command {

        string[] quotes = new string[] {
            "\"Jeg syntes jeg for mindre og mindre tøj på.\" - khave, 2016",
            "\"Some of it is actually pretty good.\" - Nyx, 2016",
            "\"NEIN NEIN NEIN NEIN\" - Shewshank, 2016",
            "\"I don't have any.\" - Nyx, 2016",
            "\"Kigger du på min søsters *Facebook*!?\" - Nyx, 2016",
            "\"Time to wake up before you do a Lomztein\" - Lomztein, 2016",
            "\"Brb finding something about eating people and fucking corpses\" - Lomztein, 2016",
            "\":kappa:\" - Gizmo Gizmo, 2016",
            "\"I am deeply regretting ever taking that photo.\" - Nyx, 2016"
        };

        public CQuote () {
            Initialize ();
            command = "quote";
            name = "Quote";
            help = "Display an incredibly meaningful quote.";
            argumentNumber = 0;
        }

        public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                Random random = new Random ();
                Program.messageControl.SendMessage (e, quotes[random.Next (quotes.Length)]);
            }
        }
    }
}
