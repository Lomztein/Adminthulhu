using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Adminthulhu {
    public class DebugCommands : CommandSet {
        public DebugCommands () {
            command = "debug";
            name = "Debug Command Set";
            help = "A set of debugging commands for *admins*.";
            isAdminOnly = true;
            commandsInSet = new Command[] { new ResetUserActivity () };
        }

        public class ResetUserActivity : Command {
            public ResetUserActivity () {
                command = "resetuseractivity";
                name = "Reset User Activity";
                argHelp = "<userid>";
                help = "Resets the user with id " + argHelp + "'s activity, for debugging reasons.";
                argumentNumber = 1;
            }

            public async override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
                base.ExecuteCommand (e, arguments);
                if (AllowExecution (e, arguments)) {
                    ulong parse;
                    if (ulong.TryParse (arguments[0], out parse)) {
                        SocketGuildUser user = Program.GetServer ().GetUser (parse);
                        if (user != null) {
                            await Task.Delay (1000);
                            UserActivityMonitor.userActivity.Remove (user.Id);
                            UserActivityMonitor.lastUserUpdate.Remove (user.Id);
                            Program.messageControl.SendMessage (e, "Succesfully reset activity of " + Program.GetUserName (user));
                        }else {
                            Program.messageControl.SendMessage (e, "Failed to reset - could not find user.");
                        }
                    } else {
                        Program.messageControl.SendMessage (e, "Failed to reset - could not parse user ID.");
                    }
                }
            }
        }
    }
}
