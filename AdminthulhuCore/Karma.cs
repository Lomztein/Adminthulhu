using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;

namespace Adminthulhu {
    public class Karma : IConfigurable {

        public static string karmaFileName = "karma";

        public static string upvote = "upvote";
        public static string downvote = "downvote";

        public static int upvotesToQuote = 5;
        public static int downvotesToDelete = -5;

        public static Data data;

        public Karma() {
            LoadConfiguration ();
            BotConfiguration.AddConfigurable (this);
            data = SerializationIO.LoadObjectFromFile<Data> (Program.dataPath + karmaFileName + Program.gitHubIgnoreType);
            if (data == null)
                data = new Data();

            Program.discordClient.ReactionAdded += async (message, channel, reaction) => {

                IMessage iMessage = await channel.GetMessageAsync (message.Id);

                if (reaction.Emote.Name == upvote) {
                    ChangeKarma (iMessage, reaction.UserId, 1);
                } else if (reaction.Emote.Name == downvote) {
                    ChangeKarma (iMessage, reaction.UserId, -1);
                }
            };

            Program.discordClient.ReactionRemoved += async (message, channel, reaction) => {

                IMessage iMessage = await channel.GetMessageAsync (message.Id);

                if (reaction.Emote.Name == upvote) {
                    ChangeKarma (iMessage, reaction.UserId, -1);
                } else if (reaction.Emote.Name == downvote) {
                    ChangeKarma (iMessage, reaction.UserId, 1);
                }
            };
        }

        public static void ChangeKarma(IMessage message, ulong giver, int change) {
            if (giver == message.Author.Id)
                return;

            if (!data.karmaCollection.ContainsKey (message.Author.Id))
                data.karmaCollection.Add (message.Author.Id, 0);
            data.karmaCollection [ message.Author.Id ] += change;

            if (!data.trackedMessages.ContainsKey (message.Id))
                data.trackedMessages.Add (message.Id, 0);
            data.trackedMessages [ message.Id ] += change;

            if (data.trackedMessages [ message.Id ] >= upvotesToQuote && upvotesToQuote > 0) {
                CQuote.AddQuoteFromMessage (message);
            }
            if (data.trackedMessages [ message.Id ] <= downvotesToDelete && downvotesToDelete < 0) { // Could be in an else-if, but it felt wrong for some reason.
                DeleteMessage (message);
            }

            SerializationIO.SaveObjectToFile (Program.dataPath + karmaFileName + Program.gitHubIgnoreType, data);
        }

        public static async void DeleteMessage(IMessage message) {
            await message.DeleteAsync ();
            data.trackedMessages.Remove (message.Id);
            data.quotedMessages.Remove (message.Id);
        }

        public static long GetKarma (ulong userID) {
            return data.karmaCollection.ContainsKey (userID) ? data.karmaCollection [userID] : 0;
        }

        public void LoadConfiguration() {
            upvote = BotConfiguration.GetSetting("Karma.UpvoteEmojiName", this, "upvote");
            downvote = BotConfiguration.GetSetting("Karma.DownvoteEmojiName", this, "downvote");
            upvotesToQuote = BotConfiguration.GetSetting("Karma.UpvotesToQuote", this, upvotesToQuote);
            downvotesToDelete = BotConfiguration.GetSetting("Karma.DownvotesToDelete", this, downvotesToDelete);
        }

        public static string GetTopKarma(int amount, out SocketGuildUser[] topUsers) {
            var items = from pair in data.karmaCollection
                        orderby pair.Value descending
                        select pair;

            topUsers = new SocketGuildUser [ amount ];

            if (items.Count () == 0) {
                topUsers = new SocketGuildUser [ 0 ];
                return "No users has ever been given karma.";
            }

            string combined = string.Empty;
            int count = Math.Min (items.Count (), amount);
            for (int i = 0; i < count; i++) {
                var item = items.ElementAt (i);

                SocketGuildUser user = Utility.GetServer ().GetUser (item.Key);
                string part = Utility.UniformStrings ($"{(i+1)} - {Utility.GetUserName (user)}", item.Value.ToString () + " karma.", " - ");
                combined += part + "\n";

                topUsers [ i ] = user;
            }

            return combined;
        }

        // I don't particularly like this autoquoting stuff, neither the idea and especially not the execution.
        public class Data {
            public Dictionary<ulong, long> karmaCollection;
            public Dictionary<ulong, long> trackedMessages;
            public List<ulong> quotedMessages;

            public Data() {
                karmaCollection = new Dictionary<ulong, long> ();
                trackedMessages = new Dictionary<ulong, long> ();
                quotedMessages = new List<ulong> ();
            }

            public Data(Dictionary<ulong, long> _collection, Dictionary<ulong, long> _tracked, List<ulong> _quoted) {
                karmaCollection = _collection;
                trackedMessages = _tracked;
                quotedMessages = _quoted;
            }
        }
    }

    public class CKarma : Command {
        public CKarma() {
            command = "karma";
            shortHelp = "Show karma.";
            catagory = Category.Fun;

            AddOverload (typeof (long), "Shows your own karma.");
            AddOverload (typeof (SocketGuildUser[]), "Shows top karma for given amount of people.");
            AddOverload (typeof (long), "Shows karma of user given by name.");
        }

        public Task<Result> Execute(SocketUserMessage e) {
            return Execute (e, Utility.GetUserName (e.Author as SocketGuildUser));
        }

        public Task<Result> Execute(SocketUserMessage e, int amount) {
            SocketGuildUser [ ] topUsers;
            string result = Karma.GetTopKarma (amount, out topUsers);
            return TaskResult (topUsers, $"```{result}```");
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
