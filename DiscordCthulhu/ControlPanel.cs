﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Discord;
using System.Collections.Concurrent;
using Discord.WebSocket;

namespace DiscordCthulhu {
    public partial class ControlPanel : Form {

        public static Thread formThread;
        public static ControlPanel currentPanel;
        public static ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

        public ControlPanel () {
            formThread = new Thread (new ThreadStart (Initialize));
            formThread.Start ();
            while (!formThread.IsAlive) {
                Console.WriteLine ("ControlPanel");
            }

            Thread.Sleep (1);
        }

        public void Initialize () {
            currentPanel = this;

            InitializeComponent ();
            ShowDialog ();
        }

        private void ControlPanel_Load ( object sender, EventArgs e ) {

        }

        private void button1_Click ( object sender, EventArgs e ) {
            if (messageText.Text.Length != 0) {

                foreach (SocketGuild server in Program.discordClient.Guilds) {
                    SocketGuildChannel channel = Program.SearchChannel (server, channelName.Text);
                    if (channel != null) {
                        //foreach (Channel channel in server.TextChannels) {
                        Program.messageControl.SendMessage (channel as SocketTextChannel, messageText.Text).GetAwaiter().GetResult();
                        messageText.Text = "";
                    }
                }

            }
        }

        private void messageText_TextChanged ( object sender, EventArgs e ) {

        }

        private void label1_Click ( object sender, EventArgs e ) {

        }

        private void channelName_TextChanged ( object sender, EventArgs e ) {

        }
    }
}
