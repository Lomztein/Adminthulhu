using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Adminthulhu {
    public class CReport : Command, IConfigurable {

        public static ulong reportTextChannel;

        public CReport () {
            command = "report";
            shortHelp = "Report something.";
            longHelp = "Reports something you don't like to the admins. Your name is recorded.";
            argumentNumber = 1;

            availableInDM = true;
            availableOnServer = false;
            catagory = Catagory.Utility;
        }

        public override void Initialize() {
            base.Initialize ();
            LoadConfiguration ();
        }

        public override Task ExecuteCommand (SocketUserMessage e, List<string> arguments) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                SocketGuild guild = Utility.GetServer ();
                ISocketMessageChannel channel = Utility.GetServer ().GetChannel (reportTextChannel) as ISocketMessageChannel;
                Program.messageControl.SendMessage (channel, "**Report from " + e.Author.Username + "**: " + arguments[0], false);
                Program.messageControl.SendMessage (e, "Report has been reported.", false);
            }
            return Task.CompletedTask;
        }

        public override void LoadConfiguration() {
            base.LoadConfiguration ();
            reportTextChannel = BotConfiguration.GetSetting<ulong> ("ReportChannelID", 0);
        }
    }
}
