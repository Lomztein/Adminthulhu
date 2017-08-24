using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Adminthulhu
{
    public class CDisplayFile : Command
    {
        public CDisplayFile() {
            command = "displayfile";
            shortHelp = "Display a file.";
            longHelp = "Displays whatever file is at <path> relative to data folder, in plaintext.";
            argumentNumber = 1;
            catagory = Catagory.Admin;
            isAdminOnly = true;
        }

        public override Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                try {
                string text = SerializationIO.LoadTextFile (Program.dataPath + arguments [ 0 ]).Singlify ();
                    Program.messageControl.SendMessage (e.Channel, text, false, "```");
            }catch {
                    Program.messageControl.SendMessage (e.Channel, "Error - File could not be found.", false);
                }
            }
            return Task.CompletedTask;
        }
    }

    public static class StringExtension {
        public static string Singlify(this string [ ] input, string seperator = "\n") {
            string result = "";
            foreach (string str in input) {
                result += str + seperator;
            }
            return result;
        }
    }
}
