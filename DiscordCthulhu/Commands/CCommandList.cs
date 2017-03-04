using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {
    public class CCommandList : Command {

        public CCommandList () {
            command = "clist";
            name = "Command List";
            help = "Reveals a full list of all commands.";
            argumentNumber = 0;
            alwaysEnabled = true;
        }

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            await base.ExecuteCommand (e, arguments);
            if (await AllowExecution (e, arguments)) {

                List<string> toDisplay = new List<string> ();

                toDisplay.Add ("```");

                for (int i = 0; i < Program.commands.Length; i++) {
                    if (Program.commands[i].AvailableOnChannel (e) || Program.commands[i].alwaysEnabled)
                        toDisplay.Add (Program.commands[i].GetShortHelp ());
                }

                if (e.Channel as SocketDMChannel == null) {
                        toDisplay.Add ("\n**ADMIN ONLY COMMANDS**");

                    for (int i = 0; i < Program.commands.Length; i++) {
                        if (Program.commands[i].isAdminOnly)
                            toDisplay.Add (Program.commands[i].GetShortHelp ());
                    }
                }

                toDisplay.Add ("```");
                string commands = "";
                for (int i = 0; i < toDisplay.Count; i++) {
                    commands += toDisplay[i] + "\n";
                }

                await Program.messageControl.SendMessage(e, commands);
            }
        }
    }
}
