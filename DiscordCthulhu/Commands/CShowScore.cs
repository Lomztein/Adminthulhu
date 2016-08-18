using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    public class CShowScore : Command {

        public CShowScore () {
            Initialize ();
            command = "showscore";
            name = "Show Score";
            help = "Shows you your own score.";
            argumentNumber = 0;
        }

        public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {

            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                Program.messageControl.SendMessage (e, "Your current score is " + Program.scoreCollection.GetScore (e.User.Name));
            }
        }
    }
}
