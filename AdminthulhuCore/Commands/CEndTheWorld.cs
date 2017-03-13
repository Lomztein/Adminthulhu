using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Adminthulhu {
    class CEndTheWorld : Command {

        public CEndTheWorld () {
            command = "endtheworld";
            name = "End the World";
            help = "Whatever you do, do not call this function. The world will end in fire.";
            argumentNumber = 0;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                string[] deathItself = SerializationIO.LoadTextFile (Program.dataPath + "theentirebeemoviescript.txt");
                string complete = "";

                for (int i = 0; i < deathItself.Length; i++) {
                    complete += deathItself[i] + "\n";
                }

                Program.messageControl.SendMessage (e, complete);
            }
            return Task.CompletedTask;
        }
    }
}
