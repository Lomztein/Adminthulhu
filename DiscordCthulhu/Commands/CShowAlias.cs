using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;

namespace DiscordCthulhu {
    public class CShowAlias : Command {

        public CShowAlias () {
            command = "showalias";
            name = "Show Alias";
            argHelp = "<alias>";
            help = "Finds and shows you the user that has the alias " + argHelp + " in their collection.";
        }

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {

            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                List<AliasCollection.User> users = Program.aliasCollection.FindUsersByAlias (arguments[0]);
                if (users != null) {
                    foreach (AliasCollection.User user in users)
                    {
                        if (user.aliasses.Count > 0)
                        {
                            await (e.Channel as SocketTextChannel).SendMessageAsync("Showing aliasses for " + user.discordAlias);
                            string aliassesCombined = "";
                            for (int i = 0; i < user.aliasses.Count; i++)
                            {
                                aliassesCombined += user.aliasses[i] + "\n";
                            }
                            await (e.Channel as SocketTextChannel).SendMessageAsync (aliassesCombined);
                        }
                        else
                        {
                            await (e.Channel as SocketTextChannel).SendMessageAsync ("No aliasses for this user found.");
                        }    
                    }
                }
                else
                {
                    Program.messageControl.SendMessage(e, "User not found in collection.");
                    //await e.Channel.SendMessage("User not found in collection.");
                }

            }
        }
    }
}
