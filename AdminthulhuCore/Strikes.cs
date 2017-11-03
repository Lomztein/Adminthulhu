using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Adminthulhu
{
    public class Strikes : IClockable, IConfigurable {

        public static ulong strikeRoleID = 272389160705327105;
        public static string strikeDataPath = "strikes";
        public static string strikeGivenMessage = "";
        public static string strikeRaisedMessage = "";

        public static Dictionary<ulong, Strike> strikes;

        private static void Save() {
            SerializationIO.SaveObjectToFile (Program.dataPath + strikeDataPath + Program.gitHubIgnoreType, strikes);
        }

        private static void Load() {
            strikes = SerializationIO.LoadObjectFromFile<Dictionary<ulong, Strike>> (Program.dataPath + strikeDataPath + Program.gitHubIgnoreType);
            if (strikes == null)
                strikes = new Dictionary<ulong, Strike> ();
        }

        public static bool IsStricken(ulong userID) {
            if (strikes == null)
                return false;
            return strikes.ContainsKey (userID);
        }

        public Task Initialize(DateTime time) {
            LoadConfiguration ();
            BotConfiguration.AddConfigurable (this);
            Load ();
            return Task.CompletedTask;
        }

        public Task OnSecondPassed(DateTime time) {
            return Task.CompletedTask;
        }

        public Task OnMinutePassed(DateTime time) {
            int count = strikes.Count;
            for (int i = 0; i < count; i++) {
                KeyValuePair<ulong, Strike> strike = strikes.ElementAt (0);
                Strike s = strike.Value;
                if (s.strikeDate.Add (s.strikeTime) < time) {
                    RaiseStrike (strike.Key);
                }
            }
            return Task.CompletedTask;
        }

        public Task OnHourPassed(DateTime time) {
            return Task.CompletedTask;
        }

        public Task OnDayPassed(DateTime time) {
            return Task.CompletedTask;
        }

        private static SocketRole GetRole() {
            return Utility.GetServer ().GetRole (strikeRoleID);
        }

        public static void AddStrike(ulong user, DateTime time, TimeSpan span, string strikeReason) {
            if (strikes.ContainsKey (user)) {
                strikes [ user ].strikeTime.Add (span);
                strikes [ user ].reason += ", " + strikeReason;
            } else {
                strikes.Add (user, new Strike () { reason = strikeReason, strikeDate = time, strikeTime = span }); // Well these names are not confusing. I suppose this is why we make constructors.
            }
            SocketGuildUser socketUser = Utility.GetServer ().GetUser (user);
            SocketRole role = GetRole ();

            if (!socketUser.Roles.Contains (role)) {
                Utility.SecureAddRole (socketUser, role);
            }

            Program.messageControl.SendMessage (socketUser, strikeGivenMessage.Replace ("{STRIKEREASON}", strikeReason).Replace ("{STRIKERAISEDATE}", strikes[user].strikeDate.Add(strikes[user].strikeTime).ToString ()));
            AutoCommands.RunEvent (AutoCommands.Event.UserStricken, user.ToString (), time.ToString (), strikeReason);

            Save ();
        }

        public static void RaiseStrike(ulong user) {
            if (strikes.ContainsKey (user)) {
                SocketRole role = GetRole ();
                SocketGuildUser socketUser = Utility.GetServer ().GetUser (user);

                Utility.SecureRemoveRole (socketUser, role);
                strikes.Remove (user);

                Program.messageControl.SendMessage (socketUser, strikeRaisedMessage);
                AutoCommands.RunEvent (AutoCommands.Event.UserStricken, user.ToString());
            }

            Save ();
        }

        public void LoadConfiguration() {
            strikeRoleID = BotConfiguration.GetSetting<ulong> ("Roles.StrikeID", this, 0);
            strikeGivenMessage = BotConfiguration.GetSetting ("Strikes.OnGivenMessage", this, "Sorry, but you've recieved a strike due to {STRIKEREASON}. This strike will automatically be raised on {STRIKERAISEDATE}");
            strikeRaisedMessage = BotConfiguration.GetSetting ("Strikes.OnRaisedMessage", this, "You've done your time, and your strike has now been risen.");
        }

        public class Strike {

            public string reason;
            public DateTime strikeDate;
            public TimeSpan strikeTime;

        }
    }

    public class StrikeCommandSet : CommandSet {
        public StrikeCommandSet() {
            command = "strikes";
            shortHelp = "Strike commands.";
            commandsInSet = new Command [ ] { new CAddStrike (), new CRemoveStrike () };
            catagory = Category.Admin;
            isAdminOnly = true;
        }

        public class CAddStrike : Command {

            public CAddStrike() {
                command = "add";
                shortHelp = "Strike someone unlawful.";
                AddOverload (typeof (ulong), "Strike a user who've broken a rule.");
            }

            public Task<Result> Execute(SocketUserMessage e, ulong id, string timespan, string reason) {
                TimeSpan span;

                if (Utility.TryParseSimpleTimespan (timespan, out span)) {
                    Strikes.AddStrike (id, e.CreatedAt.ToLocalTime ().DateTime, span, reason);
                    return TaskResult (id, "Succesfully stroke user by ID.");
                } else {
                    return TaskResult (0, "Failed to add strike - Could not parse timespan.");
                }
            }
        }

        public class CRemoveStrike : Command {

            public CRemoveStrike() {
                command = "remove";
                shortHelp = "Raise strike.";
                AddOverload (typeof (ulong), "Raises a strike from someone who doesn't deserve it anymore.");
            }

            public Task<Result>Execute(SocketUserMessage e, ulong id) {
                    Strikes.RaiseStrike (id);
                return TaskResult (id, "Succesfully gave " + id + " a strike.");
            }
        }
    }
}
