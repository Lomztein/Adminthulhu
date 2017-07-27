using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Discord.WebSocket;

namespace Adminthulhu {

    public class BotConfiguration {

        public static Dictionary<string, object> settings = new Dictionary<string, object>();
        public static List<IConfigurable> allConfigurables = new List<IConfigurable> ();
        public static string settingsFileName = "configuration";

        public static void Initialize() {
            LoadSettings ();
            if (settings == null)
                settings = new Dictionary<string, object>();
            else
                SerializationIO.SaveObjectToFile (Program.dataPath + settingsFileName + "_BACKUP" + Program.gitHubIgnoreType, settings, true);
        }

        public static void PostInit() {
            SaveSettings ();
        }

        public static void LoadSettings() {
            settings = SerializationIO.LoadObjectFromFile<Dictionary<string, object>> (Program.dataPath + settingsFileName + Program.gitHubIgnoreType);
            if (settings == null) {
                Console.WriteLine ("!CRITICAL! - Settings file was not loaded, continuing now can whipe the current file! Write \"continue\" to continue.");
                while (Console.ReadLine ().ToLower () != "continue") { } // Halt program untill button pressed.
            }
        }

        public static void SaveSettings() {
            SerializationIO.SaveObjectToFile (Program.dataPath + settingsFileName + Program.gitHubIgnoreType, settings, true);
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
        public static T GetSetting<T>(string key, string oldKey, T fallback) {
            T obj;
            string [ ] path = key.Split ('.');
            // Search for uncatagorised value, in order to maintain backwards compatability.
            if (settings.ContainsKey (oldKey)) {
                T result = Utility.SecureConvertObject<T> (settings [oldKey]);
                PurgeSetting (oldKey);
                SetSetting (key, result);
                return result;
            } else {
                object result = null;
                Dictionary<string, object> dict = settings;
                for (int i = 0; i < path.Length; i++) {
                    if (i != path.Length - 1) {
                        if (dict.ContainsKey (path [ i ])) {
                            dict = Utility.SecureConvertObject<Dictionary<string, object>> (dict [ path [ i ] ]) as Dictionary<string, object>;
                        }

                        if (dict == null)
                            break;
                    } else if (dict.ContainsKey (path [ i ])) {
                        result = Utility.SecureConvertObject<T> (dict [ path [ i ] ]);
                    }
                }


                if (result == null) {
                    if (fallback != null) ChatLogger.Log ("WARNING: Failed to load setting " + key + ", returning fallback \"" + fallback.ToString () + "\"..");
                    SetSetting (key, fallback);
                    return fallback;
                } else {
                    SetSetting (key, result); // Resave in case of format changes
                    return Utility.SecureConvertObject<T> (result);
                }
            }
        }

        public static void SetSetting(string key, object value) {
            try {
                string [ ] path = key.Split ('.');

                Dictionary<string, object> dict = settings;
                for (int i = 0; i < path.Length; i++) {
                    if (i != path.Length - 1) {
                        if (dict.ContainsKey (path [ i ])) {
                            dict[path[i]] = Utility.SecureConvertObject<Dictionary<string, object>> (dict [ path [ i ] ]) as Dictionary<string, object>;
                            dict = dict [ path [ i ] ] as Dictionary<string, object>; // The memory abuse is real.
                        } else {
                            dict.Add (path [ i ], new Dictionary<string, object> ());
                            dict = dict [ path [ i ] ] as Dictionary<string, object>;
                        }
                    } else {
                        if (dict.ContainsKey (path [ i ])) {
                            dict [ path [ i ] ] = value;
                        } else {
                            dict.Add (path [ i ], value);
                        }
                    }
                }
            } catch (Exception e) {
                ChatLogger.Log (e.Message + " - " + e.StackTrace);
            }
        }

        public static void PurgeSetting(string key) {
            if (settings.ContainsKey (key)) {
                settings.Remove (key);
            }
            ChatLogger.Log ("Purged old config key: " + key);
        }
    }

    public class CReloadConfiguration : Command {
        public CReloadConfiguration() {
            command = "reloadconfig";
            shortHelp = "Reload bot configuration.";
            longHelp = "Reloads the configuration file and enables all changed settings.";
            argumentNumber = 0;
            catagory = Catagory.Admin;
        }

        public override Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                BotConfiguration.ReloadConfiguration ();
                Program.messageControl.SendMessage (e, "Configuration succesfully reloaded!", false);
            }
            return Task.CompletedTask;
        }
    }
}