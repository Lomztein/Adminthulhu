using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {
    class CJoinGroup : Command {

        public CJoinGroup () {
            command = "joingroup";
            name = "Join Group";
            argHelp = "<groupname>";
            help = "Joins the group " + argHelp + " if it exists on this server.";
        }

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            await base.ExecuteCommand (e, arguments);
            if (await AllowExecution (e, arguments)) {

                if (await Program.playerGroups.JoinGroup (e, arguments[0])) {
                    await Program.messageControl.SendMessage (e, "Joined group succesfully.");
                } else {
                    await Program.messageControl.SendMessage (e, "Failed to join group: Group doesn't exist on this server.");
                }
            }
        }
    }
}
