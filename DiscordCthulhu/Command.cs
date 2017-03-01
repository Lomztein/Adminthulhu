using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {
    public class Command {

        public string command = null;
        public string name = null;
        public string help = null;
        public string argHelp = "";
        public int argumentNumber = 1;
        public string helpPrefix = Program.commandChar.ToString ();

        public bool isAdminOnly = false;
        public bool alwaysEnabled = false;

        public Dictionary<ulong, List<string>> enabledSettings = new Dictionary<ulong, List<string>>();

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

        public void AddToChannel (SocketMessage e, bool allChannels) {
            SocketGuild guild = (e.Channel as SocketGuildChannel).Guild;
            if (!enabledSettings.ContainsKey (guild.Id))
                enabledSettings.Add (guild.Id, new List<string> ());

            if (allChannels) {
                SocketTextChannel[] channels = guild.TextChannels.ToArray ();
                for (int i = 0; i < channels.Length; i++) {
                    if (!enabledSettings[guild.Id].Contains (channels[i].Name))
                        enabledSettings[guild.Id].Add (channels[i].Name);
                }
            } else {
                if (!enabledSettings[guild.Id].Contains (e.Channel.Name))
                    enabledSettings[guild.Id].Add (e.Channel.Name);
            }

            SaveSettings ();
        }

        public bool AvailableOnChannel (SocketMessage e) {
            SocketGuild guild = (e.Channel as SocketGuildChannel).Guild;
            if ((e.Channel as SocketDMChannel) != null) {
                return alwaysEnabled && !isAdminOnly;
            }else if (enabledSettings.ContainsKey (guild.Id)) {
                return enabledSettings[guild.Id].Contains (e.Channel.Name) || isAdminOnly;
            }
            return false;
        }

        public void RemoveFromChannel (SocketMessage e, bool allChannels) {
            if (enabledSettings.ContainsKey (e.SocketGuild.Name)) {
                if (allChannels) {
                    Channel[] channels = e.SocketGuild.TextChannels.ToArray ();
                    for (int i = 0; i < channels.Length; i++) {
                        if (enabledSettings[e.SocketGuild.Name].Contains (channels[i].Name))
                            enabledSettings[e.SocketGuild.Name].Remove (channels[i].Name);
                    }
                } else {
                    if (enabledSettings[e.SocketGuild.Name].Contains (e.Channel.Name))
                        enabledSettings[e.SocketGuild.Name].Remove (e.Channel.Name);
                }
            }

            SaveSettings ();
        }

        public virtual void ExecuteCommand ( SocketMessage e, List<string> arguments) {
            if (arguments.Count > 0 && arguments[0] == "?") {
                Program.messageControl.SendMessage(e, GetHelp ());
            }
        }

        public bool AllowExecution (SocketMessage e, List<string> args) {

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

        public virtual string GetHelp () {
            string argExists = argHelp.Length > 0 ? " " : "";
            string text = helpPrefix + command + " " + argExists + argHelp + " - " + help;
            //if (isAdminOnly)
            //    text += " - ADMIN ONLY";
            return text;
        }

        public virtual string GetShortHelp () {
            string argExists = argHelp.Length > 0 ? " " : "";
            string text = helpPrefix + command + argExists + argHelp;
            return text;
        }
    }
}
