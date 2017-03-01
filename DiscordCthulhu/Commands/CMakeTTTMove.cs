using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    class CMakeTTTMove : Command {

        public CMakeTTTMove () {
            command = "tictactoe";
            name = "Move Tic Tac Toe";
            argHelp = "<x>;<y>";
            help = "Makes a move at " + argHelp + " position if you have a Tic Tac Toe game going with Cthulhu.";
            argumentNumber = 2;
        }

        public override void ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                int x;
                int y;

                if (int.TryParse (arguments[0], out x) && int.TryParse (arguments[1], out y)) {
                    TicTacToe.MakeMove (e.User.Name, x, y, e);
                }
            }
        }
    }
}
