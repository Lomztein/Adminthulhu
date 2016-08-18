using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    class CDeleteGroup : Command {

        public CDeleteGroup () {
            Initialize ();
            command = "deletegroup";
            name = "Delete Group";
            argHelp = "<groupname>";
            help = "Deletes the " + argHelp + " subgroup if it exists.";
            argumentNumber = 1;
            isAdminOnly = true;
        }

        public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                PlayerGroups.Group group = Program.playerGroups.FindGroupByName (e.Server.Name, arguments[0]);
                if (group != null) {
                    Program.playerGroups.groups[e.Server.Name].Remove (group);
                    Program.messageControl.SendMessage (e, "Group \"" + arguments[0] + "\" succesfully deleted.");
                }else {
                    Program.messageControl.SendMessage (e, "Group \"" + arguments[0] + "\" not found.");
                }
            }
        }
    }
}
