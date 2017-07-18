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
            shortHelp = "Show command list.";
            longHelp = "Reveals a full list of all commands.";
            argumentNumber = 0;
            catagory = Catagory.Utility;

            availableInDM = true;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                var catagories = Program.commands.Where (x => x.AllowExecution (e, new List<string>(), false)).GroupBy (x => x.catagory);
                string result = "";

                foreach (var catagory in catagories) {
                    result += catagory.ElementAt (0).catagory.ToString () + " Commands\n";
                    foreach (var item in catagory) {
                        result += Utility.FormatCommand (item) + "\n";
                    }
                    result += "\n";
                }

                // I mean, it works, right?
                Program.messageControl.SendMessage(e.Channel, result, false, "```");
            }
            return Task.CompletedTask;
        }
    }
}
