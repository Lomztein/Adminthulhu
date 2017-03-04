using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {
    class CDeleteGroup : Command {

        public CDeleteGroup () {
            command = "deletegroup";
            name = "Delete Group";
            argHelp = "<groupname>";
            help = "Deletes the " + argHelp + " subgroup if it exists.";
            argumentNumber = 1;
            isAdminOnly = true;
        }

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            await base.ExecuteCommand (e, arguments);
            if (await AllowExecution (e, arguments)) {

                PlayerGroups.Group group = Program.playerGroups.FindGroupByName ((e.Channel as SocketGuildChannel).Guild.Name, arguments[0]);
                if (group != null) {
                    Program.playerGroups.groups[(e.Channel as SocketGuildChannel).Guild.Name].Remove (group);
                    await Program.messageControl.SendMessage (e, "Group \"" + arguments[0] + "\" succesfully deleted.");
                }else {
                    await Program.messageControl.SendMessage (e, "Group \"" + arguments[0] + "\" not found.");
                }
            }
        }
    }
}
