using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Adminthulhu {
    public class CCommandList : Command {

        public CCommandList () {
            command = "clist";
            name = "Command List";
            help = "Reveals a full list of all commands.";
            argumentNumber = 0;

            availableInDM = true;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                List<string> toDisplay = new List<string> ();
                List<string> adminOnly = new List<string> ();
                int minSpaces = 30;

                toDisplay.Add ("```");
                adminOnly.Add ("\nADMIN COMMANDS:");

                foreach (Command c in Program.commands) {
                    if (c.AllowExecution (e, null, false)) {
                        if (c.isAdminOnly) {
                            adminOnly.Add (Utility.FormatCommand (c));
                        } else {
                            toDisplay.Add (Utility.FormatCommand (c));
                        }
                    }
                }

                adminOnly.Add ("```");
                string commands = "";
                for (int i = 0; i < toDisplay.Count; i++) {
                    commands += toDisplay[i] + "\n";
                }

                for (int i = 0; i < adminOnly.Count; i++) {
                    commands += adminOnly[i] + "\n";
                }

                // I mean, it works, right?
                Program.messageControl.SendMessage(e, commands, false);
            }
            return Task.CompletedTask;
        }
    }
}
