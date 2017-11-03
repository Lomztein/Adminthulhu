using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Adminthulhu {
    class CEmbolden : Command {

        private char[] available = new char[] { 'a', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't'
                                              , 'u', 'v', 'w', 'x', 'y', 'z'};

        private char[] numbers = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'};
        private Dictionary<char, string> specifics = new Dictionary<char, string> ();
        private bool ignoreUnavailable = true;

        public CEmbolden () {
            command = "embolden";
            shortHelp = "Embolden.";
            catagory = Category.Fun;
            availableInDM = true;

            AddOverload (typeof (string), "Makes your given text much more bold, and kind of spammy.");
            specifics.Add ('b', "🅱");
        }

        public Task<Result> Execute(SocketUserMessage e, string input) {

            string outText = "";

            if (input == "?")
                return TaskResult ("", "");

            for (int i = 0; i < input.Length; i++) {
                if (input [ i ] == ' ') {
                    outText += "  ";
                } else {
                    char letter = input.ToLower () [ i ];
                    if (available.Contains (letter)) {
                        outText += ":regional_indicator_" + input.ToLower () [ i ] + ": ";
                    } else if (specifics.ContainsKey (letter)) {
                        outText += specifics [ letter ];
                    } else if (numbers.Contains (letter)) {
                        outText += NumberToString (letter) + " ";
                    } else if (!ignoreUnavailable) {
                        return TaskResult ("", "Unavailable character detected: " + letter);
                    }
                }
            }

            return TaskResult (outText, outText);
        }

        // Considering this is now used in more than one class, it might be wise to move it to a core class in order to remain structured.
        public static string NumberToString (char number) {
            switch (number) {
                case '0':
                    return ":zero:";

                case '1':
                    return ":one:";

                case '2':
                    return ":two:";

                case '3':
                    return ":three:";

                case '4':
                    return ":four:";

                case '5':
                    return ":five:";

                case '6':
                    return ":six:";

                case '7':
                    return ":seven:";

                case '8':
                    return ":eight:";

                case '9':
                    return ":nine:";
            }

            return "";
        }
    }
}
