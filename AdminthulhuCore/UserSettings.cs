using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using System.Globalization;
using Newtonsoft.Json;

namespace Adminthulhu {
    public static class UserSettings {

        public static Dictionary<ulong, List<Setting>> userSettings;
        public static string settingsFileName = "usersettings";

        public static void Initialize () {
            userSettings = SerializationIO.LoadObjectFromFile<Dictionary<ulong, List<Setting>>> (Program.dataPath + settingsFileName + Program.gitHubIgnoreType);
            if (userSettings == null)
                userSettings = new Dictionary<ulong, List<Setting>> ();
        }

        public static void SaveSettings () {
            SerializationIO.SaveObjectToFile (Program.dataPath + settingsFileName + Program.gitHubIgnoreType, userSettings);
        }

        public static T GetSetting<T> (ulong userID, string key, object defaultValue) {
            if (userSettings.ContainsKey (userID)) {
                List<Setting> set = userSettings[userID];
                foreach (Setting s in set) {
                    if (s.name == key) {
                        Newtonsoft.Json.Linq.JObject obj = s.value as Newtonsoft.Json.Linq.JObject;
                        if (obj != null) {
                            s.value = JsonConvert.DeserializeObject<T> (obj.ToString ());
                        }
                        return (T)s.value;
                    }
                }
            }

            return (T)defaultValue;
        }

        public static void SetSetting ( ulong userID, string key, object value ) {
            if (userSettings.ContainsKey (userID)) {
                List<Setting> set = userSettings[userID];
                foreach (Setting s in set) {
                    if (s.name == key) {
                        s.value = value;
                        userSettings [ userID ] = set;
                        SaveSettings ();
                        return;
                    }
                }

                set.Add (new Setting (key, value));
                userSettings [ userID ] = set;
                SaveSettings ();
                return;
            }

            userSettings.Add (userID, new List<Setting> ());
            userSettings [userID].Add (new Setting (key, value));
            SaveSettings ();
        }

        public static bool ToggleBoolean(ulong userID, string key) {
            bool newSetting = !GetSetting<bool> (userID, key, false);
            SetSetting (userID, key, newSetting);
            return newSetting;
        }

        public class Setting {
            public string name;
            public object value;

            public Setting (string _name, object _value) {
                name = _name;
                value = _value;
            }
        }
    }

    public class UserSettingsCommands : CommandSet {
        public UserSettingsCommands () {
            command = "settings";
            shortHelp = "User settings command set.";
            longHelp = "A set of commands about user settings.";
            commandsInSet = new Command[] { new CReminderTime (), new CSetBirthday (), new CSetCulture (), new CToggleRole (), new CToggleInternational (), new CAutomaticLooking (),
            new CToggleSnooping () };
        }

        public class CReminderTime : Command {

            public CReminderTime () {
                command = "evt";
                shortHelp = "Event reminder timespan.";
                argHelp = "<time in hours>";
                longHelp = "Change time reminds about events to" + argHelp + ". Works in hours.";
                argumentNumber = 1;
            }

            public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
                base.ExecuteCommand (e, arguments);
                if (AllowExecution (e, arguments)) {
                    int number;
                    if (int.TryParse (arguments[0], out number)) {
                        UserSettings.SetSetting (e.Author.Id, "EventRemindTime", number);
                        Program.messageControl.SendMessage (e, "You have succesfully changed remind timespan to **" + number.ToString () + "**.", false);
                    }else {
                        Program.messageControl.SendMessage (e, "Failed to change event remind timespan", false);
                    }
                }
            return Task.CompletedTask;
            }
        }

        public class CSetBirthday : Command {

            public CSetBirthday () {
                command = "birthday";
                shortHelp = "Set birthday.";
                argHelp = "<date (d-m-y h:m:s)>";
                longHelp = "Set your birthday date to " + argHelp + ", so we know when to congratulate you!";
                argumentNumber = 1;
            }

            public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
                base.ExecuteCommand (e, arguments);
                if (AllowExecution (e, arguments)) {
                    DateTime parse;
                    if (Utility.TryParseDatetime (arguments[0], e.Author.Id, out parse)) {
                        Birthdays.SetBirthday (e.Author.Id, parse);
                        CultureInfo info = new CultureInfo (UserSettings.GetSetting<string> (e.Author.Id, "Culture", "da-DK"));
                        Program.messageControl.SendMessage (e, "You have succesfully set your birthday to **" + parse.ToString (info) + "**.", false);
                    } else {
                        Program.messageControl.SendMessage (e, "Failed to set birthday - could not parse date.", false);
                    }
                }
            return Task.CompletedTask;
            }
        }

        public class CSetCulture : Command {
            public CSetCulture() {
                command = "culture";
                shortHelp = "Set culture.";
                argHelp = "<culture (language-COUNTRY)>";
                longHelp = "Sets your preferred culture. Used for proper formatting of stuff such as datetime.";
                argumentNumber = 1;
            }

            public override Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
                base.ExecuteCommand (e, arguments);
                if (AllowExecution (e, arguments)) {
                    try {
                        CultureInfo info = new CultureInfo (arguments [ 0 ]);
                        UserSettings.SetSetting (e.Author.Id, "Culture", arguments [ 0 ]);
                    } catch (CultureNotFoundException) {
                        Program.messageControl.SendMessage (e, "Failed to set culture - culture **" + arguments[0] + "** not found.", false);
                    }
                }
                return Task.CompletedTask;
            }
        }

        public class CAutomaticLooking : Command {
            public CAutomaticLooking() {
                command = "autolooking";
                shortHelp = "Toggle automatic !looking command.";
                longHelp = "Toggles automatically enabling the !looking command.";
                argumentNumber = 1;
            }

            public override Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
                base.ExecuteCommand (e, arguments);
                if (AllowExecution (e, arguments)) {
                    bool result = UserSettings.ToggleBoolean (e.Author.Id, "AutoLooking");
                    Program.messageControl.SendMessage (e, "Autolooking on voice channels enabled: " + result.ToString (), false);
                }
                return Task.CompletedTask;
            }
        }

        public class CToggleSnooping : Command {
            public CToggleSnooping() {
                command = "snooping";
                shortHelp = "Toggle Adminthulhu snooping.";
                longHelp = "Disables non-critical Adminthulhu snooping on you, if toggled off.";
                argumentNumber = 1;
            }

            public override Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
                base.ExecuteCommand (e, arguments);
                if (AllowExecution (e, arguments)) {
                    bool result = UserSettings.ToggleBoolean (e.Author.Id, "AllowSnooping");
                    Program.messageControl.SendMessage (e, "Adminthulhu snooping enabled: " + result.ToString (), false);
                }
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// A generic command, defaults to NSFW.
        /// </summary>
        public class CToggleRole : Command {
            public ulong roleID = 266882682930069504;
            public CToggleRole () {
                command = "nsfw";
                shortHelp = "Toggle NSFW access.";
                longHelp = "Toggles access to NSFW channels, by removing or adding the @Pervert role to you.";
                argumentNumber = 0;
            }

            public override Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
                base.ExecuteCommand (e, arguments);
                if (AllowExecution (e, arguments)) {
                    SocketRole role = Utility.GetServer ().GetRole (roleID);
                    if ((e.Author as SocketGuildUser).Roles.Contains (role)) {
                        Utility.SecureRemoveRole (e.Author as SocketGuildUser, role);
                    } else {
                        Utility.SecureAddRole (e.Author as SocketGuildUser, role);
                    }
                }
                return Task.CompletedTask;
            }
        }

        public class CToggleInternational : CToggleRole {
            public CToggleInternational() {
                command = "international";
                shortHelp = "Toggle international marker";
                longHelp = "Toggles the international marker on you. The international marker lets people know you don't speak danish.";
                argumentNumber = 0;
                roleID = 182563086186577920;
            }
        }
    }
}
