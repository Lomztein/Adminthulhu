using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Adminthulhu {
    public class CCallVoiceChannel : Command {

        public CCallVoiceChannel () {
            command = "call";
            shortHelp = "Mention voice channel.";
            argHelp = "<voicechannel>";
            longHelp = "Mentions all members in " + argHelp + ".";
            argumentNumber = 2;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                List<SocketVoiceChannel> voiceChannels = (e.Channel as SocketGuildChannel).Guild.VoiceChannels.ToList ();

                string text = "";
                for (int i = 0; i < voiceChannels.Count; i++) {
                    if (voiceChannels[i].Name.ToLower ().Substring (0, arguments[0].Length) == arguments[0].ToLower ()) {

                        List<SocketGuildUser> users = voiceChannels[i].Users.ToList ();
                        for (int j = 0; j < users.Count; j++) {
                            text += users[j].Mention + " ";
                        }

                        break;
                    }
                }

                Program.messageControl.SendMessage(e, e.Author.Username + ": " + text + ", " + arguments[1], false);
            }
            return Task.CompletedTask;
        }
    }
}
