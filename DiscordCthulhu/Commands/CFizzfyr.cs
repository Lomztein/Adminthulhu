﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {
    class CFizzfyr : Command {

        public CFizzfyr () {
            command = "fizzfyr13";
            name = "Fizzfyr13";
            help = "Get litteraly the sexiest picture in existance.";
            argumentNumber = 0;
        }

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            await base.ExecuteCommand (e, arguments);
            if (await AllowExecution (e, arguments)) {
                await Program.messageControl.SendImage (e.Channel as SocketTextChannel, "Looking for this sexy stud?", Program.dataPath + Program.resourceDirectory + "/fizzfyr.jpg");
            }
        }
    }

    class CSwiggity : Command {

        public CSwiggity () {
            command = "swiggity";
            name = "Swiggity";
            help = "Swiggity swooty I'm coming for dat booty!";
            argumentNumber = 0;
        }

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            await base.ExecuteCommand (e, arguments);
            if (await AllowExecution (e, arguments)) {
                await Program.messageControl.SendImage (e.Channel as SocketTextChannel, "Swiggity swooty I'm coming for dat booty!", Program.dataPath + Program.resourceDirectory + "/scorekarl.jpg");
            }
        }
    }
}
