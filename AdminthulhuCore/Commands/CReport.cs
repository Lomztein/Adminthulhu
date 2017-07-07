using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Adminthulhu {
    public class CReport : Command {

        public static string reportTextChannel = "reports";

        public CReport () {
            command = "report";
            shortHelp = "Report something.";
            longHelp = "Reports something you don't like to the admins. Your name is recorded.";
            argumentNumber = 1;

            availableInDM = true;
            availableOnServer = false;
        }

        public override Task ExecuteCommand (SocketUserMessage e, List<string> arguments) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                SocketGuild guild = Utility.GetServer ();
                ISocketMessageChannel channel = Utility.SearchChannel (guild, reportTextChannel) as ISocketMessageChannel;
                Program.messageControl.SendMessage (channel, "**Report from " + e.Author.Username + "**: " + arguments[0], false);
                Program.messageControl.SendMessage (e, "Report has been reported.", false);
            }
            return Task.CompletedTask;
        }
    }
}
