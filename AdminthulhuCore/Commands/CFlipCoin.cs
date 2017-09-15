using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Adminthulhu {
    public class CFlipCoin : Command {

        public CFlipCoin () {
            command = "flipcoin";
            shortHelp = "Flip a coin.";
            catagory = Category.Utility;

            AddOverload (typeof (int), "Flip a coin that has an equal chance of heads or tails.");
        }

        public Task<Result> Execute(SocketUserMessage e) {
            Random random = new Random ();
            if (random.Next (2) == 0) {
                return TaskResult (0, "Your coin flipped to show heads.");
            } else {
                return TaskResult (1, "Your coin flipped to show tails.");
            }
        }
    }
}
