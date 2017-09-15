using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Rest;

namespace Adminthulhu {
    public class UserGameMonitor : IConfigurable {

        public static Dictionary<ulong, List<string>> userGames;
        public static string fileName = "usergames";

        public static bool enabled = false;
        public static Dictionary<string, ulong> gameRoles = new Dictionary<string, ulong>();

        public static void Initialize() {
            UserGameMonitor config = new UserGameMonitor ();
            config.LoadConfiguration ();
            BotConfiguration.AddConfigurable (config);

            if (enabled) {
                userGames = SerializationIO.LoadObjectFromFile<Dictionary<ulong, List<string>>> (Program.dataPath + fileName + Program.gitHubIgnoreType);
                if (userGames == null)
                    userGames = new Dictionary<ulong, List<string>> ();

                Program.discordClient.GuildMemberUpdated += (before, after) => {

                    try {
                        if (!UserConfiguration.GetSetting<bool> (after.Id, "AllowSnooping"))
                            return Task.CompletedTask;

                        string gameName = after.Game.HasValue ? after.Game.Value.Name.ToString ().ToUpper () : null;
                        AddGame (after, gameName);
                    } catch (Exception e) {
                        Logging.Log (Logging.LogType.EXCEPTION,  e.Message + " - " + e.StackTrace);
                    }
                    return Task.CompletedTask;
                };
            }

        }

        public static string AddGame (SocketUser user, string gameName) {
            string result = "";
            if (gameName != null && gameName != "") {
                gameName = gameName.ToUpper ();

                bool doSave = false;
                if (userGames.ContainsKey (user.Id)) {
                    if (!userGames[user.Id].Contains (gameName)) {
                        userGames[user.Id].Add (gameName);
                        Logging.Log (Logging.LogType.BOT, "Added game " + gameName + " to gamelist of " + user.Username);
                        result = "Succesfully added game **" + gameName + "** to your gamelist.";
                        doSave = true;
                    }else {
                        result = "Failed to add game **" + gameName + "** - It's already there.";
                    }
                } else {
                    userGames.Add (user.Id, new List<string> ());
                    userGames[user.Id].Add (gameName);
                    Logging.Log (Logging.LogType.BOT, "Constructed a new gamelist for " + user.Username);
                    result = "Succesfully added game **" + gameName + "** to your gamelist.";
                    doSave = true;
                }

                ChangeGameRole (user, gameName, true);

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
            ChangeGameRole (user, gameName, false);

            return result;
        }

        public static void ChangeGameRole(SocketUser user, string gameName, bool add) {
            if (gameRoles.ContainsKey (gameName) && UserConfiguration.GetSetting<bool> (user.Id, "AutoManageGameRoles")) {
                ulong roleID = gameRoles [ gameName ];
                SocketRole role = Utility.GetServer ().GetRole (roleID);
                if (role != null) {
                    if (add) {
                        Utility.SecureAddRole (user as SocketGuildUser, role);
                    } else {
                        Utility.SecureRemoveRole (user as SocketGuildUser, role);
                    }
                } else {
                    Logging.Log (Logging.LogType.WARNING, "Failed to find game role for " + gameName + " despite the data being present, please make sure the ID's match up as well.");
                }
            }
        }

        public static List<SocketGuildUser> FindUsersWithGame (ref string gameName) {
            // I feel retarded right now, something seems off.
            gameName = gameName.ToUpper ();
            string copy = gameName;

            List<SocketGuildUser> foundUsers = new List<SocketGuildUser> ();
            int count = userGames.Count ();
            for (int i = 0; i < count; i++) {
                string foundGame = userGames.ElementAt (i).Value.Find (x => new SoftStringComparer ().Equals (x, copy));
                if (foundGame != null)
                    gameName = foundGame;

                if (userGames.ElementAt (i).Value.Contains (copy, new SoftStringComparer ()))
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
            enabled = BotConfiguration.GetSetting("Games.Enabled", "Misc.UserGameMonitorEnabled", enabled);

            gameRoles = new Dictionary<string, ulong> ();
            gameRoles.Add ("GAME NAME #1", 0);
            gameRoles.Add ("GAME NAME #2", 0); // Dictionaries are bitches to defaultize. Defaultize?
            gameRoles = BotConfiguration.GetSetting("Games.GameRoles", "", gameRoles);
        }
    }

    public class GameCommands : CommandSet {
        public GameCommands () {
            command = "games";
            shortHelp = "Game command set.";
            commandsInSet = new Command[] { new CGameOwners (), new CAddGame (), new CRemoveGame (), new CAllGames () };
            catagory = Category.Utility;
        }

        // Move this command to a seperate file later, this is just for ease of writing.
        public class CGameOwners : Command {
            public CGameOwners() {
                command = "players";
                shortHelp = "Show game players.";
                AddOverload (typeof (string), "Shows a list of everyone who've played <gamename>");
            }

            public Task<Result> Execute(SocketUserMessage e, string gamename) {
                UserGameMonitor.PurgeData ();
                string foundGame = gamename;
                List<SocketGuildUser> foundUsers = UserGameMonitor.FindUsersWithGame (ref foundGame);
                if (foundUsers.Count == 0) {
                    return TaskResult ("", "Sorry, no records of **" + foundGame + "** being played were found.");
                } else {
                    string total = "Here is the list of everyone who've been seen playing **" + foundGame + "**:```\n";
                    foreach (SocketGuildUser user in foundUsers) {
                        total += Utility.GetUserName (user) + "\n";
                    }
                    total += "```";
                    return TaskResult (total, total);
                }
            }
        }
    }

    public class CAddGame : Command {
        public CAddGame () {
            command = "add";
            shortHelp = "Manually add game.";

            AddOverload (typeof (object), "Manually adds <gameName> to your gamelist.");
        }

        public Task<Result> Execute(SocketUserMessage e, string gameName) {
            string result = UserGameMonitor.AddGame ((e.Author as SocketGuildUser), gameName);
            return TaskResult(null, result);
        }
    }

    public class CRemoveGame : Command {
        public CRemoveGame () {
            command = "remove";
            shortHelp = "Manually remove game.";
            AddOverload (typeof (object), "Manually removes <gameName> from your gamelist.");
        }

        public Task<Result> Execute(SocketUserMessage e, string gameName) {
            string result = UserGameMonitor.RemoveGame ((e.Author as SocketGuildUser), gameName);
            return TaskResult (result, result);
        }
    }

    public class CAllGames : Command {
        public CAllGames () {
            command = "all";
            shortHelp = "Show all games.";
            AddOverload (typeof (RestUserMessage), "Shows all games ever recorded on this server.");
        }

        public async Task<Result>Execute(SocketUserMessage message) {
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
                        passedGames [ game ]++;
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
            RestUserMessage userMessage = await Program.messageControl.SendBookMessage (message.Channel, "All games seen played on this server:", all, allowInMain, "```");

            return new Result (userMessage, "All games played on this server:");
        }
    }
}
