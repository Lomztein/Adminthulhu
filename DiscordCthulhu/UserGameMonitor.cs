using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    public class UserGameMonitor {

        public static Dictionary<ulong, List<string>> userGames;
        public static string fileName = "usergames";
        public const int MAX_GAMES_TO_DISPLAY = 20;

        public static void Initialize () {
            userGames = SerializationIO.LoadObjectFromFile < Dictionary<ulong, List<string>>> (Program.dataPath + fileName);
            if (userGames == null)
                userGames = new Dictionary<ulong, List<string>> ();

            Program.discordClient.UserUpdated += ( s, e ) => {
                User user = e.After;
                
                string gameName = user.CurrentGame.HasValue ? user.CurrentGame.Value.Name.ToString ().ToUpper () : null;
                AddGame (user, gameName);
            };
        }

        public static string AddGame (User user, string gameName) {
            string result = "";
            if (gameName != null && gameName != "") {
                gameName = gameName.ToUpper ();

                bool doSave = false;
                if (userGames.ContainsKey (user.Id)) {
                    if (!userGames[user.Id].Contains (gameName)) {
                        userGames[user.Id].Add (gameName);
                        ChatLogger.Log ("Added game " + gameName + " to gamelist of " + user.Name);
                        result = "Succesfully added game **" + gameName + "** to your gamelist.";
                        doSave = true;
                    }else {
                        result = "Failed to add game **" + gameName + "** - It's already there.";
                    }
                } else {
                    userGames.Add (user.Id, new List<string> ());
                    userGames[user.Id].Add (gameName);
                    ChatLogger.Log ("Constructed a new gamelist for " + user.Name);
                    result = "Succesfully added game **" + gameName + "** to your gamelist.";
                    doSave = true;
                }

                if (doSave)
                    SerializationIO.SaveObjectToFile (Program.dataPath + fileName, userGames);
            }
            return result;
        }

        public static string RemoveGame (User user, string gameName) {
            string result = "";
            gameName = gameName.ToUpper ();
            if (userGames.ContainsKey (user.Id)) {
                userGames[user.Id].Remove (gameName);
                result = "Succesfully removed **" + gameName + "** from your gamelist.";
            }
            return result;
        }

        public static List<User> FindUsersWithGame (string gameName) {
            // I feel retarded right now, something seems off.
            gameName = gameName.ToUpper ();

            List<User> foundUsers = new List<User> ();
            int count = userGames.Count ();
            for (int i = 0; i < count; i++) {
                if (userGames.ElementAt (i).Value.Contains (gameName))
                    foundUsers.Add (Program.GetServer ().GetUser (userGames.ElementAt (i).Key));
            }

            return foundUsers;
        }
    }

    public class GameCommands : CommandSet {
        public GameCommands () {
            command = "games";
            name = "Game Commands";
            help = "A set of commands specifically for game related shinanegans.";
            commandsInSet = new Command[] { new CGameOwners (), new CAddGame (), new CRemoveGame (), new CAllGames () };
        }

        // Move this command to a seperate file later, this is just for ease of writing.
        public class CGameOwners : Command {
            public CGameOwners () {
                command = "players";
                name = "Show Game Players";
                argHelp = "<gamename>";
                help = "Shows a list of everyone who've played " + argHelp;
                argumentNumber = 1;
            }

            public override void ExecuteCommand ( SocketMessage e, List<string> arguments ) {
                base.ExecuteCommand (e, arguments);
                if (AllowExecution (e, arguments)) {
                    List<User> foundUsers = UserGameMonitor.FindUsersWithGame (arguments[0]);
                    if (foundUsers.Count == 0) {
                        Program.messageControl.SendMessage (e, "Sorry, no records of **" + arguments[0] + "** being played were found.");
                    }else {
                        string total = "Here is the list of everyone who've been seen playing **" + arguments[0] + "**:```\n";
                        foreach (User user in foundUsers) {
                            total += Program.GetUserName (user) + "\n";
                        }
                        total += "```";
                        Program.messageControl.SendMessage (e, total);
                    }
                }
            }
        }
    }

    public class CAddGame : Command {
        public CAddGame () {
            command = "add";
            name = "Manually Add Game";
            argHelp = "<gamename>";
            help = "Manually adds " + argHelp + " to your gamelist.";
            argumentNumber = 1;
        }

        public override void ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                string result = UserGameMonitor.AddGame (e.User, arguments[0]);
                Program.messageControl.SendMessage (e, result);
            }            
        }
    }

    public class CRemoveGame : Command {
        public CRemoveGame () {
            command = "remove";
            name = "Manually Remove Game";
            argHelp = "<gamename>";
            help = "Manually removes " + argHelp + " from your gamelist.";
            argumentNumber = 1;
        }

        public override void ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                string result = UserGameMonitor.RemoveGame (e.User, arguments[0]);
                Program.messageControl.SendMessage (e, result);
            }
        }
    }

    public class CAllGames : Command {
        public CAllGames () {
            command = "all";
            name = "Show All Games";
            help = "Shows all games ever recorded on this server.";
            argumentNumber = 0;
        }

        public override void ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                string all = "Top " + UserGameMonitor.MAX_GAMES_TO_DISPLAY + " most played games on this server:```\n";
                Dictionary<string, int> passedGames = new Dictionary<string, int> ();
                int count = UserGameMonitor.userGames.Count ();

                for (int i = 0; i < count; i++) {
                    List<string> within = UserGameMonitor.userGames.ElementAt (i).Value;
                    foreach (string game in within) {
                        if (!passedGames.ContainsKey (game)) {
                            passedGames.Add (game, 1);
                        } else {
                            passedGames[game]++;
                        }
                    }
                }

                // Linq is wierd shit yo. Also use var just because otherwise it's a really long type.
                var items = from pair in passedGames
                            orderby pair.Value descending
                            select pair;

                count = items.Count ();
                for (int i = 0; i < Math.Min (count, UserGameMonitor.MAX_GAMES_TO_DISPLAY); i++) {
                    if (all.Length < 1900)
                        all += items.ElementAt (i).Key + " - Players: " + items.ElementAt (i).Value + "\n";
                    else
                        break;
                }
                all += "```";
                Program.messageControl.SendMessage (e, all);
            }
        }
    }
}
