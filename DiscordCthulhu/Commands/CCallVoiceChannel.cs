using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    public class CCallVoiceChannel : Command {

        public CCallVoiceChannel () {
            command = "callvoice";
            name = "Mention Voice Channel";
            help = "\"!callvoice <channelname>;<message>\" - Mentions all members in a specific voice channel.";
            argumentNumber = 2;
        }

        public override async void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (arguments)) {

                List<Channel> voiceChannels = e.Server.VoiceChannels.ToList ();

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

                await e.Channel.SendMessage (e.User.Name + ": " + text + ", " + arguments[1]);
            }
        }
    }
}
