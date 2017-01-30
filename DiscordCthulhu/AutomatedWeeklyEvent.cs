using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordCthulhu {

    public class AutomatedWeeklyEvent : IClockable {

        public static Game[] games = new Game[] { new Game ("Overwatch"), new Game ("GMod"), new Game ("Counter Strike: GO") , new Game ("Team Fortress 2") , new Game ("Dodgeball") , new Game ("Rocket League") };
        public static List<Vote> votes;

        public static string votesFileName = "votes";

        public static string eventDay = "friday";
        public static string voteEndDay = "wednesday";
        public static int daysBetween = 2;

        public enum WeeklyEventStatus { Voting, Waiting }
        public static WeeklyEventStatus status = WeeklyEventStatus.Voting;

        public static ulong votingMessageID = 275577439600508928;

        public int eventHour = 20;

        public void Initialize ( DateTime time ) {
            votes = SerializationIO.LoadObjectFromFile<List<Vote>> (Program.dataPath + votesFileName + Program.gitHubIgnoreType);
            if (votes == null)
                votes = new List<Vote> ();

            foreach (Vote vote in votes) {
                games[vote.votedGameID].votes++;
            }
        }

        public void SaveData () {
            SerializationIO.SaveObjectToFile (Program.dataPath + votesFileName + Program.gitHubIgnoreType, votes);
        }

        public void OnDayPassed ( DateTime time ) {
        }

        public void OnHourPassed ( DateTime time ) {
            switch (status) {
                case WeeklyEventStatus.Voting:
                    if (time.AddDays (-1).DayOfWeek.ToString ().ToLower () == voteEndDay) {
                        CountVotes ();
                    }
                    break;

                case WeeklyEventStatus.Waiting:
                    if (time.AddDays (-1).DayOfWeek.ToString ().ToLower () == eventDay) {
                        BeginNewVote ();
                    }
                    break;
            }
        }

        public void OnMinutePassed ( DateTime time ) {
        }

        public void OnSecondPassed ( DateTime time ) {
        }

        private void CountVotes () {
            status = WeeklyEventStatus.Waiting;

            Game highestGame = null;
            int highestVote = int.MinValue;

            foreach (Game game in games) {
                if (game.votes > highestVote) {
                    highestGame = game;
                    highestVote = game.votes;
                }
            }

            DateTime now = DateTime.Now;
            DateTime eventDay = new DateTime (now.Year, now.Month, now.Day, eventHour, 0, 0).AddDays (daysBetween);
            AutomatedEventHandling.upcomingEvents.Add (new AutomatedEventHandling.Event ("Friday Event", eventDay, highestGame.name + " has been chosen by vote!"));
        }

        private void BeginNewVote () {
            status = WeeklyEventStatus.Voting;
            foreach (Game game in games)
                game.votes = 0;

            votes = new List<Vote> ();
            UpdateVoteMessage (true);
        }

        public static void VoteForGame ( MessageEventArgs e, ulong userID, int id ) {
            Vote vote = votes.Find (x => x.voterID == userID);

            if (vote == null) {
                vote = new Vote (userID, id);
                games[id].votes++;
                votes.Add (vote);
            } else {
                games[vote.votedGameID].votes--;
                games[id].votes++;
            }

            UpdateVoteMessage (false);
        }

        public static async void UpdateVoteMessage ( bool forceNew ) {
            string text = "Vote for this " + eventDay + "'s event!```\n";
            int index = 0;
            foreach (Game game in games) {
                text += index + " - " + game.name + " - " + game.votes.ToString () + " votes.\n";
                index++;
            }
            text += "```\n";
            if (status == WeeklyEventStatus.Waiting) {
                text += "**VOTING HAS ENDED.**";
            }else {
                text += "**Vote using `!event vote <id>` to vote!**";
            }

            Channel channel = Program.SearchChannel (Program.GetServer (), "dump");
            Message message = channel.GetMessage (votingMessageID);

            /*if (message == null || forceNew || votingMessageID == 0) {
                Task<Message> task = Program.messageControl.AsyncSend (channel, text);
                await task;

                Console.WriteLine (task.Result.Id);
                votingMessageID = task.Result.Id;
            } else {*/
                await message.Edit (text);
            //}
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

        public class CEventVote : Command {
            public CEventVote () {
                command = "vote";
                name = "Vote for Event";
                argHelp = "<id>";
                help = "Vote for the next friday event!";
                argumentNumber = 1;
            }

            public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
                base.ExecuteCommand (e, arguments);
                if (AllowExecution (e, arguments)) {
                    int parse;
                    if (int.TryParse (arguments[0], out parse)) {
                        bool withinRange = parse >= 0 && parse < games.Length;
                        if (withinRange) {
                            VoteForGame (e, e.User.Id, parse);
                            Program.messageControl.SendMessage (e, "Succesfully voted for the upcoming friday event.");
                        } else {
                            Program.messageControl.SendMessage (e, "Failed to vote - outside range ( 0-" + (games.Length - 1) + ").");
                        }
                    } else {
                        Program.messageControl.SendMessage (e, "Failed to vote - could not parse vote.");
                    }
                }
            }
        }
    }
}