using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    public class CCreateTTTGame : Command {

        public CCreateTTTGame () {
            command = "starttictactoe";
            name = "Start Tic Tac Toe";
            help = "\"!starttictactoe\" - Starts a game of tic tac toe between user and Cthulhu.";
            argumentNumber = 0;
        }

        public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                TicTacToe.CreateGame (e.User.Name, e);
            }
        }
    }
}
