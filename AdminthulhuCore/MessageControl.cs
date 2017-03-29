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
            Console.WriteLine (reaction.Emoji.Name);
            if (askedUsers.ContainsKey (reaction.UserId) && channel as SocketDMChannel != null) {
                bool didAnswer = false;
                if (reaction.Emoji.Name[0] == "­👍"[1]) {
                    askedUsers [ reaction.UserId ] [ 0 ] ();
                    didAnswer = true;
                } else if (reaction.Emoji.Name[0] == "👎"[1]) {
                    didAnswer = true;
                }
                if (didAnswer) {
                    askedUsers [reaction.UserId].Remove (askedUsers [ reaction.UserId ] [ 0 ]);
                    if (askedUsers [ reaction.UserId ].Count == 0) {
                        askedUsers.Remove (reaction.UserId);
                    }
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Sends a message lol.
        /// </summary>
        /// <param name="Content event arguments"></param>
        /// <param name="message"></param>
        /// <returns The same message as is input, for laziness></returns>
        public string SendMessage(SocketMessage e, string message) {
            return SendMessage (e.Channel, message);
        }

        public string SendMessage ( ISocketMessageChannel e, string message ) {
            string[] messages = SplitMessage (message);
            //messages.Add(new MessageTimer(e, message, 5));
            for (int i = 0; i < messages.Length; i++) {
                AsyncSend (e, messages[i]);
            }

            return message;
        }

        public async Task<RestMessage> SendMessage (SocketGuildUser e, string message) {
            RestMessage finalMessage = null;

            string[] split = SplitMessage (message);

            Task<RestDMChannel> channel = e.CreateDMChannelAsync ();
            RestDMChannel result = await channel;

            for (int i = 0; i < split.Length; i++) {
                finalMessage = await result.SendMessageAsync (split[i]);
            }
            return finalMessage;
        }

        public async Task<RestUserMessage> AsyncSend (ISocketMessageChannel e, string message) {
            ChatLogger.Log ("Sending a message.");
            if (message.Length > 0) {
                Task<RestUserMessage> messageTask = e.SendMessageAsync (message);
                await messageTask;
                return messageTask.Result;
            }
            return null;
        }

        public async Task SendImage (SocketTextChannel e, string message, string imagePath) {
            ChatLogger.Log ("Sending an image!");
            try {
                await e.SendFileAsync (imagePath, message);
            } catch (Discord.Net.HttpException exception) {
                await e.SendMessageAsync ("Access denied! - " + exception.Message);
            }
        }

        public Dictionary<ulong, List<Action>> askedUsers = new Dictionary<ulong, List<Action>>();

        public async Task AskQuestion (SocketGuildUser user, string question, Action ifYes) {
            RestMessage message = await SendMessage (user, question);

                await (message as RestUserMessage).AddReactionAsync ("👍");
                await (message as RestUserMessage).AddReactionAsync ("­:thumbsdown:");

            if (!askedUsers.ContainsKey (user.Id)) {
                askedUsers.Add (user.Id, new List<Action> ());
            }

            askedUsers[user.Id].Add (ifYes);

            await Task.Delay (5 * 60 * 1000);

            if (askedUsers.ContainsKey (user.Id)) {
                askedUsers[user.Id].Remove (ifYes);
                if (askedUsers[user.Id].Count == 0) {
                    askedUsers.Remove (user.Id);
                    await SendMessage (user, "Question timed out after 5 minutes.");
                }
            }
        }
    }
}
