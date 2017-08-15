using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Rest;

namespace Adminthulhu {

    public class MessageControl {

        public static int maxCharacters = 2000;
        public Dictionary<ulong, BookMessage> bookMessages = new Dictionary<ulong, BookMessage>();

        // Honestly I have no clue if this works properly, as it is kind of difficult to test out. Sorrounder is placed on the start and end of the message.
        private string[] SplitMessage (string message, string sorrounder) {
            List<string> splitted = new List<string> ();

            int counted = 0;
            while (message.Length > 0) {

                // Give some wiggle room, to avoid any shenanagens.
                if (counted > maxCharacters - (10 + sorrounder.Length * 2)) {

                    int spaceSearch = counted;
                    while (message[spaceSearch] != '\n') {
                        spaceSearch--;
                    }

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

        public MessageControl () {
            Program.discordClient.ReactionAdded += OnReactionAdded;
            Program.discordClient.MessageReceived += OnMessageRecieved;
        }

        public async void ConstructBookMessage(RestUserMessage message, string[] pages) {
            bookMessages.Add (message.Id, new BookMessage (message.Channel.Id, message.Id, pages));
            await message.AddReactionAsync (new Emoji ("⬅"));
            await message.AddReactionAsync (new Emoji ("➡"));
            bookMessages [ message.Id ].TurnPage (0);
        }

        private async Task OnReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction) {
            if (reaction.User.Value.IsBot)
                return;

            Question question = FindByMessageID (reaction.UserId, message.Id);
            if (question != null) {
            Logging.Log (reaction.User.Value.Username + " responded to a question with " + reaction.Emote.Name);
                if (reaction.Emote.Name == "thumbsup") {
                    question.ifYes?.Invoke ();
                    askedUsers [ reaction.UserId ].Remove (question);
                } else if (reaction.Emote.Name == "thumbsdown") {
                    question.ifNo?.Invoke ();
                    askedUsers [ reaction.UserId ].Remove (question);
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
            return;
        }

        public async Task OnMessageRecieved(SocketMessage e) {
            if (questionnarireMessageQueue.ContainsKey (e.Author.Id))
                questionnarireMessageQueue[e.Author.Id].Enqueue (e.Content);
            return;
        }

        public Question FindByMessageID(ulong userID, ulong messageID) {
            if (askedUsers.ContainsKey (userID)) {
                foreach (Question question in askedUsers [ userID ]) {
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
            if (message.Length == 0)
                return;

            string[] messages = SplitMessage (message, splitSorrounder);
            Task<RestUserMessage> rMessage = AsyncSend (e, messages[0], allowInMain);
            await rMessage;

            if (messages.Length > 1) // Might just want to put this check into the function istead.
                ConstructBookMessage (rMessage.Result, messages);
        }

        public async Task<IUserMessage> SendMessage (SocketGuildUser e, string message, string splitSorrounder = "") {
            IUserMessage finalMessage = null;

            try {
                string [ ] split = SplitMessage (message, splitSorrounder);

                Task<IDMChannel> channel = e.GetOrCreateDMChannelAsync ();
                IDMChannel result = await channel;

                finalMessage = await result.SendMessageAsync (split[0]);
                if (split.Length > 1)
                    ConstructBookMessage (finalMessage as RestUserMessage, split);

            } catch (Exception exception) {
                Logging.Log ("Failed to send message: " + exception.Message + " - " + exception.StackTrace);
            }
            return finalMessage;
        }

        public async Task<IUserMessage> SendEmbed(ITextChannel channel, Embed embed, string text = "") {
            return await channel.SendMessageAsync (text, false, embed);
        }

        public async Task<RestUserMessage> AsyncSend(ISocketMessageChannel e, string message, bool allowInMain) {
            Logging.Log ("Sending a message.");
            try {

                if (!allowInMain && e.Name == Program.mainTextChannelName) {
                    return null;
                } else if (message.Length > 0) {
                    Task<RestUserMessage> messageTask = e.SendMessageAsync (message);
                    await messageTask;
                    return messageTask.Result;
                }
            } catch (Exception exception) {
                Logging.Log ("Failed to send message: " + exception.Message + " - " + exception.StackTrace);
            }
            return null;
        }

        public async Task SendImage(SocketTextChannel e, string message, string imagePath, bool allowInMain) {
            Logging.Log ("Sending an image!");
            if (!allowInMain && e.Name == Program.mainTextChannelName) {
                return;
            } else {
                try {
                    await e.SendFileAsync (imagePath, message);
                } catch (Discord.Net.HttpException exception) {
                    await e.SendMessageAsync ("Access denied! - " + exception.Message);
                }
            }
        }

        private Dictionary<ulong, List<Question>> askedUsers = new Dictionary<ulong, List<Question>>();

        public async Task AskQuestion (SocketGuildUser user, string question, Action ifYes, Action ifNo = null) {
            IUserMessage message = await SendMessage (user, question);

            await (message as RestUserMessage).AddReactionAsync (new Emoji ("👍"));
            await (message as RestUserMessage).AddReactionAsync (new Emoji ("👎"));

            if (!askedUsers.ContainsKey (user.Id)) {
                askedUsers.Add (user.Id, new List<Question> ());
            }

            Question newQuestion = new Question (message.Id, ifYes, ifNo);
            askedUsers [user.Id].Add (newQuestion);

            await Task.Delay (24 * 60 * 60 * 1000);

            if (askedUsers.ContainsKey (user.Id)) {
                if (askedUsers [ user.Id ].Contains (newQuestion)) {
                    askedUsers [ user.Id ].Remove (newQuestion);
                    await SendMessage (user, "Question timed out after 24 hours.");
                }
                if (askedUsers[user.Id].Count == 0) {
                    askedUsers.Remove (user.Id);
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

            questionnarireMessageQueue.Remove (userID);
            return result;
        }

        public class Question {

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
            string[] content;

            public BookMessage(ulong _channelID, ulong _messageID, string[] newContent) {
                channelID = _channelID;
                messageID = _messageID;
                content = newContent;
            }

            public async void TurnPage(int pagesToTurn) {
                currentPage = (currentPage + pagesToTurn) % content.Length;
                if (currentPage < 0)
                    currentPage = content.Length - 1;
                IMessage message = await GetMessage ();
                await (message as RestUserMessage).ModifyAsync (delegate (MessageProperties properties) {
                    properties.Content = "Page " + (currentPage + 1) + "/" + content.Length + "\n" + content [ currentPage ];
                });
            }

            public async Task<IMessage> GetMessage() {
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
        }
    }
}
