using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Discord.WebSocket;

namespace Adminthulhu {
    class ChatLogger {

        public static string logPath;

        public ChatLogger (string path) {
            logPath = path;
        }

        public static void Log (string message) {
            if (message == null || message.Length == 0)
                return;

            Console.WriteLine (message);
            try {
                File.AppendAllLines (Program.dataPath + Program.chatlogDirectory + DateTime.Today.DayOfWeek.ToString () + ".txt", new string[] { message });
            } catch (Exception e) {
                Console.WriteLine ("Failed to write to log file: " + e.ToString ());
            }
        }

        public static void DebugLog (string message) {
            Log (message);
            if (Utility.GetServer () != null)
                Program.messageControl.SendMessage (Utility.SearchChannel (Utility.GetServer (), Program.dumpTextChannelName) as SocketTextChannel, message, false);
        }
    }
}
