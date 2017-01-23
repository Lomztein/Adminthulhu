using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.Threading;

namespace DiscordCthulhu {

    public class UserActivityMonitor : IClockable {

        Dictionary<ulong, DateTime> userActivity;
        public string activityFileName = "useractivity";

        private int activeThresholdDays = 7;
        private int presentThresholdDays = 30;

        private ulong activeUserRole = 273017450390487041;
        private ulong presentUserRole = 273017481600434186;
        private ulong inactiveUserRole = 273017511468072960;

        public void Initialize ( DateTime time ) {
            userActivity = SerializationIO.LoadObjectFromFile<Dictionary<ulong, DateTime>> (Program.dataPath + activityFileName + Program.gitHubIgnoreType);
            if (userActivity == null)
                userActivity = new Dictionary<ulong, DateTime> ();
            Booted ();
        }

        void Booted () {
            while (Program.GetServer () == null) {
                Thread.Sleep (1000);
            }

            IEnumerable<User> users = Program.GetServer ().Users;
            foreach (User u in users) {
                RecordActivity (u.Id, DateTime.Now.AddMonths (-6));
            }

            Program.discordClient.MessageReceived += ( s, e ) => {
                RecordActivity (e.User.Id, e.Message.Timestamp);
            };

            OnDayPassed (DateTime.Now);
        }

        void RecordActivity ( ulong userID, DateTime time ) {
            if (userActivity.ContainsKey (userID)) {
                userActivity[userID] = time;
            } else {
                userActivity.Add (userID, time);
            }

            // Well that got ugly.
            Role activeRole = Program.GetServer ().GetRole (activeUserRole);
            Role presentRole = Program.GetServer ().GetRole (presentUserRole);
            Role inactiveRole = Program.GetServer ().GetRole (inactiveUserRole);
            UpdateUser (userID, time, activeRole, presentRole, inactiveRole);
        }

        public void OnDayPassed ( DateTime time ) {
            Role activeRole = Program.GetServer ().GetRole (activeUserRole);
            Role presentRole = Program.GetServer ().GetRole (presentUserRole);
            Role inactiveRole = Program.GetServer ().GetRole (inactiveUserRole);

            foreach (ulong id in userActivity.Keys) {
                UpdateUser (id, time, activeRole, presentRole, inactiveRole);
            }

            SerializationIO.SaveObjectToFile (Program.dataPath + activityFileName + Program.gitHubIgnoreType, userActivity);

        }

        private void UpdateUser ( ulong id, DateTime time, Role activeRole, Role presentRole, Role inactiveRole ) {
            DateTime lastActivity = userActivity[id];
            User user = Program.GetServer ().GetUser (id);

            List<Role> toAdd = new List<Role> ();
            List<Role> toRemove = new List<Role> ();

            // This feels like it could be done differnetly, but it'll do.
            if (lastActivity > time.AddDays (-activeThresholdDays)) {
                toAdd.Add (activeRole);
                toRemove.Add (presentRole);
                toRemove.Add (inactiveRole);
            }

            if (lastActivity < time.AddDays (-activeThresholdDays) &&
                lastActivity > time.AddDays (-presentThresholdDays)) {
                toAdd.Add (presentRole);
                toRemove.Add (activeRole);
                toRemove.Add (inactiveRole);
            }

            if (lastActivity < time.AddDays (-presentThresholdDays)) {
                toAdd.Add (inactiveRole);
                toRemove.Add (presentRole);
                toRemove.Add (activeRole);
            }

            // This might be heavy on the server during midnights.
            foreach (Role r in toAdd) {
                Program.SecureAddRole (user, r);
            }

            foreach (Role r in toRemove) {
                Program.SecureRemoveRole (user, r);
            }
        }

        public void OnHourPassed ( DateTime time ) {
        }

        public void OnMinutePassed ( DateTime time ) {
        }

        public void OnSecondPassed ( DateTime time ) {
        }
    }
}
