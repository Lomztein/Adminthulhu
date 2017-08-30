using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;

namespace Adminthulhu {
    public class Karma : IConfigurable {

        public static Dictionary<ulong, long> karmaCollection;
        public static string karmaFileName = "karmaCollection";

        public static string upvote = "upvote";
        public static string downvote = "downvote";

        public Karma() {
            LoadConfiguration ();
            BotConfiguration.AddConfigurable (this);
            karmaCollection = SerializationIO.LoadObjectFromFile<Dictionary<ulong, long>> (Program.dataPath + karmaFileName + Program.gitHubIgnoreType);
            if (karmaCollection == null)
                karmaCollection = new Dictionary<ulong, long> ();

            Program.discordClient.ReactionAdded += async (message, channel, reaction) => {

                IMessage iMessage = await channel.GetMessageAsync (message.Id);

                if (reaction.Emote.Name == upvote) {
                    ChangeKarma (iMessage.Author.Id, reaction.UserId, 1);
                } else if (reaction.Emote.Name == downvote) {
                    ChangeKarma (iMessage.Author.Id, reaction.UserId, -1);
                }
            };

            Program.discordClient.ReactionRemoved += async (message, channel, reaction) => {

                IMessage iMessage = await channel.GetMessageAsync (message.Id);

                if (reaction.Emote.Name == upvote) {
                    ChangeKarma (iMessage.Author.Id, reaction.UserId, -1);
                } else if (reaction.Emote.Name == downvote) {
                    ChangeKarma (iMessage.Author.Id, reaction.UserId, 1);
                }
            };
        }

        public static void ChangeKarma (ulong userID, ulong giver, int change) {
            if (giver == userID)
                return;

            if (!karmaCollection.ContainsKey (userID))
                karmaCollection.Add (userID, 0);
            karmaCollection[userID] += change;

            SerializationIO.SaveObjectToFile (Program.dataPath + karmaFileName + Program.gitHubIgnoreType, karmaCollection);
        }

        public static long GetKarma (ulong userID) {
            return karmaCollection.ContainsKey (userID) ? karmaCollection[userID] : 0;
        }

        public void LoadConfiguration() {
            upvote = BotConfiguration.GetSetting("Karma.UpvoteEmojiName", "UpvoteEmojiName", "upvote");
            downvote = BotConfiguration.GetSetting("Karma.DownvoteEmojiName", "DownvoteEmojiName", "downvote");
        }
    }

    public class CKarma : Command {
        public CKarma() {
            command = "karma";
            shortHelp = "Show karma.";
            catagory = Category.Fun;

            AddOverload (typeof (long), "Shows your own karma.");
            AddOverload (typeof (long), "Shows karma of user given by name.");
        }

        public Task<Result> Execute(SocketUserMessage e) {
            return Execute (e, Utility.GetUserName (e.Author as SocketGuildUser));
        }

        public Task<Result> Execute(SocketUserMessage e, string name) {
            long karmaCount = 0;

            SocketGuildUser user = e.Author as SocketGuildUser;

            user = Utility.FindUserByName ((e.Channel as SocketGuildChannel).Guild, name);
            if (user == null) {
                return TaskResult (0, "User " + name + " not found.");
            }

            karmaCount = Karma.GetKarma (user.Id);
            return TaskResult (karmaCount, "User " + Utility.GetUserName (user) + " currently has " + karmaCount + " karma.");
        }
    }
}
