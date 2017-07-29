using Discord;
using System;
using System.Collections.Generic;
using System.Linq;  
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Rest;

namespace Adminthulhu {

    public class AutomatedWeeklyEvent : IClockable, IConfigurable {

        private static string [ ] unicodeEmojis = new string [ ] { "1⃣", "2⃣", "3⃣", "4⃣", "5⃣", "6⃣", "7⃣", "8⃣", "9⃣", "🔟" };

        public static List<Game> allGames = new List<Game> () {
            /*new Game ("Overwatch"),
            new Game ("GMod"),
            new Game ("Counter Strike: GO"),
            new Game ("Team Fortress 2"),
            new Game ("Dodgeball"),
            new Game ("Guns of Icarus"),
            new Game ("Rocket League"),
            new Game ("Brawlhalla"),
            new Game ("Battlefield"),
            new Game ("Left 4 Dead 2"),
            new Game ("PlanetSide 2"),
            new Game ("RollerCoaster Tycoon 2 Multiplayer"),
            new Game ("Killing Floor"),
            new Game ("Tribes: Ascend"),
            new Game ("Quake Live"),
            new Game ("Air Brawl"),
            new Game ("Duck Game"),
            new Game ("Toribash"),
            new Game ("Robocraft"),
            new Game ("TrackMania"),
            new Game ("Robot Roller-Derby Disco Dodgeball"),*/
        };

        public static Game [ ] games;
        public static List<Vote> votes;

        public static string dataFileName = "weeklyevent";

        public static DayOfWeek voteStartDay = DayOfWeek.Monday;
        public static DayOfWeek voteEndDay = DayOfWeek.Thursday;
        public static string eventDayName = "friday";
        public static int daysBetween = 1;

        public static int votesPerPerson = 3;
        public static int gamesPerWeek = 10;

        public static string announcementsChannelName = "announcements";

        public enum WeeklyEventStatus { Voting, Waiting }
        public static WeeklyEventStatus status = WeeklyEventStatus.Voting;
        public static Game highestGame = null;

        public static ulong votingMessageID = 0;
        public static ulong joinMessageID;

        public static int eventHour = 20;
        public static string EVENT_NAME = ""+ eventDayName.Substring (0, 1).ToUpper () + eventDayName.Substring (1) +" Event"; // This is like the worst solution ever.

        public static string onEventJoinedDM = "Thank you for joining this fridays event!";
        public static string onEventLeftDM = "You've succesfully left this fridays event.";
        public static string onEventChosenByVoteMessage = "The game for this fridays event has been chosen by vote: **{VOTEDGAME}**! It can be joined by pressing the calender below!";
        public static string onVotedEventLostDM = "The game you voted for for this friday event sadly lost to **{VOTEDGAME}**, would you like to join anyways?";
        public static string onNewVoteStartedMessage = "{EVERYONEMENTION}! A new friday event vote has begun, check {ANNOUNCEMENTCHANNEL} for the votesheet!";
        public static string onMaxVotesReachedDM = "You've reached your max amount of three votes, please remove one if you wish to vote for another.";
        public static string onVotedForGameTwiceDM = "You've voted for the same game twice, which you cannot do.";

        public void LoadConfiguration() {
            votesPerPerson = BotConfiguration.GetSetting ("WeeklyEvent.VotesPerPerson", "EventVotesPerPerson", votesPerPerson);
            gamesPerWeek = BotConfiguration.GetSetting ("WeeklyEvent.GamesPerWeek", "EventGamesPerWeek", gamesPerWeek);
            announcementsChannelName = BotConfiguration.GetSetting ("Server.AnnouncementsChannelName", "AnnouncementsChannelName", "announcements");

            voteStartDay = BotConfiguration.GetSetting ("WeeklyEvent.VoteStartDay", "EventVoteStartDay", voteStartDay);
            voteEndDay = BotConfiguration.GetSetting ("WeeklyEvent.VoteEndDay", "EventVoteEndDay", voteEndDay);
            eventDayName = BotConfiguration.GetSetting ("WeeklyEvent.EventDayName", "EventDayName", eventDayName);
            daysBetween = BotConfiguration.GetSetting ("WeeklyEvent.DaysBetweenVoteAndEvent", "DaysBetweenVoteEndAndEvent", daysBetween);
            eventHour = BotConfiguration.GetSetting ("WeeklyEvent.Hour", "EventHour", eventHour);

            onEventJoinedDM = BotConfiguration.GetSetting ("WeeklyEvent.Messages.OnEventJoinedDM", "", onEventJoinedDM);
            onEventLeftDM = BotConfiguration.GetSetting ("WeeklyEvent.Messages.OnEventLeftDM", "", onEventLeftDM);
            onEventChosenByVoteMessage = BotConfiguration.GetSetting ("WeeklyEvent.Messages.OnEventChosenByVoteMessage", "", onEventChosenByVoteMessage);
            onVotedEventLostDM = BotConfiguration.GetSetting ("WeeklyEvent.Messages.OnVotedEventLostDM", "", onVotedEventLostDM);
            onNewVoteStartedMessage = BotConfiguration.GetSetting ("WeeklyEvent.Messages.OnNewVoteStartedMessage", "", onNewVoteStartedMessage);
            onMaxVotesReachedDM = BotConfiguration.GetSetting ("WeeklyEvent.Messages.OnMaxVotesReachedDM", "", onMaxVotesReachedDM);
            onVotedForGameTwiceDM = BotConfiguration.GetSetting ("WeeklyEvent.Messages.OnVotedForGameTwiceDM", "", onVotedForGameTwiceDM);
        }

        public async Task Initialize(DateTime time) {
            LoadConfiguration ();
            BotConfiguration.AddConfigurable (this);
            Data loadedData = SerializationIO.LoadObjectFromFile<Data> (Program.dataPath + dataFileName + Program.gitHubIgnoreType);

            votes = loadedData.votes;
            games = loadedData.games;
            votingMessageID = loadedData.votingMessageID;
            joinMessageID = loadedData.joinMessageID;
            allGames = loadedData.allGames;
            status = loadedData.status;

            if (allGames == null)
                allGames = new List<Game> ();

            while (Utility.GetServer () == null)
                await Task.Delay (1000);

            if (status == WeeklyEventStatus.Waiting && (int)DateTime.Now.DayOfWeek < (int)voteEndDay)
                BeginNewVote ();

            Program.discordClient.ReactionAdded += async (message, channel, reaction) => {
                OnReactionChanged (message, channel, reaction, true);
            };
            Program.discordClient.ReactionRemoved += async (message, channel, reaction) => {
                OnReactionChanged (message, channel, reaction, false);
            };
        }

        private static string GetUnicodeEmoji(int index) {
            return unicodeEmojis [ index ];
        }

        private static void OnReactionChanged(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction, bool add) {
            if (reaction.User.Value.IsBot)
                return;

            if (message.Id == votingMessageID) {
                int reactionID = -1;
                for (int i = 0; i < gamesPerWeek; i++) {
                    if (GetUnicodeEmoji (i) == reaction.Emote.Name) {
                        reactionID = i;
                        break;
                    }
                }

                if (add) {
                    if (!VoteForGame (reaction.User.Value.Id, reactionID)) {
                        message.Value.RemoveReactionAsync (reaction.Emote, message.Value.Author);
                    }
                } else {
                    RemoveVote (reaction.UserId, reactionID);
                }

                if (reactionID == -1) {
                    message.Value.RemoveReactionAsync (reaction.Emote, message.Value.Author);
                }
            }

            if (message.Id == joinMessageID) {
                if (reaction.Emote.Name == "🗓") {
                    if (add) {
                        if (DiscordEvents.JoinEvent (reaction.UserId, EVENT_NAME)) {
                            Program.messageControl.SendMessage (Utility.GetServer ().GetUser (reaction.UserId), onEventJoinedDM);
                        }
                    } else {
                        if (DiscordEvents.LeaveEvent (reaction.UserId, EVENT_NAME)) {
                            Program.messageControl.SendMessage (Utility.GetServer ().GetUser (reaction.UserId), onEventLeftDM);
                        }
                    }
                }
            }
        }

        public static void SaveData() {
            SerializationIO.SaveObjectToFile (Program.dataPath + dataFileName + Program.gitHubIgnoreType, new Data (games, votes, votingMessageID, joinMessageID, allGames, status));
        }

        public Task OnDayPassed(DateTime time) {
            if (time.DayOfWeek == voteStartDay)
                BeginNewVote ();
            if (time.DayOfWeek == voteEndDay)
                CountVotes ();
            return Task.CompletedTask; // Lets try without the votes possibly standing in the way. We trust the timer now.
        }

        public Task OnMinutePassed(DateTime time) {
            return Task.CompletedTask;
        }

        public Task OnHourPassed(DateTime time) {
            return Task.CompletedTask;
        }

        public Task OnSecondPassed(DateTime time) {
            return Task.CompletedTask;
        }

        private async void CountVotes() {
            try {
                status = WeeklyEventStatus.Waiting;

                highestGame = null;
                int highestVote = int.MinValue;

                foreach (Game game in games) {
                    if (game.votes > highestVote) {
                        highestGame = game;
                        highestVote = game.votes;
                    }
                }

                DateTime now = DateTime.Now;
                DateTime eventDay = new DateTime (now.Year, now.Month, now.Day, eventHour, 0, 0).AddDays (daysBetween);
                DiscordEvents.CreateEvent (EVENT_NAME, eventDay, highestGame.name + " has been chosen by vote!");

                SocketGuildChannel mainChannel = Utility.GetMainChannel ();
                RestUserMessage joinMessage = await Program.messageControl.AsyncSend (mainChannel as SocketTextChannel, onEventChosenByVoteMessage.Replace ("{VOTEDGAME}", highestGame.name), true);
                joinMessageID = joinMessage.Id;
                joinMessage.AddReactionAsync (new Emoji ("🗓"));

                Dictionary<ulong, bool> didWin = new Dictionary<ulong, bool> ();

                foreach (Vote vote in votes) {
                    if (!didWin.ContainsKey (vote.voterID))
                        didWin.Add (vote.voterID, false);

                    if (!didWin [ vote.voterID ]) {
                        if (highestGame.name == games [ vote.votedGameID ].name) {
                            didWin [ vote.voterID ] = true;
                        }
                    }
                }

                int count = didWin.Count ();
                for (int i = 0; i < count; i++) {
                    KeyValuePair<ulong, bool> pair = didWin.ElementAt (i);
                    if (!pair.Value) {
                        SocketGuildUser user = Utility.GetServer ().GetUser (pair.Key);
                        // AutomatedEventHandling seriously lacks wrapper functions.
                        await Program.messageControl.AskQuestion (user, onVotedEventLostDM.Replace ("{VOTEDGAME}", highestGame.name),
                            delegate () {
                                DiscordEvents.JoinEvent (pair.Key, EVENT_NAME);
                                Program.messageControl.SendMessage (user, onEventJoinedDM);
                            });
                    } else {
                        DiscordEvents.JoinEvent (pair.Key, EVENT_NAME);
                    }
                }

                await UpdateVoteMessage (false);
                SaveData ();
            } catch (Exception e) {
                ChatLogger.DebugLog (e.Message + " - " + e.StackTrace);
            }
        }

        private async Task BeginNewVote() {
            status = WeeklyEventStatus.Voting;

            for (int i = 0; i < allGames.Count; i++) {
                allGames [ i ].votes = 0;
            }

            highestGame = null;
            List<Game> possibilities = allGames.ToList ();
            List<Game> highlightedGames = possibilities.FindAll (x => x.highlight);
            possibilities.RemoveAll (x => x.highlight);

            List<Game> selectedGames = new List<Game> ();
            Random rand = new Random ();
            games = new Game [ gamesPerWeek ];

            try {
                // A C++ programmer would possibly kill me for this memory management.
                for (int i = 0; i < gamesPerWeek; i++) {
                    int index = rand.Next (0, highlightedGames.Count > 0 ? highlightedGames.Count : possibilities.Count);
                    if (highlightedGames.Count > 0) {
                        selectedGames.Add (highlightedGames [ index ]);
                        highlightedGames.RemoveAt (index);
                    } else {
                        selectedGames.Add (possibilities [ index ]);
                        possibilities.RemoveAt (index);
                    }
                }
            } catch (Exception e) {
                ChatLogger.DebugLog (e.StackTrace);
            }
            // Shuffle dat shite.
            for (int i = 0; i < gamesPerWeek; i++) {
                int index = rand.Next (0, selectedGames.Count);
                games [ i ] = selectedGames [ index ];
                selectedGames.RemoveAt (index);
            }

            // Well that's the least optimized code I've written in ages, and that says something.

            foreach (Game game in games)
                game.votes = 0;

            votes = new List<Vote> ();
            await UpdateVoteMessage (true);

            SocketGuildChannel mainChannel = Utility.GetMainChannel ();

            Program.messageControl.SendMessage (mainChannel as SocketTextChannel, onNewVoteStartedMessage.Replace ("{EVERYONEMENTION}",
                Utility.GetServer ().EveryoneRole.Mention).Replace ("{ANNOUNCEMENTCHANNEL}", "<#" + Utility.SearchChannel (Utility.GetServer (), announcementsChannelName).Id+">"), true);
            SaveData ();
        }

        public static bool VoteForGame(ulong userID, int id) {
            SocketGuildUser user = Utility.GetServer ().GetUser (userID);
            List<Vote> userVotes;

            if (HasReachedVoteLimit (userID, out userVotes)) {
                string locText = onMaxVotesReachedDM;
                Program.messageControl.SendMessage (user, locText);
                return false;
            }

            foreach (Vote v in userVotes) {
                if (v.votedGameID == id) {
                    string locText = onVotedForGameTwiceDM;
                    Program.messageControl.SendMessage (user, locText);
                    return false;
                }
            }

            votes.Add (new Vote (userID, id));
            games [ id ].votes++;

            UpdateVoteMessage (false);
            SaveData ();

            return true;
        }

        public static bool HasReachedVoteLimit(ulong userID, out List<Vote> userVotes) {
            userVotes = new List<Vote> ();
            SocketGuildUser user = Utility.GetServer ().GetUser (userID);

            foreach (Vote vote in votes) {
                if (vote.voterID == userID)
                    userVotes.Add (vote);
            }

            return (userVotes.Count >= votesPerPerson);
        }

        public static bool HighlightGame(string gameName) {
            Game game = allGames.Find (x => x.name.ToLower () == gameName.ToLower ());
            if (game != null) {
                game.highlight = !game.highlight;
                SaveData ();
                return true;
            } else {
                return false;
            }
        }

        public static bool AddGame(string gameName, bool highlight = false) {
            if (allGames.Find (x => x.name.ToLower () == gameName.ToLower ()) == null) {
                allGames.Add (new Game (gameName) { highlight = highlight }); // Not even remotely confusing.
                SaveData ();
                return true;
            }
            return false;
        }

        public static void RemoveGame(int id) {
            allGames.RemoveAt (id);
            SaveData ();
        }

        public static bool RemoveVote(ulong userID, int gameID) {
            Vote vote = votes.Find (x => x.voterID == userID && x.votedGameID == gameID);
            if (vote == null)
                return false;
            else {
                games [ gameID ].votes--;
                votes.Remove (vote);
            }

            UpdateVoteMessage (false);
            SaveData ();
            return true;
        }

        public static async Task UpdateVoteMessage(bool forceNew) {
            try {
                string text = "Vote for this " + eventDayName + "s event!```\n";
                int index = 0;
                foreach (Game game in games) {
                    text += Utility.UniformStrings ((index + 1) + " - " + game.name + (game.highlight ? " *" : ""), game.votes.ToString () + " votes.\n", " - ");
                    index++;
                }

                text += "```\n";
                if (status == WeeklyEventStatus.Waiting) {
                    text += "**VOTING HAS ENDED, " + highestGame.name.ToUpper () + " HAS WON THE VOTE.**";
                } else {
                    text += "**Vote using the reactions below. You can vote " + votesPerPerson + " times!**";
                }

                SocketGuildChannel channel = Utility.SearchChannel (Utility.GetServer (), announcementsChannelName);
                IMessage message = null;
                if (votingMessageID != 0) {
                    try {
                        message = await (channel as SocketTextChannel).GetMessageAsync (votingMessageID);
                    } catch {
                        message = null;
                    }
                }

                if (message == null || forceNew) {

                    if (message != null)
                        await message.DeleteAsync ();

                    Task<RestUserMessage> task = Program.messageControl.AsyncSend (channel as SocketTextChannel, text, false);
                    await task;
                    votingMessageID = task.Result.Id;

                    for (int i = 0; i < gamesPerWeek; i++) {
                        string emoji = GetUnicodeEmoji (i);
                        await task.Result.AddReactionAsync (new Emoji (emoji));
                    }

                } else {
                    RestUserMessage m = message as RestUserMessage;
                    await m.ModifyAsync (delegate (MessageProperties properties) {
                        properties.Content = text;
                    });

                }
            } catch (Exception e) {
                ChatLogger.DebugLog (e.Message + " - " + e.StackTrace);
            }
        }

        public class Data {
            public Game [ ] games;
            public List<Game> allGames;
            public List<Vote> votes;
            public ulong votingMessageID;
            public ulong joinMessageID;
            public WeeklyEventStatus status;

            public Data(Game[] _games, List<Vote> _votes, ulong _votingMessageID, ulong _joinMessageID, List<Game> _allGames, WeeklyEventStatus _status) {
                games = _games;
                allGames = _allGames;
                votes = _votes;
                votingMessageID = _votingMessageID;
                joinMessageID = _joinMessageID;
                status = _status;
            }
        }

        public class Game {
            public string name;
            public int votes;
            public bool highlight;

            public Game ( string gameName ) {
                name = gameName;
            }

            public void ToggleHighlight() {
                highlight = !highlight;
            }
        }

        public class Vote {
            public ulong voterID;
            public int votedGameID = -1;

            public Vote ( ulong _voterID, int _votedGameID ) {
                voterID = _voterID;
                votedGameID = _votedGameID;
            }
        }
    }

    public class CRemoveEventGame : Command {
        public CRemoveEventGame() {
            command = "removeeventgame";
            shortHelp = "Remove a game.";
            argHelp = "<id>";
            longHelp = "Remove a game from automated weekly events.";
            argumentNumber = 1;
            isAdminOnly = true;
            catagory = Catagory.Admin;
        }

        public override Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                int parse;
                if (int.TryParse (arguments [ 0 ], out parse)) {
                    bool withinRange = parse > 0 && parse <= AutomatedWeeklyEvent.games.Length;

                    if (withinRange) {
                        AutomatedWeeklyEvent.RemoveGame (parse);
                    } else {
                        Program.messageControl.SendMessage (e, "Failed to remove - outside range ( 0-" + (AutomatedWeeklyEvent.allGames.Count - 1) + " ).", false);
                    }
                } else {
                    Program.messageControl.SendMessage (e, "Failed to remove, could not parse number.", false);
                }
            }
            return Task.CompletedTask;
        }
    }

    public class CAddEventGame : Command {
        public CAddEventGame() {
            command = "addeventgame";
            shortHelp = "Add a game.";
            argHelp = "<name>;<highlight(true,false)>";
            longHelp = "Remove a game from automated weekly events.";
            argumentNumber = 2;
            isAdminOnly = true;
            catagory = Catagory.Admin;
        }

        public override Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                bool parse;
                if (bool.TryParse (arguments [ 1 ], out parse)) {
                    if (AutomatedWeeklyEvent.AddGame (arguments [ 0 ], parse)) {
                        Program.messageControl.SendMessage (e, "Succesfully added game to automated events.", false);
                    } else {
                        Program.messageControl.SendMessage (e, "Failed to add game, it might already be on the list.", false);
                    }
                } else {
                    Program.messageControl.SendMessage (e, "Failed to add, could not parse highlight bool.", false);
                }
            }
            return Task.CompletedTask;
        }
    }

    public class CHighlightEventGame : Command {
        public CHighlightEventGame() {
            command = "highlighteventgame";
            shortHelp = "Highlight a game.";
            argHelp = "<name>";
            longHelp = "Toggles whether or not a game is highlighted.";
            argumentNumber = 1;
            isAdminOnly = true;
            catagory = Catagory.Admin;
        }

        public override Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                if (AutomatedWeeklyEvent.HighlightGame (arguments [ 0 ])) {
                    Program.messageControl.SendMessage (e, "Succesfully toggled game highlight automated events.", false);
                } else {
                    Program.messageControl.SendMessage (e, "Failed to toggle game highlight.", false);
                }
            }
            return Task.CompletedTask;
        }
    }

    public class CListEventGames : Command {
        public CListEventGames() {
            command = "listeventgames";
            shortHelp = "List event games.";
            longHelp = "Lists all possible event games.";
            argumentNumber = 0;
            catagory = Catagory.Utility;
        }

        public override Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                string result = "```";
                for (int i = 0; i < AutomatedWeeklyEvent.allGames.Count; i++) {
                    result += "\n" + AutomatedWeeklyEvent.allGames [ i ].name + (AutomatedWeeklyEvent.allGames[i].highlight ? " *" : "");
                }
                result += "```";
                Program.messageControl.SendMessage (e, result, false);
            }
            return Task.CompletedTask;
        }
    }
}