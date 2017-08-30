using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Adminthulhu {
    public class CCallVoiceChannel : Command {

        public CCallVoiceChannel() {
            command = "call";
            shortHelp = "Mention voice channel.";

            AddOverload (typeof (IUser[]), "Mentions all members in voice channel given by name.");
            AddOverload (typeof (IUser[ ]), "Mentions all members in voice channel given by channel.");
        }

        public Task<Result> Execute(SocketUserMessage e, string channelName) {
            SocketVoiceChannel channel = Utility.SearchChannel (Utility.GetServer (), channelName) as SocketVoiceChannel;
            return Execute (e, channel);
        }

        public Task<Result> Execute(SocketUserMessage e, SocketVoiceChannel channel) {
            string text = "";
            List<SocketGuildUser> users = null;
            if (channel != null) {
                users = channel.Users.ToList ();
                for (int j = 0; j < users.Count; j++) {
                    text += users [ j ].Mention + " ";
                }
            }

            return TaskResult ((IUser[])users.ToArray (), text);
        }
    }
}
