using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
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
        public static bool OnMessageRecieved (MessageEventArgs message) {
            // First, add the message to the log.
            if (messageLog.ContainsKey (message.User.Id)) {
                messageLog[message.User.Id].Add (new MessageObject (message.Message.RawText));
            }else {
                messageLog.Add (message.User.Id, new List<MessageObject> ());
                messageLog[message.User.Id].Add (new MessageObject (message.Message.RawText));
            }

            // Loop through log and remove entries that are too old:
            List<MessageObject> toRemove = new List<MessageObject> ();
            foreach (MessageObject obj in messageLog[message.User.Id]) {
                if (obj.deleteTime < DateTime.Now)
                    toRemove.Add (obj);
            }

            foreach (MessageObject obj in toRemove) {
                messageLog[message.User.Id].Remove (obj);
            }

            // Go through log, check if more than defined.
            int sameMessages = 0;
            int userMessages = 0;
            foreach (MessageObject obj in messageLog[message.User.Id]) {
                userMessages++;
                if (obj.rawText == message.Message.RawText)
                    sameMessages++;
            }

            if (userMessages > maxUserMessages || sameMessages > maxSameMessages) {
                Program.messageControl.SendMessage (message.User, "Spam detected, please wait a few seconds before sending a message. This is arguably the most useless feature of this bot.");
                Program.allowedDeletedMessages.Add (message.Message.RawText);
                message.Message.Delete ();
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
