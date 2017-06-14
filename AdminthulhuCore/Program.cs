using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Adminthulhu
{
    class Program {

        public static char commandChar = '!';

        public static Command[] commands = new Command[] {
            new CCommandList (), new CSetColor (), new CRollTheDice (),
            new CFlipCoin (), new CRandomGame (), new CQuote (), new CEmbolden (),
            new CEndTheWorld (), new CFizzfyr (), new CSwiggity (),
            new CAddHeader (), new CShowHeaders (), new CKarma (), new CReport (),
            new VoiceCommands (), new EventCommands (), new UserSettingsCommands (), new DebugCommands (), new HangmanCommands (),
            new GameCommands (), new StrikeCommandSet (), new CAddEventGame (), new CRemoveEventGame (), new CHighlightEventGame (),
        };

        public static string dataPath = "";
        public static MessageControl messageControl = null;
        public static Clock clock;
        public static Karma karma;

        public static string commandSettingsDirectory = "Command Settings/";
        public static string chatlogDirectory = "ChatLogs/";
        public static string resourceDirectory = "Resources/";
        public static string eventDirectory = "Event/";
        public static string gitHubIgnoreType = ".botproperty";
        public static string avatarPath = "avatar.jpg";

        public static void Main (string[] args) {
            new Program ().ErrorCatcher (args);
        }

        public static DiscordSocketClient discordClient;

        private const int BOOT_WAIT_TIME = 5;
        private static DateTime bootedTime = new DateTime ();

        // SocketGuild data
        public static string mainTextChannelName = "main";
        public static string dumpTextChannelName = "dump";
        public static string serverName = "Monster Mash";
        public static ulong serverID = 93733172440739840;

        public static Phrase [ ] phrases = new Phrase [ ] {
            new Phrase ("Neat!", "", 100, "Very!"),
            new Phrase ("", "Nyx", 1, "*Allegedly...*"),
            new Phrase ("", "Peacekeeper", 2, "*It's always crits..*"),
            new Phrase ("wow!", "Gizmo Gizmo", 100, "INSANE AIR TIME!"),
            new Phrase ("Thx fam", "", 100, "No probs. We Gucci."),
            new Phrase ("Fuck yis", "Lomztein", 100, "Fuck no."),
            new Phrase ("<:Serviet:255721870828109824> Privet Comrades!", "Creeperskull", 100, "Privet, federal leader!"),
            new Phrase ("<:Serviet:255721870828109824> Privet Comrades!", "", 100, "Privet!"),
            new Phrase ("Who is best gem?", "Nyx", 100, "*Obviously* <:Lapis:230346614064021505> ..."),
            new Phrase ("Who is best gem?", "", 100, "Obviously <:PeriWow:230381627669348353>"),
            new Phrase ("https://www.reddit.com/", "Nyx", 100, "Wow, this is some very interesting conte- <:residentsleeper:257933177631277056> Zzz", "links"),
            new Phrase ("", "khave", 2, "¯\\_(ツ)_/¯"),
            new Phrase ("(╯°□°）╯︵ ┻━┻", 100, "Please respect tables. ┬─┬ノ(ಠ_ಠノ)"),
            new Phrase ("nice", "Twistbonk", 25, "Very nice!"),
            new Phrase ("Neato", "", 100, "Burrito!"),
        };
        public static List<string> allowedDeletedMessages = new List<string>();

        // Feedback
        public static ulong authorID = 93744415301971968;

        public async Task ErrorCatcher (string[] args) {
            try {
                await new Program ().Start (args);
            } catch (Exception e) {
                Console.WriteLine (e.Message);
            }
        }

        public async Task Start (string[] args) {

            // Linux specific test
            if (args.Length > 0) {
                dataPath = args[0] + "/data/";
            } else {
                dataPath = AppContext.BaseDirectory + "/data/";
            }

            dataPath = dataPath.Replace ('\\', '/');
            InitializeDirectories ();
            ChatLogger.Log ("Booting.. Datapath: " + dataPath);

            clock = new Clock ();

            discordClient = new DiscordSocketClient ();
            messageControl = new MessageControl ();
            karma = new Karma ();

            InitializeData ();
            InitializeCommands ();
            UserSettings.Initialize ();
            UserGameMonitor.Initialize ();

            bootedTime = DateTime.Now.AddSeconds (BOOT_WAIT_TIME);

            discordClient.MessageReceived += (e) => {

                ChatLogger.Log (Utility.GetChannelName (e) + " says: " + e.Content);
                if (e.Author != discordClient.CurrentUser && e.Content.Length > 0 && e.Content[0] == commandChar) {
                    string message = e.Content;

                    if (message.Length > 0) {

                        message = message.Substring (1);
                        string command = "";
                        List<string> arguments = Utility.ConstructArguments (message, out command);

                        FindAndExecuteCommand (e, command, arguments, commands);
                    }
                }

                FindPhraseAndRespond (e);

                if (e.Content.Length > 0 && e.Content[0] == commandChar) {
                    e.DeleteAsync ();
                    allowedDeletedMessages.Add (e.Content);
                }

                return Task.CompletedTask;
            };

            discordClient.UserJoined += async (e) => {
                Younglings.OnUserJoined (e);
                messageControl.SendMessage (Utility.GetMainChannel () as SocketTextChannel, "**" + e.Username + "** has joined this server. Bid them welcome or murder them in cold blood, it's really up to you.", true);

                string[] welcomeMessage = SerializationIO.LoadTextFile (dataPath + "welcomemessage" + gitHubIgnoreType);
                string combined = "";
                for (int i = 0; i < welcomeMessage.Length; i++) {
                    combined += welcomeMessage[i] + "\n";
                }

                await messageControl.SendMessage (e, combined);
            };

            discordClient.UserLeft += (e) => {
                messageControl.SendMessage (Utility.GetMainChannel () as SocketTextChannel, "**" + Utility.GetUserName (e) + "** has left the server. Don't worry, they'll come crawling back soon.", true);
                return Task.CompletedTask;
            };

            discordClient.UserVoiceStateUpdated += async (user, before, after) => {
                Console.WriteLine ("User voice updated: " + user.Username);
                SocketGuild guild = (user as SocketGuildUser).Guild;

                if (after.VoiceChannel != null)
                    AutomatedVoiceChannels.allVoiceChannels [ after.VoiceChannel.Id ].OnUserJoined (user as SocketGuildUser);

                await AutomatedVoiceChannels.OnUserUpdated (guild, before.VoiceChannel, after.VoiceChannel);

                return;
            };

            discordClient.GuildMemberUpdated += async (before, after) => {
                SocketGuild guild = (before as SocketGuildUser).Guild;

                SocketGuildChannel channel = Utility.GetMainChannel ();
                await AutomatedVoiceChannels.OnUserUpdated (guild, before.VoiceChannel, after.VoiceChannel);

                if ((before as SocketGuildUser).Nickname != (after as SocketGuildUser).Nickname) {
                    messageControl.SendMessage (channel as SocketTextChannel, "**" + Utility.GetUserUpdateName (before as SocketGuildUser, after as SocketGuildUser, true) + "** has changed their nickname to **" + Utility.GetUserUpdateName (before as SocketGuildUser, after as SocketGuildUser, false) + "**", true);
                }
            };

            discordClient.UserUpdated += (before, after) => {
                Console.WriteLine ("User " + before.Username + " updated.");

                SocketTextChannel channel = Utility.GetMainChannel () as SocketTextChannel;

                if (channel == null)
                    return Task.CompletedTask;

                if (before.Username != after.Username) {
                    messageControl.SendMessage (channel as SocketTextChannel, "**" + Utility.GetUserUpdateName (before as SocketGuildUser, after as SocketGuildUser, true) + "** has changed their name to **" + after.Username + "**", true);
                }

                return Task.CompletedTask;
            };

            discordClient.UserBanned += (e, guild) => {
                SocketChannel channel = Utility.GetMainChannel ();
                if (channel == null)
                    return Task.CompletedTask;

                messageControl.SendMessage (channel as SocketTextChannel, "**" + Utility.GetUserName (e as SocketGuildUser) + "** has been banned from this server, they will not be missed.", true);
                messageControl.SendMessage (e as SocketGuildUser, "Sorry to tell you like this, but you have been permabanned from Monster Mash. ;-;");

                return Task.CompletedTask;
            };

            discordClient.UserUnbanned += (e, guild) => {
                SocketChannel channel = Utility.GetMainChannel ();
                if (channel == null)
                    return Task.CompletedTask;

                messageControl.SendMessage (channel as SocketTextChannel, "**" + Utility.GetUserName (e as SocketGuildUser) + "** has been unbanned from this server, They are once more welcome in our glorious embrace.", true);
                messageControl.SendMessage (e as SocketGuildUser, "You have been unbanned from Monster Mash, we love you once more! :D");

                return Task.CompletedTask;
            };

            discordClient.MessageDeleted += (message, channel) => {
                if (channel == null)
                    return Task.CompletedTask;

                if (message.HasValue) {
                    if (!allowedDeletedMessages.Contains (message.Value.Content)) {
                        messageControl.SendMessage (channel as SocketTextChannel, "In order disallow *any* secrets except for admin secrets, I'd like to tell you that **" + Utility.GetUserName (message.Value.Author as SocketGuildUser) + "** just had a message deleted on **" + message.Value.Channel.Name + "**.", true);
                    } else {
                        allowedDeletedMessages.Remove (message.Value.Content);
                    }
                }

                return Task.CompletedTask;
            };

            discordClient.Ready += () => {
                ChatLogger.Log ("Bot is ready and running!");
                return Task.CompletedTask;
            };

            string token = SerializationIO.LoadTextFile (dataPath + "bottoken" + gitHubIgnoreType)[0];

            ChatLogger.Log ("Connecting to Discord..");
            await discordClient.LoginAsync (TokenType.Bot, token);
            await discordClient.StartAsync ();

            await discordClient.CurrentUser.ModifyAsync (delegate (SelfUserProperties properties) {
                properties.Username = "Adminthulhu";
                properties.Avatar = new Optional<Image?> (new Image (dataPath + avatarPath));
            });

            await Task.Delay (-1);
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

        private void InitializeData () {
            AutomatedVoiceChannels.InitializeData ();
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

        public static void InitializeCommands () {
            for (int i = 0; i < commands.Length; i++) {
                commands[i].Initialize ();
            }
        }

        public static Command FindCommand (string commandName) {
            for (int i = 0; i < commands.Length; i++) {
                if (commands[i].command.ToUpper () == commandName.ToUpper ())
                    return commands[i];
            }
            return null;
        }

        public static bool FindAndExecuteCommand (SocketMessage e, string commandName, List<string> arguements, Command[] commandList) {
            for (int i = 0; i < commandList.Length; i++) {
                if (commandList[i].command == commandName) {
                    commandList[i].ExecuteCommand (e as SocketUserMessage, arguements);
                    return true;
                }
            }

            return false;
        }

        public void FindPhraseAndRespond (SocketMessage e) {
            for (int i = 0; i < phrases.Length; i++) {
                if (phrases[i].CheckAndRespond (e))
                    return;
            }
        }
    }
}
