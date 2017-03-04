using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {
    public class CRandomGame : Command {

        public string[] games = new string[] {
            "Overwatch",
            "Team Fortress 2",
            "Counter Strike: Global Offensive",
            "Garry's Mod",
            "Portal 2",
            "Guns of Icarus",
            "Rocket League",
            "Brawlshalla",
            "Battlefield",
            "Left 4 Dead 2",
            "PlanetSide 2",
            "RollerCoaster Tycoon 2 Multiplayer",
            "Killing Floor",
            "Burnout Paradise",
            "Tribes: Ascend",
            "Terraria",
            "Quake Live",
            "Air Brawl",
            "Duck Game",
            "Toribash",
            "Robocraft",
            "TrackMania",
            "Robot Roller-Derby Disco Dodgeball"
        };

        public CRandomGame () {
            command = "whattoplay";
            name = "What to Play?";
            help = "Select a random game out from a list, that could be played.";
            argumentNumber = 0;
        }

        public override Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                Random random = new Random ();
                Program.messageControl.SendMessage (e, "I gloriously suggest " + games[random.Next (games.Length)]);
            }
            return Task.CompletedTask;
        }
    }
}
