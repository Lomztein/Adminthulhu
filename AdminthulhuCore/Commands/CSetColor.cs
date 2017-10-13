using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Adminthulhu {

    public class CSetColor : Command, IConfigurable {

        public static string [ ] allowed = new string [ ] {
            "GREEN", "RED", "YELLOW", "BLUE",
            "ORANGE", "PINK", "PURPLE", "WHITE",
            "DARKBLUE", "TURQUOISE", "MAGENTA",
            "GOLD", "BLACK", "DARKRED", "BROWN"
        };

        public static bool autoAddOnJoin = false;
        public bool removePrevious = true;

        public string succesText = "Your color has now been set.";
        public string failText = "Color not found, these are the supported colors:\n";

        public CSetColor() {
            command = "setcolor";
            shortHelp = "Set username color.";
            catagory = Category.Utility;

            AddOverload (typeof (SocketRole), "Sets your color to <color>, if available.");
        }

        public override void Initialize() {
            base.Initialize ();
            LoadConfiguration ();
            BotConfiguration.AddConfigurable (this);

            Program.discordClient.UserJoined += (user) => {
                if (autoAddOnJoin) {
                    try {
                        Random random = new Random ();
                        SocketRole color = Utility.GetServer ().Roles.Where (x => allowed.Contains (x.Name)).ElementAt (random.Next (0, allowed.Length));
                        Utility.SecureAddRole (user, color);
                    } catch (Exception e) {
                        Logging.Log (e);
                    }
                }
                return Task.CompletedTask;
            };
        }

        public async Task<Result> Execute(SocketUserMessage e, string color) {
            if (allowed.Contains (color.ToUpper ())) {
                SocketRole [ ] roles = (e.Channel as SocketGuildChannel).Guild.Roles.Where (x => allowed.Contains (x.Name)).ToArray ();
                SocketRole toAdd = roles.Where (x => x.Name == color.ToUpper ()).ElementAt (0);

                SocketGuildUser user = e.Author as SocketGuildUser;
                foreach (SocketRole role in roles) {
                    if (user.Roles.Contains (role)) {
                        await Utility.SecureRemoveRole (user, role);
                    }
                }

                await Utility.SecureAddRole (user, toAdd);
                return new Result (toAdd, $"Your color has been set to {toAdd.Name}.");
            } else {
                string colors = "";
                for (int i = 0; i < allowed.Length; i++) {
                    colors += allowed [ i ] + ", ";
                }
                return new Result (null, failText + colors);
            }
        }

        public static SocketRole GetUserColor(ulong userID) {
            SocketGuildUser user = Utility.GetServer ().GetUser (userID);
            if (user != null) {
                IEnumerable<SocketRole> roles = user.Roles.Where (x => allowed.Contains (x.Name)); // Ew, Contains () functions D:
                if (roles.Count () == 0)
                    return Utility.GetServer ().EveryoneRole;
                return roles.FirstOrDefault ();
            }

            return Utility.GetServer ().EveryoneRole;
        }

        public override void LoadConfiguration() {
            base.LoadConfiguration ();
            allowed = BotConfiguration.GetSetting("Misc.AvailableUsernameColors", "AvaiableUsernameColors", new string [ ] { "RED", "BLUE" });
            autoAddOnJoin = BotConfiguration.GetSetting ("Misc.AutoSetColorsOnJoin", "", autoAddOnJoin);
        }
    }
}
