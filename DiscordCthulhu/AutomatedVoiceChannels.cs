using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    public static class AutomatedVoiceChannels {

        public static Dictionary<ulong, VoiceChannel> defaultChannels = new Dictionary<ulong, VoiceChannel>();
        public static Dictionary<ulong, VoiceChannel> allVoiceChannels = new Dictionary<ulong, VoiceChannel>();
        public static List<string> awaitingChannels = new List<string>();

        public static int addChannelsIndex = 2;
        public static int fullChannels = 0;

        public static string[] extraChannelNames = new string[] {
            "Gorgeous Green",
            "Pathetic Pink",
            "Adorable Amber",
            "Flamboyant Flamingo",
            "Cringy Cyan",
            "Ordinary Orange",
            "Unreal Umber",
            "Godlike Gold",
            "Zealot Zaffre",
            "Violent Violet",
            "Creepy Cardinal",
            "Salty Salmon",
            "Wanking White"
        };

        public static Queue<string> nameQueue = new Queue<string>();
        public static List<Channel> temporaryChannels = new List<Channel> ();

        public static void InitializeData () {
            AddDefaultChannel (250545007797207040, "Radical Red", 0);
            AddDefaultChannel (250545037790674944, "Beautiful Blue", 1);

            for (int i = 0; i < extraChannelNames.Length; i++) {
                nameQueue.Enqueue (extraChannelNames[i]);
            }

            Program.discordClient.ChannelCreated += ( s, e ) => {
                if (awaitingChannels.Contains (e.Channel.Name)) {

                    int channelPos = temporaryChannels.Count + addChannelsIndex;

                    temporaryChannels.Add (e.Channel);
                    allVoiceChannels.Add (e.Channel.Id, new VoiceChannel (e.Channel.Id, e.Channel.Name, channelPos, e.Channel));

                    IEnumerable<VoiceChannel> channels = allVoiceChannels.Values.ToList ();
                    foreach (VoiceChannel channel in channels) {
                        if (channel.position >= channelPos && channel.GetChannel () != e.Channel)
                            channel.position++;
                    }

                    //UpdatePositions (allVoiceChannels.Values.ToList ());
                }
            };
        }

        private static void AddDefaultChannel (ulong id, string name, int index) {
            VoiceChannel newChannel = new VoiceChannel (id, name, index, null);

            defaultChannels.Add (id, newChannel);
            allVoiceChannels.Add (id, newChannel);
        }

        public static void UpdateVoiceChannel ( Channel voice ) {
            if (voice != null && allVoiceChannels.ContainsKey (voice.Id)) {

                Dictionary<Game, int> numPlayers = new Dictionary<Game, int> ();
                foreach (User user in voice.Users) {

                    if (user.CurrentGame.HasValue) {
                        if (numPlayers.ContainsKey (user.CurrentGame.Value)) {
                            numPlayers[user.CurrentGame.Value]++;
                        } else {
                            numPlayers.Add (user.CurrentGame.Value, 1);
                        }
                    }

                }

                int highest = int.MinValue;
                Game highestGame = new Game ("");

                for (int i = 0; i < numPlayers.Count; i++) {
                    KeyValuePair<Game, int> value = numPlayers.ElementAt (i);

                    if (value.Value > highest) {
                        highest = value.Value;
                        highestGame = value.Key;
                    }
                }

                if (highestGame.Name != "") {
                    voice.Edit (allVoiceChannels[voice.Id].name + " - " + highestGame.Name);
                } else {
                    voice.Edit (allVoiceChannels[voice.Id].name);
                }
            }
        }

        private static bool hasChecked = false;
        public static void AddMissingChannels (Server server) {
            if (hasChecked)
                return;

            foreach (Channel channel in server.VoiceChannels) {
                if (!allVoiceChannels.ContainsKey (channel.Id)) {
                    allVoiceChannels.Add (channel.Id, new VoiceChannel (channel.Id, GetChannelDefaultName (channel.Name), allVoiceChannels.Count, channel));
                }
            }

            hasChecked = true;
        }

        public static string GetChannelDefaultName (string channelName) {
            int spaceAmount = 0;
            for (int i = 0; i < channelName.Length; i++) {
                if (channelName[i] == ' ')
                    spaceAmount++;

                if (spaceAmount == 2)
                    return channelName.Substring (0, i);
            }

            return null;
        }

        public static void RemoveLeftoverChannels (Server server) {
            List<Channel> toDelete = new List<Channel> ();

            foreach (Channel channel in server.VoiceChannels) {
                if (!defaultChannels.ContainsKey (channel.Id) && channel.Users.Count () == 0) {
                    toDelete.Add (channel);
                }
            }

            for (int i = IsDefaultFull () ? 1 : 0; i < toDelete.Count; i++) {
                Channel channel = toDelete[i];

                temporaryChannels.Remove (channel);
                allVoiceChannels.Remove (channel.Id);
                channel.Delete ();
            }

            ResetVoiceChannelPositions (server);
        }

        private static void ResetVoiceChannelPositions ( Server e ) {
            IEnumerable<VoiceChannel> channels = allVoiceChannels.Values.ToList ();

            foreach (VoiceChannel channel in channels) {
                int sunkenPos = channel.position;
                while (!ChannelAtPosition (channels, sunkenPos-1)) {
                    sunkenPos--;
                }
                channel.position = sunkenPos;
            }
        }

        public static bool ChannelAtPosition (IEnumerable<VoiceChannel> channels, int position) {
            if (position < 0)
                return true;

            bool result = false;
            foreach (VoiceChannel channel in channels) {
                if (channel.position == position)
                    result = true;
            }

            return result;
        }

        public static bool IsDefaultFull () {
            IEnumerable<VoiceChannel> defaultChannelsList = defaultChannels.Values.ToList ();
            foreach (VoiceChannel channel in defaultChannelsList) {
                if (channel.GetChannel ().Users.Count () == 0)
                    return false;
            }

            return true;
        }

        public static void CheckFullAndAddIf (Server server) {
            IEnumerable<VoiceChannel> channels = allVoiceChannels.Values.ToList ();
            int count = channels.Count ();

            fullChannels = 1;
            bool allBelowFull = true;
            for (int i = 0; i < count; i++) {
                VoiceChannel cur = channels.ElementAt (i);

                if (cur.GetChannel () != null) {
                    if (cur.GetChannel ().Users.Count () == 0) {
                        if (cur.position < addChannelsIndex) {
                            allBelowFull = false;
                        }
                    }else {
                        fullChannels++;
                    }
                }
            }

            if (allBelowFull) {
                if (nameQueue.Count > 0) {
                    if (temporaryChannels.Count < fullChannels - addChannelsIndex) {
                        string channelName = nameQueue.Dequeue ();

                        server.CreateChannel (channelName, ChannelType.Voice);
                        awaitingChannels.Add (channelName);
                    }
                }
            }
        }

        public class VoiceChannel {

            public ulong id;
            public string name;
            public int position;
            public Channel channel;

            public VoiceChannel (ulong _id, string n, int pos, Channel ch) {
                id = _id;
                name = n;
                position = pos;
                channel = ch;
            }

            public Channel GetChannel () {
                if (channel != null)
                    return channel;

                channel = Program.SearchChannel (Program.GetServer (), name);
                return channel;
            }
        }
    }
}
