using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Adminthulhu
{
    public static class LegalJunk
    {
        public static Dictionary<ulong, bool> acceptedLegalJunk = new Dictionary<ulong, bool> ();

        private static string legalBabble = "Due to the Discord API's ToS (Terms of Service), I am required to tell you that by joining this server **({SERVER_NAME})**, or any server which runs this botcode, you agree to allowing this bot to collect your End User Data. End User Data in this sense is data such as usernames, user ID's, user avatars and user messages, and possibly more. None of the information gathered is otherwise unavailable to non-bot users, and can be accessed by anyone running Discord in Developer Mode. Additionally and necceseraily according to the ToS, all End User Data is encrypted. More information can currently be found here: https://www.reddit.com/r/discordapp/comments/6sq866/the_developer_terms_of_service_have_just_been/";
        private static string fileName = "legaljunk";

        private static void SaveData() {
        SerializationIO.SaveObjectToFile (Program.dataPath + fileName + Program.gitHubIgnoreType, acceptedLegalJunk);
        }

        private static void LoadData() {
            acceptedLegalJunk = SerializationIO.LoadObjectFromFile<Dictionary<ulong, bool>> (Program.dataPath + fileName + Program.gitHubIgnoreType);
        }

        public static async void Initialize() {
            LoadData ();
            if (acceptedLegalJunk == null)
                acceptedLegalJunk = new Dictionary<ulong, bool> ();

            while (!Program.FullyBooted ()) {
                await Task.Delay (1000); // This method should really be replaced by something like AwaitFullBoot or something.
            }

            IEnumerable<SocketGuildUser> allUsers = Utility.GetServer ().Users;
            int missingPeople = allUsers.Count () - acceptedLegalJunk.Where (x => Utility.GetServer ().GetUser (x.Key) != null).Count ();
            if (missingPeople != 0) {
                Logging.Log (Logging.LogType.WARNING, acceptedLegalJunk.Count + " accepting users found, sending messages to all remaining " + missingPeople + " users in 5 seconds..");
                await Task.Delay (5000);
            }

            foreach (SocketGuildUser u in allUsers) {
                await AttemptSendLegalBabble (u);
            }

            Program.discordClient.UserJoined += async (user) => {
                await AttemptSendLegalBabble (user);
                SaveData ();
            };
            SaveData ();
        }

        public static async Task AttemptSendLegalBabble(SocketGuildUser u) {
            if (!acceptedLegalJunk.ContainsKey (u.Id)) {
                acceptedLegalJunk.Add (u.Id, false);
            }

            if (acceptedLegalJunk [ u.Id ] == false) {
                await Program.messageControl.SendMessage (u, legalBabble.Replace ("{SERVER_NAME}", Program.serverName));
                acceptedLegalJunk [ u.Id ] = true;
            }
        }
    }
}
