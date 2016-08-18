using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    class CJoinGroup : Command {

        public CJoinGroup () {
            Initialize ();
            command = "joingroup";
            name = "Join Group";
            argHelp = "<groupname>";
            help = "Joins the group " + argHelp + " if it exists on this server.";
        }

        public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                if (Program.playerGroups.JoinGroup (e, arguments[0])) {
                    Program.messageControl.SendMessage (e, "Joined group succesfully.");
                } else {
                    Program.messageControl.SendMessage (e, "Failed to join group: Group doesn't exist on this server.");
                }
            }
        }
    }
}
