using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Rest;

namespace Adminthulhu
{
    public class Younglings : IClockable {

        private static ulong younglingRoleID = 316171882867064843;
        private static Dictionary<ulong, DateTime> joinDate;

        public Task Initialize(DateTime time) {
            LoadData ();
            if (joinDate == null)
                joinDate = new Dictionary<ulong, DateTime> ();

            return Task.CompletedTask;
        }

        public static void OnUserJoined(SocketGuildUser user) {
            Console.WriteLine ("On user joined.");
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
                        RestInviteMetadata metadata = await Utility.GetMainChannel (Utility.GetServer ()).CreateInviteAsync (null, 1);
                        await Program.messageControl.SendMessage (user, "Sorry mon, but you've been automatically kicked from **" + Program.serverName + "** due to going inactive within the first month. If you feel this is by mistake (which happens blame Lomztein), or you just want back in, feel free to use the following invite link: " + metadata.Url);
                        await user.KickAsync ();
                    }catch (Exception e) {
                        ChatLogger.DebugLog (e.Message);
                    }
                }

                if (user.Roles.Contains (younglingRole)) {
                    if (joinDate.ContainsKey (user.Id)) {
                        if (time > joinDate[user.Id].AddMonths (1)) {
                            await Utility.SecureRemoveRole (user, younglingRole);
                            await Program.messageControl.SendMessage (user, "Congratulations! You are no longer a youngling since you've been active here for a month, and you are now allowed permanemt membership on this server! Just in time too, Bananakin Skywalker was just about to go ham on you younglings.");
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
    }
}
