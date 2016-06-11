using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    public class Command {

        public string command = null;
        public string name = null;
        public string help = null;
        public int argumentNumber = 1;

        public bool isAdminOnly = false;

        public virtual async void ExecuteCommand ( MessageEventArgs e, List<string> arguments) {
            if (arguments.Count > 0 && arguments[0] == "?") {
                await e.Channel.SendMessage (help);
            }
        }

        public bool AllowExecution (List<string> args) {
            return argumentNumber == args.Count;
        }
    }

    public class CRollTheDice : Command {

        public CRollTheDice () {
            command = "rtd";
            name = "Roll the Dice";
            help = "\"!rtd <maxvalue>\" - Rolls a dice that returns a number between one and maxnumber.";
            argumentNumber = 1;
        }

        public override async void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (arguments)) {
                Random random = new Random ();
                int number;

                if (int.TryParse (arguments[0], out number)) {
                    await e.Channel.SendMessage ("You rolled " + (random.Next (number) + 1).ToString ());
                }
            }
        }
    }

    public class CCommandList : Command {

        public CCommandList () {
            command = "clist";
            name = "Command List";
            help = "\"!clist\" - Reveals a full list of all commands.";
            argumentNumber = 0;
        }

        public override async void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (arguments)) {
                string commands = "";
                for (int i = 0; i < Program.commands.Length; i++) {
                    commands += Program.commands[i].help + "\n";
                }
                await e.Channel.SendMessage (commands);
            }
        }
    }

    public class CCallVoiceChannel : Command {

        public CCallVoiceChannel () {
            command = "callvoice";
            name = "Mention Voice Channel";
            help = "\"!callvoice <channelname>;<message>\" - Mentions all members in a specific voice channel.";
            argumentNumber = 2;
        }

        public override async void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (arguments)) {

                List<Channel> voiceChannels = e.Server.VoiceChannels.ToList ();

                string text = "";
                for (int i = 0; i < voiceChannels.Count; i++) {
                    if (voiceChannels[i].Name.ToLower ().Substring (0, arguments[0].Length) == arguments[0].ToLower ()) {

                        List<User> users = voiceChannels[i].Users.ToList ();
                        for (int j = 0; j < users.Count; j++) {
                            text += users[j].Mention + " ";
                        }

                        break;
                    }
                }

                await e.Channel.SendMessage (e.User.Name + ": " + text + ", " + arguments[1]);
            }
        }
    }

    public class CCreateInvite : Command {

        public CCreateInvite () {
            command = "createinvite";
            name = "Create a Single Person Invite";
            help = "\"!createinvite\" - Creates a single person invite to this server.";
            argumentNumber = 0;
        }

        public override async void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (arguments)) {
                Invite invite = await e.Server.CreateInvite (1800, 1, false, false);
                await e.Channel.SendMessage ("Invite URL: " + invite.Url);
            }
        }
    }

    public class CSetAlias : Command {

        public CSetAlias () {
            command = "addalias";
            name = "Add Alias";
            help = "\"!addalias <alias>\" - Adds an alias to your collection, or creates a new collection if you don't have any.";
        }

        public override async void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (arguments)) {
                if (!Program.aliasCollection.AddAlias (e.User.Name, arguments[0])) {
                    await e.Channel.SendMessage ("Failed to add " + arguments[0] + " to your collection, as it is already there.");
                } else {
                    await e.Channel.SendMessage (arguments[0] + " added to your collection of aliasses.");
                }
            }
        }
    }

    public class CRemoveAlias : Command {

        public CRemoveAlias () {
            command = "removealias";
            name = "Remove Alias";
            help = "\"!removealias <alias>\" - Removes the alias from your collection.";
        }

        public override async void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (arguments)) {
                if (!Program.aliasCollection.RemoveAlias (e.User.Name, arguments[0])) {
                    await e.Channel.SendMessage ("Failed to remove " + arguments[0] + " from your collection, as it doesn't seem to be there.");
                } else {
                    await e.Channel.SendMessage (arguments[0] + " removed from your collection of aliasses.");
                }
            }
        }
    }

    public class CShowAlias : Command {
        
        public CShowAlias () {
            command = "showalias";
            name = "Show Alias";
            help = "\"!showalias <alias>\" - Finds and shows you the user that has this alias in their collection.";
        }

        public override async void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {

            base.ExecuteCommand (e, arguments);
            if (AllowExecution (arguments)) {
                AliasCollection.User user = Program.aliasCollection.FindUserByAlias (arguments[0]);
                if (user != null) {
                    if (user.aliasses.Count > 0) {
                        await e.Channel.SendMessage ("Showing aliasses for " + user.discordAlias);
                        string aliassesCombined = "";
                        for (int i = 0; i < user.aliasses.Count; i++) {
                            aliassesCombined += user.aliasses[i] + "\n";
                        }
                        await e.Channel.SendMessage (aliassesCombined);
                    } else {
                        await e.Channel.SendMessage ("No aliasses for this user found.");
                    }
                } else {
                    await e.Channel.SendMessage ("User not found in collection.");
                }
            }
        }
    }

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
                AliasCollection.User user = Program.aliasCollection.FindUserByAlias (e.User.Name);
                if (Program.aliasCollection.RemoveUser (user)) {
                    await e.Channel.SendMessage ("All your aliasses has been removed from the collection.");
                }else {
                    await e.Channel.SendMessage ("Couldn't find any aliasses in your name.");
                }
            }
        }
    }

    public class CSetColor : Command {

        public string[] allowed = new string[] {
            "GREEN", "RED", "YELLOW", "BLUE",
            "ORANGE", "PINK", "PURPLE", "WHITE",
            "DARKBLUE", "TURQUOISE", "MAGENTA",
            "GOLD"
        };

        public bool removePrevious = true;

        public string succesText = "Your color has now been set.";
        public string failText = "Color not found, these are the supported colors:\n";

        public CSetColor () {
            command = "setcolor";
            name = "Set Color";
            help = "\"!setcolor <colorname>\" - Sets your color to colorname, if available.";
            argumentNumber = 1;
        }

        public override async void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (arguments)) {

                if (allowed.Contains (arguments[0].ToUpper ())) {
                    Role[] roles = e.Server.FindRoles (arguments[0].ToUpper (), true).ToArray ();

                    if (roles.Length == 1) {
                        Role role = roles[0];

                        if (!role.Permissions.ManageRoles) {

                            if (removePrevious) {
                                List<Role> rList = new List<Role> ();
                                for (int i = 0; i < allowed.Length; i++) {
                                    rList.Add (e.Server.FindRoles (allowed[i], true).ToList ()[0]);
                                }

                                int removeTries = 5;
                                for (int i = 0; i < removeTries; i++) {
                                    await e.User.RemoveRoles (rList.ToArray ());
                                }
                            }

                            await e.User.AddRoles (role);
                            await e.Channel.SendMessage (succesText);
                        }
                    }
                } else {
                    string colors = "";
                    for (int i = 0; i < allowed.Length; i++) {
                        colors += allowed[i] + ", ";
                    }
                    await e.Channel.SendMessage (failText + colors);
                }
            }
        }
    }

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

    public class CRemoveGame : Command {

        public CRemoveGame () {
            command = "removegame";
            name = "Remove Game";
            help = "\"!removegame <gamename>\" - Removes you from the list of people with this game.";
        }

        public override async void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (arguments)) {

                if (CSetGame.games.Contains (arguments[0].ToUpper ())) {
                    Role[] roles = e.Server.FindRoles (arguments[0].ToUpper (), true).ToArray ();

                    if (roles.Length == 1) {
                        Role role = roles[0];
                        await e.User.RemoveRoles (role);
                    }
                }
            }
        }
    }
}
