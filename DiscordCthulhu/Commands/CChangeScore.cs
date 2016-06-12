using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {

    public class CChangeScore : Command {

        public CChangeScore () {
            command = "changescore";
            name = "Change Score";
            help = "\"!changescore <username>;<number>\" - Change the score of a specific player. - ADMIN ONLY";
            argumentNumber = 2;
            isAdminOnly = true;
        }

        public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                int number;

                if (int.TryParse (arguments[1], out number)) {

                    Program.scoreCollection.ChangeScore (arguments[0], number);
                    Program.messageControl.SendMessage (e, arguments[0] + " score has been changed by " + number.ToString () + ".\n" + 
                        "Their score now totals " + Program.scoreCollection.GetScore (arguments[0]) + ".");
                }else {
                    Program.messageControl.SendMessage (e, "Failed to parse second argument.");
                }
            }
        }
    }
}
