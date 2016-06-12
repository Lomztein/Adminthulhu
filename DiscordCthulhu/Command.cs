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
        public int argumentNumber = 1;

        public bool isAdminOnly = false;

        public virtual void ExecuteCommand ( MessageEventArgs e, List<string> arguments) {
            if (arguments.Count > 0 && arguments[0] == "?") {
                Program.messageControl.SendMessage(e, help);
            }
        }

        public bool AllowExecution (MessageEventArgs e, List<string> args) {

            if (argumentNumber != args.Count) {
                Program.messageControl.SendMessage (e, "Failed to execute: Wrong number of arguments.");
                return false;
            }

            if (isAdminOnly && !e.User.GetPermissions (e.Channel).ManageChannel) {
                Program.messageControl.SendMessage (e, "Failed to execute: Command is admin-only.");
                return false;
            }

            return true;
        }
    }
}
