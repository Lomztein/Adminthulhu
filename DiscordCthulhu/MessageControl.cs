﻿using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;

namespace DiscordCthulhu
{
    class MessageTimer
    {
        public string message { get; }
        private Timer timer;
        MessageEventArgs e;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e">MessageEventArgs</param>
        /// <param name="message">Message</param>
        /// <param name="delay">Delay in sec</param>
        public MessageTimer(MessageEventArgs e, string message, int delay)
        {
            this.message = message;
            this.e = e;
            timer = new Timer(delay * 1000);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine("Send message timer");
            await this.e.Channel.SendMessage(message);
        }

        public void StopTimer()
        {
            timer.Stop();
        }
    }

    class MessageControl
    {

        

        public List<MessageTimer> messages = new List<MessageTimer>();

        public void RemoveMessageTimer(MessageTimer messageTimer)
        {
            messageTimer.StopTimer();
            messages.Remove(messageTimer);
        }

        public async void SendMessage(MessageEventArgs e, string message)
        {
            Console.WriteLine("Send message");
            messages.Add(new MessageTimer(e, message, 5));
            await e.Channel.SendMessage(message);
        }
    }
}