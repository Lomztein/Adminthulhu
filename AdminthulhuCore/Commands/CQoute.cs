using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Adminthulhu {
    public class CQuote : Command, IConfigurable {

        string [ ] quotes;

        public CQuote () {
            command = "quote";
            shortHelp = "Show glorious quote.";
            catagory = Category.Fun;

            AddOverload (typeof (string), "Display an incredibly meaningful quote.");
        }

        public override void Initialize() {
            base.Initialize ();
            LoadConfiguration ();
            BotConfiguration.AddConfigurable (this);
        }

        public Task<Result> Execute(SocketUserMessage e) {
            Random random = new Random ();
            string quote = quotes [ random.Next (quotes.Length) ];
            return TaskResult (quote, quote);
        }

        public override void LoadConfiguration() {
            base.LoadConfiguration ();
            quotes = BotConfiguration.GetSetting ("Misc.QuoteableQuotes", "QuoteableQuotes", new string [ ] { "Quote #1", "Quote #2" });
        }
    }
}
