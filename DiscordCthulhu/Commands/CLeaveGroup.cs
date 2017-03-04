using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {
    class CLeaveGroup : Command {

        public CLeaveGroup () {
            command = "leavegroup";
            name = "Leave Group";
            argHelp = "<groupname>";
            help = "Leaves the group " + argHelp + ".";
        }

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            await base.ExecuteCommand (e, arguments);
            if (await AllowExecution (e, arguments)) {

                PlayerGroups.Group group = Program.playerGroups.FindGroupByName ((e.Channel as SocketGuildChannel).Guild.Name, arguments[0]);
                if (await Program.playerGroups.LeaveGroup (e, arguments[0])) {
                    await Program.messageControl.SendMessage (e, "You have left the group " + arguments[0]);
                    if (group == null) {
                        await Program.messageControl.SendMessage (e, "Group is empty and has been automatically removed.");
                    }
                } else {
                    await Program.messageControl.SendMessage (e, "Either you are not a member, or that group doesn't exist.");
                }
            }
        }
    }
}
