using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
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

        public static T GetSetting<T> (ulong userID, string name, object defaultValue) {
            if (userSettings.ContainsKey (userID)) {
                List<Setting> set = userSettings[userID];
                foreach (Setting s in set) {
                    if (s.name == name) {
                        return (T)s.value;
                    }
                }
            }

            return (T)defaultValue;
        }

        public static void SetSetting ( ulong userID, string name, object value ) {
            if (userSettings.ContainsKey (userID)) {
                List<Setting> set = userSettings[userID];
                foreach (Setting s in set) {
                    if (s.name == name) {
                        s.value = value;
                        return;
                    }
                }

                set.Add (new Setting (name, value));
                return;
            }

            userSettings.Add (userID, new List<Setting> ());
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
            command = "usersettings";
            name = "User Settings Command Set";
            help = "A set of commands about user settings.";
            commandsInSet = new Command[] { new CReminderTime () };
        }

        public class CReminderTime : Command {

            public CReminderTime () {
                command = "eventremindtimespan";
                name = "Event Remind Timespan";
                argHelp = "<time>";
                help = "Change time reminds about events to" + argHelp + ". Works in hours.";
                argumentNumber = 1;
            }

            public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
                base.ExecuteCommand (e, arguments);
                if (AllowExecution (e, arguments)) {
                    int number;
                    if (int.TryParse (arguments[0], out number)) {
                        UserSettings.SetSetting (e.User.Id, "EventRemindTime", number);
                        Program.messageControl.SendMessage (e, "You have succesfully changed remind timespan to **" + number.ToString () + "**.");
                    }else {
                        Program.messageControl.SendMessage (e, "Failed to change event remind timespan");
                    }
                }
            }
        }
    }
}
