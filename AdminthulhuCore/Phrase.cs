using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using System.Text.RegularExpressions;

namespace Adminthulhu {
    public class Phrase {

        public string inputText;
        public ulong user;
        public int chance;
        public string response;
        public string reaction;
        public ulong channel;

        public bool CheckAndRespond (SocketMessage e) {

            string message = e.Content;
            ulong sender = e.Author.Id;
            ulong locChannel = e.Channel.Id;

            Regex regex = new Regex (inputText); // Arguably a bad name, but can't change because JSON.

            if (regex.IsMatch (message)) {
                if (user == 0 || sender == user) {
                    if (channel == 0 || locChannel == channel) {

                        Random random = new Random ();
                        if (random.Next (100) < chance || chance == 100) {

                            if (message != "")
                                Program.messageControl.SendMessage (e, response, true);
                            if (reaction != "")
                                (e as SocketUserMessage).AddReactionAsync (new Emoji (reaction));
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public Phrase (string _input, ulong _user, int _chance, string _response, ulong _channel, string _reaction) {
            inputText = _input;
            user = _user;
            chance = _chance;
            response = _response;
            channel = _channel;
            reaction = _reaction;
        }
    }
}
