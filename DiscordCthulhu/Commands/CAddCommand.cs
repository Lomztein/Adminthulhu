using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    class CSetCommand : Command {

        public CSetCommand () {
            Initialize ();
            command = "setcommand";
            name = "Set Command";
            argHelp = "<commandname><enable(true/false);<allchannels(true/false)>";
            help = "Sets the " + argHelp + " command to be enabled or disabled on server. If \"all\" is input, all commands are added.";
            argumentNumber = 3;
            isAdminOnly = true;
        }

        public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);

            if (arguments[0] == "?") {
                string commandNames = "Available commands: ";
                for (int i = 0; i < Program.commands.Length; i++) {
                    commandNames += Program.commands[i].command + ", ";
                }
                Program.messageControl.SendMessage (e, commandNames);
            }

            if (AllowExecution (e, arguments)) {

                bool allChannels;
                bool enable;
                if (Boolean.TryParse (arguments[1], out enable) && Boolean.TryParse (arguments[2], out allChannels)) {

                    if (arguments[0].ToUpper () == "ALL") {
                        for (int i = 0; i < Program.commands.Length; i++) {
                            if (enable) {
                                Program.commands[i].AddToChannel (e, allChannels);
                            } else {
                                Program.commands[i].RemoveFromChannel (e, allChannels);
                            }
                        }
                    } else {
                        Command command = Program.FindCommand (arguments[0]);
                        if (command != null) {
                            if (enable) {
                                command.AddToChannel (e, allChannels);
                            } else {
                                command.RemoveFromChannel (e, allChannels);
                            }
                            Program.messageControl.SendMessage (e, "Succesfully added command to this channel.");
                        } else {
                            Program.messageControl.SendMessage (e, "Failed to add command: command now found.");
                        }
                    }
                }else {
                    Program.messageControl.SendMessage (e, "Failed to execute: Failed to parse argument.");
                }
            }
        }
    }
}
