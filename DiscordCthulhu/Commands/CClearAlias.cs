﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    public class CClearAliasses : Command {

        public CClearAliasses () {
            command = "clearalias";
            name = "Clear Aliassses";
            help = "\"!clearalias\" - Clears off all aliasses to your name.";
            argumentNumber = 0;
        }

        public override async void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (arguments)) {
                AliasCollection.User user = Program.aliasCollection.FindUsersByAlias (e.User.Name)[0];
                if (Program.aliasCollection.RemoveUser (user)) {
                    await e.Channel.SendMessage ("All your aliasses has been removed from the collection.");
                } else {
                    await e.Channel.SendMessage ("Couldn't find any aliasses in your name.");
                }
            }
        }
    }
}
