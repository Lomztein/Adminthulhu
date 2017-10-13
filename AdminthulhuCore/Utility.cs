using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using System.Globalization;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using System.Reflection;

namespace Adminthulhu {

    // A lot of these functions in here could be easily converted to extension methods. Would it be a good idea though?
    public static class Utility {

        public static List<SocketGuildUser> ForceGetUsers(ulong channelID) {
            SocketGuildChannel channel = GetServer ().GetChannel (channelID);
            List<SocketGuildUser> result = new List<SocketGuildUser> ();
            foreach (SocketGuildUser u in channel.Users) {
                result.Add (GetServer ().GetUser (u.Id));
            }
            return result;
        }

        private static int maxTries = 5;
        public static async Task SecureAddRole(SocketGuildUser user, SocketRole role) {
            int tries = 0;
            while (!user.Roles.Contains (role)) {
                if (tries > maxTries) {
                    break;
                }
                tries++;
                Logging.Log (Logging.LogType.BOT, "Adding role to " + user.Username + " - " + role.Name);
                await (user.AddRoleAsync (role));
                await Task.Delay (5000);
            }
        }

        public static async Task SecureRemoveRole(SocketGuildUser user, SocketRole role) {
            int tries = 0;
            while (user.Roles.Contains (role)) {
                if (tries > maxTries) {
                    break;
                }
                Logging.Log (Logging.LogType.BOT, "Removing role from " + user.Username + " - " + role.Name);
                tries++;
                await (user.RemoveRoleAsync (role));
                await Task.Delay (5000);
            }
        }

        public static List<string> ConstructArguments(string fullCommand, out string command) {
            string toSplit = fullCommand.Substring (fullCommand.IndexOf (' ') + 1);
            List<string> arguments = new List<string> ();
            command = "";

            if (fullCommand.LastIndexOf (' ') != -1) {
                // FEEL THE SPAGHETTI.
                command = fullCommand.Substring (0, fullCommand.Substring (1).IndexOf (' ') + 1);
                arguments.AddRange (SplitArgs (toSplit));



            } else {
                command = fullCommand;
            }

            return arguments;
        }

        public static string[] SplitArgs(string toSplit) {
            List<string> arguments = new List<string>();
            string arg;
            int balance = 0;
            int lastCut = 0;

            for (int i = 0; i < toSplit.Length; i++) {
                char cur = toSplit [ i ];

                switch (toSplit [ i ]) {
                    case ';':
                        if (balance == 0) {
                            arg = toSplit.Substring (lastCut, i - lastCut);
                            arguments.Add (arg);
                            lastCut = i + 1;
                        }
                        break;

                    case '(':
                        balance++;
                        break;

                    case ')':
                        balance--;
                        break;
                }
            }

            if (toSplit.Length > 0) {
                arguments.Add (toSplit.Substring (lastCut));
            }

            for (int i = 0; i < arguments.Count; i++) {
                arguments[i] = arguments [ i ].Trim (' ');
            }

            return arguments.ToArray ();
        }

        public static SocketGuild GetServer(SocketChannel channel) {
            return (channel as SocketGuildChannel).Guild;
        }

        public static string GetUserName(SocketGuildUser user) {
            if (user == null)
                return "[ERROR - NULL USER REFERENCE]";
            if (user.Nickname == null)
                return user.Username;
            return user.Nickname;
        }

        public static string GetUserUpdateName(SocketGuildUser beforeUser, SocketGuildUser afterUser, bool before) {
            if (before) {
                if (beforeUser.Nickname == null)
                    return afterUser.Username;
                return beforeUser.Nickname;
            } else {
                if (afterUser.Nickname == null)
                    return afterUser.Username;
                return afterUser.Nickname;
            }
        }

        public static SocketGuildChannel GetMainChannel() {
            return SearchChannel (Program.mainTextChannelName);
        }

        public static SocketGuildChannel SearchChannel(string name) {
            IEnumerable<SocketGuildChannel> channels = GetServer ().Channels;
            SoftStringComparer comparer = new SoftStringComparer ();
            foreach (SocketGuildChannel channel in channels) {
                try {
                    if (comparer.Equals (name, channel.Name))
                        return channel;
                } catch (Exception e) {
                    Logging.Log (e);
                }
            }
            return null;
        }

        public static SocketGuild GetServer() {
            if (Program.discordClient != null) {
                SocketGuild guild = Program.discordClient.GetGuild (Program.serverID);
                return guild;
            }
            return null;
        }

        // I thought the TrimStart and TrimEnd functions would work like this, which they may, but I couldn't get them working. Maybe I'm just an idiot, but whatever.
        private static string TrimSpaces(string input) {
            if (input.Length == 0)
                return " ";

            string trimmed = input;
            while (trimmed [ 0 ] == ' ') {
                trimmed = trimmed.Substring (1);
            }

            while (trimmed [ trimmed.Length - 1 ] == ' ') {
                trimmed = trimmed.Substring (0, trimmed.Length - 1);
            }
            return trimmed;
        }

        public static SocketGuildUser FindUserByName(SocketGuild server, string username) {
            foreach (SocketGuildUser user in server.Users) {
                string name = GetUserName (user).ToLower ();
                if (name.Length >= username.Length && name.Substring (0, username.Length) == username.ToLower ())
                    return user;
            }
            return null;
        }

        public static string GetChannelName(SocketMessage e) {
            if (e.Channel as SocketDMChannel != null) {
                SocketDMChannel dmChannel = (e.Channel) as SocketDMChannel;
                return $"[DM / {dmChannel.Recipient}] {e.Author.Username}";
            } else {
                return $"[{e.Channel.Name} / {e.Author.Username}]";
            }
        }

        public static async Task SetGame(string gameName) { // Wrapper functions ftw
            await Program.discordClient.SetGameAsync (gameName);
        }

        public static bool IsCorrectMessage(Cacheable<IUserMessage, ulong> message, SocketReaction reaction, ulong desiredMessage, string emojiName) {
            if (emojiName != null)
                return message.Id == desiredMessage && reaction.Emote.Name == emojiName;
            return message.Id == desiredMessage;
        }

        public static string FormatCommand(Command command, int minSpaces = 25) {
            return UniformStrings (command.GetCommand (), command.GetOnlyName (), " | ");
        }

        public static async Task AwaitFullBoot() {
            while (Program.FullyBooted () == false) {
                await Task.Delay (100);
            }
            return;
        }

        public static string UniformStrings (string firstString, string secondString, string connector, int minSpaces = 25) {
            string result = firstString;
            int remainingTaps = (int)Math.Floor ((minSpaces - result.Length) / 4d);
            int remainingSpaces = (minSpaces - result.Length) % 4;
            for (int i = 0; i < remainingTaps; i++)
                result += "\t";
            for (int i = 0; i < remainingSpaces; i++)
                result += " ";
            result += connector + secondString;
            return result;
        }

        public static T SecureConvertObject<T>(object input) {
            object obj;
            try {
                try {
                    Type curType = input.GetType ();
                    obj = (T)Convert.ChangeType (input, typeof (T));
                } catch (Exception) {
                    obj = (T)input;
                }
            } catch (Exception) {
                string possibleJSON = input.ToString ();
                obj = JsonConvert.DeserializeObject<T> (possibleJSON);
            }

            return (T)obj;
        }

        /// <summary>
        /// Formatted for the danish format!
        /// </summary>
        public static bool TryParseDatetime(string input, ulong userID, out DateTime result) {
            CultureInfo danishCulture = new CultureInfo (UserConfiguration.GetSetting<string>(userID, "Culture"));
            return DateTime.TryParse (input, danishCulture.DateTimeFormat, DateTimeStyles.None, out result);
        }

        public static bool TryParseSimpleTimespan(string input, out TimeSpan result) {
            int count;

            if (int.TryParse (input.Substring (0, input.Length - 1), out count)) {
                switch (input [ input.Length - 1 ]) {
                    case 'm':
                        result = new TimeSpan (0, count, 0);
                        break;

                    case 'h':
                        result = new TimeSpan (count, 0, 0);
                        break;

                    case 'd':
                        result = new TimeSpan (count, 0, 0, 0);
                        break;

                    case 'w':
                        result = new TimeSpan (count * 7, 0, 0, 0, 0);
                        break;

                    default:
                        throw new ArgumentException ("Unable to parse input, invalid identifying chararacter.");
                }
            }
            return result != null;
        }

        public static async Task<TextReader> DoJSONRequestAsync(WebRequest req) {
            try {
                var task = Task.Factory.FromAsync ((cb, o) => ((HttpWebRequest)o).BeginGetResponse (cb, o), res => ((HttpWebRequest)res.AsyncState).EndGetResponse (res), req);
                var result = await task;
                var resp = result;
                var stream = resp.GetResponseStream ();
                var sr = new StreamReader (stream);
                return sr;
            } catch {
                return null;
            }
        }

        public static async Task<TextReader> DoJSONRequestAsync(string url) {
            HttpWebRequest req = WebRequest.CreateHttp (url);
            req.AllowReadStreamBuffering = true;
            var tr = await DoJSONRequestAsync (req);
            return tr;
        }

        public static async Task<string> DoJSONRequestAsync(string url, string content, string json) {
            HttpWebRequest request = WebRequest.Create (url) as HttpWebRequest;
            request.ContentType = content;
            request.Method = "POST";

            using (StreamWriter writer = new StreamWriter (await request.GetRequestStreamAsync())) {
                writer.Write (json);

                await writer.FlushAsync ();
                writer.Close ();
            }

            HttpWebResponse response = await request.GetResponseAsync () as HttpWebResponse;
            string result = "";
            using (StreamReader reader = new StreamReader (response.GetResponseStream ())) {
                result = reader.ReadToEnd ();
            }
            return result;
        }

        public static object GetVariable(object input, string variableName) {
            FieldInfo info = input.GetType ().GetField (variableName);
            if (info != null) {
                return info.GetValue (input);
            } else {
                throw new Exception ("Field " + variableName + " in type " + input.GetType ().FullName + " not found.");
            }
        }

        public static T SelectRandom<T>(params T[] array) {
            Random random = new Random ();
            return array [ random.Next (0, array.Length) ];
        }
    }
}
