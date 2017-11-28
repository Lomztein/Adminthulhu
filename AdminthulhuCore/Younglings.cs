using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Rest;

namespace Adminthulhu
{
    public class Younglings : IClockable, IConfigurable {

        public static ulong younglingRoleID = 316171882867064843;
        private static Dictionary<ulong, Youngling> joinDate;
        public static uint daysActiveRequired = 14;

        public static string onKickedDM;
        public static string onAcceptedDM;
        public static string onAcceptedPublicAnnouncement;
        public static string onRemindDM;
        public static int capraPopFlySprawlsYeekYoungin = 2; // This is your fault, Fred.

        public void LoadConfiguration() {
            younglingRoleID = BotConfiguration.GetSetting<ulong> ("Roles.YounglingID", this, 0);
            onKickedDM = BotConfiguration.GetSetting ("Activity.Younglings.OnKickedDM", this, "Sorry, but you've been kicked from my server due to early inactivity. If you feel this was a mistake, feel free to use this invite link: {INVITELINK}");
            onAcceptedDM = BotConfiguration.GetSetting ("Activity.Younglings.OnAcceptedDM", this, "Congratulations, you now have full membership of my server!");
            onAcceptedPublicAnnouncement = BotConfiguration.GetSetting ("Activity.Younglings.OnAcceptedPublicAnnouncement", this, "Congratulations to {USERNAME} as they've today been granted permanemt membership of this server!");
            onRemindDM = BotConfiguration.GetSetting ("Activity.Younglings.OnRemindDM", this, "Heads up! You will soon be kicked from my server in 2 days due to inactivity! To avoid this, please send a message in any channel or join a voice channel.");
            daysActiveRequired = BotConfiguration.GetSetting ("Activity.Younglings.DaysActiveRequired", this, daysActiveRequired);
            capraPopFlySprawlsYeekYoungin = BotConfiguration.GetSetting ("Activity.Younglings.RemindBeforeKickDays", this, capraPopFlySprawlsYeekYoungin);
        }

        public Task Initialize(DateTime time) {
            LoadConfiguration ();
            BotConfiguration.AddConfigurable (this);

            LoadData ();
            if (joinDate == null)
                joinDate = new Dictionary<ulong, Youngling> ();

            return Task.CompletedTask;
        }

        public static void OnUserJoined(SocketGuildUser user) {
            if (joinDate != null)
                AddYoungling (user, DateTime.Now);
            SaveData ();
        }

        public static void AddYoungling(SocketGuildUser user, DateTime time) {
            SocketRole role = Utility.GetServer ().GetRole (younglingRoleID);
            Utility.SecureAddRole (user, role);

            if (joinDate.ContainsKey (user.Id))
                joinDate.Remove (user.Id);
            joinDate.Add (user.Id, new Youngling (time));
            SaveData ();
        }

        public static void SaveData() {
            SerializationIO.SaveObjectToFile (Program.dataPath + "younglings" + Program.gitHubIgnoreType, joinDate, true, false);
        }

        public static void LoadData() {
            joinDate = SerializationIO.LoadObjectFromFile <Dictionary<ulong, Youngling>> (Program.dataPath + "younglings" + Program.gitHubIgnoreType);
        }

        public async Task OnMinutePassed(DateTime time) {
            SocketRole activeRole = Utility.GetServer ().GetRole (UserActivityMonitor.activeUserRole);
            SocketRole younglingRole = Utility.GetServer ().GetRole (younglingRoleID);

            List<ulong> toRemove = new List<ulong> ();

            foreach (var pair in joinDate) { // A bit of optimization, so it doesn't test any unneccesary users.
                SocketGuildUser user = Utility.GetServer ().GetUser (pair.Key);

                if (user != null) {
                    if (user.Roles.Contains (younglingRole) && !user.Roles.Contains (activeRole)) {
                        try {
                            Program.SetKickReason (user.Id, "Kicked due to youngling-stage inactivity.");
                            RestInviteMetadata metadata = await Utility.GetMainChannel ().CreateInviteAsync (null, 1, false, true);
                            await Program.messageControl.SendMessage (user, onKickedDM.Replace ("{INVITELINK}", metadata.Url));
                            await user.KickAsync ();
                        } catch (Exception e) {
                            Logging.DebugLog (Logging.LogType.EXCEPTION, e.Message);
                        }
                    }



                    if (user.Roles.Contains (younglingRole)) {
                        bool pastYounglingStage = UserActivityMonitor.GetLastActivity (user.Id) > pair.Value.joinDate.AddDays (daysActiveRequired);
                        bool remindAboutKick = UserActivityMonitor.GetLastActivity (user.Id) < DateTime.Now.AddDays (-(UserActivityMonitor.activeThresholdDays - capraPopFlySprawlsYeekYoungin));

                        if (pastYounglingStage) {
                            await Utility.SecureRemoveRole (user, younglingRole);
                            await Program.messageControl.SendMessage (user, onAcceptedDM);
                            Program.messageControl.SendMessage (Utility.GetMainChannel () as SocketTextChannel, onAcceptedPublicAnnouncement.Replace ("{USERNAME}", Utility.GetUserName (user)), true);
                            toRemove.Add (user.Id);
                        }
                        if (remindAboutKick && pair.Value.reminded == false) {
                            pair.Value.reminded = true;
                            await Program.messageControl.SendMessage (user, onRemindDM);
                        }
                    } else {
                        toRemove.Add (user.Id);
                        Logging.Log (Logging.LogType.BOT, "Purged manually removed user from younglings joinDate dictionary.");
                    }
                } else {
                    toRemove.Add (pair.Key);
                    Logging.Log (Logging.LogType.BOT, "Purge user who has left the server from joinDate dictionary.");
                }
            }

            foreach (ulong id in toRemove) {
                joinDate.Remove (id);
            }
            if (toRemove.Count > 0)
                SaveData ();
        }

        public Task OnHourPassed(DateTime time) {
            return Task.CompletedTask;
        }

        public Task OnDayPassed(DateTime time) {
            return Task.CompletedTask;
        }

        public Task OnSecondPassed(DateTime time) {
            return Task.CompletedTask;
        }

        public static async void ForceAcceptYoungling(SocketGuildUser user) {
            await Utility.SecureRemoveRole (user, Utility.GetServer ().GetRole (younglingRoleID));
            Program.messageControl.SendMessage (user, onAcceptedDM);
            Program.messageControl.SendMessage (Utility.GetMainChannel () as SocketTextChannel, onAcceptedPublicAnnouncement.Replace ("{USERNAME}", Utility.GetUserName (user)), true);
            joinDate.Remove (user.Id);
        }

        public class Youngling {
            public DateTime joinDate;
            public bool reminded;

            public Youngling(DateTime _joinDate) {
                joinDate = _joinDate;
            } 
        }
    }

    public class CAcceptYoungling : Command {
        public CAcceptYoungling() {
            command = "acceptyoungling";
            shortHelp = "Instantly accept youngling.";
            isAdminOnly = true;
            catagory = Category.Admin;

            AddOverload (typeof (SocketGuildUser), "Instantly accepts a youngling by ID.");
            AddOverload (typeof (SocketGuildUser), "Instantly accepts a youngling.");
            AddOverload (typeof (SocketGuildUser), "Instantly accepts a youngling by name.");
        }

        public Task<Result> Execute(SocketUserMessage e, ulong userID) {
            SocketGuildUser user = Utility.GetServer ().GetUser (userID);
            if (user != null) {
                SocketRole younglingRole = Utility.GetServer ().GetRole (Younglings.younglingRoleID);
                if (user.Roles.Contains (younglingRole)) {
                    Younglings.ForceAcceptYoungling (user);
                    return TaskResult (user, $"Succesfully accepted {Utility.GetUserName (user)} into full membership!");
                } else {
                    return TaskResult (null, "Failed to accept youngling - user not a youngling.");
                }
            } else {
                return TaskResult (null, "Failed to accept youngling - user not found");
            }
        }

        public Task<Result> Execute(SocketUserMessage e, SocketGuildUser user) {
            return Execute (e, user.Id);
        }

        public Task<Result> Execute(SocketUserMessage e, string username) {
            SocketGuildUser user = Utility.FindUserByName (Utility.GetServer (), username);
            return Execute (e, user.Id);
        }
    }

    public class CSetYoungling : Command {
        public CSetYoungling() {
            command = "setyoungling";
            shortHelp = "Force a user to be youngling.";
            catagory = Category.Admin;

            AddOverload (typeof (SocketGuildUser), "Forces a user by ID to become a yougling, as if they've joined at the given date.");
            AddOverload (typeof (SocketGuildUser), "Forces a given user to become a yougling, as if they've joined at the given date.");
            AddOverload (typeof (SocketGuildUser), "Forces a user by name to become a yougling, as if they've joined at the given date.");
        }

        public Task<Result> Execute(SocketUserMessage e, ulong userID, DateTime time) {
            SocketGuildUser user = Utility.GetServer ().GetUser (userID);
            if (user != null) {
                SocketRole younglingRole = Utility.GetServer ().GetRole (Younglings.younglingRoleID);
                Younglings.AddYoungling (user, time);
                return TaskResult (user, $"Succesfully set {Utility.GetUserName (user)} as youngling at set time.");
            } else {
                return TaskResult (null, "Failed to set youngling - User not found.");
            }
        }

        public Task<Result> Execute(SocketUserMessage e, SocketGuildUser user, DateTime time) {
            return Execute (e, user.Id, time);
        }

        public Task<Result> Execute(SocketUserMessage e, string username, DateTime time) {
            SocketGuildUser user = Utility.FindUserByName (Utility.GetServer (), username);
            return Execute (e, user.Id, time);
        }
    }
}
