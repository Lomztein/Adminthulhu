using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    public class Phrase {

        public string inputText;
        public string user;
        public int chance;
        public string response;
        public string channel;

        public void CheckAndRespond (MessageEventArgs e) {

            string message = e.Message.Text;
            string sender = e.User.Name;
            string locChannel = e.Channel.Name;

            if (message.Length < inputText.Length)
                return;

            if (inputText == "" || message.Substring (0, inputText.Length).ToUpper () == inputText.ToUpper ()) {
                if (user == "" || sender.ToUpper() == user.ToUpper ()) {
                    if (channel == "" || locChannel.ToUpper () == channel.ToUpper ()) {

                        Random random = new Random ();
                        if (random.Next (100) < chance || chance == 100) {

                            Program.messageControl.SendMessage (e, response);
                        }
                    }
                }
            }
        }

        public Phrase (string input, string us, int ch, string re, string cha) {
            inputText = input;
            user = us;
            chance = ch;
            response = re;
            channel = cha;
        }

        public Phrase ( string input, int ch, string re ) {
            inputText = input;
            chance = ch;
            response = re;
            user = "";
            channel = "";
        }

        public Phrase ( string input, string us, int ch, string re ) {
            inputText = input;
            user = us;
            chance = ch;
            response = re;
            channel = "";
        }
    }
}
