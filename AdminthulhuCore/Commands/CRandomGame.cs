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
            catagory = Category.Utility;

            AddOverload (typeof (WeeklyEvents.Game), "Select a random game out from a list, that could be played.");
        }

        public override void Initialize() {
            base.Initialize ();
        }

        public Task<Result> Execute(SocketUserMessage e) {
            Random random = new Random ();
            string gameName = WeeklyEvents.allGames [ random.Next (WeeklyEvents.allGames.Count) ].name;
            return TaskResult (gameName, "I gloriously suggest " + gameName);
        }
    }
}
