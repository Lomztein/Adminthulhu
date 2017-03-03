using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {
    class CEmbolden : Command {

        private char[] available = new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't'
                                              , 'u', 'v', 'w', 'x', 'y', 'z'};

        private char[] numbers = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'};
        private bool ignoreUnavailable = true;

        public CEmbolden () {
            command = "embolden";
            name = "Embolden";
            help = "Makes your text much more bold, and kind of spammy.";
            argumentNumber = 1;
            alwaysEnabled = true;
        }

        public override void ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                string inText = arguments[0];
                string outText = "";

                // TEMPORARY CODE, REMOVE AT ONCE.
                if (arguments[0] == "the entire bee movie script" && e.Author.Username == "Lomztein") {
                    inText = "";
                    string[] deathItself = SerializationIO.LoadTextFile (Program.dataPath + "theentirebeemoviescript.txt");
                    for (int i = 0; i < deathItself.Length; i++) {
                        inText += deathItself[i];
                    }
                }

                if (inText == "?")
                    return;

                for (int i = 0; i < inText.Length; i++) {
                    if (inText[i] == ' ') {
                        outText += "  ";
                    }else {
                        char letter = inText.ToLower ()[i];
                        if (available.Contains (letter)) {
                            outText += ":regional_indicator_" + inText.ToLower ()[i] + ": ";
                        } else if (numbers.Contains (letter)) {
                            outText += NumberToString (letter) + " ";
                        } else if (!ignoreUnavailable) {
                            Program.messageControl.SendMessage (e, "Unavailable character detected: " + letter);
                            return;
                        }
                    }
                }

                Program.messageControl.SendMessage (e, outText);
            }
        }

        public string NumberToString (char number) {
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
