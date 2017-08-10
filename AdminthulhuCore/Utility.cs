﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using System.Globalization;
using Newtonsoft.Json;
using System.Net;
using System.IO;

namespace Adminthulhu {
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
                    Program.messageControl.SendMessage (SearchChannel (GetServer (), Program.dumpTextChannelName) as SocketTextChannel, "Error - tried to add role too many times.", false);
                    break;
                }
                tries++;
                ChatLogger.Log ("Adding role to " + user.Username + " - " + role.Name);
                await (user.AddRoleAsync (role));
                await Task.Delay (5000);
            }
        }

        public static async Task SecureRemoveRole(SocketGuildUser user, SocketRole role) {
            int tries = 0;
            while (user.Roles.Contains (role)) {
                if (tries > maxTries) {
                    Program.messageControl.SendMessage (SearchChannel (GetServer (), Program.dumpTextChannelName) as SocketTextChannel, "Error - tried to remove role too many times.", false);
                    break;
                }
                ChatLogger.Log ("Removing role from " + user.Username + " - " + role.Name);
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
                string [ ] loc = toSplit.Split (';');
                for (int i = 0; i < loc.Length; i++) {

                    loc [ i ] = TrimSpaces (loc [ i ]);
                    arguments.Add (loc [ i ]);
                }
            } else {
                command = fullCommand;
            }

            return arguments;
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
            return SearchChannel (GetServer (), Program.mainTextChannelName);
        }

        [Obsolete]
        public static SocketGuildChannel GetChannelByName(SocketGuild server, string name) {
            if (server == null)
                return null;

            SocketGuildChannel channel = SearchChannel (server, name);
            return channel;
        }

        public static SocketGuildChannel SearchChannel(SocketGuild server, string name) {
            IEnumerable<SocketGuildChannel> channels = server.Channels;
            foreach (SocketGuildChannel channel in channels) {
                if (channel.Name.Length >= name.Length && channel.Name.Substring (0, name.Length) == name)
                    return channel;
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
                return "Private message: " + e.Author.Username;
            } else {
                return (e.Channel as SocketGuildChannel).Guild.Name + "/" + e.Channel.Name + "/" + e.Author.Username;
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

        public static T SecureConvertObject<T>(object obj) {
            try {
                string possibleJSON = obj.ToString ();
                obj = JsonConvert.DeserializeObject<T> (possibleJSON);
            } catch (Exception) {
                try {
                    Type curType = obj.GetType ();
                    obj = (T)Convert.ChangeType (obj, typeof (T));
                } catch (Exception) {
                    obj = (T)obj;
                }
            }

            return (T)obj;
        }

        /// <summary>
        /// Formattet for the danish format!
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
                }
            }
            return result != null;
        }

        public static async Task<TextReader> DoJSONRequestAsync(WebRequest req) {
            var task = Task.Factory.FromAsync ((cb, o) => ((HttpWebRequest)o).BeginGetResponse (cb, o), res => ((HttpWebRequest)res.AsyncState).EndGetResponse (res), req);
            var result = await task;
            var resp = result;
            var stream = resp.GetResponseStream ();
            var sr = new StreamReader (stream);
            return sr;
        }

        public static async Task<TextReader> DoJSONRequestAsync(string url) {
            HttpWebRequest req = WebRequest.CreateHttp (url);
            req.AllowReadStreamBuffering = true;
            var tr = await DoJSONRequestAsync (req);
            return tr;
        }
    }
}