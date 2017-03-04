using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace DiscordCthulhu {
    class CSetCommand : Command {

        public CSetCommand () {
            command = "setcommand";
            name = "Set Command";
            argHelp = "<commandname><enable(true/false);<allchannels(true/false)>";
            help = "Sets the " + argHelp + " command to be enabled or disabled on server. If \"all\" is input, all commands are added.";
            argumentNumber = 3;
            isAdminOnly = true;
        }

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            await base.ExecuteCommand (e, arguments);

            if (arguments[0] == "?") {
                string commandNames = "Available commands: ";
                for (int i = 0; i < Program.commands.Length; i++) {
                    commandNames += Program.commands[i].command + ", ";
                }
                await Program.messageControl.SendMessage (e, commandNames);
            }

            if (await AllowExecution (e, arguments)) {

                bool allChannels;
                bool enable;
                if (Boolean.TryParse (arguments[1], out enable) && Boolean.TryParse (arguments[2], out allChannels)) {

                    if (arguments[0].ToUpper () == "ALL") {
                        for (int i = 0; i < Program.commands.Length; i++) {
                            if (enable) {
                                await Program.commands[i].AddToChannel (e, allChannels);
                            } else {
                                await Program.commands[i].RemoveFromChannel (e, allChannels);
                            }
                        }
                    } else {
                        Command command = Program.FindCommand (arguments[0]);
                        if (command != null) {
                            if (enable) {
                                await command.AddToChannel (e, allChannels);
                            } else {
                                await command.RemoveFromChannel (e, allChannels);
                            }
                            await Program.messageControl.SendMessage (e, "Succesfully added command to this channel.");
                        } else {
                            await Program.messageControl.SendMessage (e, "Failed to add command: command now found.");
                        }
                    }
                }else {
                    await Program.messageControl.SendMessage (e, "Failed to execute: Failed to parse argument.");
                }
            }
        }
    }
}
