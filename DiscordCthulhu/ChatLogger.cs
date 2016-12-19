using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DiscordCthulhu {
    class ChatLogger {

        public static string logPath;

        public ChatLogger (string path) {
            logPath = path;
        }

        public static void Log (string message) {
            if (message == null || message.Length == 0)
                return;

            Console.WriteLine (message);
            File.AppendAllLines (Program.dataPath + Program.chatlogDirectory + DateTime.Today.DayOfWeek.ToString () + ".txt",new string[] { message });
        }

        public static void DebugLog (string message) {
            Log (message);
            Program.messageControl.SendMessage (Program.SearchChannel (Program.GetServer (), Program.dumpTextChannelName), "**DEBUG: **" + message);
        }
    }
}
