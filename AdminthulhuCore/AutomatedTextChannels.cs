using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.IO;
using Discord.WebSocket;

namespace Adminthulhu {

    class AutomatedTextChannels : IClockable {

        public static string additionalHeaderPath = "additionalheaders.dat";

        public static List<string> headers = new List<string> ();
        /* "<:pogchamp:245217372137455616> When that ass is thicc",
            "<:Serviet:255721870828109824> Privet, comrades!",
            "When life gives you lemons, take them - its free stuff!",
            "\"Tiderne var bedre dengang Nick var admin\" - Nick.",
            "The massively oversexualised unofficial offical fanclub of Rênëgédë CROWBAR Rèvêngéncë, the god of eternal faggotry.. <3",
            "Where is the walrus pit! <:gooseman:271588350677221377>", */

        public static void AddHeaders (params string[] additional) {
           headers.AddRange (additional);
        }

        public Task Initialize ( DateTime time ) {
            string filePath = Program.dataPath + additionalHeaderPath;
            if (File.Exists (filePath)) {

                string[] additional = SerializationIO.LoadTextFile (filePath);
                AddHeaders (additional);
            }
            return Task.CompletedTask;
        }

        public Task OnDayPassed ( DateTime time ) {
            SocketGuildChannel mainChannel = Utility.GetMainChannel ();
            SocketTextChannel channel = mainChannel as SocketTextChannel;

            Random random = new Random ();
            int number = random.Next (headers.Count);

            string topic = headers[number];
            channel.ModifyAsync (delegate (TextChannelProperties properties) {  (properties).Topic = topic; } );
            return Task.CompletedTask;
        }

        public Task OnHourPassed ( DateTime time ) {
            return Task.CompletedTask;
        }

        public Task OnMinutePassed ( DateTime time ) {
            return Task.CompletedTask;
        }

        public Task OnSecondPassed ( DateTime time ) {
            return Task.CompletedTask;
        }
    }

    public class CAddHeader : Command {

        public CAddHeader () {
            command = "addheader";
            shortHelp = "Add new header.";
            isAdminOnly = true;
            catagory = Category.Admin;

            AddOverload (typeof (bool), "Adds a new header <header> to main channel");
        }

        public Task<Result> Execute(string header) {
            string filePath = Program.dataPath + AutomatedTextChannels.additionalHeaderPath;
            AutomatedTextChannels.AddHeaders (header);
            SerializationIO.SaveTextFile (filePath, header);

            return TaskResult (header, "Succesfully added header to list of additional headers!");
        }
    }

    public class CShowHeaders : Command {

        public CShowHeaders () {
            command = "showheaders";
            shortHelp = "Show header.";
            catagory = Category.Utility;
            AddOverload (typeof (string), "Shows all current possible headers of the main channel.");
        }

        public Task<Result> Execute(SocketUserMessage e) {
            string complete = "```";
            foreach (string h in AutomatedTextChannels.headers) {
                complete += "\n" + h;
            }
            complete += "```";
            return TaskResult (complete, "All current possible headers are: " + complete);
        }
    }
}