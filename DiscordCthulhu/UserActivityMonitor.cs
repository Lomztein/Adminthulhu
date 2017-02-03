﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.Threading;

namespace DiscordCthulhu {

    public class UserActivityMonitor : IClockable {

        public static Dictionary<ulong, DateTime> userActivity;
        public static Dictionary<ulong, DateTime> lastUserUpdate = new Dictionary<ulong, DateTime>();
        public static int minTimeBetweenUpdates = 10;

        public static string activityFileName = "useractivity";

        private static int activeThresholdDays = 7;
        private static int presentThresholdDays = 30;

        private static string activeUserRole = "Active!";
        private static string presentUserRole = "Present";
        private static string inactiveUserRole = "Inactive :(";

        public void Initialize ( DateTime time ) {
            userActivity = SerializationIO.LoadObjectFromFile<Dictionary<ulong, DateTime>> (Program.dataPath + activityFileName + Program.gitHubIgnoreType);
            if (userActivity == null)
                userActivity = new Dictionary<ulong, DateTime> ();
            Booted ();
        }

        async void Booted () {
            while (Program.GetServer () == null) {
                await Task.Delay (1000);
            }

            await Task.Delay (5000);

            IEnumerable<User> users = Program.GetServer ().Users;
            foreach (User u in users) {
                if (!userActivity.ContainsKey (u.Id)) {
                    RecordActivity (u.Id, DateTime.Now.AddMonths (-6), false);
                }
            }

            Program.discordClient.MessageReceived += ( s, e ) => {
                RecordActivity (e.User.Id, DateTime.Now, true);
            };

            Program.discordClient.UserUpdated += ( s, e ) => {
                if (e.Before.VoiceChannel != e.After.VoiceChannel) {
                    if (e.After.VoiceChannel != null) {
                        RecordActivity (e.After.Id, DateTime.Now, true);
                    }
                }
            };

            OnDayPassed (DateTime.Now);
        }

        public static void RecordActivity ( ulong userID, DateTime time, bool single ) {
            if (userActivity.ContainsKey (userID)) {
                userActivity[userID] = time;
            } else {
                userActivity.Add (userID, time);
            }

            // Well that got ugly.
            Role activeRole = Program.GetServer ().FindRoles (activeUserRole).FirstOrDefault ();
            Role presentRole = Program.GetServer ().FindRoles (presentUserRole).FirstOrDefault ();
            Role inactiveRole = Program.GetServer ().FindRoles (inactiveUserRole).FirstOrDefault ();
            UpdateUser (userID, activeRole, presentRole, inactiveRole);

            if (single) {
                SerializationIO.SaveObjectToFile (Program.dataPath + activityFileName + Program.gitHubIgnoreType, userActivity);
            }
        }

        public void OnDayPassed ( DateTime time ) {
            Role activeRole = Program.GetServer ().FindRoles (activeUserRole).FirstOrDefault ();
            Role presentRole = Program.GetServer ().FindRoles (presentUserRole).FirstOrDefault ();
            Role inactiveRole = Program.GetServer ().FindRoles (inactiveUserRole).FirstOrDefault ();

            foreach (ulong id in userActivity.Keys) {
                UpdateUser (id, activeRole, presentRole, inactiveRole);
            }

            SerializationIO.SaveObjectToFile (Program.dataPath + activityFileName + Program.gitHubIgnoreType, userActivity);
        }

        private static async void UpdateUser ( ulong id, Role activeRole, Role presentRole, Role inactiveRole ) {
            DateTime time = DateTime.Now;

            if (lastUserUpdate.ContainsKey (id) && time < lastUserUpdate[id])
                return;

            DateTime lastActivity = userActivity[id];
            User user = Program.GetServer ().GetUser (id);

            List<Role> toAdd = new List<Role> ();
            List<Role> toRemove = new List<Role> ();

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

            // This might be heavy on the server during midnights.
            if (!user.HasRole (toAdd[0])) {
                ChatLogger.Log ("Adding role " + toAdd[0].Name + " to user " + user.Name);
                await user.AddRoles (toAdd[0]);
            }

            await Task.Delay (10000);
            bool missingAny = false;
            foreach (Role r in toRemove) {
                if (user.HasRole (r)) {
                    ChatLogger.Log ("Removing role " + r.Name + " from user " + user.Name);
                    missingAny = true;
                }
            }
            if (missingAny)
                await user.RemoveRoles (toRemove.ToArray ());

            if (lastUserUpdate.ContainsKey (id)) {
                lastUserUpdate[id] = time.AddSeconds (minTimeBetweenUpdates);
            } else {
                lastUserUpdate.Add (id, time.AddSeconds (minTimeBetweenUpdates));
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
