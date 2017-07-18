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

        public static T GetSetting<T>(string key, T fallback) {
            T obj;
            if (settings.ContainsKey (key)) {

                try {
                    string possibleJSON = settings [ key ].ToString ();
                    obj = JsonConvert.DeserializeObject<T> (possibleJSON);
                    if (obj != null)
                        settings [ key ] = obj;
                } catch (Exception) {
                    try {
                        Type curType = settings [ key ].GetType ();
                        obj = (T)Convert.ChangeType (settings [ key ], typeof (T));
                    } catch (Exception) {
                        obj = (T)settings [ key ];
                    }
                }

                return obj;
            } else {
                ChatLogger.Log ("WARNING: Failed to load setting " + key + ", returning " + fallback.ToString () + "..");
                SetSetting (key, fallback);
                return fallback;
            }
        }

        public static void SetSetting(string key, object value) {
            if (settings.ContainsKey (key)) {
                settings [ key ] = value;
            } else {
                settings.Add (key, value);
            }
            SaveSettings ();
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