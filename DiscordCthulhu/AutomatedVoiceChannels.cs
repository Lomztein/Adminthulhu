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
        public static List<Channel> temporaryChannels = new List<Channel> ();
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

        public static void UpdateVoiceChannel ( Channel voice ) {
            Game highestGame = new Game ("");

            if (voice != null && allVoiceChannels.ContainsKey (voice.Id)) {

                if (voice.Name == afkChannel.name)
                    return;

                if (voice.Users.Count () == 0)
                    allVoiceChannels[voice.Id].Unlock (false);

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

                for (int i = 0; i < numPlayers.Count; i++) {
                    KeyValuePair<Game, int> value = numPlayers.ElementAt (i);

                    if (value.Value > highest) {
                        highest = value.Value;
                        highestGame = value.Key;
                    }
                }

                string lockString = allVoiceChannels[voice.Id].IsLocked () ? lockIcon : "";
                if (highestGame.Name != "") {
                    voice.Edit (lockString + allVoiceChannels[voice.Id].name + " - " + highestGame.Name);
                } else {
                    voice.Edit (lockString + allVoiceChannels[voice.Id].name);
                }
                allVoiceChannels[voice.Id].CheckLocker ();
            }
        }

        private static bool hasChecked = false;
        public static void AddMissingChannels (Server server) {
            if (hasChecked)
                return;

            foreach (Channel channel in server.VoiceChannels) {
                if (!allVoiceChannels.ContainsKey (channel.Id)) {
                    allVoiceChannels.Add (channel.Id, new VoiceChannel (channel.Id, GetChannelDefaultName (channel.Name), allVoiceChannels.Count));
                }
                if (channel.Name[0] == lockIcon[0]) {
                    if (channel.Users.Count () == 0) {
                        allVoiceChannels[channel.Id].Unlock (true);
                    } else {
                        User user = channel.Users.ElementAt (0);
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

        public static void RemoveLeftoverChannels (Server server) {
            List<Channel> toDelete = new List<Channel> ();

            foreach (Channel channel in server.VoiceChannels) {
                if (!defaultChannels.ContainsKey (channel.Id) && channel.Users.Count () == 0) {
                    toDelete.Add (channel);
                }
            }

            for (int i = IsDefaultFull () ? 1 : 0; i < toDelete.Count; i++) {
                Channel channel = toDelete[i];
                nameQueue.Enqueue (allVoiceChannels[channel.Id].name);

                temporaryChannels.Remove (channel);
                allVoiceChannels.Remove (channel.Id);
                channel.Delete ();
            }

            ResetVoiceChannelPositions (server);
        }

        private static void ResetVoiceChannelPositions ( Server e ) {
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

        public static async void CheckFullAndAddIf (Server server) {
            IEnumerable<VoiceChannel> channels = allVoiceChannels.Values.ToList ();
            int count = channels.Count ();

            fullChannels = 0;
            for (int i = 0; i < count; i++) {
                VoiceChannel cur = channels.ElementAt (i);
                if (cur == afkChannel)
                    continue;

                if (cur.GetChannel () != null) {
                    if (cur.GetChannel ().Users.Count () != 0) {
                        fullChannels++;
                    }
                }
            }

            // If the amount of full channels are more than or equal to the amount of channels, add a new one.
            if (fullChannels == count - 1) {
                if (nameQueue.Count > 0 && awaitingChannels.Count == 0) {
                    string channelName = nameQueue.Dequeue ();

                    Task<Channel> createTask = server.CreateChannel (channelName, ChannelType.Voice);
                    Channel channel = await createTask;

                    int channelPos = temporaryChannels.Count + addChannelsIndex;

                    temporaryChannels.Add (channel);
                    allVoiceChannels.Add (channel.Id, new VoiceChannel (channel.Id, channel.Name, channelPos));
                    awaitingChannels.Remove (channelName);

                    IEnumerable<VoiceChannel> allChannels = allVoiceChannels.Values.ToList ();
                    foreach (VoiceChannel loc in allChannels) {
                        if (loc.position >= channelPos && loc.GetChannel () != channel)
                            loc.position++;
                    }

                    await afkChannel.GetChannel ().Edit (null, null, int.MaxValue);
                }
            }
        }

        public static Channel GetEmptyChannel (Server server) {
            foreach (Channel channel in server.VoiceChannels) {
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

            public Channel GetChannel () {
                return Program.discordClient.GetChannel (id);
            }

            public bool IsLocked () {
                return allowedUsers.Count != 0;
            }

            public void Lock (User lockingUser, bool update) {
                if (lockable) {
                    lockerID = lockingUser.Id;

                    List<ulong> alreadyIn = new List<ulong> ();
                    foreach (User user in GetChannel ().Users) {
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

            public bool InviteUser (User sender, User user) {
                if (!allowedUsers.Contains (user.Id) && IsLocked ()) {
                    allowedUsers.Add (user.Id);
                    Program.messageControl.SendMessage (user, "**" + Program.GetUserName (sender) + "** has invited you to join the locked channel **" + name + "** on **" + Program.serverName + "**.");
                    return true;
                }
                return false;
            }

            public void RequestInvite (User requester) {
                if (IsLocked ()) {
                    Program.messageControl.SendMessage (GetLocker (), "**" + Program.GetUserName (requester) + "** on **" + Program.serverName + "** requests access to your locked voice channel.");
                }
            }

            public void Kick (User user) {
                allowedUsers.Remove (user.Id);
                OnUserJoined (user);
            }

            public void OnUserJoined (User user) {
                // IsLocked is kind of redundant, but I like having it there.
                if (IsLocked ()) {
                    if (!allowedUsers.Contains (user.Id) && !user.ServerPermissions.ManageChannels) {
                        Program.messageControl.SendMessage (user,"Sorry man, but you do not have access to voice channel **" + name + "**.");
                        user.Edit (null, null, afkChannel.GetChannel ());
                    }
                }
            }

            public User GetLocker () {
                return Program.GetServer ().GetUser (lockerID);
            }

            public void CheckLocker () {
                bool containsLocker = false;
                foreach (User user in GetChannel ().Users) {
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
