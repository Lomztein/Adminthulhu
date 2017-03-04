using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {

    public class CSetColor : Command {

        public string[] allowed = new string[] {
            "GREEN", "RED", "YELLOW", "BLUE",
            "ORANGE", "PINK", "PURPLE", "WHITE",
            "DARKBLUE", "TURQUOISE", "MAGENTA",
            "GOLD", "BLACK", "DARKRED", "BROWN"
        };

        public bool removePrevious = true;

        public string succesText = "Your color has now been set.";
        public string failText = "Color not found, these are the supported colors:\n";

        public CSetColor () {
            command = "setcolor";
            name = "Set Color";
            argHelp = "<colorname>";
            help = "Sets your color to " + argHelp + ", if available.";
            argumentNumber = 1;
        }

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                if (allowed.Contains (arguments[0].ToUpper ())) {
                    SocketRole[] roles = (e.Channel as SocketGuildChannel).Guild.Roles.Where (x => allowed.Contains(x.Name)).ToArray ();
                    SocketRole toAdd = roles.Where (x => x.Name == arguments[0].ToUpper ()).ElementAt(0);

                    SocketGuildUser user = e.Author as SocketGuildUser;
                    foreach (SocketRole role in roles) {
                        if (user.Roles.Contains (role)) {
                            await user.RemoveRolesAsync (role);
                        }
                    }

                    await user.AddRolesAsync (toAdd);
                    
                } else {
                    string colors = "";
                    for (int i = 0; i < allowed.Length; i++) {
                        colors += allowed[i] + ", ";
                    }
                    Program.messageControl.SendMessage(e, failText + colors);
                }
            }
        }
    }
}
