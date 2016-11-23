using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    class CEmbolden : Command {

        private char[] available = new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't'
                                              , 'u', 'v', 'w', 'x', 'y', 'z'};
        private bool ignoreUnavailable = true;

        public CEmbolden () {
            command = "embolden";
            name = "Embolden";
            help = "Makes your text much more bold, and kind of spammy.";
            argumentNumber = 1;
            alwaysEnabled = true;
        }

        public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                string inText = arguments[0];
                string outText = "";

                if (inText == "?")
                    return;

                for (int i = 0; i < inText.Length; i++) {
                    if (inText[i] == ' ') {
                        outText += "  ";
                    }else {
                        char letter = inText.ToLower ()[i];
                        if (available.Contains (letter)) {
                            outText += ":regional_indicator_" + inText.ToLower ()[i] + ": ";
                        } else if (!ignoreUnavailable) {
                            Program.messageControl.SendMessage (e, "Unavailable character detected: " + letter);
                            return;
                        }
                    }
                }

                Program.messageControl.SendMessage (e, outText);
            }
        }
    }
}
