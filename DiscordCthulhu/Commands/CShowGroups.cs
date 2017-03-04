using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {
    class CShowGroups : Command {

        public CShowGroups () {
            command = "showgroups";
            name = "Show Groups";
            help = "Shows the name of all groups on this server.";
            argumentNumber = 0;
        }

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            await base.ExecuteCommand (e, arguments);
            if (await AllowExecution (e, arguments)) {

                if (Program.playerGroups.groups.ContainsKey ((e.Channel as SocketGuildChannel).Guild.Name)) {
                    string toSend = "Groups on this server: \n\n";
                    List<PlayerGroups.Group> groups = Program.playerGroups.groups[(e.Channel as SocketGuildChannel).Guild.Name];
                    for (int i = 0; i < groups.Count; i++) {
                        toSend += groups[i].groupName + " - " + groups[i].memberMentions.Count + " members.\n";
                    }

                    await Program.messageControl.SendMessage (e, toSend);
                } else {
                    await Program.messageControl.SendMessage (e, "No groups found on this server.");
                }
            }
        }
    }
}
