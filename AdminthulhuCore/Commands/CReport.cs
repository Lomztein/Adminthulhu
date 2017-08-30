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

            availableInDM = true;
            availableOnServer = false;
            catagory = Category.Utility;

            AddOverload (typeof (object), "Reports something you don't like to the admins. Your name is recorded.");
        }

        public override void Initialize() {
            base.Initialize ();
            LoadConfiguration ();
            BotConfiguration.AddConfigurable (this);
        }

        public Task<Result> Execute(SocketUserMessage e, string report) {
            SocketGuild guild = Utility.GetServer ();
            ISocketMessageChannel channel = Utility.GetServer ().GetChannel (reportTextChannel) as ISocketMessageChannel;
            Program.messageControl.SendMessage (channel, "**Report from " + e.Author.Username + "**: " + report, false);
            return TaskResult ("", "Report has been reported.");
        }

        public override void LoadConfiguration() {
            base.LoadConfiguration ();
            reportTextChannel = BotConfiguration.GetSetting<ulong> ("Server.ReportChannelID", "ReportChannelID", 0);
        }
    }
}
