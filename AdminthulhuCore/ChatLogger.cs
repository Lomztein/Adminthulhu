using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Discord.WebSocket;

namespace Adminthulhu {
    class Logging : IClockable {

        public static Queue<string> loggingQueue = new Queue<string> ();

        public static void Log (string message) {
            if (message == null || message.Length == 0)
                return;

            Console.WriteLine (message);
            loggingQueue.Enqueue (message);
        }

        public static void DebugLog (string message) {
            Log (message);
            if (Utility.GetServer () != null)
                Program.messageControl.SendMessage (Utility.SearchChannel (Utility.GetServer (), Program.dumpTextChannelName) as SocketTextChannel, message, false);
        }

        public Task Initialize(DateTime time) {
            return Task.CompletedTask;
        }

        public Task OnSecondPassed(DateTime time) {
            return Task.CompletedTask;
        }

        public Task OnMinutePassed(DateTime time) {
            if (loggingQueue.Count == 0)
                return Task.CompletedTask;

            try {
                File.AppendAllLines (Program.dataPath + Program.chatlogDirectory + DateTime.Today.DayOfWeek.ToString () + ".txt", loggingQueue.ToList ());
                loggingQueue = new Queue<string> ();
            } catch (IOException io) {
                Console.WriteLine ("Failed to write to log file: " + io.Message);
            }
            return Task.CompletedTask;
        }

        public Task OnHourPassed(DateTime time) {
            return Task.CompletedTask;
        }

        public Task OnDayPassed(DateTime time) {
            return Task.CompletedTask;
        }
    }
}
