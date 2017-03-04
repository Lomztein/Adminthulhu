using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace DiscordCthulhu {

    public class AutomatedWeeklyEvent : IClockable {

        public static Game[] allGames = new Game[] {
            new Game ("Overwatch"),
            new Game ("GMod"),
            new Game ("Counter Strike: GO"),
            new Game ("Team Fortress 2"),
            new Game ("Dodgeball"),
            new Game ("Guns of Icarus"),
            new Game ("Rocket League"),
            new Game ("Brawlshalla"),
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

        public static string eventDay = "friday";
        public static string voteEndDay = "wednesday";
        public static int daysBetween = 2;

        public static int votesPerPerson = 3;
        public static int gamesPerWeek = 10;

        public enum WeeklyEventStatus { Voting, Waiting }
        public static WeeklyEventStatus status = WeeklyEventStatus.Voting;
        public static Game highestGame = null;

        public static ulong votingMessageID = 276023604892663808;

        public int eventHour = 20;

        public async Task Initialize ( DateTime time ) {
            votes = await SerializationIO.LoadObjectFromFile<List<Vote>> (Program.dataPath + Program.eventDirectory + votesFileName + Program.gitHubIgnoreType);
            games = await SerializationIO.LoadObjectFromFile<Game[]> (Program.dataPath + Program.eventDirectory + gamesFileName + Program.gitHubIgnoreType);

            if (votes == null)
                votes = new List<Vote> ();

            foreach (Vote v in votes) {
                Console.WriteLine (games[v.votedGameID].name);
            }

            while (Program.GetServer () == null)
                await Task.Delay (1000);

            if (games == null)
                await BeginNewVote ();
        }

        public static async Task SaveData () {
            await SerializationIO.SaveObjectToFile (Program.dataPath + Program.eventDirectory + votesFileName + Program.gitHubIgnoreType, votes);
            await SerializationIO.SaveObjectToFile (Program.dataPath + Program.eventDirectory + gamesFileName + Program.gitHubIgnoreType, games);
        }

        public Task OnDayPassed ( DateTime time ) {
            return Task.CompletedTask;
        }

        public async Task OnHourPassed ( DateTime time ) {
            switch (status) {
                case WeeklyEventStatus.Voting:
                    if (time.AddDays (-1).DayOfWeek.ToString ().ToLower () == voteEndDay) {
                        await CountVotes ();
                    }
                    break;

                case WeeklyEventStatus.Waiting:
                    if (time.AddDays (-1).DayOfWeek.ToString ().ToLower () == eventDay) {
                        await BeginNewVote ();
                    }
                    break;
            }
        }

        public Task OnMinutePassed ( DateTime time ) {
            return Task.CompletedTask;
        }

        public Task OnSecondPassed ( DateTime time ) {
            return Task.CompletedTask;
        }

        private async Task CountVotes () {
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
            await AutomatedEventHandling.CreateEvent ("Friday Event", eventDay, highestGame.name + " has been chosen by vote!");

            SocketGuildChannel mainChannel = Program.GetMainChannel (Program.GetServer ());
            await Program.messageControl.SendMessage (mainChannel as SocketTextChannel, "The game for this fridays event has been chosen by vote: **" + highestGame.name + "**! It can be joined using command `!event join friday event`");

            List<ulong> processed = new List<ulong> ();
            foreach (Vote vote in votes) {
                if (processed.Contains (vote.voterID))
                    continue;

                if (highestGame != games[vote.votedGameID]) {
                    SocketGuildUser user = Program.GetServer ().GetUser (vote.voterID);
                    // AutomatedEventHandling seriously lacks wrapper functions.
                    await Program.messageControl.AskQuestion (user, "The friday event you voted for sadly lost to **" + highestGame.name + "** , do you want to join the event anyways?",
                        async () => {
                            await AutomatedEventHandling.JoinEvent (vote.voterID, "friday event");
                            await Program.messageControl.SendMessage (user, "You have joined the friday event succesfully!"); 
                        } );
                }else {
                    await AutomatedEventHandling.JoinEvent (vote.voterID, "friday event");
                }
                processed.Add (vote.voterID);
            }

            await UpdateVoteMessage (false);
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

            SocketGuildChannel mainChannel = Program.GetMainChannel (Program.GetServer ());
            await Program.messageControl.SendMessage (mainChannel as SocketTextChannel, "A new vote for next friday event has begun, see pinned messages in <#188106821154766848> for votesheet.");


            await SaveData();
        }

        public static async Task<bool> VoteForGame ( SocketMessage e, ulong userID, int id ) {
            List<Vote> userVotes = new List<Vote> ();

            foreach (Vote vote in votes) {
                if (vote.voterID == userID)
                    userVotes.Add (vote);
            }

            if (userVotes.Count >= votesPerPerson) {
                await Program.messageControl.SendMessage (e, "You've already voted for " + votesPerPerson + " games, you'll have to remove one to vote using `!event removevote <id>`, before you can place another.");
                return false;
            }

            foreach (Vote v in userVotes) {
                if (v.votedGameID == id) {
                    await Program.messageControl.SendMessage (e, "You've already voted for **" + games[id].name + "**. You can't vote for the same game more than once.");
                    return false;
                }
            }

            await Program.messageControl.SendMessage (e, "Succesfully voted for **" + games[id].name + "**, in the upcoming friday event.");
            votes.Add (new Vote (userID, id));
            games[id].votes++;

            await UpdateVoteMessage (false);
            await SaveData();

            return true;
        }

        public static async Task<bool> RemoveVote (SocketMessage e, ulong userID, int gameID) {
            Vote vote = votes.Find (x => x.voterID == userID && x.votedGameID == gameID);
            if (vote == null) {
                await Program.messageControl.SendMessage (e,"Failed to remove vote, you haven't voted for **" + games[gameID].name + "**");
                return false;
            }else {
                votes.Remove (vote);
                await Program.messageControl.SendMessage (e, "Succesfully removed vote from **" + games[gameID].name + "**.");
            }

            games[gameID].votes--;
            await UpdateVoteMessage (false);
            await SaveData();
            return true;
        }

        public static async Task UpdateVoteMessage ( bool forceNew ) {
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

            SocketGuildChannel channel = Program.SearchChannel (Program.GetServer (), "announcements");
            SocketUserMessage message = (channel as SocketTextChannel).GetMessageAsync (votingMessageID).Result as SocketUserMessage;

            /*if (message == null || forceNew || votingMessageID == 0) {
                Task<Content> task = Program.messageControl.AsyncSend (channel, text);
                await task;

                Console.WriteLine (task.Result.Id);
                votingMessageID = task.Result.Id;
            } else {*/
            await message.ModifyAsync (delegate ( MessageProperties properties ) { properties.Content = text; });
            //}
        }

        [Serializable]
        public class Game {
            public string name;
            public int votes;

            public Game ( string gameName ) {
                name = gameName;
            }
        }

        [Serializable]
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

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            await base.ExecuteCommand (e, arguments);
            if (await AllowExecution (e, arguments)) {
                if (AutomatedWeeklyEvent.status == AutomatedWeeklyEvent.WeeklyEventStatus.Voting) {
                    int parse;
                    if (int.TryParse (arguments[0], out parse)) {
                        bool withinRange = parse > 0 && parse <= AutomatedWeeklyEvent.games.Length;

                        if (withinRange) {
                            await AutomatedWeeklyEvent.VoteForGame (e, e.Author.Id, parse - 1);
                        } else {
                            await Program.messageControl.SendMessage (e, "Failed to vote - outside range ( 1-" + (AutomatedWeeklyEvent.games.Length) + " ).");
                        }
                    } else {
                        await Program.messageControl.SendMessage (e, "Failed to vote - could not parse vote.");
                    }
                } else {
                    await Program.messageControl.SendMessage (e, "Failed to vote - voting not in progress.");
                }
            }
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

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            await base.ExecuteCommand (e, arguments);
            if (await AllowExecution (e, arguments)) {
                if (AutomatedWeeklyEvent.status == AutomatedWeeklyEvent.WeeklyEventStatus.Voting) {
                    int parse;
                    if (int.TryParse (arguments[0], out parse)) {
                        bool withinRange = parse > 0 && parse <= AutomatedWeeklyEvent.games.Length;

                        if (withinRange) {
                            await AutomatedWeeklyEvent.RemoveVote (e, e.Author.Id, parse - 1);
                        } else {
                            await Program.messageControl.SendMessage (e, "Failed to remove vote - outside range ( 1-" + (AutomatedWeeklyEvent.games.Length) + " ).");
                        }
                    } else {
                        await Program.messageControl.SendMessage (e, "Failed to remove vote - could not parse vote.");
                    }
                } else {
                    await Program.messageControl.SendMessage (e, "Failed to remove vote - voting not in progress.");
                }
            }
        }
    }
}