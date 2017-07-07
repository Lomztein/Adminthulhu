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

        // Honestly I have no clue if this works properly, as it is kind of difficult to test out.
        private string[] SplitMessage (string message) {
            List<string> splitted = new List<string> ();

            int counted = 0;

            while (message.Length > 0) {

                // Give some wiggle room, to avoid any shenanagens.
                if (counted > maxCharacters - 10) {

                    int spaceSearch = counted;
                    while (message[spaceSearch] != '\n') {
                        spaceSearch--;
                    }

                    string substring = message.Substring (0, spaceSearch);
                    splitted.Add (substring);
                    message = message.Substring (spaceSearch);

                    counted = 0;
                } else if (counted >= message.Length) {

                    splitted.Add (message);
                    message = "";
                }

                counted++;
            }

            return splitted.ToArray ();
        }

        public MessageControl () {
            Program.discordClient.ReactionAdded += CheckForQuestions;
        }

        private Task CheckForQuestions(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction) {
            Question question = FindByMessageID (reaction.UserId, message.Id);
            ChatLogger.Log (reaction.User.Value.Username + " responded to a question with " + reaction.Emote.Name);
            if (question != null) {
                if (reaction.Emote.Name == "thumbsup") {
                    question.ifYes ();
                    askedUsers [ reaction.UserId ].Remove (question);
                } else if (reaction.Emote.Name == "thumbsdown") {
                    askedUsers [ reaction.UserId ].Remove (question);
                }
            }
            return Task.CompletedTask;
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

        /// <summary>
        /// Sends a message lol.
        /// </summary>
        /// <param name="Content event arguments"></param>
        /// <param name="message"></param>
        /// <returns The same message as is input, for laziness></returns>
        public string SendMessage(SocketMessage e, string message, bool allowInMain) {
            return SendMessage (e.Channel, message, allowInMain);
        }

        public string SendMessage ( ISocketMessageChannel e, string message, bool allowInMain) {
            string[] messages = SplitMessage (message);
            //messages.Add(new MessageTimer(e, message, 5));
            for (int i = 0; i < messages.Length; i++) {
                AsyncSend (e, messages[i], allowInMain);
            }

            return message;
        }

        public async Task<IUserMessage> SendMessage (SocketGuildUser e, string message) {
            IUserMessage finalMessage = null;

            string[] split = SplitMessage (message);

            Task<IDMChannel> channel = e.GetOrCreateDMChannelAsync ();
            IDMChannel result = await channel;

            for (int i = 0; i < split.Length; i++) {
                finalMessage = await result.SendMessageAsync (split[i]);
            }
            return finalMessage;
        }

        public async Task<RestUserMessage> AsyncSend (ISocketMessageChannel e, string message, bool allowInMain) {
            ChatLogger.Log ("Sending a message.");
            if (!allowInMain && e.Name == Program.mainTextChannelName) {
                return null;
            } else if (message.Length > 0) {
                Task<RestUserMessage> messageTask = e.SendMessageAsync (message);
                await messageTask;
                return messageTask.Result;
            }
            return null;
        }

        public async Task SendImage(SocketTextChannel e, string message, string imagePath, bool allowInMain) {
            ChatLogger.Log ("Sending an image!");
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

        public async Task AskQuestion (SocketGuildUser user, string question, Action ifYes) {
            IUserMessage message = await SendMessage (user, question);

            await (message as RestUserMessage).AddReactionAsync (new Emoji ("👍"));
            await (message as RestUserMessage).AddReactionAsync (new Emoji ("👎"));

            if (!askedUsers.ContainsKey (user.Id)) {
                askedUsers.Add (user.Id, new List<Question> ());
            }

            Question newQuestion = new Question (message.Id, ifYes);
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

        public class Question {

            public ulong messageID;
            public Action ifYes;

            public Question(ulong messageID, Action ifYes) {
                this.messageID = messageID;
                this.ifYes = ifYes;
            }

        }
    }
}
