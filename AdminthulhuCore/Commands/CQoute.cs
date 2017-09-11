using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Adminthulhu {
    public class CQuote : Command {

        public static List<string> quotes;

        public CQuote () {
            command = "quote";
            shortHelp = "Show glorious quote.";
            catagory = Category.Fun;

            AddOverload (typeof (string), "Display an incredibly meaningful quote.");
        }

        public override void Initialize() {
            base.Initialize ();
            quotes = SerializationIO.LoadObjectFromFile<List<string>> (Program.dataPath + "quotes" + Program.gitHubIgnoreType);
            if (quotes == null)
                quotes = new List<string> ();
        }

        public static void SaveData() {
            SerializationIO.SaveObjectToFile (Program.dataPath + "quotes" + Program.gitHubIgnoreType, quotes, true, false);
        }

        public Task<Result> Execute(SocketUserMessage e) {
            Random random = new Random ();
            if (quotes.Count > 0) {
               string quote = quotes [ random.Next (quotes.Count) ];
               return TaskResult (quote, quote);
            }
            return TaskResult ("", "No quotes available.");
        }

        public static void AddQuoteFromMessage(IMessage message) {
            string newQuote = $"\"{message.Content}\" - {Utility.GetUserName (message.Author as SocketGuildUser)} {message.Timestamp.Year}";
            quotes.Add (newQuote);
            SaveData ();
        }
    }
}
