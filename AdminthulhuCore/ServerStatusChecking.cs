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
using System.IO;
using Newtonsoft.Json.Linq;

namespace Adminthulhu.ServerStatusChecking {
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
            addresses.Add (new Address ("Address 1 Name", "Address 1 IP", 0, ""));
            addresses.Add (new Address ("Address 2 Name", "Address 2 IP", 0, ""));
            testEveryMinutes = BotConfiguration.GetSetting ("Misc.ServerStatusChecker.TestEveryXMinutes", this, testEveryMinutes);
            addresses = BotConfiguration.GetSetting ("Misc.ServerStatusChecker.Addresses", this, addresses);
            statusMessageChannel = BotConfiguration.GetSetting ("Misc.ServerStatusChecker.MessageChannel", this, statusMessageChannel);
            statusMessageHeader = BotConfiguration.GetSetting ("Misc.ServerStatusChecker.MessageHeader", this, statusMessageHeader);
            statusMessageID = BotConfiguration.GetSetting ("Misc.ServerStatusChecker.MessageID", this, statusMessageID);

            for (int i = 0; i < addresses.Count; i++) {
                if (addresses [ i ].typeName != null && addresses [ i ].typeName.Length > 0) {
                    addresses [ i ] = ChangeType (addresses [ i ]);
                }
            }
        }

        public Address ChangeType(Address original) {
            switch (original.typeName) {
                case "MinecraftJava": // There must be a generic way of doing this..
                    return new Address.MinecraftJava (original.name, original.ip, original.port, original.typeName);
            }
            return original;
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
            try { // Did I ever mention that I don't like web development?
                string result = statusMessageHeader + "\n```";
                foreach (Address add in addresses) {
                    result += add.GetNameWithAddress () + " - Status: " + await add.GetResult () + "\n";
                }
                await UpdateMessage (result + "```");
            } catch (Exception e) {
                Logging.Log (Logging.LogType.EXCEPTION, e.Message);
            }
        }

        public async Task UpdateMessage(string contents) {
            if (statusMessageChannel != 0) {
                SocketGuildChannel channel = Utility.GetServer ().GetChannel (statusMessageChannel);
                IMessage message = null;

                if (statusMessageID != 0) {
                    message = await (channel as SocketTextChannel).GetMessageAsync (statusMessageID);
                    if (message != null && message.Content != contents) {
                        await (message as RestUserMessage).ModifyAsync (delegate (MessageProperties properties) {
                            properties.Content = contents;
                        });
                    }
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
            public string typeName;

            public Address(string _name, string _ip, int _port, string _typeName) {
                name = _name;
                ip = _ip;
                port = _port;
                typeName = _typeName;
            }

            public string GetNameWithAddress() {
                return name + " - " + ip + ":" + port;
            }

            public virtual async Task<string> GetResult() {
                using (TcpClient client = new TcpClient ()) {
                    try {
                        Task task = client.ConnectAsync (ip, port);
                        if (await Task.WhenAny (task, Task.Delay (1000)) == task) {
                            client.Close ();
                            return "ONLINE";
                        } else {
                            client.Close ();
                            return "OFFLINE";
                        }
                    } catch (Exception e) {
                        client.Close ();
                        return $"ERROR - {e.Message}";
                    }
                }
            }

            // Put specialized below here.
            public class MinecraftJava : Address {

                public override async Task<string> GetResult() {
                    using (TextReader reader = await Utility.DoJSONRequestAsync ("https://mcapi.us/server/status?ip=" + ip + "&port=" + port.ToString ())) {
                        string json = reader.ReadToEnd ();

                        // https://www.youtube.com/watch?v=PKg2ZzPKl2M
                        JObject obj = JObject.Parse (json);
                        JToken players = obj [ "players" ];
                        int max = players.ElementAt (0).ToObject<int> ();
                        int min = players.ElementAt (1).ToObject<int> ();
                        string online = obj [ "status" ].ToObject<string>() == "success" ? "ONLINE - " + min + "/" + max + " Players" : "OFFLINE";
                        return online;
                    }
                }

                public MinecraftJava(string _name, string _ip, int _port, string _typeName) : base (_name, _ip, _port, _typeName) {
                    name = _name;
                    ip = _ip;
                    port = _port;
                    typeName = _typeName;
                }
            }
        }
    }
}
