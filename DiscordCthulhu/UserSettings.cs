using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {
    public static class UserSettings {

        public static Dictionary<ulong, List<Setting>> userSettings;
        public static string settingsFileName = "usersettings";

        public static async Task Initialize () {
            userSettings = await SerializationIO.LoadObjectFromFile<Dictionary<ulong, List<Setting>>> (Program.dataPath + settingsFileName + Program.gitHubIgnoreType);
            if (userSettings == null)
                userSettings = new Dictionary<ulong, List<Setting>> ();
        }

        public static async Task SaveSettings () {
            await SerializationIO.SaveObjectToFile (Program.dataPath + settingsFileName + Program.gitHubIgnoreType, userSettings);
        }

        public static T GetSetting<T> (ulong userID, string key, object defaultValue) {
            if (userSettings.ContainsKey (userID)) {
                List<Setting> set = userSettings[userID];
                foreach (Setting s in set) {
                    if (s.name == key) {
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
                        return;
                    }
                }

                set.Add (new Setting (key, value));
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
            commandsInSet = new Command[] { new CReminderTime (), new CSetBirthday (), new CSetAlias () };
        }

        public class CReminderTime : Command {

            public CReminderTime () {
                command = "eventremindtimespan";
                name = "Event Remind Timespan";
                argHelp = "<time>";
                help = "Change time reminds about events to" + argHelp + ". Works in hours.";
                argumentNumber = 1;
            }

            public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
                await base.ExecuteCommand (e, arguments);
                if (await AllowExecution (e, arguments)) {
                    int number;
                    if (int.TryParse (arguments[0], out number)) {
                        UserSettings.SetSetting (e.Author.Id, "EventRemindTime", number);
                        await Program.messageControl.SendMessage (e, "You have succesfully changed remind timespan to **" + number.ToString () + "**.");
                    }else {
                        await Program.messageControl.SendMessage (e, "Failed to change event remind timespan");
                    }
                }
            }
        }

        public class CSetBirthday : Command {

            public CSetBirthday () {
                command = "setbirthday";
                name = "Set Birthday";
                argHelp = "<date>";
                help = "Set your birthday date to " + argHelp + ", so we know when to congratulate you!";
                argumentNumber = 1;
            }

            public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
                await base.ExecuteCommand (e, arguments);
                if (await AllowExecution (e, arguments)) {
                    DateTime parse;
                    if (DateTime.TryParse (arguments[0], out parse)) {
                        Birthdays.SetBirthday (e.Author.Id, parse);
                        await Program.messageControl.SendMessage (e, "You have succesfully set your birthday to **" + parse.ToString () + "**.");
                    } else {
                        await Program.messageControl.SendMessage (e, "Failed to set birthday - could not parse date.");
                    }
                }
            }
        }

    }
}
