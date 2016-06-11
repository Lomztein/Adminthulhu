using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    public class CShowAlias : Command {

        public CShowAlias () {
            command = "showalias";
            name = "Show Alias";
            help = "\"!showalias <alias>\" - Finds and shows you the user that has this alias in their collection.";
        }

        public override async void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {

            base.ExecuteCommand (e, arguments);
            if (AllowExecution (arguments)) {
                List<AliasCollection.User> users = Program.aliasCollection.FindUsersByAlias (arguments[0]);
                if (users != null) {
                    foreach (AliasCollection.User user in users)
                    {
                        if (user.aliasses.Count > 0)
                        {
                            await e.Channel.SendMessage("Showing aliasses for " + user.discordAlias);
                            string aliassesCombined = "";
                            for (int i = 0; i < user.aliasses.Count; i++)
                            {
                                aliassesCombined += user.aliasses[i] + "\n";
                            }
                            await e.Channel.SendMessage(aliassesCombined);
                        }
                        else
                        {
                            await e.Channel.SendMessage("No aliasses for this user found.");
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
