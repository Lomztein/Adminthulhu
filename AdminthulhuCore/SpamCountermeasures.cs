using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Adminthulhu {
    public static class SpamCountermeasures {

        public static int maxSameMessages = 5;
        public static int maxUserMessages = 10;
        public static int messageTime = 5;

        public static Dictionary<ulong, List<MessageObject>> messageLog = new Dictionary<ulong, List<MessageObject>>();

        /// <summary>
        /// Returns true if message isn't spam.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool OnMessageRecieved (SocketMessage message) {
            // First, add the message to the log.
            if (messageLog.ContainsKey (message.Author.Id)) {
                messageLog[message.Author.Id].Add (new MessageObject (message.Content));
            }else {
                messageLog.Add (message.Author.Id, new List<MessageObject> ());
                messageLog[message.Author.Id].Add (new MessageObject (message.Content));
            }

            // Loop through log and remove entries that are too old:
            List<MessageObject> toRemove = new List<MessageObject> ();
            foreach (MessageObject obj in messageLog[message.Author.Id]) {
                if (obj.deleteTime < DateTime.Now)
                    toRemove.Add (obj);
            }

            foreach (MessageObject obj in toRemove) {
                messageLog[message.Author.Id].Remove (obj);
            }

            // Go through log, check if more than defined.
            int sameMessages = 0;
            int userMessages = 0;
            foreach (MessageObject obj in messageLog[message.Author.Id]) {
                userMessages++;
                if (obj.rawText == message.Content)
                    sameMessages++;
            }

            if (userMessages > maxUserMessages || sameMessages > maxSameMessages) {
                Program.messageControl.SendMessage (message.Author as SocketGuildUser, "Spam detected, please wait a few seconds before sending a message. This is arguably the most useless feature of this bot.");
                Program.allowedDeletedMessages.Add (message.Content);
                message.DeleteAsync ();
                return true;
            }

            return false;
        }

        public class MessageObject {

            public DateTime deleteTime;
            public string rawText;

            public MessageObject (string text) {
                rawText = text;
                deleteTime = DateTime.Now.AddSeconds (messageTime);
            }

        }

    }
}
