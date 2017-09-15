using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;

namespace Adminthulhu
{
    public class CSpoiler : Command
    {
        public CSpoiler() {
            command = "spoiler";
            shortHelp = "Print out a GIF containing a spoiler for something.";
            catagory = Category.Utility;
        }

        public Task<Result> Execute(SocketUserMessage e, string title, string text) {

            string combined = title + "\t\t" + text;
            using (Stream stream = new MemoryStream()) {
                using (StreamWriter writer = new StreamWriter (stream)) {
                    writer.WriteLine (combined);
                    stream.Position = 0;
                    Program.messageControl.SendImage (e.Channel as SocketTextChannel, "**[SPOILER]**", stream, "spoiler.txt", allowInMain);
                }
            }

            return TaskResult (null, "");
        }
    }
}
