using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {
    public static class TicTacToe {

        public static List<TicTacToeGame> currentGames = new List<TicTacToeGame>();
        public static intVector2[] directions = new intVector2[] {
            new intVector2 (-1,-1),
            new intVector2 (0,-1),
            new intVector2 (1,-1),
            new intVector2 (1,0),
            new intVector2 (1,-1),
            new intVector2 (0,-1),
            new intVector2 (-1,1),
            new intVector2 (-1,0),
        };

        public const int EMTPY_ID = 0;
        public const int PLAYER_ID = 1;
        public const int BOT_ID = 2;

        public static char[] playerChars = new char[] { ' ', 'O', 'X'};

        // Returns -1 if no game, 0 if the game continues, 1 if player victory and 2 if bot victory.
        public static async Task<int> MakeMove (string playerName, int x, int y, SocketMessage e) {
            TicTacToeGame game = FindPlayersGame (playerName);
            if (game == null) {
                await Program.messageControl.SendMessage (e, "You have no game in progress currently, start a new one if you want to play.");
                return -1;
            }

            bool succesful = game.SetBlock (PLAYER_ID, x, y);
            if (succesful) {
                if (game.CheckForVictory (PLAYER_ID)) {
                    await Program.messageControl.SendMessage (e, game.Render ());
                    await Program.messageControl.SendMessage (e, "Wha-, you.. you beat me? How is this possible? I can barely dream of your seemingly infinite intelligence.");
                    await Program.scoreCollection.ChangeScore (playerName, 1);
                    currentGames.Remove (game);

                    return 1;
                }
                await Program.messageControl.SendMessage (e, game.Render ());
                await Program.messageControl.SendMessage (e, "A strange move.. Now tremble at my immense intelligence!");
                PlayerInputAI.MakeMove (game, BOT_ID);
                if (game.CheckForVictory (BOT_ID)) {
                    await Program.messageControl.SendMessage (e, game.Render ());
                    await Program.messageControl.SendMessage (e, "I am victorious! As always with you puny humans, the mighty Cthulhu triumps above any man!");
                    currentGames.Remove (game);

                    return 2;
                }
                await Program.messageControl.SendMessage (e, game.Render ());
            }

            return 0;
        }

        public static async Task CreateGame (string playerName, SocketMessage e, int size) {
            if (FindPlayersGame (playerName) != null) {
                await Program.messageControl.SendMessage (e, "You already have a game in progress.");
            }else {
                TicTacToeGame newGame = new TicTacToeGame (playerName, size);
                currentGames.Add (newGame);
                await Program.messageControl.SendMessage (e, "You have challenged Cthulhu to a match of Tic Tac Toe, prepare to die.");
                await Program.messageControl.SendMessage (e, newGame.Render ());
            }
        }

        public static TicTacToeGame FindPlayersGame (string playerName) {
            for (int i = 0; i < currentGames.Count; i++) {
                if (currentGames[i].owner == playerName)
                    return currentGames[i];
            }
            return null;
        }

        public class TicTacToeGame {

            public string owner;
            public int[,] blocks;

            public int rows = 3;
            public int columns = 3;
            public int rowsRequired = 3;

            public TicTacToeGame (string playerName, int size) {
                owner = playerName;
                rows = size;
                columns = size;
                rowsRequired = size;
                blocks = new int[rows, columns];
            }

            public bool SetBlock (int player, int x, int y) {
                ChatLogger.Log (x + ", " + y + ", " + player + " - " + Environment.StackTrace);
                if (IsInsidePlayArea (x, y) && blocks[x,y] == EMTPY_ID) {
                    blocks[x, y] = player;
                    return true;
                }
                return false;
            }

            public bool IsInsidePlayArea (int x, int y) {
                if (x < 0)
                    return false;
                if (y < 0)
                    return false;
                if (x > rows - 1)
                    return false;
                if (y > columns - 1)
                    return false;
                return true;
            }

            bool AnyFreeBlocks () {
                for (int y = 0; y < columns; y++) {
                    for (int x = 0; x < rows; x++) {
                        if (blocks[x, y] == EMTPY_ID)
                            return true;
                    }
                }
                return false;
            }

            public bool CheckForVictory ( int status ) {
                for (int y = 0; y < columns; y++) {
                    for (int x = 0; x < rows; x++) {

                        for (int i = 0; i < directions.Length; i++) {
                            int count = 0;
                            for (int j = 0; j < rowsRequired; j++) {

                                if (IsInsidePlayArea (x + directions[i].x * j, y + directions[i].y * j)) {
                                    if (blocks[x + directions[i].x * j, y + directions[i].y * j] == status) {
                                        count++;
                                        if (count >= rowsRequired)
                                            return true;
                                    }
                                } else {
                                    continue;
                                }
                            }
                        }
                    }
                }
                return false;
            }

            public string Render () {
                string render = "```\n ";
                // These two loops could likely be done better, but this will do for now.
                for (int y = 0; y < columns; y++) {
                    render += " " + y + " ";
                }

                render += "\n";
                for (int y = 0; y < columns; y ++) {
                    for (int x = 0; x < rows; x++) {
                        if (x == 0)
                            render += y;
                        render += "[" + playerChars[blocks[x, y]] + "]";
                    }
                    render += "\n";
                }
                render += "```";
                return render;
            }
        }

        public static class PlayerInputAI {

            public static void MakeMove (TicTacToeGame game, int pawn) {
                // First, find out if AI can win in this move.
                for (int y = 0; y < game.columns; y++) {
                    for (int x = 0; x < game.rows; x++) {

                        if (game.blocks[x, y] != pawn) {
                            continue;
                        }

                        for (int i = 0; i < TicTacToe.directions.Length; i++) {
                            if (GetAmountOfSpecificBlocks (x, y, game.rowsRequired, directions[i], pawn, game) == game.rowsRequired) {
                                game.SetBlock (pawn, x, y);
                                return;
                            }
                        }
                    }
                }

                // Secondly, check if enemy can win next move.
                if (FindAndStopOtherPlayersVictory (game))
                    FindAndPlaceAtOptimalLocation (game);
            }

            public static bool FindAndStopOtherPlayersVictory (TicTacToeGame game) {
                intVector2 pos = new intVector2 ();
                bool stopped = false;

                for (int y = 0; y < game.columns; y++) {
                    for (int x = 0; x < game.rows; x++) {

                        if (game.blocks[x, y] != EMTPY_ID && game.blocks[x, y] != BOT_ID)
                            continue;

                        for (int i = 0; i < directions.Length; i++) {
                            int count = 0;
                            for (int j = 0; j < game.rowsRequired; j++) {

                                if (game.IsInsidePlayArea (x + directions[i].x * j, y + directions[i].y * j)) {
                                    if (game.blocks[x + directions[i].x * j, y + directions[i].y * j] == BOT_ID) {
                                        count++;
                                    }
                                }
                            }
                            if (count == game.rowsRequired - 1) {

                                for (int j = 0; j < game.rowsRequired; j++) {
                                    if (game.IsInsidePlayArea (x + directions[i].x * j, y + directions[i].y * j)) {
                                        if (game.blocks[x + directions[i].x * j, y + directions[i].y * j] == EMTPY_ID) {
                                            pos.x = x + directions[i].x * j;
                                            pos.y = y + directions[i].y * j;
                                            stopped = true;
                                        }
                                    }
                                }
                            }
                        }

                        if (stopped)
                            game.SetBlock (BOT_ID, pos.x, pos.y);
                    }
                }
                return !stopped;
            }

            public static int GetAmountOfFreeBlocks ( int x, int y, int rows, intVector2 direction, TicTacToeGame game ) {
                int count = 0;
                for (int j = 0; j < game.rowsRequired; j++) {

                    if (game.IsInsidePlayArea (x + direction.x * j, y + direction.y * j)) {
                        if (game.blocks[x + direction.x * j, y + direction.y * j] == BOT_ID) {
                            count++;
                        } else if (game.blocks[x + direction.x * j, y + direction.y * j] == EMTPY_ID)
                            count++;
                    }
                }
                return count;
            }

            public static int GetAmountOfSpecificBlocks ( int x, int y, int rows, intVector2 direction, int type, TicTacToeGame game ) {
                int count = 0;
                for (int j = 0; j < game.rowsRequired; j++) {

                    if (game.IsInsidePlayArea (x + direction.x * j, y + direction.y * j)) {
                        if (game.blocks[x + direction.x * j, y + direction.y * j] == type) {
                            count++;
                        }
                    }
                }
                return count;
            }

            public static void FindAndPlaceAtOptimalLocation ( TicTacToeGame game ) {

                List<intVector2> suitable = new List<intVector2> ();
                int least = int.MaxValue;

                for (int y = 0; y < game.columns; y++) {
                    for (int x = 0; x < game.rows; x++) {

                        if (game.blocks[x, y] != EMTPY_ID)
                            continue;

                        for (int i = 0; i < directions.Length; i++) {
                            int loc = GetAmountOfFreeBlocks (x, y, game.rowsRequired, directions[i], game);

                            if (loc < least) {
                                suitable.Clear ();
                                suitable.Add (new intVector2 (x, y));
                                least = loc;
                            } else if (loc == least) {
                                suitable.Add (new intVector2 (x, y));
                            }
                        }
                    }
                }

                intVector2 pos = suitable[0];
                double dist = double.MaxValue;
                for (int i = 0; i < suitable.Count; i++) {
                    double d = GetDistance (new intVector2 (suitable[i].x, suitable[i].y), new intVector2 ((game.rows - 1) / 2, (game.columns - 1) / 2));
                    if (d < dist) {
                        dist = d;
                        pos = suitable[i];
                    }
                }
                game.SetBlock (BOT_ID, pos.x, pos.y);
            }
        }

        private static double GetDistance (intVector2 from, intVector2 to) {
            double a = Math.Pow (from.x - to.x, 2.0);
            double b = Math.Pow (from.y - to.y, 2.0);

            return Math.Sqrt (a + b);
        }

    }
}

