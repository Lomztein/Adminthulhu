using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Adminthulhu
{
    public class Strikes : IClockable {

        public static ulong strikeRoleID = 272389160705327105;
        public static string strikeDataPath = "strikes";

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
            return strikes.ContainsKey (userID);
        }

        public Task Initialize(DateTime time) {

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
            return Program.GetServer ().GetRole (strikeRoleID);
        }

        public static void AddStrike(ulong user, DateTime time, TimeSpan span, string strikeReason) {
            if (strikes.ContainsKey (user)) {
                strikes [ user ].strikeTime.Add (span);
                strikes [ user ].reason += ", " + strikeReason;
            } else {
                strikes.Add (user, new Strike () { reason = strikeReason, strikeDate = time, strikeTime = span }); // Well these names are not confusing. I suppose this is why we make constructors.
            }
            SocketGuildUser socketUser = Program.GetServer ().GetUser (user);
            SocketRole role = GetRole ();

            if (!socketUser.Roles.Contains (role)) {
                Program.SecureAddRole (socketUser, role);
            }

            Program.messageControl.SendMessage (socketUser, "https://media.giphy.com/media/l2SpND5MD3Ig5QVc4/giphy.gif You've recieved a strike: " + strikeReason + 
                "\nYou will be unable to use some server features untill " + strikes[user].strikeDate.Add(strikes[user].strikeTime).ToString ());

            Save ();
        }

        public static void RaiseStrike(ulong user) {
            if (strikes.ContainsKey (user)) {
                SocketRole role = GetRole ();
                SocketGuildUser socketUser = Program.GetServer ().GetUser (user);

                Program.SecureRemoveRole (socketUser, role);
                strikes.Remove (user);

                Program.messageControl.SendMessage (socketUser, "Congratulations, you've served your time. You strike has been raised, and you have full access to features once more.");
            }

            Save ();
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
            name = "Strike Commands";
            help = "A set of commands specifically for strikes.";
            commandsInSet = new Command [ ] { new CAddStrike (), new CRemoveStrike () };

            isAdminOnly = true;
        }

        public class CAddStrike : Command {

            public CAddStrike() {
                command = "add";
                name = "Strike Someone";
                help = "Strike a user who've broken a rule.";
                argumentNumber = 3;
            }

            public override Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
                base.ExecuteCommand (e, arguments);
                if (AllowExecution (e, arguments)) {
                    ulong id;
                    if (ulong.TryParse (arguments [ 0 ], out id)) {
                        TimeSpan span = Clock.GetTimespan (arguments [ 1 ]);
                        Strikes.AddStrike (id, e.CreatedAt.ToLocalTime().DateTime, span, arguments[2]);
                    }
                }
                return Task.CompletedTask;
            }
        }

        public class CRemoveStrike : Command {

            public CRemoveStrike() {
                command = "remove";
                name = "Raise Strike";
                help = "Raises a strike from someone who doesn't deserve it anymore.";
                argumentNumber = 1;
            }

            public override Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
                base.ExecuteCommand (e, arguments);
                if (AllowExecution (e, arguments)) {
                    ulong id;
                    if (ulong.TryParse (arguments [ 0 ], out id)) {
                        Strikes.RaiseStrike (id);
                    }
                }
                return Task.CompletedTask;
            }
        }
    }
}
