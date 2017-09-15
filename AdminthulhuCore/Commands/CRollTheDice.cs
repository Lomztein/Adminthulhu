using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Adminthulhu {
    public class CRollTheDice : Command {

        public CRollTheDice () {
            command = "rtd";
            shortHelp = "Roll the dice.";
            catagory = Category.Utility;
            AddOverload (typeof (int), "Rolls a dice that returns a number between one and the given max number.");
        }

        public Task<Result> Execute (SocketUserMessage e, int maxnumber) {
            Random random = new Random ();
            int number = (random.Next (maxnumber) + 1);
            return TaskResult (number, "You've rolled " + number + "!");
        }
    }
}
