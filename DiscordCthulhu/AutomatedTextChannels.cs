﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.IO;
using Discord.WebSocket;

namespace DiscordCthulhu {

    class AutomatedTextChannels : IClockable {

        public static string additionalHeaderPath = "additionalheaders.dat";

        public static List<string> headers = new List<string> {
            "<:pogchamp:245217372137455616> When that ass is thicc",
            "<:Serviet:255721870828109824> Privet, comrades!",
            "When life gives you lemons, take them - its free stuff!",
            "\"Bim bam ba dum, ba-ba-di-da, bim bam ba dum, ba-ba-di-da.\" - That guy from KoRN.",
            "\"Tiderne var bedre dengang Nick var admin\" - Nick.",
            "The massively oversexualised unofficial offical fanclub of Rênëgédë CROWBAR Rèvêngéncë, the god of eternal faggotry.. <3",
            "Where is the walrus pit! <:gooseman:271588350677221377>",
        };

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
            SocketGuildChannel mainChannel = Program.GetMainChannel (Program.GetServer ());

            Random random = new Random ();
            int number = random.Next (headers.Count);

            string topic = headers[number];
            //mainChannel.ModifyAsync (delegate ( GuildChannelProperties properties ) { properties. = topic; } )); Not yet implemented in 1.0

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
            name = "Add New Header";
            argHelp = "<header>";
            help = "Adds a new header " + argHelp + " to main channel";
            argumentNumber = 1;
            isAdminOnly = true;
        }

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            await base.ExecuteCommand (e, arguments);
            if (await AllowExecution (e, arguments)) {
                string filePath = Program.dataPath + AutomatedTextChannels.additionalHeaderPath;
                AutomatedTextChannels.AddHeaders (arguments[0]);
                SerializationIO.SaveTextFile (filePath, arguments[0]);

                await Program.messageControl.SendMessage (e, "Succesfully added header to list of additional headers!");
            }
        }
    }

    public class CShowHeaders : Command {

        public CShowHeaders () {
            command = "showheaders";
            name = "Show Header";
            help = "Shows all current possible headers of the main channel.";
            argumentNumber = 0;
        }

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            await base.ExecuteCommand (e, arguments);
            if (await AllowExecution (e, arguments)) {
                string complete = "```";
                foreach (string h in AutomatedTextChannels.headers) {
                    complete += "\n" + h;
                }
                complete += "```";
                await Program.messageControl.SendMessage (e, "All current possible headers are: " + complete);
            }
        }
    }
}