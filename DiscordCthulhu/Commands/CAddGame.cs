﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    public class CSetGame : CSetColor {

        public static string[] games = new string[] { "OVERWATCH", "TF2", "GMOD" };

        public CSetGame () {
            command = "addgame";
            name = "Set Game";
            help = "\"!addgame <gamename>\" - Adds you to this game, allowing people to mention all with it.";
            removePrevious = false;
            succesText = "You have been added to that game.";
            failText = "That game could not be found, current games supported are:\n";
            allowed = games;
        }
    }
}