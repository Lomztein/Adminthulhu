using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {
    public class CFlipCoin : Command {

        public CFlipCoin () {
            command = "flipcoin";
            name = "Flip a Coin";
            help = "Flip a coin that has an equal chance of heads or tails.";
            argumentNumber = 0;
        }

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            await base.ExecuteCommand (e, arguments);
            if (await AllowExecution (e, arguments)) {
                Random random = new Random ();
                if (random.Next (2) == 0) {
                    await Program.messageControl.SendMessage (e, "Your coin flipped to show heads.");
                } else {
                    await Program.messageControl.SendMessage (e, "Your coin flipped to show tails.");
                }
            }
        }
    }
}
