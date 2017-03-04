using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.Threading;
using Discord.WebSocket;

namespace DiscordCthulhu {

    public class UserActivityMonitor : IClockable {

        public static Dictionary<ulong, DateTime> userActivity;
        public static Dictionary<ulong, DateTime> lastUserUpdate = new Dictionary<ulong, DateTime>();
        public static int minTimeBetweenUpdates = 10;

        public static string activityFileName = "useractivity";

        private static int activeThresholdDays = 7;
        private static int presentThresholdDays = 30;

        private static ulong activeUserRole = 273017450390487041;
        private static ulong presentUserRole = 273017481600434186;
        private static ulong inactiveUserRole = 273017511468072960;

        public async Task Initialize ( DateTime time ) {
            userActivity = SerializationIO.LoadObjectFromFile<Dictionary<ulong, DateTime>> (Program.dataPath + activityFileName + Program.gitHubIgnoreType);
            if (userActivity == null)
                userActivity = new Dictionary<ulong, DateTime> ();
            await Booted ();
        }

        async Task Booted () {
            while (Program.GetServer () == null) {
                await Task.Delay (1000);
            }

            await Task.Delay (5000);

            IEnumerable<SocketGuildUser> users = Program.GetServer ().Users;
            foreach (SocketGuildUser u in users) {
                if (!userActivity.ContainsKey (u.Id)) {
                    RecordActivity (u.Id, DateTime.Now.AddMonths (-6), false);
                }
            }

            Program.discordClient.MessageReceived += ( e ) => {
                RecordActivity (e.Author.Id, DateTime.Now, true);
                return Task.CompletedTask;
            };

            Program.discordClient.UserUpdated += ( before, after ) => {
                SocketGuildUser afterUser = after as SocketGuildUser;
                if ((before as SocketGuildUser).VoiceChannel != afterUser.VoiceChannel) {
                    if (afterUser.VoiceChannel != null) {
                        RecordActivity (after.Id, DateTime.Now, true);
                    }
                }

                return Task.CompletedTask;
            };

            await OnDayPassed (DateTime.Now);
        }

        public static void RecordActivity ( ulong userID, DateTime time, bool single ) {
            if (userActivity.ContainsKey (userID)) {
                userActivity[userID] = time;
            } else {
                userActivity.Add (userID, time);
            }

            // Well that got ugly.
            SocketRole activeRole = Program.GetServer ().GetRole (activeUserRole);
            SocketRole presentRole = Program.GetServer ().GetRole (presentUserRole);
            SocketRole inactiveRole = Program.GetServer ().GetRole (inactiveUserRole);
            UpdateUser (userID, activeRole, presentRole, inactiveRole);

            if (single) {
                SerializationIO.SaveObjectToFile (Program.dataPath + activityFileName + Program.gitHubIgnoreType, userActivity);
            }
        }

        public Task OnDayPassed ( DateTime time ) {
            SocketRole activeRole = Program.GetServer ().GetRole (activeUserRole);
            SocketRole presentRole = Program.GetServer ().GetRole (presentUserRole);
            SocketRole inactiveRole = Program.GetServer ().GetRole (inactiveUserRole);

            foreach (ulong id in userActivity.Keys) {
                UpdateUser (id, activeRole, presentRole, inactiveRole);
            }

            SerializationIO.SaveObjectToFile (Program.dataPath + activityFileName + Program.gitHubIgnoreType, userActivity);
            return Task.CompletedTask;
        }

        private static async Task UpdateUser ( ulong id, SocketRole activeRole, SocketRole presentRole, SocketRole inactiveRole ) {
            DateTime time = DateTime.Now;

            if (lastUserUpdate.ContainsKey (id) && time < lastUserUpdate[id])
                return;

            if (lastUserUpdate.ContainsKey (id)) {
                lastUserUpdate[id] = time.AddSeconds (minTimeBetweenUpdates);
            } else {
                lastUserUpdate.Add (id, time.AddSeconds (minTimeBetweenUpdates));
            }

            DateTime lastActivity = userActivity[id];
            SocketGuildUser user = Program.GetServer ().GetUser (id);

            List<SocketRole> toAdd = new List<SocketRole> ();
            List<SocketRole> toRemove = new List<SocketRole> ();

            // This feels like it could be done differnetly, but it'll do.
            if (lastActivity > time.AddDays (-activeThresholdDays)) {
                toAdd.Add (activeRole);
                toRemove.Add (presentRole);
                toRemove.Add (inactiveRole);
            } else if (lastActivity < time.AddDays (-activeThresholdDays) &&
                lastActivity > time.AddDays (-presentThresholdDays)) {
                toAdd.Add (presentRole);
                toRemove.Add (activeRole);
                toRemove.Add (inactiveRole);
            } else if (lastActivity < time.AddDays (-presentThresholdDays)) {
                toAdd.Add (inactiveRole);
                toRemove.Add (presentRole);
                toRemove.Add (activeRole);
            }

            foreach (SocketRole r in toRemove) {
                if (user.Roles.Contains (r)) {
                    ChatLogger.Log ("Removing role " + r.Name + " from user " + user.Username);
                    await user.RemoveRolesAsync (r);
                }
            }

            // This might be heavy on the server during midnights.
            if (!user.Roles.Contains (toAdd[0])) {
                ChatLogger.Log ("Adding role " + toAdd[0].Name + " to user " + user.Username);
                await user.AddRolesAsync (toAdd[0]);
            }
        }

        public Task OnHourPassed ( DateTime time ) {
            return Task.CompletedTask;
        }

        public Task OnMinutePassed ( DateTime time ) {
            return Task.CompletedTask;
        }

        public Task OnSecondPassed ( DateTime time ) {
            return Task.CompletedTask;
        }
    }
}
