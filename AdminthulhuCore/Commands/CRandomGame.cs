using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Adminthulhu {
    public class CRandomGame : Command {

        public CRandomGame () {
            command = "whattoplay";
            shortHelp = "What to play?";
            longHelp = "Select a random game out from a list, that could be played.";
            argumentNumber = 0;
            catagory = Catagory.Utility;
        }

        public override void Initialize() {
            base.Initialize ();
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                Random random = new Random ();
                Program.messageControl.SendMessage (e, "I gloriously suggest " + AutomatedWeeklyEvent.allGames[random.Next (AutomatedWeeklyEvent.allGames.Count)], false);
            }
            return Task.CompletedTask;
        }
    }
}
