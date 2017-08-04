using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Sockets;
using Discord.Rest;
using Discord.WebSocket;
using Discord;

namespace Adminthulhu {
    public class ServerStatusChecking : IClockable, IConfigurable {

        public static uint testEveryMinutes = 10;
        private static uint minuteIndex = 10; // One-indexing? This shit is getting worse by the class!

        public ulong statusMessageChannel = 0;
        public ulong statusMessageID = 0;
        public string statusMessageHeader = "Server Statuses";

        public static List<Address> addresses = new List<Address> ();

        public Task Initialize(DateTime time) {
            LoadConfiguration ();
            BotConfiguration.AddConfigurable (this);
            return Task.CompletedTask;
        }

        public void LoadConfiguration() {
            addresses.Add (new Address ("Address 1 Name", "Address 1 IP", 0));
            addresses.Add (new Address ("Address 2 Name", "Address 2 IP", 0));
            testEveryMinutes = BotConfiguration.GetSetting ("Misc.ServerStatusChecker.TestEveryXMinutes", "", testEveryMinutes);
            addresses = BotConfiguration.GetSetting ("Misc.ServerStatusChecker.Addresses", "", addresses);
            statusMessageChannel = BotConfiguration.GetSetting ("Misc.ServerStatusChecker.MessageChannel", "", statusMessageChannel);
            statusMessageHeader = BotConfiguration.GetSetting ("Misc.ServerStatusChecker.MessageHeader", "", statusMessageHeader);
            statusMessageID = BotConfiguration.GetSetting ("Misc.ServerStatusChecker.MessageID", "", statusMessageID);
        }

        public Task OnDayPassed(DateTime time) {
            return Task.CompletedTask;
        }

        public Task OnHourPassed(DateTime time) {
            return Task.CompletedTask;
        }

        public Task OnMinutePassed(DateTime time) {
            minuteIndex++;
            if (minuteIndex >= testEveryMinutes) {
                UpdateAddresses ();
                minuteIndex = 1;
            }
            return Task.CompletedTask;
        }

        public Task OnSecondPassed(DateTime time) {
            return Task.CompletedTask;
        }

        public async void UpdateAddresses() {
            string result = statusMessageHeader + "\n```";
            foreach (Address add in addresses) {
                result += await add.GetResult () + "\n";
            }
            await UpdateMessage (result + "```");
        }

        public async Task UpdateMessage(string contents) {
            if (statusMessageChannel != 0) {
                SocketGuildChannel channel = Utility.GetServer ().GetChannel (statusMessageChannel);
                RestUserMessage message = null;

                if (statusMessageID != 0) {
                    message = await (channel as SocketTextChannel).GetMessageAsync (statusMessageID) as RestUserMessage;

                    await message.ModifyAsync (delegate (MessageProperties properties) {
                        properties.Content = contents;
                    });
                } else {
                    message = await Program.messageControl.AsyncSend (channel as ISocketMessageChannel, contents, true);
                    BotConfiguration.SetSetting ("Misc.ServerStatusChecker.MessageID", message.Id);
                    statusMessageID = message.Id;
                }

            }
        }

        public class Address {
            public string name;
            public string ip;
            public int port;

            public Address(string _name, string _ip, int _port) {
                name = _name;
                ip = _ip;
                port = _port;
            }

            public string GetNameWithAddress() {
                return name + " - " + ip + ":" + port;
            }

            public virtual async Task<string> GetResult() {
                TcpClient client = new TcpClient ();
                try {
                    Task task = client.ConnectAsync (ip, port);
                    if (await Task.WhenAny (task, Task.Delay (1000)) == task) {
                        return Utility.UniformStrings (GetNameWithAddress (), "Online", " - ");
                    } else {
                        return Utility.UniformStrings (GetNameWithAddress (), "Offline", " - ");
                    }
                } catch (Exception e) {
                    return Utility.UniformStrings (GetNameWithAddress (), "Offline", " - ");
                }
            }
        }
    }
}
