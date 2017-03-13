using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Adminthulhu {
    public class Clock {

        public static Thread timeThread;
        public DateTime lastMesauredTime;
        public int checkDelay = 1000; // The time between each check in milliseconds.

        public IClockable[] clockables = new IClockable[] {new AutomatedEventHandling (), new AutomatedTextChannels (), new UserActivityMonitor (), new Birthdays (), new AutomatedWeeklyEvent ()};

        public Clock () {
            timeThread = new Thread (new ThreadStart (Initialize));
            timeThread.Start ();
            while (!timeThread.IsAlive) {
                ChatLogger.Log ("Initializing clock thread..");
            }

            Thread.Sleep (1);
        }

        public void Initialize () {
            DateTime now = DateTime.Now;
            foreach (IClockable c in clockables) {
                c.Initialize (now);
            }

            while (timeThread.IsAlive) {
                now = DateTime.Now;

                // This could possibly be improved with delegates, but I have no idea how they work.
                // Don't do anything before the server is ready.
                if (Program.GetServer () != null) {

                    if (now.Second != lastMesauredTime.Second) {
                        foreach (IClockable c in clockables)
                            c.OnSecondPassed (now);
                    }

                    if (now.Minute != lastMesauredTime.Minute) {
                        foreach (IClockable c in clockables)
                            c.OnMinutePassed (now);
                    }

                    if (now.Hour != lastMesauredTime.Hour) {
                        foreach (IClockable c in clockables)
                            c.OnHourPassed (now);
                    }

                    if (now.Day != lastMesauredTime.Day) {
                        foreach (IClockable c in clockables)
                            c.OnDayPassed (now);
                    }
                }


                Thread.Sleep (checkDelay);
                lastMesauredTime = now;
            }
        }
    }
}
