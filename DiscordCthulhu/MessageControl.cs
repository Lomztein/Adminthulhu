using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Rest;

namespace DiscordCthulhu
{
    class MessageTimer
    {
        public string message { get; }
        private Timer timer;
        SocketMessage e;

        public MessageTimer(SocketMessage e, string message, int delay)
        {
            this.message = message;
            this.e = e;
            timer = new Timer(delay * 1000);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ChatLogger.Log("Send message timer: "  + message);
            await this.e.Channel.SendMessageAsync(message);
        }

        public void StopTimer()
        {
            timer.Stop();
        }
    }

    class MessageControl
    {
        public List<MessageTimer> messages = new List<MessageTimer>();
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
            Program.discordClient.MessageReceived += CheckForQuestions;
        }

        private Task CheckForQuestions ( SocketMessage e ) {
            if (askedUsers.ContainsKey (e.Author.Id)) {
                bool didAnswer = false;
                if (e.Content == "y") {
                    askedUsers[e.Author.Id][0] ();
                    didAnswer = true;
                }else if (e.Content == "n") {
                    didAnswer = true;
                }
                if (didAnswer) {
                    askedUsers[e.Author.Id].Remove (askedUsers[e.Author.Id][0]);
                    if (askedUsers[e.Author.Id].Count == 0) {
                        askedUsers.Remove (e.Author.Id);
                    }
                }
            }

            return Task.CompletedTask;
        }

        public void RemoveMessageTimer(MessageTimer messageTimer)
        {
            messageTimer.StopTimer();
            messages.Remove(messageTimer);
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

        public async void SendMessage (SocketGuildUser e, string message) {
            string[] split = SplitMessage (message);

            Task<RestDMChannel> channel = e.CreateDMChannelAsync ();
            RestDMChannel result = await channel;

            for (int i = 0; i < split.Length; i++) {
                await result.SendMessageAsync (split[i]);
            }
        }

        public async void AsyncSend (ISocketMessageChannel e, string message) {
            ChatLogger.Log ("Sending a message.");
            if (message.Length > 0) {
                Task<RestUserMessage> messageTask = e.SendMessageAsync (message);
                await messageTask;
            }
        }

        public async void SendImage (SocketTextChannel e, string message, string imagePath) {
            ChatLogger.Log ("Sending an image!");
            try {
                await e.SendFileAsync (imagePath, message);
            } catch (Discord.Net.HttpException exception) {
                await e.SendMessageAsync ("Access denied! - " + exception.Message);
            }
        }

        public Dictionary<ulong, List<Action>> askedUsers = new Dictionary<ulong, List<Action>>();

        public async void AskQuestion (SocketGuildUser user, string question, Action ifYes) {
            SendMessage (user, question + " (y/n)");

            if (!askedUsers.ContainsKey (user.Id)) {
                askedUsers.Add (user.Id, new List<Action> ());
            }
            askedUsers[user.Id].Add (ifYes);

            await Task.Delay (30 * 1000);

            if (askedUsers.ContainsKey (user.Id)) {
                askedUsers[user.Id].Remove (ifYes);
                if (askedUsers[user.Id].Count == 0) {
                    askedUsers.Remove (user.Id);
                    SendMessage (user, "Question timed out after 30 seconds.");
                }
            }
        }
    }
}
