using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {
    class CCreateGroup : Command {

        public CCreateGroup () {
            command = "creategroup";
            name = "Create Group";
            argHelp = "<groupname>";
            help = "Creates a subgroup with the name " + argHelp + ".";
            isAdminOnly = true;
        }

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            await base.ExecuteCommand (e, arguments);
            if (await AllowExecution (e, arguments)) {

                if (await Program.playerGroups.CreateGroup (e, arguments[0])) {
                    await Program.messageControl.SendMessage (e, "Group " + arguments[0] + " has been created succesfully.");
                }else {
                    await Program.messageControl.SendMessage (e, "Unable to create group: Group by that name already exists.");
                }
            }
        }

    }
}
