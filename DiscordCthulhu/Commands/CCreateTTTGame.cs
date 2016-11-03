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
            argHelp = "<size>";
            help = "Starts a game of tic tac toe between of size " + argHelp + " user and Cthulhu.";
            argumentNumber = 1;
        }

        public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                int number;

                if (int.TryParse (arguments[0], out number)) {
                    if (number <= 10) {
                        TicTacToe.CreateGame (e.User.Name, e, number);
                    }else {
                        Program.messageControl.SendMessage (e, "Sorry, I can't handle a size larger than 10.");
                    }
                }
            }
        }
    }
}
