using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {

    public class CSetColor : Command {

        public string[] allowed = new string[] {
            "GREEN", "RED", "YELLOW", "BLUE",
            "ORANGE", "PINK", "PURPLE", "WHITE",
            "DARKBLUE", "TURQUOISE", "MAGENTA",
            "GOLD"
        };

        public bool removePrevious = true;

        public string succesText = "Your color has now been set.";
        public string failText = "Color not found, these are the supported colors:\n";

        public CSetColor () {
            command = "setcolor";
            name = "Set Color";
            help = "\"!setcolor <colorname>\" - Sets your color to colorname, if available.";
            argumentNumber = 1;
        }

        public override async void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (arguments)) {

                if (allowed.Contains (arguments[0].ToUpper ())) {
                    Role[] roles = e.Server.FindRoles (arguments[0].ToUpper (), true).ToArray ();

                    if (roles.Length == 1) {
                        Role role = roles[0];

                        if (!role.Permissions.ManageRoles) {

                            if (removePrevious) {
                                List<Role> rList = new List<Role> ();
                                for (int i = 0; i < allowed.Length; i++) {
                                    rList.Add (e.Server.FindRoles (allowed[i], true).ToList ()[0]);
                                }

                                int removeTries = 5;
                                for (int i = 0; i < removeTries; i++) {
                                    await e.User.RemoveRoles (rList.ToArray ());
                                }
                            }

                            await e.User.AddRoles (role);
                            await e.Channel.SendMessage (succesText);
                        }
                    }
                } else {
                    string colors = "";
                    for (int i = 0; i < allowed.Length; i++) {
                        colors += allowed[i] + ", ";
                    }
                    await e.Channel.SendMessage (failText + colors);
                }
            }
        }
    }
}
