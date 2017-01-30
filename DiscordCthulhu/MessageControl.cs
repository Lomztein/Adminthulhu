using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;

namespace DiscordCthulhu
{
    class MessageTimer
    {
        public string message { get; }
        private Timer timer;
        MessageEventArgs e;

        public MessageTimer(MessageEventArgs e, string message, int delay)
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
            await this.e.Channel.SendMessage(message);
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

        public void RemoveMessageTimer(MessageTimer messageTimer)
        {
            messageTimer.StopTimer();
            messages.Remove(messageTimer);
        }

        /// <summary>
        /// Sends a message lol.
        /// </summary>
        /// <param name="Message event arguments"></param>
        /// <param name="message"></param>
        /// <returns The same message as is input, for laziness></returns>
        public string SendMessage(MessageEventArgs e, string message) {
            return SendMessage (e.Channel, message);
        }

        public string SendMessage (Channel e, string message) {
            string[] messages = SplitMessage (message);
            //messages.Add(new MessageTimer(e, message, 5));
            for (int i = 0; i < messages.Length; i++) {
                AsyncSend (e, messages[i]);
            }

            return message;
        }

        public string SendMessage (User e, string message) {
            string[] split = SplitMessage (message);
            for (int i = 0; i < split.Length; i++) {
                e.SendMessage (split[i]);
            }
            return message;
        }

        public async Task<Message> AsyncSend (Channel e, string message) {
            ChatLogger.Log ("Sending a message.");
            if (message.Length > 0) {
                Task<Message> messageTask = e.SendMessage (message);
                await messageTask;
                return messageTask.Result;
            }
            return null;
        }

        public async void SendImage (Channel e, string message, string imagePath) {
            ChatLogger.Log ("Sending an image!");
            await e.SendMessage (message);
            try {
                await e.SendFile (imagePath);
            } catch (Discord.Net.HttpException exception) {
                await e.SendMessage ("Access denied! - " + exception.Message);
            }
        }
    }
}
