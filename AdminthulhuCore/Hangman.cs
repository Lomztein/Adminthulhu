using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Discord.WebSocket;
using Discord;

namespace Adminthulhu {
    public class Hangman {

        public string hangmanGraphics =
            "  |---\\ \n" +
            "  |   | \n" +
            "  |   O \n" +
            "  |  /|\\\n" +
            "  |   | \n" +
            "  |  / \\\n" +
            "/===\\   \n";

        public string hangmanGraphicsIndex =
            "00233330\n" +
            "00200030\n" +
            "00200040\n" +
            "00200657\n" +
            "00200050\n" +
            "00200809\n" +
            "11111000\n";

        public static Hangman currentGame;

        public string word;
        public string progress;

        public List<char> guessedLetters;

        public int tries = 0;
        public int maxTries = 8;

        public static char[] letterWhitelist = new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'æ', 'ø', 'å', };

        public static string ToUnderscores (string input) {
            input = input.ToLower ();
            string underscores = "";
            foreach (char c in input) {
                if (!letterWhitelist.Contains (c)) {
                    underscores += c.ToString ();
                } else {
                    underscores += "-";
                }
            }
            return underscores;
        }

        public Hangman ( string _word ) {
            word = _word;
            progress = ToUnderscores (word);

            guessedLetters = new List<char> ();
            Utility.SetGame ("Hangman");
        }

        public bool GuessLetter (SocketMessage e, char letter) {
            SocketTextChannel channel = (e.Channel as SocketTextChannel);
            Program.messageControl.SendMessage (channel, Utility.GetUserName (e.Author as SocketGuildUser) + " guessed the letter **" + letter + "**.", false);
            if (!letterWhitelist.Contains (letter)) {
                Program.messageControl.SendMessage (channel, "Error: No numbers or special characters allowed.", false);
                return false;
            }

            if (!guessedLetters.Contains (letter)) {
                // Find all occurances of that letter in WORD, and place letter en PROGRESS whereever that was.
                List<int> occurances = new List<int> ();
                string low = word.ToLower ();
                for (int i = 0; i < word.Length; i++) {
                    if (low[i] == letter)
                        occurances.Add (i);
                }
                guessedLetters.Add (letter);

                if (occurances.Count != 0) {
                    foreach (int i in occurances) {

                        progress = progress.Insert (i, letter.ToString ());
                        string firstPart = progress.Substring (0, i + 1);
                        firstPart += progress.Substring (i + 2);
                        progress = firstPart.ToUpper ();
                    }

                    Program.messageControl.SendMessage (channel, "Success! Current progress: `" + progress + "`", false);

                    if (progress.ToLower () == word.ToLower ()) {
                        Program.messageControl.SendMessage (channel, "Well I'll be damned, it seems you are victorious!", false);
                        currentGame = null;
                        Utility.SetGame (null);
                    }
                    return true;
                }else{
                    tries++;

                    if (tries > maxTries) {
                        Program.messageControl.SendMessage (channel, "Bad news, you've lost the game. Also this game. Word was **" + word.ToUpper () + "**.\n" + DrawHangman (), false);
                        currentGame = null;
                    } else {
                        Program.messageControl.SendMessage (channel, "Incorrect.\n" + DrawHangman (), false);
                    }
                    return false;
                }
            }
            Program.messageControl.SendMessage (channel, "Can't do that, that letter has already been guessed.", false);
            return false;
        }

        public string DrawHangman() {
            string graphic = "```";
            for (int i = 0; i < hangmanGraphics.Length; i++) {
                if (hangmanGraphics [ i ] == '\n') {
                    graphic += '\n';
                } else {
                    graphic += int.Parse (hangmanGraphicsIndex [ i ].ToString ()) <= tries ? hangmanGraphics [ i ].ToString () : " "; // Speedy code? What's that! On the bright side this is one line.
                }
            }
            graphic += "```";
            return graphic;
        }
    }

    public class HangmanCommands : CommandSet {
        public HangmanCommands () {
            shortHelp = "Hangman command set.";
            command = "hangman";
            commandsInSet = new Command[] { new CStartHangman (), new CGuessHangman (), new CShowUsed (), new CShowProgress () };
            catagory = Category.Fun;
        }

        public class CStartHangman : Command {
            public CStartHangman() {
                command = "start";
                shortHelp = "Start new hangman.";
                overloads.Add (new Overload (typeof (Hangman), "Creates a new game of Hangman with the given word."));
            }

            public Task<Result> Execute(SocketUserMessage e, string word) {
                if (Hangman.currentGame == null) {
                    // First of all, check if there are any hidden characters.
                    bool anyHidden = false;
                    foreach (char c in word) {
                        if (Hangman.letterWhitelist.Contains (c)) {
                            anyHidden = true;
                            break;
                        }
                    }

                    if (anyHidden) {
                        Hangman.currentGame = new Hangman (word);
                        return TaskResult (Hangman.currentGame, "Succesfully started new game of Hangman! Care to take a guess? `" + Hangman.ToUnderscores (word) + "`.\n" + Hangman.currentGame.DrawHangman ());
                    } else {
                        return TaskResult (null, "Failed to start a new game of Hangman - word must contain letters of the alfabet.");
                    }
                } else {
                    return TaskResult (Hangman.currentGame,"Failed to start new game of Hangman - a game is already in progress!");
                }
            }
        }

        public class CGuessHangman : Command {
            public CGuessHangman() {
                command = "guess";
                shortHelp = "Start new hangman.";
                overloads.Add (new Overload (typeof (bool), "Attempts to guess the given character in the main game of Hangman."));
            }

            public Task<Result> Execute(SocketUserMessage e, char character) {
                if (Hangman.currentGame == null) {
                    return TaskResult (false, "Sorry man, but no game of Hangman is in progress. Why not start one? :D");
                } else {
                    return TaskResult (Hangman.currentGame.GuessLetter (e, character.ToString ().ToLower () [ 0 ]), "");
                }
            }
        }

        public class CShowUsed : Command {
            public CShowUsed() {
                command = "used";
                shortHelp = "Show used letters.";
                overloads.Add (new Overload (typeof (string), "Shows the currently used letters in the main game of Hangman."));
            }

            public Task<Result> Execute(SocketUserMessage e) {
                if (Hangman.currentGame == null) {
                    return TaskResult ("", "No current Hangman game in progress.");
                } else {
                    string combined = "Currently used letters: `";
                    foreach (char c in Hangman.currentGame.guessedLetters) {
                        combined += c.ToString ().ToUpper () + " ";
                    }
                    combined += "`";
                    return TaskResult (combined, combined);
                }
            }
        }

        public class CShowProgress : Command {
            public CShowProgress () {
                command = "progress";
                shortHelp = "Show current progress.";
                overloads.Add (new Overload (typeof (string), "Shows the progress of the main game of Hangman."));
            }

            public Task<Result> Execute (SocketUserMessage e) {
                if (Hangman.currentGame == null) {
                    return TaskResult (null, "No current Hangman game in progress.");
                } else {
                    return TaskResult (Hangman.currentGame.progress, "Current progress: `" + Hangman.currentGame.progress + "`");
                }
            }
        }
    }
}
