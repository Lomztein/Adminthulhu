using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    class CLeaveGroup : Command {

        public CLeaveGroup () {
            command = "leavegroup";
            name = "Leave Group";
            argHelp = "<groupname>";
            help = "Leaves the group " + argHelp + ".";
        }

        public override void ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                PlayerGroups.Group group = Program.playerGroups.FindGroupByName (e.SocketGuild.Name, arguments[0]);
                if (Program.playerGroups.LeaveGroup (e, arguments[0])) {
                    Program.messageControl.SendMessage (e, "You have left the group " + arguments[0]);
                    if (group == null) {
                        Program.messageControl.SendMessage (e, "Group is empty and has been automatically removed.");
                    }
                } else {
                    Program.messageControl.SendMessage (e, "Either you are not a member, or that group doesn't exist.");
                }
            }
        }
    }
}
