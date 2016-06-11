using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    public class CCommandList : Command {

        public CCommandList () {
            command = "clist";
            name = "Command List";
            help = "\"!clist\" - Reveals a full list of all commands.";
            argumentNumber = 0;
        }

        public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (arguments)) {
                string commands = "";
                for (int i = 0; i < Program.commands.Length; i++) {
                    commands += Program.commands[i].help + "\n";
                }
                Program.messageControl.SendMessage(e, commands);
            }
        }
    }
}
