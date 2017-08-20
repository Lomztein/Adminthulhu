using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Discord.WebSocket;

namespace Adminthulhu {
    class Logging : IClockable {

        public enum LogType {
            CHAT, SYSTEM, WARNING, CRITICAL, CONFIG, EXCEPTION, BOT
        }

        public static Queue<string> loggingQueue = new Queue<string> ();

        public static void Log (LogType logType, string message) {
            if (message == null || message.Length == 0)
                return;
            string combine = "[" + logType.ToString () + "] - " + message;

            Console.WriteLine (combine);
            loggingQueue.Enqueue (combine);

            if (logType == LogType.CRITICAL) {
                while (true) {
                    Console.WriteLine ("SYSTEM HALTED DUE TO CRITICAL ERROR, WRITE 'cont' TO CONTINUE AT OWN RISK.");
                    if (Console.ReadLine () == "cont")
                        break;
                }
            }
        }

        public static void DebugLog (LogType logType, string message) {
            Log (logType, message);
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
