using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    public class Command {

        public string command = null;
        public string name = null;
        public string help = null;
        public string argHelp = "";
        public int argumentNumber = 1;

        public bool isAdminOnly = false;
        public bool alwaysEnabled = false;

        public Dictionary<string, List<string>> enabledSettings = new Dictionary<string, List<string>>();

        public void SaveSettings () {
            SerializationIO.SaveObjectToFile (Program.dataPath + Program.commandSettingsDirectory + command + Program.gitHubIgnoreType, enabledSettings);
        }

        public static Dictionary<string, List<string>> LoadSettings (string commandName) {
            Dictionary<string, List<string>> settings = SerializationIO.LoadObjectFromFile<Dictionary<string, List<string>>> (Program.dataPath + Program.commandSettingsDirectory + commandName + Program.gitHubIgnoreType);
            if (settings == null) {
                return new Dictionary<string, List<string>> ();
            } else {
                return settings;
            }
        }

        // Add and remove commands functions are very similar, however they are too different
        // for a general function to really be worth it.

        public void AddToChannel (MessageEventArgs e, bool allChannels) {
            if (!enabledSettings.ContainsKey (e.Server.Name))
                enabledSettings.Add (e.Server.Name, new List<string> ());

            if (allChannels) {
                Channel[] channels = e.Server.TextChannels.ToArray ();
                for (int i = 0; i < channels.Length; i++) {
                    if (!enabledSettings[e.Server.Name].Contains (channels[i].Name))
                        enabledSettings[e.Server.Name].Add (channels[i].Name);
                }
            } else {
                if (!enabledSettings[e.Server.Name].Contains (e.Channel.Name))
                    enabledSettings[e.Server.Name].Add (e.Channel.Name);
            }

            SaveSettings ();
        }

        public bool AvailableOnChannel (MessageEventArgs e) {
            if (e.Channel.IsPrivate) {
                return alwaysEnabled && !isAdminOnly;
            }else if (enabledSettings.ContainsKey (e.Server.Name)) {
                return enabledSettings[e.Server.Name].Contains (e.Channel.Name);
            }
            return false;
        }

        public void RemoveFromChannel (MessageEventArgs e, bool allChannels) {
            if (enabledSettings.ContainsKey (e.Server.Name)) {
                if (allChannels) {
                    Channel[] channels = e.Server.TextChannels.ToArray ();
                    for (int i = 0; i < channels.Length; i++) {
                        if (enabledSettings[e.Server.Name].Contains (channels[i].Name))
                            enabledSettings[e.Server.Name].Remove (channels[i].Name);
                    }
                } else {
                    if (enabledSettings[e.Server.Name].Contains (e.Channel.Name))
                        enabledSettings[e.Server.Name].Remove (e.Channel.Name);
                }
            }

            SaveSettings ();
        }

        public virtual void ExecuteCommand ( MessageEventArgs e, List<string> arguments) {
            if (arguments.Count > 0 && arguments[0] == "?") {
                Program.messageControl.SendMessage(e, GetHelp ());
            }
        }

        public bool AllowExecution (MessageEventArgs e, List<string> args) {

            if (e.Channel.IsPrivate && (isAdminOnly && !alwaysEnabled)) {
                Program.messageControl.SendMessage (e, "Failed to execute: Not available in private chat.");
                return false;
            }

            if (argumentNumber != args.Count) {
                if (!(args.Count == 1 && args[0] == "?"))
                    Program.messageControl.SendMessage (e, "Failed to execute: Wrong number of arguments.");
                return false;
            }

            if (isAdminOnly && !e.User.GetPermissions (e.Channel).ManageChannel) {
                Program.messageControl.SendMessage (e, "Failed to execute: Command is admin-only.");
                return false;
            }

            if (!isAdminOnly && !alwaysEnabled && !AvailableOnChannel (e)) {
                Program.messageControl.SendMessage (e, "Command not available on this server or channel.");
                return false;
            }

            return true;
        }

        public virtual void Initialize () {
            enabledSettings = LoadSettings (command);
        }

        public string GetHelp () {
            string argExists = argHelp.Length > 0 ? " " : "";
            string text = "\"" + Program.commandChar + command + "\"" + argExists + argHelp + "\" - " + help;
            //if (isAdminOnly)
            //    text += " - ADMIN ONLY";
            return text;
        }

        public string GetShortHelp () {
            string argExists = argHelp.Length > 0 ? " " : "";
            string text = "\"" + Program.commandChar + command + "\"" + argExists + argHelp;
            return text;
        }
    }
}
