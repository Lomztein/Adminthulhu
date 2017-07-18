using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Adminthulhu {
    public class CQuote : Command, IConfigurable {

        string[] quotes = new string[] {
            "\"Jeg syntes jeg for mindre og mindre tøj på.\" - khave, 2016",
            "\"Some of it is actually pretty good.\" - Nyx, 2016",
            "\"NEIN NEIN NEIN NEIN\" - Shewshank, 2016",
            "\"I don't have any.\" - Nyx, 2016",
            "\"Kigger du på min søsters *Facebook*!?\" - Nyx, 2016",
            "\"Time to wake up before you do a Lomztein\" - Lomztein, 2016",
            "\"Brb finding something about eating people and fucking corpses\" - Lomztein, 2016",
            "\":kappa:\" - Gizmo Gizmo, 2016",
            "\"I am deeply regretting ever taking that photo.\" - Nyx, 2016",
            "\"Mit ultimate er kun på 80%!\" - Nyx, 2016",
            "\"***TILTED BEYOND REPAIR***\" - DoritoFighter231, 2016",
            "\"Prone to bugs. Fuckbawls shitstain.\" - Lomztein, 2016",
            "\"!setcommand fuck;true;true\" - Lomztein 2016"
        };

        public CQuote () {
            command = "quote";
            shortHelp = "Show glorious quote.";
            longHelp = "Display an incredibly meaningful quote.";
            argumentNumber = 0;
            catagory = Catagory.Fun;
        }

        public override void Initialize() {
            base.Initialize ();
            LoadConfiguration ();
            BotConfiguration.AddConfigurable (this);
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                Random random = new Random ();
                Program.messageControl.SendMessage (e, quotes[random.Next (quotes.Length)], true);
            }
            return Task.CompletedTask;
        }

        public void LoadConfiguration() {
            quotes = BotConfiguration.GetSetting<string [ ]> ("QuoteableQuotes", new string [ ] { "Quote #1", "Quote #2" });
        }
    }
}
