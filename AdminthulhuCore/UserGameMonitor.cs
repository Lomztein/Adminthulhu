using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Adminthulhu {
    public class UserGameMonitor : IConfigurable {

        public static Dictionary<ulong, List<string>> userGames;
        public static string fileName = "usergames";

        public static bool enabled = false;

        public static void Initialize() {
            UserGameMonitor config = new UserGameMonitor ();
            config.LoadConfiguration ();
            BotConfiguration.AddConfigurable (config);

            userGames = SerializationIO.LoadObjectFromFile<Dictionary<ulong, List<string>>> (Program.dataPath + fileName + Program.gitHubIgnoreType);
            if (userGames == null)
                userGames = new Dictionary<ulong, List<string>> ();

            Program.discordClient.GuildMemberUpdated += (before, after) => {

                if (UserConfiguration.GetSetting<bool> (after.Id, "AllowSnooping", true))
                    return Task.CompletedTask;

                string gameName = after.Game.HasValue ? after.Game.Value.Name.ToString ().ToUpper () : null;
                AddGame (after, gameName);

                return Task.CompletedTask;
            };
        }

        public static string AddGame (SocketUser user, string gameName) {
            string result = "";
            if (gameName != null && gameName != "") {
                gameName = gameName.ToUpper ();

                bool doSave = false;
                if (userGames.ContainsKey (user.Id)) {
                    if (!userGames[user.Id].Contains (gameName)) {
                        userGames[user.Id].Add (gameName);
                        ChatLogger.Log ("Added game " + gameName + " to gamelist of " + user.Username);
                        result = "Succesfully added game **" + gameName + "** to your gamelist.";
                        doSave = true;
                    }else {
                        result = "Failed to add game **" + gameName + "** - It's already there.";
                    }
                } else {
                    userGames.Add (user.Id, new List<string> ());
                    userGames[user.Id].Add (gameName);
                    ChatLogger.Log ("Constructed a new gamelist for " + user.Username);
                    result = "Succesfully added game **" + gameName + "** to your gamelist.";
                    doSave = true;
                }

                if (doSave)
                    SerializationIO.SaveObjectToFile (Program.dataPath + fileName + Program.gitHubIgnoreType, userGames);
            }
            return result;
        }

        public static string RemoveGame (SocketGuildUser user, string gameName) {
            string result = "";
            gameName = gameName.ToUpper ();
            if (userGames.ContainsKey (user.Id)) {
                userGames[user.Id].Remove (gameName);
                result = "Succesfully removed **" + gameName + "** from your gamelist.";
            }
            return result;
        }

        public static List<SocketGuildUser> FindUsersWithGame (string gameName, out string foundGame) {
            // I feel retarded right now, something seems off.
            gameName = gameName.ToUpper ();
            foundGame = gameName;

            List<SocketGuildUser> foundUsers = new List<SocketGuildUser> ();
            int count = userGames.Count ();
            for (int i = 0; i < count; i++) {
                foundGame = userGames.ElementAt (i).Value.Find (x => new SoftStringComparer ().Equals (x, gameName));

                if (userGames.ElementAt (i).Value.Contains (foundGame, new SoftStringComparer ()))
                    foundUsers.Add (Utility.GetServer ().GetUser (userGames.ElementAt (i).Key));
            }

            return foundUsers;
        }

        public static void PurgeData() {
            List<ulong> toRemove = new List<ulong> ();
                foreach (KeyValuePair<ulong, List<string>> pair in userGames) {
                SocketGuildUser user = Utility.GetServer ().GetUser (pair.Key);
                if (user == null || user.IsBot) {
                    toRemove.Add (pair.Key);
                }
            }

            foreach (ulong id in toRemove) {
                userGames.Remove (id);
            }
        }

        public void LoadConfiguration() {
            enabled = BotConfiguration.GetSetting("UserGameMonitorEnabled", enabled);
        }
    }

    public class GameCommands : CommandSet {
        public GameCommands () {
            command = "games";
            shortHelp = "Game command set.";
            longHelp = "A set of commands specifically for game related shinanegans.";
            commandsInSet = new Command[] { new CGameOwners (), new CAddGame (), new CRemoveGame (), new CAllGames () };
            catagory = Catagory.Utility;
        }

        // Move this command to a seperate file later, this is just for ease of writing.
        public class CGameOwners : Command {
            public CGameOwners () {
                command = "players";
                shortHelp = "Show game players.";
                argHelp = "<gamename>";
                longHelp = "Shows a list of everyone who've played " + argHelp;
                argumentNumber = 1;
            }

            public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
                base.ExecuteCommand (e, arguments);
                if (AllowExecution (e, arguments)) {
                    UserGameMonitor.PurgeData ();
                    string foundGame = "";
                    List<SocketGuildUser> foundUsers = UserGameMonitor.FindUsersWithGame (arguments[0], out foundGame);
                    if (foundUsers.Count == 0) {
                        Program.messageControl.SendMessage (e, "Sorry, no records of **" + foundGame + "** being played were found.", false);
                    }else {
                        string total = "Here is the list of everyone who've been seen playing **" + foundGame + "**:```\n";
                        foreach (SocketGuildUser user in foundUsers) {
                            total += Utility.GetUserName (user) + "\n";
                        }
                        total += "```";
                        Program.messageControl.SendMessage (e, total, false);
                    }
                }
            return Task.CompletedTask;
            }
        }
    }

    public class CAddGame : Command {
        public CAddGame () {
            command = "add";
            shortHelp = "Manually add game.";
            argHelp = "<gamename>";
            longHelp = "Manually adds " + argHelp + " to your gamelist.";
            argumentNumber = 1;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                string result = UserGameMonitor.AddGame ((e.Author as SocketGuildUser), arguments[0]);
                Program.messageControl.SendMessage (e, result, false);
            }            
            return Task.CompletedTask;
        }
    }

    public class CRemoveGame : Command {
        public CRemoveGame () {
            command = "remove";
            shortHelp = "Manually remove game.";
            argHelp = "<gamename>";
            longHelp = "Manually removes " + argHelp + " from your gamelist.";
            argumentNumber = 1;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                string result = UserGameMonitor.RemoveGame ((e.Author as SocketGuildUser), arguments[0]);
                Program.messageControl.SendMessage (e, result, false);
            }
            return Task.CompletedTask;
        }
    }

    public class CAllGames : Command {
        public CAllGames () {
            command = "all";
            shortHelp = "Show all games.";
            longHelp = "Shows all games ever recorded on this server.";
            argumentNumber = 0;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                UserGameMonitor.PurgeData ();
                Dictionary<string, int> passedGames = new Dictionary<string, int> ();
                int count = UserGameMonitor.userGames.Count ();

                string all = "";
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
                for (int i = 0; i < count; i++) {
                    all += Utility.UniformStrings (items.ElementAt (i).Key, "Players: " + items.ElementAt (i).Value + "\n", " - ");
                }
                Program.messageControl.SendMessage (e, "All games played on this server:", false);
                Program.messageControl.SendMessage (e.Channel, all, false, "```");
            }
            return Task.CompletedTask;
        }
    }
}
