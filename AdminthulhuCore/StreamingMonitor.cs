using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;

namespace Adminthulhu
{
    public class StreamingMonitor : IConfigurable {

        private static Dictionary<StreamType, string> streamSiteIcon = new Dictionary<StreamType, string> () {
            { StreamType.Twitch, "https://vignette.wikia.nocookie.net/logopedia/images/8/83/Twitch_icon.svg/revision/latest/scale-to-width-down/421?cb=20140727180700"}
        };

        private List<ulong> toMonitor = new List<ulong> ();
        private ulong announceStreamChannel = 0;
        private string announceMessageHeader = "Hey fellas, {USERNAME} has begun streaming!";

        public void LoadConfiguration() {
            toMonitor.Add (0);
            toMonitor = BotConfiguration.GetSetting ("StreamingMonitor.ToMonitor", this, toMonitor);
            announceStreamChannel = BotConfiguration.GetSetting ("StreamingMonitor.ChannelID", this, announceStreamChannel);
            announceMessageHeader = BotConfiguration.GetSetting ("StreamingMonitor.MessageHeader", this, announceMessageHeader);
        }

        public StreamingMonitor () {
            LoadConfiguration ();
            BotConfiguration.AddConfigurable (this);

            Program.discordClient.UserUpdated += (before, after) => {
                if (before.Game.HasValue && before.Game.Value.StreamType == StreamType.NotStreaming) {
                    if (after.Game.HasValue && after.Game.Value.StreamType != StreamType.NotStreaming) {
                        TestAndAnnounceIf (after.Id, after.Game.Value);
                    }
                }

                return Task.CompletedTask;
            };
        }

        public bool TestAndAnnounceIf (ulong user, Game userGame) {
            if (toMonitor.Contains (user)) {
                Embed embed = ConstructEmbed (user, userGame);
                SocketTextChannel channel = Utility.GetServer ().GetTextChannel (announceStreamChannel);

                Program.messageControl.SendEmbed (channel, embed);

                return true;
            }
            return false;
        }

        private Embed ConstructEmbed (ulong user, Game userGame) {
            EmbedBuilder builder = new EmbedBuilder ().WithTitle (announceMessageHeader.Replace ("{USERNAME}", Utility.GetUserName (Utility.GetServer ().GetUser (user))))
                .WithColor (Color.Purple)
                .WithDescription (userGame.Name)
                .WithUrl (userGame.StreamUrl)
                .WithFooter (userGame.StreamType.ToString (), streamSiteIcon [ userGame.StreamType ])
                .WithCurrentTimestamp ();

            return builder.Build ();
        }
    }
}
