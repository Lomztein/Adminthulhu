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
            "\"Some of it is actually pretty good.\" - Nyx, 2016"
        };

        public CQuote () {
            command = "quote";
            name = "Quote";
            help = "\"!quote\" - Display an incredibly meaningful quote.";
            argumentNumber = 0;
        }

        public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (arguments)) {
                Random random = new Random ();
                Program.messageControl.SendMessage (e, quotes[random.Next (quotes.Length)]);
            }
        }
    }
}
