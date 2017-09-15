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
            catagory = Category.Admin;
            isAdminOnly = true;

            AddOverload (typeof (string), "Displays whatever file is at <path> relative to data folder, in plaintext.");
        }

        public Task<Result> Execute(SocketUserMessage e, string path) {
            try {
                string text = SerializationIO.LoadTextFile (Program.dataPath + path).Singlify ();
                return TaskResult (text, text);
            } catch {
                return TaskResult ("", "Error - File could not be found.");
            }
        }
    }
}
