﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {
    public class Phrase {

        public string inputText;
        public string user;
        public int chance;
        public string response;
        public string channel;

        public async Task<bool> CheckAndRespond (SocketMessage e) {

            string message = e.Content;
            string sender = e.Author.Username;
            string locChannel = e.Channel.Name;

            if (message.Length < inputText.Length)
                return false;

            if (inputText == "" || message.Substring (0, inputText.Length).ToUpper () == inputText.ToUpper ()) {
                if (user == "" || sender.ToUpper() == user.ToUpper ()) {
                    if (channel == "" || locChannel.ToUpper () == channel.ToUpper ()) {

                        Random random = new Random ();
                        if (random.Next (100) < chance || chance == 100) {

                            await Program.messageControl.SendMessage (e, response);
                            return true;
                        }
                    }
                }
            }
            return false;
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
