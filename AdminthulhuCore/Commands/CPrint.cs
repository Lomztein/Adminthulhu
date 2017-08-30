using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Adminthulhu
{
    public class CPrint : Command {
        public CPrint() {
            command = "print";
            shortHelp = "Prints stuff.";

            AddOverload (typeof (string), "Prints whatever is put into it, regardless of position in chain.");
        }

        public Task<Result> Execute(SocketUserMessage message, object obj) {
            if (obj != null) {
                Program.messageControl.SendMessage (message, obj.ToString (), allowInMain);
                return TaskResult (obj.ToString (), "");
            } else {
                Program.messageControl.SendMessage (message, "null", allowInMain);
                return TaskResult ("null", "");
            }
        }
    }
}
