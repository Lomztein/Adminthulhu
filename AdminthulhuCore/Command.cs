using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Adminthulhu {
    public class Command {

        public enum Catagory {
            None, Utility, Fun, Set, Admin
        }

        public const int CHARS_TO_HELP = 4;

        public string command = null;
        public string shortHelp = null;
        public string longHelp = null;
        public string argHelp = "";
        public int argumentNumber = 1;
        public string helpPrefix = Program.commandChar.ToString ();
        public Catagory catagory = Catagory.None;

        public bool isAdminOnly = false;
        public bool availableInDM = false;
        public bool availableOnServer = true;
        public bool commandEnabled = false;

        public virtual Task ExecuteCommand ( SocketUserMessage e, List<string> arguments) {
            return Task.CompletedTask;
        }

        public bool AllowExecution (SocketMessage e, List<string> args, bool returnMessage = true) {

            if (commandEnabled == false)
                return false;

            if (!availableInDM && e.Channel as SocketDMChannel != null) {
                if (returnMessage)
                    Program.messageControl.SendMessage (e.Channel, "Failed to execute - Not available in DM channels.", false);
                return false;
            }

            if (isAdminOnly && !(e.Author as SocketGuildUser).GuildPermissions.Administrator) {
                if (returnMessage)
                    Program.messageControl.SendMessage (e.Channel, "Failed to execute - User is not admin.", false);
                return false;
            }

            if (!availableOnServer && e.Channel as SocketGuildChannel != null) {
                if (returnMessage)
                    Program.messageControl.SendMessage (e.Channel, "Failed to execute - Not avaiable on server.", false);
                return false;
            }

            if (returnMessage && args.Count != argumentNumber) { // Arguably a little dirty, but it'll do what it needs to do.
                Program.messageControl.SendMessage (e.Channel, "Failed to execute - Wrong number of arguments.", false);
                return false;
            }

            return true;
        }

        public virtual void Initialize () {
            commandEnabled = BotConfiguration.GetSetting<bool> ("Command" + command.Substring (0,1).ToUpper () + command.Substring (1) + "Enabled", false); // Gotta capitalize that shite.
        }

        public virtual string GetHelp () {
            string argExists = argHelp.Length > 0 ? " " : "";
            string text = "`" + helpPrefix + command + argExists + argHelp + " - " + longHelp;
            if (isAdminOnly)
                text += " - ADMIN ONLY";
            return text + "`";
        }

        public virtual string GetShortHelp () {
            string argExists = argHelp.Length > 0 ? " " : "";
            string text = "`" + shortHelp + "\t\t" + helpPrefix + command + argExists + argHelp + "`";
            return text;
        }

        public virtual string GetCommand() {
            return helpPrefix + command;
        }

        public virtual string GetOnlyName() {
            return shortHelp; // Wrapper functions ftw
        }
    }
}
