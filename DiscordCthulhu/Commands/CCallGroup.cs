using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {
    class CCallGroup : Command {

        public CCallGroup () {
            command = "callgroup";
            name = "Call Group";
            argHelp = "<groupname>";
            help = "Calls the " + argHelp + " subgroup if it exists.";
            argumentNumber = 2;
        }

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            await base.ExecuteCommand (e, arguments);
            if (await AllowExecution (e, arguments)) {

                PlayerGroups.Group group = Program.playerGroups.FindGroupByName ((e.Channel as SocketGuildChannel).Guild.Name, arguments[0]);

                if (group != null) {
                    string toSend = "Calling group " + group.groupName + ": ";

                    for (int i = 0; i < group.memberMentions.Count; i++) {
                        toSend += group.memberMentions[i] + ", ";
                    }
                    toSend += "\n" + arguments[1];
                    await Program.messageControl.SendMessage (e, toSend);
                } else {
                    await Program.messageControl.SendMessage (e, "Group not found on this server.");
                }
            }
        }

    }
}
