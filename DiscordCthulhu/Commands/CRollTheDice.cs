using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    public class CRollTheDice : Command {

        public CRollTheDice () {
            command = "rtd";
            name = "Roll the Dice";
            help = "\"!rtd <maxvalue>\" - Rolls a dice that returns a number between one and maxnumber.";
            argumentNumber = 1;
        }

        public override async void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (arguments)) {
                Random random = new Random ();
                int number;

                if (int.TryParse (arguments[0], out number)) {
                    await e.Channel.SendMessage ("You rolled " + (random.Next (number) + 1).ToString ());
                }
            }
        }
    }
}
