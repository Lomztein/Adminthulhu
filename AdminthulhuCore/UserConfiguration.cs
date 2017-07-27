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
    public class UserConfiguration : IConfigurable {

        public static Dictionary<ulong, List<Setting>> userSettings;
        public static string settingsFileName = "usersettings";
        public static List<UserSettingsCommandBase> commands = new List<UserSettingsCommandBase> ();
        public static Dictionary<string, object> defaultValues = new Dictionary<string, object> ();

        public static void Initialize() {
            userSettings = SerializationIO.LoadObjectFromFile<Dictionary<ulong, List<Setting>>> (Program.dataPath + settingsFileName + Program.gitHubIgnoreType);
            if (userSettings == null)
                userSettings = new Dictionary<ulong, List<Setting>> ();

            UserConfiguration config = new UserConfiguration ();
            config.LoadConfiguration ();
        }

        public static void SaveSettings() {
            SerializationIO.SaveObjectToFile (Program.dataPath + settingsFileName + Program.gitHubIgnoreType, userSettings);
        }

        public static void AddCommand(UserSettingsCommandBase command) {
            commands.Add (command);
        }

        public static T GetSetting<T>(ulong userID, string key) {
            if (userSettings.ContainsKey (userID)) {
                List<Setting> set = userSettings [ userID ];
                foreach (Setting s in set) {
                    if (s.name == key) {
                        return Utility.SecureConvertObject<T> (s.value);
                    }
                }
            }

          return (T)Convert.ChangeType (defaultValues[key], typeof (T));
        }

        public static void SetSetting(ulong userID, string key, object value) {
            if (userSettings.ContainsKey (userID)) {
                List<Setting> set = userSettings [ userID ];
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
            userSettings [ userID ].Add (new Setting (key, value));
            SaveSettings ();
        }

        public static bool ToggleBoolean(ulong userID, string key) {
            bool newSetting = !GetSetting<bool> (userID, key);
            SetSetting (userID, key, newSetting);
            return newSetting;
        }

        public void LoadConfiguration() {
            foreach (UserSettingsCommandBase settingBase in commands) {
                defaultValues.Add (settingBase.key, BotConfiguration.GetSetting ("UserSettings." + settingBase.key + "Default", "", settingBase.superDefaultValue));
            }
        }

        public class Setting {
            public string name;
            public object value;

            public Setting(string _name, object _value) {
                name = _name;
                value = _value;
            }
        }
    }

    public class UserSettingsCommandBase : Command { // It is said that names close to each other is a bad idea. /shrug

        public string key;
        public object superDefaultValue; // The value default from the code.

        public override void Initialize() {
            base.Initialize ();
            UserConfiguration.AddCommand (this);
        }
    }

    public class UserSettingsCommands : CommandSet {
        public UserSettingsCommands() {
            command = "settings";
            shortHelp = "User settings command set.";
            longHelp = "A set of commands about user settings.";
            commandsInSet = new Command [ ] { new CReminderTime (), new CSetBirthday (), new CSetCulture (), new CToggleRole (), new CToggleInternational (), new CAutomaticLooking (),
            new CToggleSnooping (), new CAutoManageGameRoles () };
        }

        public class CReminderTime : UserSettingsCommandBase {

            public CReminderTime() {
                command = "evt";
                shortHelp = "Event reminder timespan.";
                argHelp = "<time in hours>";
                longHelp = "Change time reminds about events to" + argHelp + ". Works in hours.";
                key = "EventRemindTime";
                superDefaultValue = 2;
                argumentNumber = 1;
            }

            public override Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
                base.ExecuteCommand (e, arguments);
                if (AllowExecution (e, arguments)) {
                    int number;
                    if (int.TryParse (arguments [ 0 ], out number)) {
                        UserConfiguration.SetSetting (e.Author.Id, "EventRemindTime", number);
                        Program.messageControl.SendMessage (e, "You have succesfully changed remind timespan to **" + number.ToString () + "**.", false);
                    } else {
                        Program.messageControl.SendMessage (e, "Failed to change event remind timespan", false);
                    }
                }
                return Task.CompletedTask;
            }
        }

        public class CSetBirthday : UserSettingsCommandBase {

            public CSetBirthday() {
                command = "birthday";
                shortHelp = "Set birthday.";
                argHelp = "<date (d-m-y h:m:s)>";
                longHelp = "Set your birthday date to " + argHelp + ", so we know when to congratulate you!";
                argumentNumber = 1;
                key = "Birthday";
                superDefaultValue = null;
            }

            public override Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
                base.ExecuteCommand (e, arguments);
                if (AllowExecution (e, arguments)) {
                    DateTime parse;
                    if (Utility.TryParseDatetime (arguments [ 0 ], e.Author.Id, out parse)) {
                        Birthdays.SetBirthday (e.Author.Id, parse);
                        CultureInfo info = new CultureInfo (UserConfiguration.GetSetting<string> (e.Author.Id, "Culture"));
                        Program.messageControl.SendMessage (e, "You have succesfully set your birthday to **" + parse.ToString (info) + "**.", false);
                    } else {
                        Program.messageControl.SendMessage (e, "Failed to set birthday - could not parse date.", false);
                    }
                }
                return Task.CompletedTask;
            }
        }

        public class CSetCulture : UserSettingsCommandBase {
            public CSetCulture() {
                command = "culture";
                shortHelp = "Set culture.";
                argHelp = "<culture (language-COUNTRY)>";
                longHelp = "Sets your preferred culture. Used for proper formatting of stuff such as datetime.";
                key = "Culture";
                superDefaultValue = "da-DK";
                argumentNumber = 1;
            }

            public override Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
                base.ExecuteCommand (e, arguments);
                if (AllowExecution (e, arguments)) {
                    try {
                        CultureInfo info = new CultureInfo (arguments [ 0 ]);
                        UserConfiguration.SetSetting (e.Author.Id, "Culture", arguments [ 0 ]);
                    } catch (CultureNotFoundException) {
                        Program.messageControl.SendMessage (e, "Failed to set culture - culture **" + arguments [ 0 ] + "** not found.", false);
                    }
                }
                return Task.CompletedTask;
            }
        }

        public class CAutomaticLooking : UserSettingsCommandBase {
            public CAutomaticLooking() {
                command = "autolooking";
                shortHelp = "Toggle automatic !looking command.";
                longHelp = "Toggles automatically enabling the !looking command.";
                argumentNumber = 1;
                key = "AutoLooking";
                superDefaultValue = false;
            }

            public override Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
                base.ExecuteCommand (e, arguments);
                if (AllowExecution (e, arguments)) {
                    bool result = UserConfiguration.ToggleBoolean (e.Author.Id, "AutoLooking");
                    Program.messageControl.SendMessage (e, "Autolooking on voice channels enabled: " + result.ToString (), false);
                }
                return Task.CompletedTask;
            }
        }

        public class CToggleSnooping : UserSettingsCommandBase {
            public CToggleSnooping() {
                command = "snooping";
                shortHelp = "Toggle Adminthulhu snooping.";
                longHelp = "Disables non-critical bot snooping on you, if toggled off.";
                key = "AllowSnooping";
                superDefaultValue = true;
                argumentNumber = 0;
            }

            public override Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
                base.ExecuteCommand (e, arguments);
                if (AllowExecution (e, arguments)) {
                    bool result = UserConfiguration.ToggleBoolean (e.Author.Id, "AllowSnooping");
                    Program.messageControl.SendMessage (e, "Bot snooping enabled: " + result.ToString (), false);
                }
                return Task.CompletedTask;
            }
        }

        public class CAutoManageGameRoles : UserSettingsCommandBase {
            public CAutoManageGameRoles() {
                command = "autoroles";
                shortHelp = "Toggle automanage game roles.";
                longHelp = "Determines if game roles will be added to you automatically.";
                argumentNumber = 0;
                key = "AutoManageGameRoles";
                superDefaultValue = false;
            }

            public override Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
                base.ExecuteCommand (e, arguments);
                if (AllowExecution (e, arguments)) {
                    bool result = UserConfiguration.ToggleBoolean (e.Author.Id, "AutoManageGameRoles");
                    Program.messageControl.SendMessage (e, "Auto roles enabled: " + result.ToString (), false);
                }
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// A generic command, defaults to NSFW.
        /// </summary>
        public class CToggleRole : Command {
            public ulong roleID = 266882682930069504;
            public CToggleRole() {
                command = "nsfw";
                shortHelp = "Toggle NSFW access.";
                longHelp = "Toggles access to NSFW channels, by removing or adding the NSFW role to you.";
                argumentNumber = 0;
            }

            public override async Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
                await base.ExecuteCommand (e, arguments);
                if (AllowExecution (e, arguments)) {
                    SocketRole role = Utility.GetServer ().GetRole (roleID);
                    if ((e.Author as SocketGuildUser).Roles.Contains (role)) {
                        await Utility.SecureRemoveRole (e.Author as SocketGuildUser, role);
                        Program.messageControl.SendMessage (e, "Succesfully removed " + command + " role.", false);
                    } else {
                        await Utility.SecureAddRole (e.Author as SocketGuildUser, role);
                        Program.messageControl.SendMessage (e, "Succesfully added " + command + " role.", false);
                    }
                }
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