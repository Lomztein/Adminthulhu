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

        public override Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                if (Program.playerGroups.CreateGroup (e, arguments[0])) {
                    Program.messageControl.SendMessage (e, "Group " + arguments[0] + " has been created succesfully.");
                }else {
                    Program.messageControl.SendMessage (e, "Unable to create group: Group by that name already exists.");
                }
            }
            return Task.CompletedTask;
        }

    }
}
