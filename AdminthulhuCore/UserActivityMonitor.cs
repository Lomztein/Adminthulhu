using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.Threading;
using Discord.WebSocket;

namespace Adminthulhu {

    public class UserActivityMonitor : IClockable, IConfigurable {

        public static Dictionary<ulong, DateTime> userActivity;
        public static Dictionary<ulong, DateTime> lastUserUpdate = new Dictionary<ulong, DateTime>();
        public static int minTimeBetweenUpdates = 10;

        public static string activityFileName = "useractivity";

        public static int activeThresholdDays = 7;
        public static int presentThresholdDays = 30;

        public static ulong activeUserRole = 273017450390487041;
        public static ulong presentUserRole = 273017481600434186;
        public static ulong inactiveUserRole = 273017511468072960;

        public void LoadConfiguration() {
            activeThresholdDays = BotConfiguration.GetSetting ("Activity.ActiveThresholdDays", this, 7);
            presentThresholdDays = BotConfiguration.GetSetting ("Activity.PresentThresholdDays", this, 14);
            activeUserRole = BotConfiguration.GetSetting<ulong> ("Roles.ActiveID", this, 0);
            presentUserRole = BotConfiguration.GetSetting<ulong> ("Roles.PresentID", this, 0);
            inactiveUserRole = BotConfiguration.GetSetting<ulong> ("Roles.InactiveID", this, 0);
        }

        public async Task Initialize(DateTime time) {
            LoadConfiguration ();
            BotConfiguration.AddConfigurable (this);
            userActivity = SerializationIO.LoadObjectFromFile<Dictionary<ulong, DateTime>> (Program.dataPath + activityFileName + Program.gitHubIgnoreType);
            if (userActivity == null)
                userActivity = new Dictionary<ulong, DateTime> ();
            await Booted ();
        }

        async Task Booted () {
            while (Utility.GetServer () == null) {
                await Task.Delay (1000);
            }

            await Task.Delay (5000);

            IEnumerable<SocketGuildUser> users = Utility.GetServer ().Users;
            foreach (SocketGuildUser u in users) {
                if (!userActivity.ContainsKey (u.Id)) {
                    RecordActivity (u.Id, DateTime.Now.AddMonths (-6), false);
                }
            }

            Program.discordClient.MessageReceived += ( e ) => {
                RecordActivity (e.Author.Id, DateTime.Now, true);
                return Task.CompletedTask;
            };

            Program.discordClient.UserVoiceStateUpdated += ( user, before, after ) => {
                SocketGuildUser afterUser = user as SocketGuildUser;
                if (afterUser.VoiceChannel != null) {
                    RecordActivity (user.Id, DateTime.Now, true);
                }

                return Task.CompletedTask;
            };

            Program.discordClient.UserJoined += (user) => {
                RecordActivity (user.Id, DateTime.Now, true);
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
            SocketRole activeRole = Utility.GetServer ().GetRole (activeUserRole);
            SocketRole presentRole = Utility.GetServer ().GetRole (presentUserRole);
            SocketRole inactiveRole = Utility.GetServer ().GetRole (inactiveUserRole);
            UpdateUser (userID, activeRole, presentRole, inactiveRole);

            if (single) {
                SerializationIO.SaveObjectToFile (Program.dataPath + activityFileName + Program.gitHubIgnoreType, userActivity);
            }
        }

        public Task OnDayPassed ( DateTime time ) {
            SocketRole activeRole = Utility.GetServer ().GetRole (activeUserRole);
            SocketRole presentRole = Utility.GetServer ().GetRole (presentUserRole);
            SocketRole inactiveRole = Utility.GetServer ().GetRole (inactiveUserRole);

            foreach (ulong id in userActivity.Keys) {
                UpdateUser (id, activeRole, presentRole, inactiveRole);
            }

            SerializationIO.SaveObjectToFile (Program.dataPath + activityFileName + Program.gitHubIgnoreType, userActivity);
            return Task.CompletedTask;
        }

        public static DateTime GetLastActivity(ulong userID) {
            return userActivity [ userID ];
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
            SocketGuildUser user = Utility.GetServer ().GetUser (id);

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
                    Logging.Log (Logging.LogType.BOT, "Removing role " + r.Name + " from user " + user.Username);
                    await user.RemoveRoleAsync (r);
                }
            }

            // This might be heavy on the server during midnights.
            if (!user.Roles.Contains (toAdd[0])) {
                Logging.Log (Logging.LogType.BOT,"Adding role " + toAdd[0].Name + " to user " + user.Username);
                await user.AddRoleAsync (toAdd[0]);
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


    public class SetLastActivity : Command {

        public SetLastActivity() {
            command = "setlastactivity";
            shortHelp = "Set the last activity.";
            AddOverload (typeof (void), "Set the last activity of a user, for debugging reasons.");
            isAdminOnly = true;
        }

        public Task<Result> Execute(SocketUserMessage e, SocketGuildUser user, DateTime time) {
            UserActivityMonitor.RecordActivity (user.Id, time, true);
            return TaskResult (null, "Succesfully set activity of user!");
        }

    }
}
