using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Rest;
using System.IO;

namespace Adminthulhu {

    public class MessageControl {

        public static int maxCharacters = 2000;
        public Dictionary<ulong, BookMessage> bookMessages = new Dictionary<ulong, BookMessage> ();
        private static string [ ] defaultPollUnicodeEmojis = new string [ ] { "1⃣", "2⃣", "3⃣", "4⃣", "5⃣", "6⃣", "7⃣", "8⃣", "9⃣", "🔟" };

        // Honestly I have no clue if this works properly, as it is kind of difficult to test out. Sorrounder is placed on the start and end of the message.
        private string [ ] SplitMessage(string message, string sorrounder) {
            List<string> splitted = new List<string> ();

            int counted = 0;
            while (message.Length > 0) {

                // Give some wiggle room, to avoid any shenanagens.
                int margin = 10 + sorrounder.Length * 2;
                if (counted > maxCharacters - margin) {

                    int spaceSearch = counted; // First, try newlines.
                    while (message [ spaceSearch ] != '\n' && spaceSearch > 0) {
                        spaceSearch--;
                    }

                    if (spaceSearch == 0) { // No newlines were found, try spaces instead.
                        spaceSearch = counted;
                        while (message [ spaceSearch ] != ' ' && spaceSearch > 0) {
                            spaceSearch--;
                        }
                    }

                    if (spaceSearch == 0) // No spaces found? Jeez, just cut of as late as possible then.
                        spaceSearch = counted;

                    string substring = message.Substring (0, spaceSearch);
                    splitted.Add (sorrounder + substring + sorrounder);
                    message = message.Substring (spaceSearch);

                    counted = 0;
                } else if (counted >= message.Length) {

                    splitted.Add (sorrounder + message + sorrounder);
                    message = "";
                }

                counted++;
            }

            return splitted.ToArray ();
        }

        public MessageControl() {
            Program.discordClient.ReactionAdded += OnReactionAdded;
            Program.discordClient.ReactionRemoved += OnReactionRemoved;
            Program.discordClient.MessageReceived += OnMessageRecieved;
        }

        private Task OnReactionRemoved(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction) {
            OnPollReactionChanged (message, reaction, false);
            return Task.CompletedTask;
        }

        public async void ConstructBookMessage(RestUserMessage message, string header, string [ ] pages) {
            bookMessages.Add (message.Id, new BookMessage (message.Channel.Id, message.Id, header, pages));
            await message.AddReactionAsync (new Emoji ("⬅"));
            await message.AddReactionAsync (new Emoji ("➡"));
            bookMessages [ message.Id ].TurnPage (0);
        }

        public async Task<RestUserMessage> SendBookMessage(ISocketMessageChannel channel, string header, string content, bool allowInMain, string sorrounder = "```") {
            return await SendBookMessage (channel, header, SplitMessage (content, sorrounder), allowInMain);
        }

        public async Task<RestUserMessage> SendBookMessage(ISocketMessageChannel channel, string header, string [ ] contents, bool allowInMain) {
            RestUserMessage message = await AsyncSend (channel, contents[0], allowInMain);
            if (message != null)
                ConstructBookMessage (message, header, contents);
            return message;
        }

        private async Task OnReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction) {
            if (reaction.User.Value.IsBot)
                return;

            Question question = FindByMessageID (channel.Id, message.Id);
            if (question == null)
                question = FindByMessageID (reaction.UserId, message.Id);

            if (question != null) {
                bool doRemove = false;
                if (reaction.Emote.Name == "👍") {
                    question.ifYes?.Invoke ();
                    doRemove = true;
                } else if (reaction.Emote.Name == "👎") {
                    question.ifNo?.Invoke ();
                    doRemove = true;
                }

                if (doRemove) {
                    if (question.type == Question.Type.User) {
                        currentQuestions [ message.Id ].Remove (question);
                    } else if (question.type == Question.Type.Channel) {
                        currentQuestions [ channel.Id ].Remove (question);
                    }
                }
            }

            if (bookMessages.ContainsKey (message.Id)) {
                IMessage iMessage = await bookMessages [ message.Id ].GetMessage ();
                if (reaction.Emote.Name == "⬅") {
                    bookMessages [ message.Id ].TurnPage (-1);
                    await (iMessage as RestUserMessage).RemoveReactionAsync (new Emoji ("⬅"), reaction.User.Value);
                } else if (reaction.Emote.Name == "➡") {
                    bookMessages [ message.Id ].TurnPage (1);
                    await (iMessage as RestUserMessage).RemoveReactionAsync (new Emoji ("➡"), reaction.User.Value);
                }
            }

            OnPollReactionChanged (message, reaction, true);
        }

        public async void OnPollReactionChanged (Cacheable<IUserMessage, ulong> message, SocketReaction reaction, bool add) {
            Poll reactedPoll = currentPolls.Find (x => x.messageID == message.Id);
            if (reactedPoll != null) {
                int reactionID = -1;
                for (int i = 0; i < reactedPoll.options.Count; i++) {
                    if (GetUnicodeEmoji (i) == reaction.Emote.Name) {
                        reactionID = i;
                        break;
                    }
                }

                if (reactionID != -1) {
                    if (add) {
                        if (!reactedPoll.DoVote (reaction.User.Value.Id, reactionID)) {
                            if (message.HasValue) {
                                await message.Value.RemoveReactionAsync (reaction.Emote, reaction.User.Value);
                            }
                        }
                    } else {
                        reactedPoll.RemoveVote (reaction.UserId, reactionID);
                    }
                } else {
                    if (message.HasValue) {
                        await message.Value.RemoveReactionAsync (reaction.Emote, reaction.User.Value);
                    }
                }
            }
        }

        public async Task OnMessageRecieved(SocketMessage e) {
            if (questionnarireMessageQueue.ContainsKey (e.Author.Id))
                questionnarireMessageQueue[e.Author.Id].Enqueue (e.Content);
            return;
        }

        public Question FindByMessageID(ulong userID, ulong messageID) {
            if (currentQuestions.ContainsKey (userID)) {
                foreach (Question question in currentQuestions [ userID ]) {
                    if (question.messageID == messageID)
                        return question;
                }
            }
            return null;
        }

        public void SendMessage(SocketMessage e, string message, bool allowInMain) {
            SendMessage (e.Channel, message, allowInMain);
        }

        public async void SendMessage ( ISocketMessageChannel e, string message, bool allowInMain, string splitSorrounder = "") {
            if (message == null || message.Length == 0)
                return;

            string[] messages = SplitMessage (message, splitSorrounder);
            Task<RestUserMessage> rMessage = AsyncSend (e, messages[0], allowInMain);
            await rMessage;

            if (messages.Length > 1) // Might just want to put this check into the function istead.
                ConstructBookMessage (rMessage.Result, "", messages);
        }

        public async Task<IUserMessage> SendMessage (SocketGuildUser e, string message, string splitSorrounder = "") {
            IUserMessage finalMessage = null;

            try {
                string [ ] split = SplitMessage (message, splitSorrounder);

                Task<IDMChannel> channel = e.GetOrCreateDMChannelAsync ();
                IDMChannel result = await channel;

                finalMessage = await result.SendMessageAsync (split[0]);
                if (split.Length > 1)
                    ConstructBookMessage (finalMessage as RestUserMessage, "", split);

            } catch (Exception exception) {
                Logging.Log (Logging.LogType.EXCEPTION, "Failed to send message: " + exception.Message + " - " + exception.StackTrace);
            }
            return finalMessage;
        }

        public async Task<IUserMessage> SendEmbed(ISocketMessageChannel channel, Embed embed, string text = "") {
            return await channel.SendMessageAsync (text, false, embed);
        }

        public async Task<RestUserMessage> AsyncSend(ISocketMessageChannel e, string message, bool allowInMain) {
            Logging.Log (Logging.LogType.BOT, "Sending a message.");
            try {

                if (!allowInMain && e.Name == Program.mainTextChannelName) {
                    return null;
                } else if (message != null && message.Length > 0) {
                    Task<RestUserMessage> messageTask = e.SendMessageAsync (message);
                    await messageTask;
                    return messageTask.Result;
                }
            } catch (Exception exception) {
                Logging.Log (Logging.LogType.EXCEPTION, "Failed to send message: " + exception.Message + " - " + exception.StackTrace);
            }
            return null;
        }

        public async Task SendImage(SocketTextChannel e, string message, Stream stream, string filename, bool allowInMain) {
            Logging.Log (Logging.LogType.BOT, "Sending an image!");
            if (!allowInMain && e.Name == Program.mainTextChannelName) {
                return;
            } else {
                try {
                    await e.SendFileAsync (stream, filename, message);
                } catch (Exception exc) {
                    await e.SendMessageAsync ("Error - " + exc.Message);
                }
            }
        }

        public async Task SendImage(SocketTextChannel e, string message, string filePath, bool allowInMain) {
            using (StreamReader stream = new StreamReader (filePath)) {
                SendImage (e, message, stream.BaseStream, Path.GetFileName (filePath), allowInMain);
            }
        }

        private Dictionary<ulong, List<Question>> currentQuestions = new Dictionary<ulong, List<Question>>();

        public async Task AskQuestion (ulong id, string question, Action ifYes, Action ifNo = null) {
            SocketGuildUser possibleUser = Utility.GetServer ().GetUser (id);
            SocketChannel possibleChannel = Utility.GetServer ().GetChannel (id);

            IUserMessage message = null;

            if (possibleUser != null)
                message = await SendMessage (possibleUser, question);
            if (possibleChannel != null)
                message = await AsyncSend (possibleChannel as ISocketMessageChannel, question, true);

            await (message as RestUserMessage).AddReactionAsync (new Emoji ("👍"));
            await (message as RestUserMessage).AddReactionAsync (new Emoji ("👎"));

            if (!currentQuestions.ContainsKey (id)) {
                currentQuestions.Add (id, new List<Question> ());
            }

            Question newQuestion = new Question (message.Id, ifYes, ifNo);
            if (possibleChannel != null)
                newQuestion.type = Question.Type.Channel;
            else
                newQuestion.type = Question.Type.User;

            currentQuestions [ id ].Add (newQuestion);

            await Task.Delay (24 * 60 * 60 * 1000);

            if (currentQuestions.ContainsKey (id)) {
                if (currentQuestions [ id ].Contains (newQuestion)) {
                    currentQuestions [ id ].Remove (newQuestion);

                    if (possibleUser != null)
                        await SendMessage (possibleUser, "Question timed out after 24 hours.");
                    if (possibleChannel != null)
                        await AsyncSend (possibleChannel as ISocketMessageChannel, "Question timed out after 24 hours.", true);

                }
                if (currentQuestions[ id ].Count == 0) {
                    currentQuestions.Remove (id);
                }
            }
        }

        /// <summary>
        /// QuestionnaireElement shortened to QE to save space when using the CreateQuestionnaire function.
        /// </summary>
        public class QE {
            public string name;
            public Type type;

            public QE(string _name, Type _type) {
                name = _name;
                type = _type;
            }
        }

        // This might be incredibly fancy, or might not work. We'll see!
        private static Dictionary<ulong, Queue<string>> questionnarireMessageQueue = new Dictionary<ulong, Queue<string>>();
        public static async Task<List<object>> CreateQuestionnaire(ulong userID, ISocketMessageChannel channel, params QE [] elements) {
            if (questionnarireMessageQueue.ContainsKey (userID)) {
                throw new Exception ("User attempted a new questionnaire while they had one ongoing.");
            } else {
                questionnarireMessageQueue.Add (userID, new Queue<string> ());
            }

            List<object> result = new List<object> ();
            foreach (QE e in elements) {
                while (true) {
                    if (e.type.IsArray) {
                        List<object> arrayResult = new List<object> ();
                        while (true) {
                            Program.messageControl.SendMessage (channel, e.name.Replace ("#", "#" + (arrayResult.Count + 1)), false);
                            while (questionnarireMessageQueue [ userID ].Count == 0) {
                                await Task.Delay (100);
                            }

                            string input = questionnarireMessageQueue [ userID ].Dequeue ();
                            if (input == "end") {
                                break;
                            }
                            try {
                                arrayResult.Add (Convert.ChangeType (input, e.type.GetElementType ()));
                            } catch (Exception exception) {
                                Program.messageControl.SendMessage (channel, exception.Message, false);
                            }
                        }
                        result.Add (arrayResult.ToArray ());
                        break;
                    } else {
                        Program.messageControl.SendMessage (channel, e.name, false);
                        while (questionnarireMessageQueue [ userID ].Count == 0) {
                            await Task.Delay (100);
                        }
                        string input = questionnarireMessageQueue [ userID ].Dequeue ();
                        if (input == "cancel") {
                            questionnarireMessageQueue.Remove (userID);
                            throw new TimeoutException ("Questionnaire was cancelled.");
                        }

                        try {
                            result.Add (Convert.ChangeType (input, e.type));
                            break;
                        } catch (Exception exception) {
                            Program.messageControl.SendMessage (channel, exception.Message, false);
                        }
                    }
                }
            }

            questionnarireMessageQueue.Remove (userID);
            return result;
        }

        public static async Task<IMessage> GetMessage(ulong channelID, ulong messageID) {
            SocketTextChannel channel = Utility.GetServer ().GetChannel (channelID) as SocketTextChannel;
            if (channel != null) {
                return await channel.GetMessageAsync (messageID);
            }
            SocketUser user = Utility.GetServer ().GetUser (messageID);
            if (user != null) {
                IDMChannel userChannel = await user.GetOrCreateDMChannelAsync ();
                return await userChannel.GetMessageAsync (messageID);
            }

            return null;
        }

    private static string GetUnicodeEmoji(int index) {
        return defaultPollUnicodeEmojis [ index ];
    }

    public static async Task<IMessage> CreatePoll(Poll poll) {
            currentPolls.Add (poll);
            await poll.UpdateMessage ();
            return await poll.GetMessage ();
        }

        public class PollClock : IClockable {
            public Task Initialize(DateTime time) {
                return Task.CompletedTask;
            }

            public Task OnDayPassed(DateTime time) {
                return Task.CompletedTask;
            }

            public Task OnHourPassed(DateTime time) {
                return Task.CompletedTask;
            }

            public Task OnMinutePassed(DateTime time) {
                List<Poll> toRemove = new List<Poll> ();
                foreach (Poll poll in currentPolls) {
                    if (poll.endDate < time) {
                        poll.DeclareWinner ();
                        toRemove.Add (poll);
                    }
                }
                foreach (Poll r in toRemove) {
                    currentPolls.Remove (r);
                }
                return Task.CompletedTask;
            }

            public Task OnSecondPassed(DateTime time) {
                return Task.CompletedTask;
            }
        }

        public static List<Poll> currentPolls = new List<Poll> ();
        public class Poll {
            public string header;
            public ulong channelID;
            public ulong messageID;

            public DateTime endDate;
            public int votesPerPerson = 1;

            public List<PollOption> options = new List<PollOption>();
            public List<Vote> votes = new List<Vote>();

            public Action<Poll> onEnded;
            public PollOption winner;

            public Poll(string _header, ulong _channelID, ulong _messageID, DateTime _endDate, int _votesPerPerson, Action<Poll> _onEnded, params string [ ] pollOptions) {
                header = _header;
                channelID = _channelID;
                messageID = _messageID;
                endDate = _endDate;
                votesPerPerson = _votesPerPerson;
                onEnded = _onEnded;

                foreach (string str in pollOptions) {
                    options.Add (new PollOption (str, this));
                }
            }

            public async Task AwaitEnd() {
                int testRepeatTime = 1000 * 60; // Once a minute. Can't think of other ways to do this at the moment.
                while (DateTime.Now < endDate) {
                    Task.Delay (testRepeatTime);
                }
                return;
            }

            public async Task UpdateMessage() {
                string contents = header + "\n```";
                int index = 0;
                foreach (PollOption option in options) {
                    contents += Utility.UniformStrings ((index + 1) + " - " + option.name, option.CountVotes ().ToString () + " votes.\n", " - ");
                    index++;
                }

                contents += "```\n";
                if (endDate < DateTime.Now) {
                    contents += "**VOTING HAS ENDED, " + winner.name.ToUpper () + " HAS WON THE VOTE.**";
                } else {
                    contents += "**Vote using the reactions below. You can vote " + votesPerPerson + " times!**";
                }

                if (messageID == 0) {
                    SocketTextChannel channel = Utility.GetServer ().GetChannel (channelID) as SocketTextChannel;
                    RestUserMessage message = await Program.messageControl.AsyncSend (channel, contents, false);
                    messageID = message.Id;
                    for (int i = 0; i < options.Count; i++) {
                        await message.AddReactionAsync (new Emoji (GetUnicodeEmoji (i)));
                    }
                } else {
                    IMessage message = await GetMessage ();
                    await (message as RestUserMessage).ModifyAsync (delegate (MessageProperties properties) {
                        properties.Content = contents;
                    });
                }
            }

            public void DeclareWinner() {
                PollOption curHighest;
                int highestNumber = 0;
                foreach (PollOption option in options) {
                    int locCount = option.CountVotes ();
                    if (locCount > highestNumber) {
                        highestNumber = locCount;
                        curHighest = option;
                    }
                }
                onEnded?.Invoke (this);
            }

            public async Task<IMessage> GetMessage() {
                return await MessageControl.GetMessage (channelID, messageID);
            }

            public class PollOption {
                public string name;
                private Poll parent;

                public PollOption(string _name, Poll _parent) {
                    name = _name;
                    parent = _parent;
                }

                public int CountVotes() {
                    int result = 0;
                    foreach (Vote v in parent.votes) {
                        if (v.optionIndex == parent.options.IndexOf (this)) {
                            result++;
                        }
                    }
                    return result;
                }
            }

            public int GetUserVotes (ulong userID) {
                return votes.Where (x => x.voterID == userID).Count ();
            }

            public class Vote {
                public ulong voterID;
                public int optionIndex;

                public Vote(ulong _voterID, int _index) {
                    voterID = _voterID;
                    optionIndex = _index;
                }
            }

            public bool DoVote (ulong userID, int optionIndex) {
                if (GetUserVotes (userID) <= votesPerPerson) {
                    votes.Add (new Vote (userID, optionIndex));
                    UpdateMessage ();
                    return true;
                }
                return false;
            }

            public bool RemoveVote(ulong userID, int optionIndex) {
                if (GetUserVotes (userID) != 0) {
                    Vote vote = votes.Find (x => x.voterID == userID && x.optionIndex == optionIndex);
                    if (vote != null) {
                        votes.Remove (vote);
                        UpdateMessage ();
                        return true;
                    }
                }
                return false;
            }
        }

        public class Question {

            public enum Type {
                User, Channel,
            }
            public Type type;
            public ulong messageID;
            public Action ifYes;
            public Action ifNo;

            public Question(ulong messageID, Action ifYes, Action ifNo) {
                this.messageID = messageID;
                this.ifYes = ifYes;
                this.ifNo = ifNo;
            }

        }

        public class BookMessage {
            ulong channelID;
            ulong messageID;

            int currentPage;
            string header = "";
            string[] content;

            public BookMessage(ulong _channelID, ulong _messageID, string _header, string[] _content) {
                channelID = _channelID;
                messageID = _messageID;
                header = _header;
                content = _content;
            }

            public async void TurnPage(int pagesToTurn) {
                currentPage = (currentPage + pagesToTurn) % content.Length;
                if (currentPage < 0)
                    currentPage = content.Length - 1;
                IMessage message = await GetMessage ();
                try {
                    await (message as RestUserMessage).ModifyAsync (delegate (MessageProperties properties) {
                        properties.Content = "Page " + (currentPage + 1) + "/" + content.Length + "\n" + content [ currentPage ];
                    });
                }catch (Exception e) {
                    Logging.Log (Logging.LogType.EXCEPTION, e.Message);
                }
            }

            public async Task<IMessage> GetMessage() {
                return await MessageControl.GetMessage (channelID, messageID);
            }
        }
    }
}
