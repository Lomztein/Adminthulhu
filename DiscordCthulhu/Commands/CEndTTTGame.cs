using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {
    class CEndTTTGame : Command {

        public CEndTTTGame () {
            command = "endtictactoe";
            name = "End Tic Tac Toe Game";
            help = "Ends your current game if Tic Tac Toe, if there is one.";
            argumentNumber = 0;
        }

        public override Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                TicTacToe.TicTacToeGame game = TicTacToe.FindPlayersGame (e.Author.Username);
                if (game != null) {
                    TicTacToe.currentGames.Remove (game);
                    Program.messageControl.SendMessage (e, "Your current game has now been ended. I can only assume you gave up.");
                }else {
                    Program.messageControl.SendMessage (e, "You are not currently playing a game of Tic Tac Toe.");
                }
            }
            return Task.CompletedTask;
        }
    }
}
