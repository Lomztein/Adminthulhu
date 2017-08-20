using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Adminthulhu
{
    class Program : IConfigurable {

        public static string commandTrigger = "!";
        public static string commandTriggerHidden = "/";

        public static Command [ ] commands = new Command [ ] {
            new CCommandList (), new CSetColor (), new CRollTheDice (),
            new CFlipCoin (), new CRandomGame (), new CQuote (), new CEmbolden (),
            new CAddHeader (), new CShowHeaders (), new CKarma (), new CReport (),
            new VoiceCommands (), new EventCommands (), new UserSettingsCommands (), new DebugCommands (), new HangmanCommands (),
            new GameCommands (), new StrikeCommandSet (), new CAddEventGame (), new CRemoveEventGame (), new CHighlightEventGame (),
            new CAcceptYoungling (), new CReloadConfiguration (), new CCreateBook (), new CSetYoungling (), new CCreatePoll (), 
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
        public static string mainTextChannelName = "";
        public static string dumpTextChannelName = "";
        public static string serverName = "";
        public static ulong serverID = 0;

        public static string onUserJoinMessage;
        public static string onUserLeaveMessage;
        public static string onUserBannedMessage;
        public static string onUserUnbannedMessage;
        public static string onUserChangedNameMessage;


        public static Phrase [ ] phrases = new Phrase [ ] { };
        public static List<string> allowedDeletedMessages = new List<string>();

        // Feedback
        public static ulong authorID = 93744415301971968;

        public async Task ErrorCatcher (string[] args) {
            try {
                await new Program ().Start (args);
            } catch (Exception e) {
                Logging.DebugLog (Logging.LogType.EXCEPTION, e.Message + "\n" + e.StackTrace);
            }
        }

        public void LoadConfiguration() {
            mainTextChannelName = BotConfiguration.GetSetting ("Server.MainTextChannelName", "MainTextChannelName", "general");
            dumpTextChannelName = BotConfiguration.GetSetting ("Server.DumpTextChannelName", "DumpTextChannelName", "dump");
            serverName = BotConfiguration.GetSetting ("Server.Name", "ServerName", "Discord Server");
            serverID = BotConfiguration.GetSetting<ulong> ("Server.ID", "ServerID", 0);

            onUserJoinMessage = BotConfiguration.GetSetting ("Server.Messages.OnUserJoin", "", "{USERNAME} has joined this server!");
            onUserLeaveMessage = BotConfiguration.GetSetting ("Server.Messages.OnUserLeave", "", "{USERNAME} has left this server.");
            onUserBannedMessage = BotConfiguration.GetSetting ("Server.Messages.OnUserBanned", "", "{USERNAME} has been banned from this server.");
            onUserUnbannedMessage = BotConfiguration.GetSetting ("Server.Messages.OnUserUnbanned", "", "{USERNAME} has been unbanned from this server!");
            onUserChangedNameMessage = BotConfiguration.GetSetting ("Server.Messages.OnUserChangedName", "", "{OLDNAME} has changed their name to {NEWNAME}!");

            commandTrigger = BotConfiguration.GetSetting ("Command.Trigger", "","!");
            commandTriggerHidden = BotConfiguration.GetSetting ("Command.HiddenTrigger", "", "/");

            phrases = BotConfiguration.GetSetting ("Misc.ResponsePhrases", "ResponsePhrases", new Phrase [ ] { new Phrase ("Neat!", 0, 100, "Very!", 0, ""), new Phrase ("Neato", 0, 100, "", 0, "🌯") });
        }

        public async Task Start(string [ ] args) {

            // Linux specific test
            if (args.Length > 0) {
                dataPath = args [ 0 ] + "/data/";
            } else {
                dataPath = AppContext.BaseDirectory + "/data/";
            }

            dataPath = dataPath.Replace ('\\', '/');
            InitializeDirectories ();
            Logging.Log (Logging.LogType.BOT, "Initializing bot.. Datapath: " + dataPath);
            BotConfiguration.Initialize ();
            Encryption.Initialize ();

            BotConfiguration.AddConfigurable (this);
            LoadConfiguration ();

            discordClient = new DiscordSocketClient ();
            messageControl = new MessageControl ();
            karma = new Karma ();

            LegalJunk.Initialize ();
            Logging.Log (Logging.LogType.BOT, "Loading data..");
            InitializeCommands ();
            UserConfiguration.Initialize ();
            clock = new Clock ();

            InitializeData ();
            UserGameMonitor.Initialize ();

            bootedTime = DateTime.Now.AddSeconds (BOOT_WAIT_TIME);

            Logging.Log (Logging.LogType.BOT, "Setting up events..");
            discordClient.MessageReceived += (e) => {

                Logging.Log (Logging.LogType.CHAT, Utility.GetChannelName (e) + " says: " + e.Content);
                bool hideTrigger = false;
                bool foundCommand = false;
                if (e.Author.Id != discordClient.CurrentUser.Id && e.Content.Length > 0 && ContainsCommandTrigger (e.Content, out hideTrigger)) {
                    string message = e.Content;

                    if (message.Length > 0) {

                        message = message.Substring (1);
                        string command = "";
                        List<string> arguments = Utility.ConstructArguments (message, out command);

                        foundCommand = FindAndExecuteCommand (e, command, arguments, commands);
                    }
                }

                FindPhraseAndRespond (e);

                if (e.Content.Length > 0 && hideTrigger && foundCommand) {
                    e.DeleteAsync ();
                    allowedDeletedMessages.Add (e.Content);
                }

                return Task.CompletedTask;
            };

            discordClient.UserJoined += async (e) => {
                Younglings.OnUserJoined (e);
                messageControl.SendMessage (Utility.GetMainChannel () as SocketTextChannel, onUserJoinMessage.Replace ("{USERNAME}", Utility.GetUserName (e)), true);

                string[] welcomeMessage = SerializationIO.LoadTextFile (dataPath + "welcomemessage" + gitHubIgnoreType);
                string combined = "";
                for (int i = 0; i < welcomeMessage.Length; i++) {
                    combined += welcomeMessage[i] + "\n";
                }

                await messageControl.SendMessage (e, combined);
            };

            discordClient.UserLeft += (e) => {
                messageControl.SendMessage (Utility.GetMainChannel () as SocketTextChannel, onUserLeaveMessage.Replace ("{USERNAME}", Utility.GetUserName (e)), true);
                return Task.CompletedTask;
            };

            discordClient.UserVoiceStateUpdated += async (user, before, after) => {
                Logging.Log (Logging.LogType.BOT, "User voice updated: " + user.Username);
                SocketGuild guild = (user as SocketGuildUser).Guild;

                if (after.VoiceChannel != null)
                    Voice.allVoiceChannels [ after.VoiceChannel.Id ].OnUserJoined (user as SocketGuildUser);

                await Voice.OnUserUpdated (guild, before.VoiceChannel, after.VoiceChannel);

                return;
            };

            discordClient.GuildMemberUpdated += async (before, after) => {
                SocketGuild guild = (before as SocketGuildUser).Guild;

                SocketGuildChannel channel = Utility.GetMainChannel ();
                await Voice.OnUserUpdated (guild, before.VoiceChannel, after.VoiceChannel);

                if ((before as SocketGuildUser).Nickname != (after as SocketGuildUser).Nickname) {
                    MentionNameChange (before, after);
                }
            };

            discordClient.UserUpdated += (before, after) => {
                Logging.Log (Logging.LogType.BOT, "User " + before.Username + " updated.");

                if (before.Username != after.Username && (after as SocketGuildUser).Nickname == "") {
                    MentionNameChange (before as SocketGuildUser, after as SocketGuildUser);
                }

                return Task.CompletedTask;
            };

            discordClient.UserBanned += (e, guild) => {
                SocketChannel channel = Utility.GetMainChannel ();
                if (channel == null)
                    return Task.CompletedTask;

                messageControl.SendMessage (channel as SocketTextChannel, onUserBannedMessage.Replace ("{USERNAME}", Utility.GetUserName (e as SocketGuildUser)), true);

                return Task.CompletedTask;
            };

            discordClient.UserUnbanned += (e, guild) => {
                SocketChannel channel = Utility.GetMainChannel ();
                if (channel == null)
                    return Task.CompletedTask;

                messageControl.SendMessage (channel as SocketTextChannel, onUserUnbannedMessage.Replace ("{USERNAME}", Utility.GetUserName (e as SocketGuildUser)), true);

                return Task.CompletedTask;
            };

            discordClient.Ready += () => {
                Logging.Log (Logging.LogType.BOT, "Bot is ready and running!");
                return Task.CompletedTask;
            };

            string token = SerializationIO.LoadTextFile (dataPath + "bottoken" + gitHubIgnoreType)[0];

            Logging.Log (Logging.LogType.BOT, "Connecting to Discord..");
            await discordClient.LoginAsync (TokenType.Bot, token);
            await discordClient.StartAsync ();

            BotConfiguration.PostInit ();

            await Task.Delay (-1);
        }

        public bool ContainsCommandTrigger(string message, out bool isHidden) {
            isHidden = false;
            if (commandTrigger.Length > 0 && message.Substring (0, commandTrigger.Length) == commandTrigger) {
                isHidden = false;
                return true;
            }else if (commandTriggerHidden.Length > 0 && message.Substring (0, commandTriggerHidden.Length) == commandTriggerHidden) {
                isHidden = true;
                return true;
            }
            return false;
        }

        private void MentionNameChange(SocketGuildUser before, SocketGuildUser after) {
            SocketTextChannel channel = Utility.GetMainChannel () as SocketTextChannel;
            messageControl.SendMessage (channel, onUserChangedNameMessage.Replace ("{OLDNAME}", Utility.GetUserUpdateName (before, after, true)).Replace ("{NEWNAME}", Utility.GetUserUpdateName (before, after, false)), true);
        }

        private static bool hasBooted = false;
        public static bool FullyBooted () {
            if (hasBooted)
                return hasBooted;

            if (Utility.GetServer () != null) {
                hasBooted = true;
                Logging.Log (Logging.LogType.BOT, "Bot has fully booted.");
            }
            return hasBooted;
        }

        private void InitializeData () {
            Voice.InitializeData ();
        }

        public static void InitializeDirectories () {
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
                    if (arguements.Count > 0 && arguements [ 0 ] == "?") {
                        Command command = commandList [ i ];
                        messageControl.SendMessage (e as SocketUserMessage, command.GetHelp (e), false);
                    } else
                        commandList [ i ].ExecuteCommand (e as SocketUserMessage, arguements);
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
