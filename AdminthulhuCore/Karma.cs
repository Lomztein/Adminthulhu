using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;

namespace Adminthulhu {
    public class Karma {

        public static Dictionary<ulong, long> karmaCollection;
        public static string karmaFileName = "karmaCollection";

        public static string upvote = "upvote";
        public static string downvote = "downvote";

        public Karma () {
            karmaCollection = SerializationIO.LoadObjectFromFile<Dictionary<ulong, long>> (Program.dataPath + karmaFileName + Program.gitHubIgnoreType);
            if (karmaCollection == null)
                karmaCollection = new Dictionary<ulong, long> ();

            Program.discordClient.ReactionAdded += async ( message, channel, reaction ) => {

                IMessage iMessage = await channel.GetMessageAsync (message.Id);

                if (reaction.Emote.Name == upvote) {
                    ChangeKarma (iMessage.Author.Id, 1);
                } else if (reaction.Emote.Name == downvote) {
                    ChangeKarma (iMessage.Author.Id, -1);
                }
            };

            Program.discordClient.ReactionRemoved += async ( message, channel, reaction ) => {
                
                IMessage iMessage = await channel.GetMessageAsync (message.Id);

                if (reaction.Emote.Name == upvote) {
                    ChangeKarma (iMessage.Author.Id, -1);
                } else if (reaction.Emote.Name == downvote) {
                    ChangeKarma (iMessage.Author.Id, +1);
                }
            };
        }

        public static void ChangeKarma (ulong userID, int change) {
            if (!karmaCollection.ContainsKey (userID))
                karmaCollection.Add (userID, 0);
            karmaCollection[userID] += change;

            SerializationIO.SaveObjectToFile (Program.dataPath + karmaFileName + Program.gitHubIgnoreType, karmaCollection);
        }

        public static long GetKarma (ulong userID) {
            return karmaCollection.ContainsKey (userID) ? karmaCollection[userID] : 0;
        }
    }

    public class CKarma : Command {
        public CKarma () {
            command = "karma";
            name = "Show Karma";
            help = "Shows karma of <me/user>.";
            argumentNumber = 1;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                long karmaCount = 0;

                SocketGuildUser user = e.Author as SocketGuildUser;

                if (arguments[0] != "me") {
                    user = Utility.FindUserByName ((e.Channel as SocketGuildChannel).Guild, arguments[0]);
                    if (user == null) {
                        Program.messageControl.SendMessage (e.Channel, "User " + arguments[0] + " not found.");
                        return Task.CompletedTask;
                    }
                }

                karmaCount = Karma.GetKarma (user.Id);
                Program.messageControl.SendMessage (e.Channel, "User " + Utility.GetUserName (user) + " currently has " + karmaCount + " karma.");
            }
            return Task.CompletedTask;
        }
    }
}
