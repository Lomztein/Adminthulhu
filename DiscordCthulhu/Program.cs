using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using System.IO;
using System.Linq;

namespace DiscordCthulhu {
    class Program {

        public static char commandChar = '!';

        public static Command[] commands = new Command[] {
            new CCommandList (), new CRollTheDice (), new CCallVoiceChannel (), new CSetColor (),
            new CFlipCoin (), new CRandomGame (), new CQuote (), new CSetCommand (), new CEmbolden (),
            new CEndTheWorld (), new CChangeScore (), new CShowScore (), new CFizzfyr (), new CSwiggity ()
        };

        public static Phrase[] phrases = new Phrase[] {
            new Phrase ("Neat!", "", 100, "Very!"),
            new Phrase ("", "Nyx", 1, "*Allegedly...*"),
            new Phrase ("", "Peacekeeper", 2, "*It's always crits..*"),
            new Phrase ("wow!", "Gizmo Gizmo", 100, "INSANE AIR TIME!"),
            new Phrase ("Thx fam", "", 100, "No props. We Gucci."),
            new Phrase ("Fuck yis", "Lomztein", 100, "Fuck no."),
            new Phrase ("<:Serviet:255721870828109824> Privet Comrades!", "Creeperskull", 100, "Privet, federal leader!"),
            new Phrase ("<:Serviet:255721870828109824> Privet Comrades!", "", 100, "Privet!"),
            new Phrase ("Who is best gem?", "Nyx", 100, "*Obviously* <:Lapis:230346614064021505> ..."),
            new Phrase ("Who is best gem?", "", 100, "Obviously <:PeriWow:230381627669348353>"),
            new Phrase ("https://www.reddit.com/r/overwatch", "Gizmo Gizmo", 100, "Wow, this is some very interesting conte- <:residentsleeper:257933177631277056> Zzz", "links"),
            new Phrase ("", "khave", 2, "¯\\_(ツ)_/¯"),
            new Phrase ("(╯°□°）╯︵ ┻━┻", 100, "Please respect tables. ┬─┬ノ(ಠ_ಠノ)")
        };

        public static string dataPath = "";
        public static AliasCollection aliasCollection = null;
        public static ScoreCollection scoreCollection = new ScoreCollection ();
        public static PlayerGroups playerGroups;
        public static MessageControl messageControl = null;
        public static string commandSettingsDirectory = "Command Settings/";
        public static string chatlogDirectory = "ChatLogs/";
        public static string resourceDirectory = "Resources/";
        public static string gitHubIgnoreType = ".botproperty";

        static void Main ( string[] args ) => new Program ().Start (args);

        public static DiscordClient discordClient;

        // Monster Mash data
        public static string mainTextChannelName = "main";
        public static string dumpTextChannelName = "dump";
        public static string serverName = "Monster Mash";
        public static Server server;

        // Feedback
        public static ulong authorID = 93744415301971968;

        public static ControlPanel controlPanel = null;

        public void Start (string[] args) {

            ChatLogger.Log ("Booting..");

            InitializeDirectories ();

            // Linux specific test
            if (args.Length > 0) {
                dataPath = args[0];
            } else {
                dataPath = System.IO.Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly ().GetName ().CodeBase);
                dataPath = dataPath.Substring (dataPath.IndexOf ('\\') + 1);
                dataPath += "\\";
            }

            aliasCollection = AliasCollection.Load ();
            scoreCollection.scores = ScoreCollection.Load ();
            playerGroups = PlayerGroups.Load ();

            discordClient = new DiscordClient ();
            messageControl = new MessageControl();
            controlPanel = new ControlPanel ();

            InitializeData ();
            InitializeCommands ();

            discordClient.MessageReceived += async ( s, e ) => {

                ChatLogger.Log (GetChannelName (e) + " says: " + e.Message.Text);
                if (!e.Message.IsAuthor && e.Message.Text.Length > 0 && e.Message.Text[0] == commandChar) {
                    string message = e.Message.Text;

                    string command = message.Substring (1);
                    List<string> arguments = new List<string> ();

                    if (message.LastIndexOf (' ') != -1) {
                        command = message.Substring (1, message.IndexOf (' ') - 1);
                        string[] loc = message.Substring (message.IndexOf (' ') + 1).Split (';');
                        for (int i = 0; i < loc.Length; i++) {

                            loc[i] = TrimSpaces (loc[i]);
                            arguments.Add (loc[i]);
                        }
                    }

                    await FindAndExecuteAsync (e, command, arguments);
                }
                else if (e.Message.IsAuthor)
                {
                    if (messageControl.messages.Count > 0)
                    {
                        foreach(MessageTimer messageTimer in messageControl.messages)
                        {
                            if (messageTimer.message == e.Message.RawText)
                            {
                                ChatLogger.Log("Removed!");
                                messageControl.RemoveMessageTimer(messageTimer);
                                break;
                            }
                        }
                    }
                }

                FindPhraseAndRespond (e);
            };

            discordClient.UserJoined += ( s, e ) => {
                messageControl.SendMessage (GetMainChannel (e.Server), "**" + e.User.Name + "** has joined this server. Bid them welcome or murder them in cold blood, it's really up to you.");

                string[] welcomeMessage = SerializationIO.LoadTextFile (dataPath + "welcomemessage" + gitHubIgnoreType);
                string combined = "";
                for (int i = 0; i < welcomeMessage.Length; i++) {
                    combined += welcomeMessage[i] + "\n";
                }

                messageControl.SendMessage (e.User, combined);
            };

            discordClient.UserLeft += ( s, e ) => {
                messageControl.SendMessage (GetMainChannel (e.Server), "**" + e.User.Name + "** has left the server. Don't worry, they'll come crawling back soon.");
            };

            discordClient.UserUpdated += ( s, e ) => {
                // Maybe, just maybe put these into a single function.
                AutomatedVoiceChannels.AddMissingChannels (e.Server);
                AutomatedVoiceChannels.UpdateVoiceChannel (e.Before.VoiceChannel);
                AutomatedVoiceChannels.UpdateVoiceChannel (e.After.VoiceChannel);
                AutomatedVoiceChannels.CheckFullAndAddIf (e.Server);
                AutomatedVoiceChannels.RemoveLeftoverChannels (e.Server);

                Channel channel = GetMainChannel (e.Server);
                if (channel == null)
                    return;

                if (e.Before.Name != e.After.Name) {
                    messageControl.SendMessage (channel, "**"+ GetUserUpdateName (e, true) + "** has changed their name to **" + e.After.Name + "**");
                }

                if (e.Before.Nickname != e.After.Nickname) {
                    messageControl.SendMessage (channel, "**" + GetUserUpdateName (e, true) + "** has changed their nickname to **" + GetUserUpdateName (e, false) + "**");
                }
            };

            discordClient.UserBanned += ( s, e ) => {
                Channel channel = GetMainChannel (e.Server);
                if (channel == null)
                    return;

                messageControl.SendMessage (channel, "**" + GetUserName (e.User) + "** has been banned from this server, they will not be missed.");
                messageControl.SendMessage (e.User, "Sorry to tell you like this, but you have been permabanned from Monster Mash. ;-;");
            };

            discordClient.UserUnbanned += ( s, e ) => {
                Channel channel = GetMainChannel (e.Server);
                if (channel == null)
                    return;

                messageControl.SendMessage (channel, "**" + GetUserName (e.User) + "** has been unbanned from this server, They are once more welcome in our arms of glory.");
                messageControl.SendMessage (e.User, "You have been unbanned from Monster Mash, we love you once more! :D");
            };

            discordClient.MessageDeleted += ( s, e ) => {
                Channel channel = SearchChannel (e.Server, dumpTextChannelName);
                if (channel == null)
                    return;

                messageControl.SendMessage (channel, "In order disallow *any* secrets except for admin secrets, I'd like to tell you that **" + GetUserName (e.User) + "** just deleted a message on **" + e.Channel.Name + "**.");
            };

            discordClient.Ready += (s, e) => {
                ChatLogger.Log ("Bot is ready and running!");
            };

            ChatLogger.Log (dataPath);
            string token = SerializationIO.LoadTextFile (dataPath + "bottoken" + gitHubIgnoreType)[0];
            ChatLogger.Log ("Connecting using token: " + token);

            discordClient.ExecuteAndWait (async () => {
                await discordClient.Connect (token, TokenType.Bot);
            });
        }

        public string GetUserName (User user) {
            if (user != null)
                return "[ERROR - NULL USER REFERENCE]";
            if (user.Nickname == null)
                return user.Name;
            return user.Nickname;
        }

        public string GetUserUpdateName (UserUpdatedEventArgs e, bool before) {
            if (before) {
                if (e.Before.Nickname == null)
                    return e.Before.Name;
                return e.Before.Nickname;
            } else {
                if (e.After.Nickname == null)
                    return e.After.Name;
                return e.After.Nickname;
            }
        }

        public static Channel GetMainChannel (Server server) {
            return SearchChannel (server, mainTextChannelName);
        }

        [Obsolete]
        public static Channel GetChannelByName (Server server, string name) {
            if (server == null)
                return null;

            Channel channel = SearchChannel (server, name);
            return channel;
        }

        public static Channel SearchChannel (Server server, string name) {
            IEnumerable<Channel> channels = server.AllChannels;
            foreach (Channel channel in channels) {
                if (channel.Name.Length >= name.Length && channel.Name.Substring (0, name.Length) == name)
                    return channel;
            }

            return null;
        }

        private void InitializeData () {
            AutomatedVoiceChannels.InitializeData ();
        }

        public static Server GetServer () {
            if (server != null)
                return server;

            IEnumerable<Server> servers = discordClient.FindServers (serverName);
            if (servers.Count () != 0)
                server = servers.ElementAt (0);

            return server;
        }

        // I thought the TrimStart and TrimEnd functions would work like this, which they may, but I couldn't get them working. Maybe I'm just an idiot, but whatever.
        private string TrimSpaces (string input) {
            string trimmed = input;
            while (trimmed[0] == ' ')
                trimmed = trimmed.Substring (1);

            while (trimmed[trimmed.Length - 1] == ' ')
                trimmed = trimmed.Substring (0, trimmed.Length - 1);
            return trimmed;
        }

        public static void InitializeDirectories () {
            CreateAbsentDirectory (dataPath + commandSettingsDirectory);
            CreateAbsentDirectory (dataPath + chatlogDirectory);
        }

        public static void CreateAbsentDirectory (string path) {
            if (!Directory.Exists (path))
                Directory.CreateDirectory (path);
        }

        public static void InitializeCommands () {
            for (int i = 0; i < commands.Length; i++) {
                commands[i].Initialize ();
            }
        }

        public Task FindAndExecuteAsync ( MessageEventArgs e, string commandName, List<string> arguements ) {
            return Task.Run (() => FindAndExecuteCommand (e, commandName, arguements));
        }

        public static Command FindCommand (string commandName) {
            for (int i = 0; i < commands.Length; i++) {
                if (commands[i].command.ToUpper () == commandName.ToUpper ())
                    return commands[i];
            }
            return null;
        }

        public bool FindAndExecuteCommand (MessageEventArgs e, string commandName, List<string> arguements) {
            for (int i = 0; i < commands.Length; i++) {
                if (commands[i].command == commandName) {
                    commands[i].ExecuteCommand (e, arguements);
                    return true;
                }
            }

            return false;
        }

        public static User FindUserByName (Server server, string username) {
            foreach (User user in server.Users) {
                if (user.Nickname == username)
                    return user;
            }
            return null;
        }

        public void FindPhraseAndRespond ( MessageEventArgs e ) {
            for (int i = 0; i < phrases.Length; i++) {
                if (phrases[i].CheckAndRespond (e))
                    return;
            }
        }

        public static string GetChannelName (MessageEventArgs e) {
            if (e.Channel.IsPrivate) {
                return "Private message: " + e.User.Name;
            }else {
                return e.Server.Name + "/" + e.Channel.Name + "/" + e.User.Name;
            }
        }
    }
}
