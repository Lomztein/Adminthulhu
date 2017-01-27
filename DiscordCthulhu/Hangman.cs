using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Discord;

namespace DiscordCthulhu {
    public class Hangman {

        public static Hangman currentGame;

        public string word;
        public string progress;

        public List<char> guessedLetters;

        public int tries = 0;
        public int maxTries = 9;

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
        }

        public bool GuessLetter (MessageEventArgs e, char letter) {
            Channel channel = e.Channel;
            Program.messageControl.SendMessage (channel, Program.GetUserName (e.User) + " guessed the letter **" + letter + "**.");
            if (letterWhitelist.Contains (letter)) {
                Program.messageControl.SendMessage (channel, "Error: No numbers or special characters allowed.");
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

                    Program.messageControl.SendMessage (channel, "Success! Current progress: `" + progress + "`");

                    if (progress.ToLower () == word.ToLower ()) {
                        Program.messageControl.SendMessage (channel, "Well I'll be damned, it seems you are victorious!");
                        currentGame = null;
                    }
                    return true;
                }else{
                    tries++;

                    if (tries > maxTries) {
                        Program.messageControl.SendMessage (channel, "Bad news, you've lost the game. Also this game. Word was **" + word.ToUpper () + "**.");
                        currentGame = null;
                    } else {
                        Program.messageControl.SendMessage (channel, "Sorry, but that letter isn't in the word you're trying to guess. You are now one step closer to eternal doom. Try " + tries + " / " + maxTries + ".");
                    }
                    return false;
                }
            }
            Program.messageControl.SendMessage (channel, "Can't do that, that letter has already been guessed.");
            return false;
        }
    }

    public class HangmanCommands : CommandSet {
        public HangmanCommands () {
            name = "Hangman Command Set";
            command = "hangman";
            help = "A set of Hangman related commands.";
            commandsInSet = new Command[] { new CStartHangman (), new CGuessHangman (), new CShowUsed (), new CShowProgress () };
        }

        public class CStartHangman : Command {
            public CStartHangman () {
                command = "start";
                name = "Start New Hangman";
                argHelp = "<word>";
                help = "Creates a new game of Hangman with the word " + argHelp + ".";
                argumentNumber = 1;
            }

            public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
                base.ExecuteCommand (e, arguments);
                if (AllowExecution (e, arguments)) {
                    if (Hangman.currentGame == null) {
                        // First of all, check if there are any hidden characters.
                        bool anyHidden = false;
                        foreach (char c in arguments[0]) {
                            if (Hangman.letterWhitelist.Contains (c)) {
                                anyHidden = true;
                                break;
                            }
                        }

                        if (anyHidden) {

                        Hangman.currentGame = new Hangman (arguments[0]);
                        Program.messageControl.SendMessage (e, "Succesfully started new game of Hangman! Care to take a guess? `" + Hangman.ToUnderscores (arguments[0]) + "`.");
                        }else {
                            Program.messageControl.SendMessage (e, "Failed to start a new game of Hangman - word must contain letters of the alfabet.");
                        }
                    } else {
                        Program.messageControl.SendMessage (e, "Failed to start new game of Hangman - a game is already in progress!");
                    }
                }
            }
        }

        public class CGuessHangman : Command {
            public CGuessHangman () {
                command = "guess";
                name = "Start New Hangman";
                argHelp = "<letter>";
                help = "Guesses the letter " + argHelp + " to the current game of Hangman.";
                argumentNumber = 1;
            }

            public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
                base.ExecuteCommand (e, arguments);
                if (AllowExecution (e, arguments)) {
                    if (Hangman.currentGame == null) {
                        Program.messageControl.SendMessage (e, "Sorry man, but no game of Hangman is in progress. Why not start one? :D");
                    } else {
                        if (arguments[0].Length == 1) {
                            Hangman.currentGame.GuessLetter (e, arguments[0].ToLower ()[0]);
                        }else {
                            Program.messageControl.SendMessage (e, "Failed to guess - you can only guess one letter at a time.");
                        }
                    }
                }
            }
        }

        public class CShowUsed : Command {
            public CShowUsed () {
                command = "used";
                name = "Show Used Letters";
                help = "Shows the currently used letters of hangman.";
                argumentNumber = 0;
            }

            public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
                base.ExecuteCommand (e, arguments);
                if (AllowExecution (e, arguments)) {
                    if (Hangman.currentGame == null) {
                        Program.messageControl.SendMessage (e, "No current Hangman game in progress.");
                    } else {
                        string combined = "Currently used letters: `";
                        foreach (char c in Hangman.currentGame.guessedLetters) {
                            combined += c.ToString ().ToUpper () + " ";
                        }
                        combined += "`";
                        Program.messageControl.SendMessage (e, combined);
                    }
                }
            }
        }

        public class CShowProgress : Command {
            public CShowProgress () {
                command = "progress";
                name = "Show Current Progress";
                help = "Shows the currently progress letters of Hangman.";
                argumentNumber = 0;
            }

            public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
                base.ExecuteCommand (e, arguments);
                if (AllowExecution (e, arguments)) {
                    if (Hangman.currentGame == null) {
                        Program.messageControl.SendMessage (e, "No current Hangman game in progress.");
                    } else {
                        Program.messageControl.SendMessage (e, "Current progress: `" + Hangman.currentGame.progress + "`");
                    }
                }
            }
        }
    }
}
