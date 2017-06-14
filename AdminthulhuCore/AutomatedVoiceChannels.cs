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

        public static Dictionary<ulong, VoiceChannel> defaultChannels = new Dictionary<ulong, VoiceChannel> ();
        public static Dictionary<ulong, VoiceChannel> allVoiceChannels = new Dictionary<ulong, VoiceChannel> ();
        public static List<string> awaitingChannels = new List<string> ();

        public static int addChannelsIndex = 2;
        public static int fullChannels = 0;

        public static string [ ] extraChannelNames = new string [ ] {
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

        public static Queue<string> nameQueue = new Queue<string> ();
        public static List<IVoiceChannel> temporaryChannels = new List<IVoiceChannel> ();
        public static VoiceChannel afkChannel = new VoiceChannel (265832231845625858, "Corner of Shame", int.MaxValue);

        public static VoiceChannelTag [ ] voiceChannelTags = {
            new VoiceChannelTag ("🎵", delegate (VoiceChannelTag.ActionData data) { data.hasTag = Utility.ForceGetUsers (data.channel.id).Find (x => x.Id == 174535711632916480) != null; } ),
            new VoiceChannelTag ("🌎", delegate (VoiceChannelTag.ActionData data) {
                SocketRole internationalRole = Utility.GetServer ().GetRole (182563086186577920);
                data.hasTag = Utility.ForceGetUsers (data.channel.id).Find (x => x.Roles.Contains (internationalRole)) != null;  }),
            new VoiceChannelTag ("🔒", delegate (VoiceChannelTag.ActionData data) { data.hasTag = data.channel.IsLocked (); }),
            //new VoiceChannelTag ("🍞", delegate (VoiceChannelTag.ActionData data) { data.hasTag = Utility.ForceGetUsers (data.channel.id).Where (x => x.Id == 110406708299329536).Count () != 0; } ),
            new VoiceChannelTag ("🎮", delegate (VoiceChannelTag.ActionData data) { data.hasTag = data.channel.status == VoiceChannel.VoiceChannelStatus.Looking; }),
            new VoiceChannelTag ("❌", delegate (VoiceChannelTag.ActionData data) { data.hasTag = data.channel.status == VoiceChannel.VoiceChannelStatus.Full; }),
            new VoiceChannelTag ("📆", delegate (VoiceChannelTag.ActionData data) { data.hasTag = ((DateTime.Now.DayOfWeek == DayOfWeek.Friday) && (DateTime.Now.Hour >= 20) && Utility.ForceGetUsers (data.channel.id).Count >= 3); }),
            new VoiceChannelTag ("🍰", delegate (VoiceChannelTag.ActionData data) { data.hasTag = Utility.ForceGetUsers (data.channel.id).Find (x => Birthdays.IsUsersBirthday (x)) != null; }),
            new VoiceChannelTag ("📹", delegate (VoiceChannelTag.ActionData data) { data.hasTag = Utility.ForceGetUsers (data.channel.id).Where (x => x.Game.HasValue).Where (x => x.Game.Value.StreamType > 0).Count () != 0; }),
            new VoiceChannelTag ("🔥", delegate (VoiceChannelTag.ActionData data) { data.hasTag = Utility.ForceGetUsers (data.channel.id).Where (x => x.GuildPermissions.Administrator).Count () >= 3; }),
            new VoiceChannelTag ("👶", delegate (VoiceChannelTag.ActionData data) {
                SocketRole younglingRole = Utility.GetServer ().GetRole (316171882867064843);
                data.hasTag = Utility.ForceGetUsers (data.channel.id).Find (x => x.Roles.Contains (younglingRole)) != null;  }),
        };

        public static void InitializeData() {
            AddDefaultChannel (250545007797207040, "Radical Red", 0);
            AddDefaultChannel (250545037790674944, "Beautiful Blue", 1);
            AddDefaultChannel (afkChannel);
            afkChannel.lockable = false;

            for (int i = 0; i < extraChannelNames.Length; i++) {
                nameQueue.Enqueue (extraChannelNames [ i ]);
            }
        }

        public static async Task OnUserUpdated(SocketGuild guild, SocketVoiceChannel before, SocketVoiceChannel after) {
            // Maybe, just maybe put these into a single function. Oh shit I just did.
            if (Program.FullyBooted ()) {
                AddMissingChannels (guild);
                await CheckFullAndAddIf (guild);
                RemoveLeftoverChannels (guild);

                await UpdateVoiceChannel (before);
                await UpdateVoiceChannel (after);
            }
        }

        private static void AddDefaultChannel(ulong id, string name, int index) {
            VoiceChannel newChannel = new VoiceChannel (id, name, index);

            defaultChannels.Add (id, newChannel);
            allVoiceChannels.Add (id, newChannel);
        }

        private static void AddDefaultChannel(VoiceChannel channel) {
            defaultChannels.Add (channel.id, channel);
            allVoiceChannels.Add (channel.id, channel);
        }

        public static async Task UpdateVoiceChannel(SocketVoiceChannel voice) {
            Game highestGame = new Game ("", "", StreamType.NotStreaming);

            if (voice != null && allVoiceChannels.ContainsKey (voice.Id)) {

                VoiceChannel voiceChannel = allVoiceChannels [ voice.Id ];
                List<SocketGuildUser> users = Utility.ForceGetUsers (voice.Id);

                if (voice.Name == afkChannel.name)
                    return;

                if (voice.Users.Count () == 0) {
                    voiceChannel.Unlock (false);
                    voiceChannel.status = VoiceChannel.VoiceChannelStatus.None;
                    voiceChannel.desiredMembers = 0;
                    voiceChannel.SetCustomName ("", false);
                } else {
                    if (voiceChannel.desiredMembers > 0) {
                        if (users.Count >= voiceChannel.desiredMembers)
                            voiceChannel.SetStatus (VoiceChannel.VoiceChannelStatus.Full, false);
                        else
                            voiceChannel.SetStatus (VoiceChannel.VoiceChannelStatus.Looking, false);
                    }
                }

                GetTags (voiceChannel);
                Dictionary<Game, int> numPlayers = new Dictionary<Game, int> ();
                foreach (SocketGuildUser user in users) {

                    SocketGuildUser forcedUser = Utility.GetServer ().GetUser (user.Id);
                    if (forcedUser.Game.HasValue) {
                        if (numPlayers.ContainsKey (user.Game.Value)) {
                            numPlayers [ forcedUser.Game.Value ]++;
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

                string tagsString = "";
                foreach (VoiceChannelTag tag in voiceChannel.currentTags) {
                    tagsString += tag.tagEmoji;
                }
                tagsString += tagsString.Length > 0 ? " " : "";

                // Trying to optimize API calls here, just to spare those poor souls at the Discord API HQ stuff
                string nameLetter = voiceChannel.name [ 0 ] + ""; // Eeeeeeuhhh, yes.
                string newName = highestGame.Name != "" ? tagsString + nameLetter + nameLetter + " - " + highestGame.Name : tagsString + voiceChannel.name;
                if (voiceChannel.customName != "")
                    newName = tagsString + nameLetter + nameLetter + " - " + voiceChannel.customName;

                if (voice.Name != newName) {
                    ChatLogger.Log ("Channel name updated: " + newName);
                    await voice.ModifyAsync ((delegate (VoiceChannelProperties properties) { properties.Name = newName; }));
                }
                voiceChannel.CheckLocker ();
            }
        }

        private static bool hasChecked = false;
        public static void AddMissingChannels(SocketGuild server) {
            if (hasChecked)
                return;

            foreach (SocketVoiceChannel channel in server.VoiceChannels) {
                if (!allVoiceChannels.ContainsKey (channel.Id)) {
                    allVoiceChannels.Add (channel.Id, new VoiceChannel (channel.Id, GetChannelDefaultName (channel.Name), allVoiceChannels.Count));
                }
                if (channel.Users.Count () == 0) {
                    allVoiceChannels [ channel.Id ].Unlock (true);
                }
            }

            hasChecked = true;
        }

        public static string GetChannelDefaultName(string channelName) {
            int spaceAmount = 0;

            // Start at two to ignore the locked channel icon. This will cause issues with channels with very, very short names.
            for (int i = 2; i < channelName.Length; i++) {
                if (channelName [ i ] == ' ')
                    spaceAmount++;

                if (spaceAmount == 2 || channelName.Length < i + 1)
                    return channelName.Substring (0, i);
            }

            return null;
        }

        public static void RemoveLeftoverChannels(SocketGuild server) {
            List<SocketVoiceChannel> toDelete = new List<SocketVoiceChannel> ();

            foreach (SocketVoiceChannel channel in server.VoiceChannels) {
                if (!defaultChannels.ContainsKey (channel.Id) && channel.Users.Count () == 0) {
                    toDelete.Add (channel);
                }
            }

            for (int i = IsDefaultFull () ? 1 : 0; i < toDelete.Count; i++) {
                SocketVoiceChannel channel = toDelete [ i ];
                nameQueue.Enqueue (allVoiceChannels [ channel.Id ].name);

                temporaryChannels.Remove (channel);
                allVoiceChannels.Remove (channel.Id);
                channel.DeleteAsync ();
            }

            ResetVoiceChannelPositions (server);
        }

        private static void ResetVoiceChannelPositions(SocketGuild e) {
            IEnumerable<VoiceChannel> channels = allVoiceChannels.Values.ToList ();

            foreach (VoiceChannel channel in channels) {
                if (channel == afkChannel)
                    continue;

                int sunkenPos = channel.position;
                while (!ChannelAtPosition (channels, sunkenPos - 1)) {
                    sunkenPos--;
                }
                channel.position = sunkenPos;
            }
        }

        public static bool ChannelAtPosition(IEnumerable<VoiceChannel> channels, int position) {
            if (position < 0)
                return true;

            bool result = false;
            foreach (VoiceChannel channel in channels) {
                if (channel.position == position)
                    result = true;
            }

            return result;
        }

        public static bool IsDefaultFull() {
            IEnumerable<VoiceChannel> defaultChannelsList = defaultChannels.Values.ToList ();
            foreach (VoiceChannel channel in defaultChannelsList) {
                if (afkChannel != channel && channel.GetChannel ().Users.Count () == 0)
                    return false;
            }

            return true;
        }

        public static async Task CheckFullAndAddIf(SocketGuild server) {
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
                }
            }
        }

        public static SocketVoiceChannel GetEmptyChannel(SocketGuild server) {
            foreach (SocketVoiceChannel channel in server.VoiceChannels) {
                if (channel.Users.Count () == 0)
                    return channel;
            }

            return null;
        }

        public static void GetTags(VoiceChannel channel) {
            for (int i = 0; i < voiceChannelTags.Length; i++) {
                VoiceChannelTag curTag = voiceChannelTags [ i ];

                VoiceChannelTag.ActionData data = new VoiceChannelTag.ActionData (channel);
                try {
                    curTag.run (data);
                }catch (Exception e) {
                    ChatLogger.DebugLog (e.StackTrace);
                }

                if (data.hasTag) {
                    if (!channel.currentTags.Contains (curTag))
                        channel.currentTags.Add (curTag);
                } else {
                    if (channel.currentTags.Contains (curTag))
                        channel.currentTags.Remove (curTag);
                }
            }
        }

        public class VoiceChannelTag {
            public string tagEmoji = "🍞";
            public Action<ActionData> run;

            public VoiceChannelTag(string emoji, Action<ActionData> command) {
                tagEmoji = emoji;
                run = command;
            }

            public class ActionData {
                public bool hasTag = false;
                public VoiceChannel channel;

                public ActionData (VoiceChannel c) {
                    channel = c;
                }
            }
        }

        public class VoiceChannel {

            public enum VoiceChannelStatus {
                None, Looking, Full
            }

            public ulong id;
            public string name;
            public int position;
            public bool lockable = true;
            public VoiceChannelStatus status = VoiceChannelStatus.None;
            public uint desiredMembers = 0;
            public string customName = "";

            public List<VoiceChannelTag> currentTags = new List<VoiceChannelTag> ();

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

            public void OnUserJoined(SocketGuildUser user) {
                if (IsLocked ()) {
                    if (!allowedUsers.Contains (user.Id)) {
                        user.ModifyAsync (delegate (GuildUserProperties properties) {
                            properties.Channel = afkChannel.GetChannel ();
                        });
                        Program.messageControl.SendMessage (user, "You've automatically been moved to the AFK channel, since channel **" + name + "** is locked by " + Utility.GetUserName (GetLocker ()));
                    }
                }
            }

            public void UpdateLockPermissions() {
                if (IsLocked ()) {
                    GetChannel ().ModifyAsync (delegate (VoiceChannelProperties properties) {
                        properties.UserLimit = allowedUsers.Count;
                    });
                } else {
                    GetChannel ().ModifyAsync (delegate (VoiceChannelProperties properties) {
                        properties.UserLimit = 0;
                    });
                }
            }

            public void Kick ( SocketGuildUser user ) {
                allowedUsers.Remove (user.Id);
                OnUserJoined (user);
            }

            public SocketGuildUser GetLocker () {
                return Utility.GetServer ().GetUser (lockerID);
            }

            public void ToggleStatus (VoiceChannelStatus stat) {
                if (desiredMembers > 0) {
                    SetDesiredMembers (0);
                    SetStatus (stat, true);
                    return;
                }

                status = status == stat ? VoiceChannelStatus.None : stat;
                UpdateVoiceChannel (GetChannel ());
            }

            public void SetStatus(VoiceChannelStatus stat, bool update) {
                status = stat;
                if (update)
                    UpdateVoiceChannel (GetChannel ());
            }

            public void SetDesiredMembers(uint number) {
                desiredMembers = number;
                UpdateVoiceChannel (GetChannel ());
            }

            public void SetCustomName(string n, bool update) {
                customName = n;
                if (update)
                    UpdateVoiceChannel (GetChannel ());
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
