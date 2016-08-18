using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    class CCallGroup : Command {

        public CCallGroup () {
            Initialize ();
            command = "callgroup";
            name = "Call Group";
            argHelp = "<groupname>";
            help = "Calls the " + argHelp + " subgroup if it exists.";
            argumentNumber = 2;
        }

        public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                PlayerGroups.Group group = Program.playerGroups.FindGroupByName (e.Server.Name, arguments[0]);

                if (group != null) {
                    string toSend = "Calling group " + group.groupName + ": ";

                    for (int i = 0; i < group.memberMentions.Count; i++) {
                        toSend += group.memberMentions[i] + ", ";
                    }
                    toSend += "\n" + arguments[1];
                    Program.messageControl.SendMessage (e, toSend);
                } else {
                    Program.messageControl.SendMessage (e, "Group not found on this server.");
                }
            }
        }

    }
}
