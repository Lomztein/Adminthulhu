using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Discord.WebSocket;

namespace Adminthulhu {
    public class Clock : IConfigurable {

        public static Thread timeThread;
        public DateTime lastMesauredTime;
        public int checkDelay = 1000; // The time between each check in milliseconds.
        public int offsetHours;

        public SocketGuildUser [ ] lastTest = new SocketGuildUser [ 5 ];

        public IClockable [ ] clockables = new IClockable [ ] {
            new DiscordEvents (), new AutomatedTextChannels (), new UserActivityMonitor (), new Birthdays (), new WeeklyEvents (),
            new Strikes (), new AprilFools (), new Younglings (), new ServerStatusChecking.ServerStatusChecking (), new Voice.TemporaryChannelsChecker (),
            new Logging (), new AutoPatcher (),
        };
        private bool [ ] clockablesEnabled;

        public Clock () {
            LoadConfiguration ();
            BotConfiguration.AddConfigurable (this);

            timeThread = new Thread (new ThreadStart (Initialize));
            timeThread.Start ();
            while (!timeThread.IsAlive) {
                Logging.Log (Logging.LogType.BOT, "Initializing clock thread..");
            }
            Thread.Sleep (1);
        }

        public void Initialize () {
            DateTime now = DateTime.Now;
            for (int i = 0; i < clockables.Length; i++) {
                if (clockablesEnabled[i])
                    clockables[i].Initialize (now);
            }

            while (timeThread.IsAlive) {
                now = DateTime.Now.AddHours (-offsetHours);

                // This could possibly be improved with delegates, but I have no idea how they work.
                // Don't do anything before the server is ready.
                if (Program.FullyBooted ()) {

                    if (now.Second != lastMesauredTime.Second) {
                        for (int i = 0; i < clockables.Length; i++) {
                            if (clockablesEnabled [ i ])
                                clockables [ i ].OnSecondPassed (now);
                        }
                    }

                    if (now.Minute != lastMesauredTime.Minute) {
                        for (int i = 0; i < clockables.Length; i++) {
                            if (clockablesEnabled [ i ])
                               clockables [ i ].OnMinutePassed (now);
                        }
                    }

                    if (now.Hour != lastMesauredTime.Hour) {
                        for (int i = 0; i < clockables.Length; i++) {
                            if (clockablesEnabled [ i ])
                                clockables [ i ].OnHourPassed (now);
                        }
                    }

                    if (now.Day != lastMesauredTime.Day) {
                        for (int i = 0; i < clockables.Length; i++) {
                            if (clockablesEnabled [ i ])
                                clockables [ i ].OnDayPassed (now);
                        }
                    }
                }


                Thread.Sleep (checkDelay);
                lastMesauredTime = now;
            }
        }

        public void LoadConfiguration() {
            clockablesEnabled = new bool [ clockables.Length ];
            offsetHours = BotConfiguration.GetSetting ("Clockables.OffsetHours", "", offsetHours);
            for (int i = 0; i < clockablesEnabled.Length; i++) {
                clockablesEnabled [ i ] = BotConfiguration.GetSetting ("Clockables." + clockables[i].GetType ().Name + "Enabled", clockables [ i ].GetType ().Name + "Enabled", false);
            }
        }
    }
}
