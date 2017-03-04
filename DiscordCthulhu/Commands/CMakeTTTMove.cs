using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {
    class CMakeTTTMove : Command {

        public CMakeTTTMove () {
            command = "tictactoe";
            name = "Move Tic Tac Toe";
            argHelp = "<x>;<y>";
            help = "Makes a move at " + argHelp + " position if you have a Tic Tac Toe game going with Cthulhu.";
            argumentNumber = 2;
        }

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            await base.ExecuteCommand (e, arguments);
            if (await AllowExecution (e, arguments)) {

                int x;
                int y;

                if (int.TryParse (arguments[0], out x) && int.TryParse (arguments[1], out y)) {
                    await TicTacToe.MakeMove (e.Author.Username, x, y, e);
                }
            }
        }
    }
}
