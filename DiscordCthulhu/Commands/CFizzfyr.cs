using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    class CFizzfyr : Command {

        public CFizzfyr () {
            Initialize ();
            command = "fizzfyr13";
            name = "Fizzfyr13";
            help = "Get litteraly the sexiest picture in existance.";
            argumentNumber = 0;
        }

        public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                Program.messageControl.SendImage (e.Channel, "Looking for this sexy stud?", Program.dataPath + Program.resourceDirectory + "/fizzfyr.jpg");
            }
        }
    }

    class CSwiggity : Command {

        public CSwiggity () {
            Initialize ();
            command = "swiggity";
            name = "Swiggity";
            help = "Swiggity swooty I'm coming for dat booty!";
            argumentNumber = 0;
        }

        public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                Program.messageControl.SendImage (e.Channel, "Swiggity swooty I'm coming for dat booty!", Program.dataPath + Program.resourceDirectory + "/scorekarl.jpg");
            }
        }
    }
}
