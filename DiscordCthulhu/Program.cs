using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using System.IO;
using System.Linq;
using Discord.WebSocket;

namespace DiscordCthulhu {
    class Program {

        public static char commandChar = '!';

        public static Command[] commands = new Command[] {
            new CCommandList (), new CSetColor (), new CRollTheDice (),
            new CFlipCoin (), new CRandomGame (), new CQuote (), new CSetCommand (), new CEmbolden (),
            new CEndTheWorld (), new CChangeScore (), new CShowScore (), new CFizzfyr (), new CSwiggity (),
            new CAddHeader (), new CShowHeaders (),
            new VoiceCommands (), new EventCommands (), new UserSettingsCommands (), new DebugCommands (), new HangmanCommands (),
            new GameCommands ()
        };

        public static string dataPath = "";
        public static AliasCollection aliasCollection = null;
        public static ScoreCollection scoreCollection = new ScoreCollection ();
        public static PlayerGroups playerGroups;
        public static MessageControl messageControl = null;
        public static Clock clock;
        public static string commandSettingsDirectory = "Command Settings/";
        public static string chatlogDirectory = "ChatLogs/";
        public static string resourceDirectory = "Resources/";
        public static string eventDirectory = "Event/";
        public static string gitHubIgnoreType = ".botproperty";

        static void Main ( string[] args ) => new Program ().Start (args).GetAwaiter().GetResult();

        public static DiscordSocketClient discordClient;

        private const int BOOT_WAIT_TIME = 5;
        private static DateTime bootedTime = new DateTime ();

        // SocketGuild data
        public static string mainTextChannelName = "main";
        public static string dumpTextChannelName = "dump";
        public static string serverName = "Monster Mash";
        public static SocketGuild server;

        public static Phrase[] phrases = new Phrase[] {
            new Phrase ("!", "", 100, "Please don't use bot commands in the main channel, so we avoid spamizzle forshizzle.", mainTextChannelName),
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
            new Phrase ("https://www.youtube.com/", "Gizmo Gizmo", 100, "Wow, this is some very interesting conte- <:residentsleeper:257933177631277056> Zzz", "links"),
            new Phrase ("", "khave", 2, "¯\\_(ツ)_/¯"),
            new Phrase ("(╯°□°）╯︵ ┻━┻", 100, "Please respect tables. ┬─┬ノ(ಠ_ಠノ)")
        };
        public static List<string> allowedDeletedMessages = new List<string>();

        // Feedback
        public static ulong authorID = 93744415301971968;

        public static ControlPanel controlPanel = null;

        public async Task Start (string[] args) {

            // Linux specific test
            if (args.Length > 0) {
                dataPath = args[0];
            } else {
                dataPath = Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly ().GetName ().CodeBase);
                dataPath = dataPath.Substring (dataPath.IndexOf ('\\') + 1);
                dataPath += "\\";
            }
            dataPath.Replace ("\\", "/");

            InitializeDirectories ();
            ChatLogger.Log ("Booting..");

            aliasCollection = await AliasCollection.Load ();
            scoreCollection.scores = await ScoreCollection.Load ();
            playerGroups = await PlayerGroups.Load ();
            clock = new Clock ();

            discordClient = new DiscordSocketClient ();
            messageControl = new MessageControl();
            //controlPanel = new ControlPanel (); //Removed due to being practically useless.

            InitializeData ();
            await InitializeCommands();
            await UserSettings.Initialize ();
            await UserGameMonitor.Initialize ();

            bootedTime = DateTime.Now.AddSeconds (BOOT_WAIT_TIME);

            discordClient.MessageReceived += async ( e ) => {

                ChatLogger.Log (GetChannelName (e) + " says: " + e.Content);
                if (e.Author != discordClient.CurrentUser  && e.Content.Length > 0 && e.Content[0] == commandChar) {
                    string message = e.Content;

                    if (message.Length > 0) {

                        message = message.Substring (1);
                        string command = "";
                        List<string> arguments = ConstructArguments (message, out command);

                        await FindAndExecuteCommand (e, command, arguments, commands);
                    }
                } else if (e.Author == discordClient.CurrentUser) {
                    if (messageControl.messages.Count > 0) {
                        foreach (MessageTimer messageTimer in messageControl.messages) {
                            if (messageTimer.message == e.Content) {
                                ChatLogger.Log ("Removed!");
                                messageControl.RemoveMessageTimer (messageTimer);
                                break;
                            }
                        }
                    }
                }

                await FindPhraseAndRespond(e);

                if (e.Content.Length > 0 && e.Content[0] == commandChar) {
                    await e.DeleteAsync ();
                    allowedDeletedMessages.Add (e.Content);
                }
            };

            discordClient.UserJoined += async ( e ) => {
                await messageControl.SendMessage (GetMainChannel (e.Guild) as SocketTextChannel, "**" + e.Username + "** has joined this server. Bid them welcome or murder them in cold blood, it's really up to you.");

                string[] welcomeMessage = SerializationIO.LoadTextFile (dataPath + "welcomemessage" + gitHubIgnoreType);
                string combined = "";
                for (int i = 0; i < welcomeMessage.Length; i++) {
                    combined += welcomeMessage[i] + "\n";
                }

                await messageControl.SendMessage (e, combined);
            };

            discordClient.UserLeft += async ( e ) => {
                await messageControl.SendMessage (GetMainChannel (e.Guild) as SocketTextChannel, "**" + GetUserName (e) + "** has left the server. Don't worry, they'll come crawling back soon.");
            };

            discordClient.UserUpdated += async ( before, after ) => {
                SocketGuildUser beforeUser = (before as SocketGuildUser);
                SocketGuildUser afterUser = (after as SocketGuildUser);
                SocketGuild guild = beforeUser.Guild;

                // Maybe, just maybe put these into a single function.
                if (FullyBooted ()) {

                    if (beforeUser.VoiceChannel != afterUser.VoiceChannel) {
                        // Voice channel change detected.
                        ChatLogger.Log ("Voice channel change detected with user " + before.Username);

                        await AutomatedVoiceChannels.AddMissingChannels (guild);
                        await AutomatedVoiceChannels.CheckFullAndAddIf (guild);
                        AutomatedVoiceChannels.RemoveLeftoverChannels (guild);
                    }

                    await AutomatedVoiceChannels.UpdateVoiceChannel (beforeUser.VoiceChannel);
                    await AutomatedVoiceChannels.UpdateVoiceChannel (afterUser.VoiceChannel);
                }

                SocketGuildChannel channel = GetMainChannel (afterUser.Guild);
                if (channel == null)
                    return;

                if (beforeUser.Username != afterUser.Username) {
                    await messageControl.SendMessage (channel as SocketTextChannel, "**"+ GetUserUpdateName (beforeUser, afterUser, true) + "** has changed their name to **" + afterUser.Username + "**");
                }

                if (beforeUser.Nickname != afterUser.Nickname) {
                    await messageControl.SendMessage (channel as SocketTextChannel, "**" + GetUserUpdateName (beforeUser, afterUser, true) + "** has changed their nickname to **" + GetUserUpdateName (beforeUser, afterUser, false) + "**");
                }
            };

            discordClient.UserBanned += async ( e, guild ) => {
                SocketChannel channel = GetMainChannel (guild);
                if (channel == null)
                    return;

                await messageControl.SendMessage (channel as SocketTextChannel, "**" + GetUserName (e as SocketGuildUser) + "** has been banned from this server, they will not be missed.");
                await messageControl.SendMessage (e as SocketGuildUser, "Sorry to tell you like this, but you have been permabanned from Monster Mash. ;-;");
            };

            discordClient.UserUnbanned += async ( e, guild ) => {
                SocketChannel channel = GetMainChannel (guild);
                if (channel == null)
                    return;

                await messageControl.SendMessage (channel as SocketTextChannel, "**" + GetUserName (e as SocketGuildUser) + "** has been unbanned from this server, They are once more welcome in our arms of glory.");
                await messageControl.SendMessage (e as SocketGuildUser, "You have been unbanned from Monster Mash, we love you once more! :D");
            };

            discordClient.MessageDeleted += async (index, e) => {
                SocketGuildChannel channel = SearchChannel ((e.Value.Channel as SocketGuildChannel).Guild, dumpTextChannelName);
                if (channel == null)

                if (!allowedDeletedMessages.Contains (e.Value.Content)) {
                    await messageControl.SendMessage (channel as SocketTextChannel, "In order disallow *any* secrets except for admin secrets, I'd like to tell you that **" + GetUserName (e.Value.Author as SocketGuildUser) + "** just had a message deleted on **" + e.Value.Channel.Name + "**.");
                }else {
                    allowedDeletedMessages.Remove (e.Value.Content);
                }
            };

            discordClient.Ready += () => {
                ChatLogger.Log ("Bot is ready and running!");
                return Task.CompletedTask;
            };

            ChatLogger.Log (dataPath);

            string token = SerializationIO.LoadTextFile (dataPath + "bottoken" + gitHubIgnoreType)[0];

            Console.WriteLine ("Connecting using token: " + token);
            await discordClient.LoginAsync (TokenType.Bot, token);
            await discordClient.StartAsync ();

            await Task.Delay (-1);
        }   

        private static int maxTries = 5;
        public static async Task SecureAddRole (SocketGuildUser user, SocketRole role) {
            int tries = 0;
            while (!user.Roles.Contains (role)) {
                if (tries > maxTries) {
                    await messageControl.SendMessage (SearchChannel (GetServer (), dumpTextChannelName) as SocketTextChannel, "Error - tried to add role too many times.");
                    break;
                }
                tries++;
                ChatLogger.Log ("Adding role to " + user.Username + " - " + role.Name);
                await (user.AddRolesAsync (role));
                await Task.Delay (5000);
            }
        }

        public static List<SocketGuildUser> ForceGetUsers (SocketGuildChannel channel) {
            List<SocketGuildUser> result = new List<SocketGuildUser> ();
            foreach (SocketGuildUser u in channel.Users) {
                result.Add (GetServer ().GetUser (u.Id));
            }
            return result;
        }

        public static async Task SecureRemoveRole ( SocketGuildUser user, SocketRole role ) {
            int tries = 0;
            while (user.Roles.Contains (role)) {
                if (tries > maxTries) {
                    await messageControl.SendMessage (SearchChannel (GetServer (), dumpTextChannelName) as SocketTextChannel, "Error - tried to remove role too many times.");
                    break;
                }
                ChatLogger.Log ("Removing role from " + user.Username + " - " + role.Name);
                tries++;
                await (user.RemoveRolesAsync (role));
                await Task.Delay (5000);
            }
        }

        private static bool hasBooted = false;
        public static bool FullyBooted () {
            if (hasBooted)
                return hasBooted;

            if (DateTime.Now > bootedTime) {
                hasBooted = true;
                ChatLogger.Log ("Booted flag set to true.");
            }
            return hasBooted;
        }

        public static List<string> ConstructArguments (string fullCommand, out string command) {
            string toSplit = fullCommand.Substring (fullCommand.IndexOf (' ') + 1);
            List<string> arguments = new List<string> ();
            command = "";

            if (fullCommand.LastIndexOf (' ') != -1) {
                // FEEL THE SPAGHETTI.
                command = fullCommand.Substring (0, fullCommand.Substring(1).IndexOf (' ') + 1);
                string[] loc = toSplit.Split (';');
                for (int i = 0; i < loc.Length; i++) {
        
                    loc[i] = TrimSpaces (loc[i]);
                    arguments.Add (loc[i]);
                }
            }else {
                command = fullCommand;
            }

            return arguments;
        }

        public static SocketGuild GetServer (SocketChannel channel) {
            return (channel as SocketGuildChannel).Guild;
        }

        public static string GetUserName (SocketGuildUser user) {
            if (user == null)
                return "[ERROR - NULL USER REFERENCE]";
            if (user.Nickname == null)
                return user.Username;
            return user.Nickname;
        }

        public string GetUserUpdateName (SocketGuildUser beforeUser, SocketGuildUser afterUser, bool before) {
            if (before) {
                if (beforeUser.Nickname == null)
                    return afterUser.Username;
                return beforeUser.Nickname;
            } else {
                if (afterUser.Nickname == null)
                    return afterUser.Username;
                return afterUser.Nickname;
            }
        }

        public static SocketGuildChannel GetMainChannel (SocketGuild server) {
            return SearchChannel (server, mainTextChannelName);
        }

        [Obsolete]
        public static SocketGuildChannel GetChannelByName (SocketGuild server, string name) {
            if (server == null)
                return null;

            SocketGuildChannel channel = SearchChannel (server, name);
            return channel;
        }

        public static SocketGuildChannel SearchChannel (SocketGuild server, string name) {
            IEnumerable<SocketGuildChannel> channels = server.Channels;
            foreach (SocketGuildChannel channel in channels) {
                if (channel.Name.Length >= name.Length && channel.Name.Substring (0, name.Length) == name)
                    return channel;
            }

            return null;
        }

        private void InitializeData () {
            AutomatedVoiceChannels.InitializeData ();
        }

        public static SocketGuild GetServer () {
            if (server != null)
                return server;

            if (discordClient != null) {
                IEnumerable<SocketGuild> servers = discordClient.Guilds;
                if (servers.Count () != 0)
                    server = servers.ElementAt (0);
            }else {
                return null;
            }

            return server;
        }

        // I thought the TrimStart and TrimEnd functions would work like this, which they may, but I couldn't get them working. Maybe I'm just an idiot, but whatever.
        private static string TrimSpaces (string input) {
            if (input.Length == 0)
                return " ";

            string trimmed = input;
            while (trimmed[0] == ' ') {
                trimmed = trimmed.Substring (1);
            }

            while (trimmed[trimmed.Length - 1] == ' ') {
                trimmed = trimmed.Substring (0, trimmed.Length - 1);
            }
            return trimmed;
        }

        public static void InitializeDirectories () {
            CreateAbsentDirectory (dataPath + commandSettingsDirectory);
            CreateAbsentDirectory (dataPath + chatlogDirectory);
            CreateAbsentDirectory (dataPath + chatlogDirectory);
            CreateAbsentDirectory (dataPath + eventDirectory);
        }

        public static void CreateAbsentDirectory (string path) {
            if (!Directory.Exists (path))
                Directory.CreateDirectory (path);
        }

        public static async Task InitializeCommands () {
            for (int i = 0; i < commands.Length; i++) {
                await commands[i].Initialize ();
            }
        }

        public static Command FindCommand (string commandName) {
            for (int i = 0; i < commands.Length; i++) {
                if (commands[i].command.ToUpper () == commandName.ToUpper ())
                    return commands[i];
            }
            return null;
        }

        public static async Task<bool> FindAndExecuteCommand (SocketMessage e, string commandName, List<string> arguements, Command[] commandList) {
            for (int i = 0; i < commandList.Length; i++) {
                if (commandList[i].command == commandName) {
                    await commandList[i].ExecuteCommand (e, arguements);
                    return true;
                }
            }

            return false;
        }

        public static SocketGuildUser FindUserByName (SocketGuild server, string username) {
            foreach (SocketGuildUser user in server.Users) {
                string name = GetUserName (user).ToLower ();
                if (name.Length >= username.Length && name.Substring (0, username.Length) == username.ToLower ())
                    return user;
            }
            return null;
        }

        public async Task FindPhraseAndRespond ( SocketMessage e ) {  
            for (int i = 0; i < phrases.Length; i++) {
                if (await phrases[i].CheckAndRespond (e))
                    return;
            }
        }

        public static string GetChannelName (SocketMessage e) {
            if (e.Channel as SocketDMChannel != null) {
                return "Private message: " + e.Author.Username;
            }else {
                return (e.Channel as SocketGuildChannel).Name + "/" + e.Channel.Name + "/" + e.Author.Username;
            }
        }
    }
}
