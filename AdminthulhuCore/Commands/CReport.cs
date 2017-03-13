using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Adminthulhu {
    public class CReport : Command {

        public static string reportTextChannel = "reports";

        public CReport () {
            command = "report";
            name = "Report Something";
            help = "Reports something you don't like to the admins. Your name is recorded.";
            argumentNumber = 1;

            availableInDM = true;
            availableOnServer = false;
        }

        public override Task ExecuteCommand (SocketUserMessage e, List<string> arguments) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                SocketGuild guild = Program.GetServer ();
                ISocketMessageChannel channel = Program.SearchChannel (guild, reportTextChannel) as ISocketMessageChannel;
                Program.messageControl.SendMessage (channel, "**Report from " + e.Author.Username + "**: " + arguments[0]);
                Program.messageControl.SendMessage (e, "Report has been reported.");
            }
            return Task.CompletedTask;
        }
    }
}
