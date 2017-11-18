using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Discord.WebSocket;
using System.Text.RegularExpressions;

namespace Adminthulhu {

    public class BotConfiguration {

        public static Dictionary<string, object> settings = new Dictionary<string, object> ();
        public static List<IConfigurable> allConfigurables = new List<IConfigurable> ();
        public static string settingsFileName = "configuration";
        public static Dictionary<string, Entry> allEntries = new Dictionary<string, Entry> ();

        public static void Initialize() {
            LoadSettings ();
            if (settings == null)
                settings = new Dictionary<string, object> ();
            else
                SerializationIO.SaveObjectToFile (Program.dataPath + settingsFileName + "_BACKUP" + Program.gitHubIgnoreType, settings, true, false);
        }

        public static void PostInit() {
            SaveSettings ();
        }

        public static void LoadSettings() {
            settings = SerializationIO.LoadObjectFromFile<Dictionary<string, object>> (Program.dataPath + settingsFileName + Program.gitHubIgnoreType);
        }

        public static void SaveSettings() {
            SerializationIO.SaveObjectToFile (Program.dataPath + settingsFileName + Program.gitHubIgnoreType, settings, true, false);
        }

        public static void ReloadConfiguration() {
            LoadSettings ();
            foreach (IConfigurable configurable in allConfigurables) {
                configurable.LoadConfiguration ();
            }
        }

        public static void AddConfigurable(IConfigurable configurable) {
            if (!allConfigurables.Contains (configurable)) {
                allConfigurables.Add (configurable);
            }
        }

        // This feels very wrong..
        public static T GetSetting<T>(string key, IConfigurable gettingConfigurable, T fallback) {
            if (gettingConfigurable != null) {
                if (!allEntries.ContainsKey (key)) // This seems borderline useless and very slow. Fits the rest I guess then lel.
                    allEntries.Add (key, new Entry (key, gettingConfigurable));
                else
                    allEntries [ key ].AddConfigurable (gettingConfigurable);
            }

            string [ ] path = key.Split ('.');

            object result = null;
            Dictionary<string, object> dict = settings;
            for (int i = 0; i < path.Length; i++) {
                if (i != path.Length - 1) {
                    if (dict.ContainsKey (path [ i ])) {
                        try { // Avoid trying to cast when its much more likely to require JSON parsing, since casting is quite slow.
                            dict = JsonConvert.DeserializeObject<Dictionary<string, object>> (dict[path[i]].ToString ());
                        } catch (Exception) {
                            dict = Utility.SecureConvertObject<Dictionary<string, object>> (dict [ path [ i ] ]);
                        }
                    }

                    if (dict == null)
                        break;
                } else if (dict.ContainsKey (path [ i ])) {
                    result = Utility.SecureConvertObject<T> (dict [ path [ i ] ]);
                }
            }

            if (result == null) {
                if (fallback != null)
                    Logging.Log (Logging.LogType.WARNING, "Failed to load setting " + key + ", returning fallback \"" + fallback.ToString () + "\"..");
                SetSetting (key, fallback);
                return fallback;
            } else {
                Logging.Log (Logging.LogType.CONFIG, $"Loading configuration key: {key} - {result}");
                return Utility.SecureConvertObject<T> (result);
            }
        }

        public static bool SetSetting(string key, object value, bool allowNew = true) {
            bool success = false;
            try {
                string [ ] path = key.Split ('.');

                Dictionary<string, object> dict = settings;
                for (int i = 0; i < path.Length; i++) {
                    if (i != path.Length - 1) {
                        if (dict.ContainsKey (path [ i ])) {
                            dict [ path [ i ] ] = Utility.SecureConvertObject<Dictionary<string, object>> (dict [ path [ i ] ]) as Dictionary<string, object>;
                            dict = dict [ path [ i ] ] as Dictionary<string, object>; // The memory abuse is real.
                        } else {
                            if (allowNew) {
                                dict.Add (path [ i ], new Dictionary<string, object> ());
                                dict = dict [ path [ i ] ] as Dictionary<string, object>;
                            }
                        }
                    } else {
                        if (dict.ContainsKey (path [ i ])) {
                            dict [ path [ i ] ] = value;
                            success = true;
                        } else {
                            if (allowNew) {
                                dict.Add (path [ i ], value);
                            }
                        }
                    }
                }

            } catch (Exception e) {
                Logging.Log (Logging.LogType.EXCEPTION, e.Message + " - " + e.StackTrace);
            }
            return success;
        }

        public static List<Entry> RegexSearchEntries(string pattern) {
            Regex regex = new Regex (pattern);
            List<Entry> matches = new List<Entry> ();
            foreach (var entry in allEntries) {
                if (regex.IsMatch (entry.Key)) {
                    matches.Add (entry.Value);
                }
            }
            return matches;
        }

        public static bool HasSetting(string key) {
            string [ ] path = key.Split ('.');

            Dictionary<string, object> dict = settings;
            for (int i = 0; i < path.Length; i++) {
                if (dict.ContainsKey (path [ i ])) {
                    if (i != path.Length - 1) {
                        if (dict.ContainsKey (path [ i ])) {
                            dict [ path [ i ] ] = Utility.SecureConvertObject<Dictionary<string, object>> (dict [ path [ i ] ]) as Dictionary<string, object>;
                            dict = dict [ path [ i ] ] as Dictionary<string, object>; // The memory abuse is real.
                        } else {
                            return false;
                        }
                    } else {
                        if (dict.ContainsKey (path [ i ])) {
                            return true;
                        } else {
                            return false;
                        }
                    }
                }
            }

            return false;
        }

        public static void PurgeSetting(string key) {
            if (settings.ContainsKey (key)) {
                settings.Remove (key);
            }
            Logging.Log (Logging.LogType.CONFIG, "Purged old config key: " + key);
        }

        public class Entry {

            public string name;
            public List<IConfigurable> affectedConfigurables = new List<IConfigurable> ();

            public Entry(string _name, IConfigurable _starting) {
                name = _name;
                affectedConfigurables.Add (_starting);
            }

            public void AddConfigurable(IConfigurable configurable) {
                if (!affectedConfigurables.Contains (configurable))
                    affectedConfigurables.Add (configurable);
            }

            public void ReloadConfigurables() {
                foreach (IConfigurable configurable in affectedConfigurables) {
                    configurable.LoadConfiguration ();
                }
            }
        }
    }

    public class CReloadConfiguration : Command {
        public CReloadConfiguration() {
            command = "reloadconfig";
            shortHelp = "Reload bot configuration.";
            catagory = Category.Admin;
            isAdminOnly = true;

            AddOverload (typeof (object), "Reloads the configuration file and enables all changed settings.");
        }

        public Task<Result> Execute (SocketUserMessage e) {
                BotConfiguration.ReloadConfiguration ();
                return TaskResult (null, "Configuration succesfully reloaded!");
        }
    }

    public class CSetSetting : Command {
        public CSetSetting() {
            command = "setconfig";
            shortHelp = "Set a config value.";
            catagory = Category.Admin;
            isAdminOnly = true;

            AddOverload (typeof (object), "Edit bot config entries to <input> using a regex expression. Using !reloadconfig after changes is recommended. More advanced data structures must be set in files.");
        }

        public Task<Result> Execute(SocketUserMessage e, string expression, object input) {

            List<BotConfiguration.Entry> toModify = BotConfiguration.RegexSearchEntries (expression);
            IEnumerable<string> names = toModify.Select (x => x.name);

            Program.messageControl.SendMessage (e.Channel, names.ToArray ().Singlify (), false, "```");
            int succesful = 0;

            Program.messageControl.AskQuestion (e.Channel.Id, "Confirm edit of these configuration entries?", delegate () {
                foreach (string entry in names) {
                    object current = BotConfiguration.GetSetting (entry, null, default (object));
                    object possibleJSON = null;
                    try {
                        possibleJSON = JsonConvert.DeserializeObject (current.ToString ());
                    } catch { }

                    object newObject = null;
                    try {
                        newObject = Convert.ChangeType (input, current.GetType ());
                        BotConfiguration.SetSetting (entry, newObject, false);
                        succesful++;
                    } catch (Exception exception) {
                        Logging.Log (exception);
                    }
                }

                BotConfiguration.SaveSettings ();
                Program.messageControl.SendMessage (e, $"Succesfully edited {succesful} out of {toModify.Count} entries.", false);

                List<BotConfiguration.Entry> reloaded = new List<BotConfiguration.Entry> (); 
                foreach (var entry in toModify) {
                    if (!reloaded.Contains (entry)) {
                        entry.ReloadConfigurables ();
                        reloaded.Add (entry);
                    }
                }
            });

            return TaskResult (null, "");
        }
    }
}