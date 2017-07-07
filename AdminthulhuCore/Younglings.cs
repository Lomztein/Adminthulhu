using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Rest;

namespace Adminthulhu
{
    public class Younglings : IClockable {

        public static ulong younglingRoleID = 316171882867064843;
        private static Dictionary<ulong, DateTime> joinDate;

        public Task Initialize(DateTime time) {
            LoadData ();
            if (joinDate == null)
                joinDate = new Dictionary<ulong, DateTime> ();

            return Task.CompletedTask;
        }

        public static void OnUserJoined(SocketGuildUser user) {
            try {
                SocketRole role = Utility.GetServer ().GetRole (younglingRoleID);
                Utility.SecureAddRole (user, role);

                if (joinDate.ContainsKey (user.Id))
                    joinDate.Remove (user.Id);
                joinDate.Add (user.Id, DateTime.Now);
                SaveData ();
            } catch (Exception e) {
                ChatLogger.DebugLog (e.Message);
            }
        }

        public static void SaveData() {
            SerializationIO.SaveObjectToFile (Program.dataPath + "younglings" + Program.gitHubIgnoreType, joinDate);
        }

        public static void LoadData() {
            joinDate = SerializationIO.LoadObjectFromFile <Dictionary<ulong, DateTime>> (Program.dataPath + "younglings" + Program.gitHubIgnoreType);
        }

        public async Task OnDayPassed(DateTime time) {
            SocketRole younglingRole = Utility.GetServer ().GetRole (younglingRoleID);
            SocketRole presentRole = Utility.GetServer ().GetRole (UserActivityMonitor.presentUserRole);
            foreach (SocketGuildUser user in Utility.GetServer ().Users) {
                if (user.Roles.Contains (younglingRole) && user.Roles.Contains (presentRole)) {
                    try {
                        RestInviteMetadata metadata = await Utility.GetMainChannel ().CreateInviteAsync (null, 1, false, true);
                        await Program.messageControl.SendMessage (user, "Sorry mon, but you've been automatically kicked from **" + Program.serverName + "** due to going inactive within the first two weeks. If you feel this is by mistake (which happens blame Lomztein), or you just want back in, feel free to use the following invite link: " + metadata.Url + "\nThe invite will be valid for a month after this message. If the invite is broken, or you ran out of time, you can get a new link from any member of the server.");
                        await user.KickAsync ();
                    }catch (Exception e) {
                        ChatLogger.DebugLog (e.Message);
                    }
                }

                if (user.Roles.Contains (younglingRole)) {
                    if (joinDate.ContainsKey (user.Id)) {
                        if (time > joinDate[user.Id].AddDays (14)) {
                            await Utility.SecureRemoveRole (user, younglingRole);
                            await Program.messageControl.SendMessage (user, "Congratulations! You are no longer a youngling since you've been active here for two weeks, and you are now allowed permanemt membership on " + Program.serverName + "! Just in time too, Bananakin Skywanker was just about to go ham on you younglings.");
                            joinDate.Remove (user.Id);
                            SaveData ();
                        }
                    }
                }
            }
        }

        public Task OnHourPassed(DateTime time) {
            return Task.CompletedTask;
        }

        public Task OnMinutePassed(DateTime time) {
            return Task.CompletedTask;
        }

        public Task OnSecondPassed(DateTime time) {
            return Task.CompletedTask;
        }

        public static async void ForceAcceptYoungling(SocketGuildUser user) {
            await Utility.SecureRemoveRole (user, Utility.GetServer ().GetRole (younglingRoleID));
            joinDate.Remove (user.Id);
        }
    }

    public class CAcceptYoungling : Command {
        public CAcceptYoungling() {
            command = "acceptyoungling";
            shortHelp = "Instantly accept youngling.";
            longHelp = "Instantly accepts a youngling, in case it's someone already known for a while.";
            argHelp = "<younglingID>";
            argumentNumber = 1;
            isAdminOnly = true;
        }

        public override Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                ulong id;
                if (ulong.TryParse (arguments [ 0 ], out id)) {
                    SocketGuildUser user = Utility.GetServer ().GetUser (id);
                    if (user != null) {
                        SocketRole younglingRole = Utility.GetServer ().GetRole (Younglings.younglingRoleID);
                        if (user.Roles.Contains (younglingRole)) {
                            Younglings.ForceAcceptYoungling (user);
                            Program.messageControl.SendMessage (user, "Congratulations! You've manually been given permanemt membership on " + Program.serverName + "!");
                        } else {
                            Program.messageControl.SendMessage (e, "Failed to accept youngling - user not a youngling.", false);
                        }
                    } else {
                        Program.messageControl.SendMessage (e, "Failed to accept youngling - user not found.", false);
                    }
                } else {
                    Program.messageControl.SendMessage (e, "Failed to accept youngling - user failed to parse ID.", false);
                }
            }
            return Task.CompletedTask;
        }
    }
}
