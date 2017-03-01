using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    public class CCallVoiceChannel : Command {

        public CCallVoiceChannel () {
            command = "call";
            name = "Mention Voice Channel";
            argHelp = "<voicechannel>";
            help = "Mentions all members in " + argHelp + ".";
            argumentNumber = 2;
        }

        public override void ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                List<Channel> voiceChannels = e.SocketGuild.VoiceChannels.ToList ();

                string text = "";
                for (int i = 0; i < voiceChannels.Count; i++) {
                    if (voiceChannels[i].Name.ToLower ().Substring (0, arguments[0].Length) == arguments[0].ToLower ()) {

                        List<User> users = voiceChannels[i].Users.ToList ();
                        for (int j = 0; j < users.Count; j++) {
                            text += users[j].Mention + " ";
                        }

                        break;
                    }
                }

                Program.messageControl.SendMessage(e, e.User.Name + ": " + text + ", " + arguments[1]);
            }
        }
    }
}
