using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Adminthulhu {
    class CFizzfyr : Command {

        public CFizzfyr () {
            command = "fizzfyr13";
            shortHelp = "Fizzfyr13";
            longHelp = "Get litteraly the sexiest picture in existance.";
            argumentNumber = 0;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                Program.messageControl.SendImage (e.Channel as SocketTextChannel, "Looking for this sexy stud?", Program.dataPath + Program.resourceDirectory + "/fizzfyr.jpg", false);
            }
            return Task.CompletedTask;
        }
    }

    class CSwiggity : Command {

        public CSwiggity () {
            command = "swiggity";
            shortHelp = "Swiggity";
            longHelp = "Swiggity swooty I'm coming for dat booty!";
            argumentNumber = 0;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                Program.messageControl.SendImage (e.Channel as SocketTextChannel, "Swiggity swooty I'm coming for dat booty!", Program.dataPath + Program.resourceDirectory + "/scorekarl.jpg", false);
            }
            return Task.CompletedTask;
        }
    }
}
