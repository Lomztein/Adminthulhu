using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Rest;

namespace Adminthulhu {
    public static class AutomatedVoiceChannels {

        public static Dictionary<ulong, VoiceChannel> defaultChannels = new Dictionary<ulong, VoiceChannel>();
        public static Dictionary<ulong, VoiceChannel> allVoiceChannels = new Dictionary<ulong, VoiceChannel>();
        public static List<string> awaitingChannels = new List<string>();

        public static string lockIcon = "🔒 ";
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
            "Wanking White",
            "Indeginous Indigo",
            "Laughing Lime",
            "Mangled Magenta"
        };

        public static Queue<string> nameQueue = new Queue<string>();
        public static List<IVoiceChannel> temporaryChannels = new List<IVoiceChannel> ();
        public static VoiceChannel afkChannel = new VoiceChannel (265832231845625858, "Corner of Shame", int.MaxValue);

        public static void InitializeData () {
            AddDefaultChannel (250545007797207040, "Radical Red", 0);
            AddDefaultChannel (250545037790674944, "Beautiful Blue", 1);
            AddDefaultChannel (afkChannel);
            afkChannel.lockable = false;

            for (int i = 0; i < extraChannelNames.Length; i++) {
                nameQueue.Enqueue (extraChannelNames[i]);
            }
        }

        private static void AddDefaultChannel (ulong id, string name, int index) {
            VoiceChannel newChannel = new VoiceChannel (id, name, index);

            defaultChannels.Add (id, newChannel);
            allVoiceChannels.Add (id, newChannel);
        }

        private static void AddDefaultChannel (VoiceChannel channel) {
            defaultChannels.Add (channel.id, channel);
            allVoiceChannels.Add (channel.id, channel);
        }

        public static async Task UpdateVoiceChannel ( SocketVoiceChannel voice ) {
            Game highestGame = new Game ("", "", StreamType.NotStreaming);

            if (voice != null && allVoiceChannels.ContainsKey (voice.Id)) {

                if (voice.Name == afkChannel.name)
                    return;

                if (voice.Users.Count () == 0)
                    allVoiceChannels[voice.Id].Unlock (false);

                Dictionary<Game, int> numPlayers = new Dictionary<Game, int> ();
                List<SocketGuildUser> users = voice.Users.ToList ();
                foreach (SocketGuildUser user in users) {

                    SocketGuildUser forcedUser = Utility.GetServer().GetUser (user.Id);
                    if (forcedUser.Game.HasValue) {
                        if (numPlayers.ContainsKey (user.Game.Value)) {
                            numPlayers[forcedUser.Game.Value]++;
                        } else {
                            numPlayers.Add (forcedUser.Game.Value, 1);
                        }
                    }

                }

                int highest = int.MinValue;

                for (int i = 0; i < numPlayers.Count; i++) {
                    KeyValuePair<Game, int> value = numPlayers.ElementAt (i);

                    if (value.Value > highest) {
                        highest = value.Value;
                        highestGame = value.Key;
                    }
                }

                string lockString = allVoiceChannels[voice.Id].IsLocked () ? lockIcon : "";
                // Trying to optimize API calls here, just to spare those poor souls at the Discord API HQ stuff
                string newName = highestGame.Name != "" ? lockString + allVoiceChannels[voice.Id].name + " - " + highestGame.Name : lockString + allVoiceChannels[voice.Id].name;
                if (voice.Name != newName) {
                    ChatLogger.Log ("Channel name updated: " + newName);
                    await voice.ModifyAsync ((delegate ( VoiceChannelProperties properties ) { properties.Name = newName; } ));
                }
                allVoiceChannels[voice.Id].CheckLocker ();
            }
        }

        private static bool hasChecked = false;
        public static void AddMissingChannels (SocketGuild server) {
            if (hasChecked)
                return;

            foreach (SocketVoiceChannel channel in server.VoiceChannels) {
                if (!allVoiceChannels.ContainsKey (channel.Id)) {
                    allVoiceChannels.Add (channel.Id, new VoiceChannel (channel.Id, GetChannelDefaultName (channel.Name), allVoiceChannels.Count));
                }
                if (channel.Name[0] == lockIcon[0]) {
                    if (channel.Users.Count () == 0) {
                        allVoiceChannels[channel.Id].Unlock (true);
                    } else {
                        SocketGuildUser user = channel.Users.ElementAt (0);
                        allVoiceChannels[channel.Id].Lock (user, true);
                        Program.messageControl.SendMessage (user, "After a reboot of this bot, you have automatically been granted ownership of locked channel **" + allVoiceChannels[channel.Id].name + "**.");
                    }
                }
            }

            hasChecked = true;
        }

        public static string GetChannelDefaultName (string channelName) {
            int spaceAmount = 0;

            // Start at two to ignore the locked channel icon. This will cause issues with channels with very, very short names.
            for (int i = 2; i < channelName.Length; i++) {
                if (channelName[i] == ' ')
                    spaceAmount++;

                if (spaceAmount == 2 || channelName.Length < i + 1)
                    return channelName.Substring (0, i);
            }

            return null;
        }

        public static void RemoveLeftoverChannels (SocketGuild server) {
            List<SocketVoiceChannel> toDelete = new List<SocketVoiceChannel> ();

            foreach (SocketVoiceChannel channel in server.VoiceChannels) {
                if (!defaultChannels.ContainsKey (channel.Id) && channel.Users.Count () == 0) {
                    toDelete.Add (channel);
                }
            }

            for (int i = IsDefaultFull () ? 1 : 0; i < toDelete.Count; i++) {
                SocketVoiceChannel channel = toDelete[i];
                nameQueue.Enqueue (allVoiceChannels[channel.Id].name);

                temporaryChannels.Remove (channel);
                allVoiceChannels.Remove (channel.Id);
                channel.DeleteAsync ();
            }

            ResetVoiceChannelPositions (server);
        }

        private static void ResetVoiceChannelPositions ( SocketGuild e ) {
            IEnumerable<VoiceChannel> channels = allVoiceChannels.Values.ToList ();

            foreach (VoiceChannel channel in channels) {
                if (channel == afkChannel)
                    continue;

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
                if (afkChannel != channel && channel.GetChannel ().Users.Count () == 0)
                    return false;
            }

            return true;
        }

        public static async Task CheckFullAndAddIf (SocketGuild server) {
            IEnumerable<VoiceChannel> channels = allVoiceChannels.Values.ToList ();
            int count = channels.Count ();

            fullChannels = 0;
            for (int i = 0; i < count; i++) {
                VoiceChannel cur = channels.ElementAt (i);
                if (cur == afkChannel)
                    continue;

                if (cur.GetChannel () != null) {
                    SocketVoiceChannel channel = cur.GetChannel ();
                    if (Utility.ForceGetUsers (channel.Id).Count != 0) {
                        fullChannels++;
                    }
                }
            }

            // If the amount of full channels are more than or equal to the amount of channels, add a new one.
            if (fullChannels == count - 1) {
                if (nameQueue.Count > 0 && awaitingChannels.Count == 0) {
                    string channelName = nameQueue.Dequeue ();

                    RestVoiceChannel channel;
                    try {
                        Task<RestVoiceChannel> createTask = server.CreateVoiceChannelAsync (channelName);
                        channel = await createTask;
                    } catch (Exception e) {
                        throw;
                    }

                    int channelPos = temporaryChannels.Count + addChannelsIndex;

                    temporaryChannels.Add (channel);
                    allVoiceChannels.Add (channel.Id, new VoiceChannel (channel.Id, channel.Name, channelPos));
                    awaitingChannels.Remove (channelName);

                    IEnumerable<VoiceChannel> allChannels = allVoiceChannels.Values.ToList ();
                    foreach (VoiceChannel loc in allChannels) {
                        if (loc.position >= channelPos && loc.GetChannel () as IVoiceChannel != channel)
                            loc.position++;
                    }

                    await afkChannel.GetChannel ().ModifyAsync (delegate ( VoiceChannelProperties properties ) { properties.Position = allVoiceChannels.Count - 1; } );
                    await afkChannel.GetChannel ().ModifyAsync (delegate ( VoiceChannelProperties properties ) { properties.Position = allVoiceChannels.Count; } ); // Yay for hacky stuffz!
                }
            }
        }

        public static SocketVoiceChannel GetEmptyChannel ( SocketGuild server) {
            foreach (SocketVoiceChannel channel in server.VoiceChannels) {
                if (channel.Users.Count () == 0)
                    return channel;
            }

            return null;
        }

        public class VoiceChannel {

            public ulong id;
            public string name;
            public int position;
            public bool lockable = true;

            public ulong lockerID;
            public List<ulong> allowedUsers = new List<ulong>();

            public VoiceChannel (ulong _id, string n, int pos) {
                id = _id;
                name = n;
                position = pos;
                //channel = ch;
            }

            public SocketVoiceChannel GetChannel () {
                return Program.discordClient.GetChannel (id) as SocketVoiceChannel;
            }

            public bool IsLocked () {
                return allowedUsers.Count != 0;
            }

            public void Lock (SocketGuildUser lockingUser, bool update) {
                if (lockable) {
                    lockerID = lockingUser.Id;

                    List<ulong> alreadyIn = new List<ulong> ();
                    foreach (SocketGuildUser user in GetChannel ().Users) {
                        alreadyIn.Add (user.Id);
                    }

                    allowedUsers = alreadyIn;
                    if (update)
                        UpdateVoiceChannel (GetChannel ());
                }
            }

            public void Unlock (bool update) {
                allowedUsers = new List<ulong> ();
                lockerID = 0;
                if (update)
                    UpdateVoiceChannel (GetChannel ());
            }

            public bool InviteUser (SocketGuildUser sender, SocketGuildUser user ) {
                if (!allowedUsers.Contains (user.Id) && IsLocked ()) {
                    allowedUsers.Add (user.Id);
                    Program.messageControl.SendMessage (user, "**" + Utility.GetUserName (sender) + "** has invited you to join the locked channel **" + name + "** on **" + Program.serverName + "**.");
                    return true;
                }
                return false;
            }

            public void RequestInvite ( SocketGuildUser requester ) {
                if (IsLocked ()) {
                    Program.messageControl.AskQuestion (GetLocker (), "**" + Utility.GetUserName (requester) + "** on **" + Program.serverName + "** requests access to your locked voice channel.",
                        delegate () {
                            allowedUsers.Add (requester.Id);
                            Program.messageControl.SendMessage (requester, "Your request to join **" + name + "** has been accepted.");
                            Program.messageControl.SendMessage (GetLocker (), "Succesfully accepted requiest.");
                        } );
                }
            }

            public void Kick ( SocketGuildUser user ) {
                allowedUsers.Remove (user.Id);
            }

            public SocketGuildUser GetLocker () {
                return Utility.GetServer ().GetUser (lockerID);
            }

            public void CheckLocker () {
                bool containsLocker = false;
                foreach (SocketGuildUser user in GetChannel ().Users) {
                    if (user.Id == lockerID) {
                        containsLocker = true;
                        break;
                    }
                }

                if (IsLocked () && !containsLocker) {
                    lockerID = GetChannel ().Users.ElementAt (0).Id;
                    Program.messageControl.SendMessage (GetLocker (), "Since the previous locker left, you are now the new locker of voice channel **" + name + "**.");
                }
            }
        }
    }
}
