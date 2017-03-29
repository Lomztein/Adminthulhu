using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Rest;

namespace Adminthulhu {

    public class AutomatedWeeklyEvent : IClockable {

        public static Game[] allGames = new Game[] {
            new Game ("Overwatch"),
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
            new Game ("Robot Roller-Derby Disco Dodgeball"),
        };

        public static Game[] games;
        public static List<Vote> votes;

        public static string votesFileName = "votes";
        public static string allGamesFileName = "allgames";
        public static string gamesFileName = "games";
        public static string statusFileName = "status";

        public static string eventDay = "friday";
        public static string voteEndDay = "tuesday";
        public static int daysBetween = 2;

        public static int votesPerPerson = 3;
        public static int gamesPerWeek = 10;

        public static string announcementsChannelName = "announcements";

        public enum WeeklyEventStatus { Voting, Waiting }
        public static WeeklyEventStatus status = WeeklyEventStatus.Voting;
        public static Game highestGame = null;

        public static ulong votingMessageID = 0;

        public int eventHour = 20;

        public async Task Initialize(DateTime time) {
            votes = SerializationIO.LoadObjectFromFile<List<Vote>> (Program.dataPath + Program.eventDirectory + votesFileName + Program.gitHubIgnoreType);
            games = SerializationIO.LoadObjectFromFile<Game [ ]> (Program.dataPath + Program.eventDirectory + gamesFileName + Program.gitHubIgnoreType);
            votingMessageID = SerializationIO.LoadObjectFromFile<ulong> (Program.dataPath + Program.eventDirectory + statusFileName + Program.gitHubIgnoreType);

            if (votes == null)
                votes = new List<Vote> ();

            foreach (Vote v in votes) {
                Console.WriteLine (games [ v.votedGameID ].name);
            }

            while (Utility.GetServer () == null)
                await Task.Delay (1000);

            if (games == null)
                BeginNewVote ();

            Program.discordClient.ReactionAdded += async (message, channel, reaction) => {
                if (message.Id == votingMessageID) {
                    int reactionID = 0;
                    for (int i = 0; i < gamesPerWeek; i++) {
                        if (CEmbolden.NumberToString ((i + 1).ToString () [ 0 ]) == reaction.Emoji.Name) {
                            reactionID = i;
                            break;
                        }
                        VoteForGame (null, reaction.UserId, reactionID);
                        await message.Value.RemoveReactionAsync (reaction.Emoji, reaction.User.Value);
                    }
                }
            };
        }

        public static void SaveData() {
            SerializationIO.SaveObjectToFile(Program.dataPath + Program.eventDirectory + votesFileName + Program.gitHubIgnoreType, votes);
            SerializationIO.SaveObjectToFile(Program.dataPath + Program.eventDirectory + gamesFileName + Program.gitHubIgnoreType, games);
            SerializationIO.SaveObjectToFile(Program.dataPath + Program.eventDirectory + statusFileName + Program.gitHubIgnoreType, votingMessageID);
        }

        public Task OnDayPassed(DateTime time) {
            return Task.CompletedTask;
        }

        public Task OnMinutePassed(DateTime time) {
            if (votes != null) {
                if (time.DayOfWeek.ToString().ToLower() == voteEndDay) {
                    CountVotes();
                }
            }else{
                if (time.DayOfWeek.ToString().ToLower() == eventDay) {
                    BeginNewVote();
                }
            }
            return Task.CompletedTask;
        }

        public Task OnHourPassed ( DateTime time ) {
            return Task.CompletedTask;
        }

        public Task OnSecondPassed ( DateTime time ) {
            return Task.CompletedTask;
        }

        private void CountVotes () {
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
            AutomatedEventHandling.CreateEvent ("Friday Event", eventDay, highestGame.name + " has been chosen by vote!");

            SocketGuildChannel mainChannel = Utility.GetMainChannel (Utility.GetServer ());
            Program.messageControl.SendMessage (mainChannel as SocketTextChannel, "The game for this fridays event has been chosen by vote: **" + highestGame.name + "**! It can be joined using command `!event join friday event`");

            Dictionary<ulong, bool> didWin = new Dictionary<ulong, bool>();

            foreach (Vote vote in votes) {
                if (!didWin.ContainsKey (vote.voterID))
                    didWin.Add (vote.voterID, false);

                if (!didWin [ vote.voterID ]) {
                    if (highestGame == games [ vote.votedGameID ]) {
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
                    Program.messageControl.AskQuestion (user, "The friday event you voted for sadly lost to **" + highestGame.name + "** , do you want to join the event anyways?",
                        delegate () {
                            AutomatedEventHandling.JoinEvent (pair.Key, "friday event");
                            Program.messageControl.SendMessage (user, "You have joined the friday event succesfully!"); 
                        } );
                }else {
                    AutomatedEventHandling.JoinEvent (pair.Key, "friday event");
                }
            }

            games = null;
            votes = null;

            UpdateVoteMessage (false);
        }

        private async Task BeginNewVote () {
            status = WeeklyEventStatus.Voting;

            highestGame = null;
            List<Game> possibilities = allGames.ToList ();
            Random rand = new Random ();
            games = new Game[gamesPerWeek];

            for (int i = 0; i < games.Length; i++) {
                int index = rand.Next (0, possibilities.Count);
                games[i] = possibilities[index];
                possibilities.RemoveAt (index);
            }

            foreach (Game game in games)
                game.votes = 0;

            votes = new List<Vote> ();
            await UpdateVoteMessage (true);

            SocketGuildChannel mainChannel = Utility.GetMainChannel (Utility.GetServer ());

            Program.messageControl.SendMessage (mainChannel as SocketTextChannel, "A new vote for next friday event has begun, see pinned messages in <#188106821154766848> for votesheet.");
            SaveData ();
        }

        public static bool VoteForGame (SocketMessage e, ulong userID, int id ) {
            List<Vote> userVotes = new List<Vote> ();
            SocketGuildUser user = Utility.GetServer ().GetUser (userID);
            
            foreach (Vote vote in votes) {
                if (vote.voterID == userID)
                    userVotes.Add (vote);
            }

            if (userVotes.Count >= votesPerPerson) {
                string locText = "You've already voted for " + votesPerPerson + " games, you'll have to remove one to vote using `!event removevote <id>`, before you can place another.";
                if (e == null) {
                    Program.messageControl.SendMessage (user, locText);
                } else {
                    Program.messageControl.SendMessage (e, locText);
                }
                return false;
            }

            foreach (Vote v in userVotes) {
                if (v.votedGameID == id) {
                    string locText = "You've already voted for **" + games [ id ].name + "**. You can't vote for the same game more than once.";
                    if (e == null) {
                        Program.messageControl.SendMessage (user, locText);
                    } else {
                        Program.messageControl.SendMessage (e, locText);
                    }
                    return false;
                }
            }

            string text = "Succesfully voted for **" + games [ id ].name + "**, in the upcoming friday event.";
            if (e == null) {
                Program.messageControl.SendMessage (user, text);
            } else {
                Program.messageControl.SendMessage (e, text);
            }
            votes.Add (new Vote (userID, id));
            games[id].votes++;

            UpdateVoteMessage (false);
            SaveData ();

            return true;
        }

        public static bool RemoveVote (SocketMessage e, ulong userID, int gameID) {
            Vote vote = votes.Find (x => x.voterID == userID && x.votedGameID == gameID);
            if (vote == null) {
                Program.messageControl.SendMessage (e,"Failed to remove vote, you haven't voted for **" + games[gameID].name + "**");
                return false;
            }else {
                votes.Remove (vote);
                Program.messageControl.SendMessage (e, "Succesfully removed vote from **" + games[gameID].name + "**.");
            }

            games[gameID].votes--;
            UpdateVoteMessage (false);
            SaveData ();
            return true;
        }

        public static async Task UpdateVoteMessage(bool forceNew) {
            string text = "Vote for this " + eventDay + "s event!```\n";
            int index = 0;
            foreach (Game game in games) {
                text += (index + 1) + " - " + game.name + " - " + game.votes.ToString () + " votes.\n";
                index++;
            }

            text += "```\n";
            if (status == WeeklyEventStatus.Waiting) {
                text += "**VOTING HAS ENDED, " + highestGame.name + " HAS WON THE VOTE.**";
            } else {
                text += "**Vote using `!event vote <id>` to vote. You can vote 3 times, and also remove votes using `!event removevote <id>`!**";
            }

            SocketGuildChannel channel = Utility.SearchChannel (Utility.GetServer (), "announcements");
            IMessage message = null;
            if (votingMessageID != 0) {
                try {
                    message = await (channel as SocketTextChannel).GetMessageAsync (votingMessageID);
                } catch {
                    message = null;
                }
            }

            ChatLogger.Log ("Updating vote message.");

            if (message == null || forceNew) {

                Task<RestUserMessage> task = Program.messageControl.AsyncSend (channel as SocketTextChannel, text);
                await task;
                votingMessageID = task.Result.Id;

                for (int i = 0; i < gamesPerWeek; i++) {
                    string emoji = CEmbolden.NumberToString ((i + 1).ToString()[0]); // wat
                    await task.Result.AddReactionAsync (emoji);
                }

            } else {
                try {
                    RestUserMessage m = message as RestUserMessage;
                    await m.ModifyAsync (delegate (MessageProperties properties) {
                        properties.Content = text;
                    });
                } catch (Exception e) {
                    ChatLogger.DebugLog (e.Message);
                }
            }
        }

        public class Game {
            public string name;
            public int votes;

            public Game ( string gameName ) {
                name = gameName;
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

    public class CEventVote : Command {
        public CEventVote () {
            command = "vote";
            name = "Vote for Event";
            argHelp = "<id>";
            help = "Vote for the next friday event!";
            argumentNumber = 1;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                if (AutomatedWeeklyEvent.status == AutomatedWeeklyEvent.WeeklyEventStatus.Voting) {
                    int parse;
                    if (int.TryParse (arguments[0], out parse)) {
                        bool withinRange = parse > 0 && parse <= AutomatedWeeklyEvent.games.Length;

                        if (withinRange) {
                            AutomatedWeeklyEvent.VoteForGame (e, e.Author.Id, parse - 1);
                        } else {
                            Program.messageControl.SendMessage (e, "Failed to vote - outside range ( 1-" + (AutomatedWeeklyEvent.games.Length) + " ).");
                        }
                    } else {
                        Program.messageControl.SendMessage (e, "Failed to vote - could not parse vote.");
                    }
                } else {
                    Program.messageControl.SendMessage (e, "Failed to vote - voting not in progress.");
                }
            }
            return Task.CompletedTask;
        }
    }

    public class CRemoveVote : Command {
        public CRemoveVote () {
            command = "removevote";
            name = "Remove vote for Event";
            argHelp = "<id>";
            help = "Remove a vote for the next friday event.";
            argumentNumber = 1;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                if (AutomatedWeeklyEvent.status == AutomatedWeeklyEvent.WeeklyEventStatus.Voting) {
                    int parse;
                    if (int.TryParse (arguments[0], out parse)) {
                        bool withinRange = parse > 0 && parse <= AutomatedWeeklyEvent.games.Length;

                        if (withinRange) {
                            AutomatedWeeklyEvent.RemoveVote (e, e.Author.Id, parse - 1);
                        } else {
                            Program.messageControl.SendMessage (e, "Failed to remove vote - outside range ( 1-" + (AutomatedWeeklyEvent.games.Length) + " ).");
                        }
                    } else {
                        Program.messageControl.SendMessage (e, "Failed to remove vote - could not parse vote.");
                    }
                } else {
                    Program.messageControl.SendMessage (e, "Failed to remove vote - voting not in progress.");
                }
            }
            return Task.CompletedTask;
        }
    }
}