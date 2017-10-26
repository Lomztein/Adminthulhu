using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using System.Net.Http;
using Discord.Rest;

namespace Adminthulhu
{
    class Program : IConfigurable {

        public static string commandTrigger = "!";
        public static string commandTriggerHidden = "/";

        public static Command [ ] commands = new Command [ ] {
            new CCommandList (), new CSetColor (), new CRollTheDice (),
            new CFlipCoin (), new CRandomGame (), new CQuote (), new CEmbolden (),
            new CAddHeader (), new CShowHeaders (), new CKarma (), new CReport (),
            new VoiceCommands (), new EventCommands (), new UserSettingsCommands (), new HangmanCommands (),
            new GameCommands (), new StrikeCommandSet (), new CAddEventGame (), new CRemoveEventGame (), new CHighlightEventGame (),
            new CAcceptYoungling (), new CReloadConfiguration (), new CCreateBook (), new CSetYoungling (), new CCreatePoll (), new CCheckPatch (),
            new CSetSetting (), new CDisplayFile (), new CUrbanDictionary (), new CPrint (), new PermissionCommands (),
            new DiscordCommandSet (), new MiscCommandSet (), new FlowCommandSet (), new MathCommandSet (), new VariableCommandSet (), new CommandChain.CustomCommandSet (),
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

        public static string[] onUserJoinMessage = new string [ ] { "**{USERNAME}** has joined this server!" };
        public static string[] onUserJoinFromInviteMessage = new string [ ] { "**{USERNAME}** has joined this server by the help of {INVITERNAME}!" };
        public static string [ ] onUserLeaveMessage = new string [ ] { "**{USERNAME}** has left this server :(" };
        public static string [ ] onUserBannedMessage = new string [ ] { "**{USERNAME}** has been banned from this server." };
        public static string [ ] onUserUnbannedMessage = new string [ ] { "**{USERNAME}** has been pardoned in this server!" };
        public static string [ ] onUserChangedNameMessage = new string [ ] { "**{OLDNAME}** has changed their name to **{NEWNAME}**" };
        public static ulong onPatchedAnnounceChannel;

        public static Phrase [ ] phrases = new Phrase [ ] { };
        public static List<string> allowedDeletedMessages = new List<string>();
        public static Dictionary<ulong, string> automaticLeftReason = new Dictionary<ulong, string> ();

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

            onUserJoinMessage = BotConfiguration.GetSetting ("Server.Messages.OnUserJoin", "", onUserJoinMessage);
            onUserJoinFromInviteMessage = BotConfiguration.GetSetting ("Server.Messages.OnUserJoinFromInvite", "", onUserJoinFromInviteMessage);
            onUserLeaveMessage = BotConfiguration.GetSetting ("Server.Messages.OnUserLeave", "", onUserLeaveMessage);
            onUserBannedMessage = BotConfiguration.GetSetting ("Server.Messages.OnUserBanned", "", onUserBannedMessage);
            onUserUnbannedMessage = BotConfiguration.GetSetting ("Server.Messages.OnUserUnbanned", "", onUserUnbannedMessage);
            onUserChangedNameMessage = BotConfiguration.GetSetting ("Server.Messages.OnUserChangedName", "", onUserChangedNameMessage);
            onPatchedAnnounceChannel = BotConfiguration.GetSetting ("Server.Messages.OnPatchedAnnounceChannel", "", (ulong)0);

            commandTrigger = BotConfiguration.GetSetting ("Command.Trigger", "","!");
            commandTriggerHidden = BotConfiguration.GetSetting ("Command.HiddenTrigger", "", "/");

            phrases = BotConfiguration.GetSetting ("Misc.ResponsePhrases", "ResponsePhrases", new Phrase [ ] { new Phrase ("Neat!", 0, 100, "Very!", 0, ""), new Phrase ("Neato", 0, 100, "", 0, "🌯") });
        }

        public async Task Start(string [ ] args) {

            dataPath = AppContext.BaseDirectory + "/data/";
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
            InviteHandler.Initialize ();
            CommandChain.Initialize ();
            Permissions.Initialize ();
            clock = new Clock ();

            InitializeData ();
            UserGameMonitor.Initialize ();

            bootedTime = DateTime.Now.AddSeconds (BOOT_WAIT_TIME);

            Logging.Log (Logging.LogType.BOT, "Setting up events..");
            discordClient.MessageReceived += async (e) => {

                Logging.Log (Logging.LogType.CHAT, Utility.GetChannelName (e) + " says: " + e.Content);

                bool hideTrigger = false;
                if (e.Author.Id != discordClient.CurrentUser.Id && e.Content.Length > 0 && ContainsCommandTrigger (e.Content, out hideTrigger)) {
                    string message = e.Content;

                    if (message.Length > 0) {

                        message = message.Substring (1);
                        string command = "";
                        List<string> arguments = Utility.ConstructArguments (message, out command);

                        FindAndExecuteCommand (e, command, arguments, commands, 0, true);
                    }
                }

                FindPhraseAndRespond (e);

                if (e.Content.Length > 0 && hideTrigger) {
                    e.DeleteAsync ();
                    allowedDeletedMessages.Add (e.Content);
                }
            };

            discordClient.UserJoined += async (e) => {
                Younglings.OnUserJoined (e);
                RestInviteMetadata possibleInvite = await InviteHandler.FindInviter ();
                SocketGuildUser inviter;

                if (possibleInvite != null) {
                    inviter = Utility.GetServer ().GetUser (possibleInvite.Inviter.Id);
                    string joinMessage = Utility.SelectRandom (onUserJoinFromInviteMessage);
                    messageControl.SendMessage (Utility.GetMainChannel () as SocketTextChannel, joinMessage.Replace ("{USERNAME}", Utility.GetUserName (e)).Replace ("{INVITERNAME}", Utility.GetUserName (inviter)), true);
                } else {
                    string joinMessage = Utility.SelectRandom (onUserJoinMessage);
                    messageControl.SendMessage (Utility.GetMainChannel () as SocketTextChannel, joinMessage.Replace ("{USERNAME}", Utility.GetUserName (e)), true);
                }

                string [ ] welcomeMessage = SerializationIO.LoadTextFile (dataPath + "welcomemessage" + gitHubIgnoreType);
                string combined = "";
                for (int i = 0; i < welcomeMessage.Length; i++) {
                    combined += welcomeMessage [ i ] + "\n";
                }

                await messageControl.SendMessage (e, combined);
            };

            discordClient.UserLeft += (e) => {
                string leftMessage = Utility.SelectRandom (onUserLeaveMessage);
                if (automaticLeftReason.ContainsKey (e.Id)) {
                    leftMessage = $"**{Utility.GetUserName (e)}** left - " + automaticLeftReason [ e.Id ];
                    automaticLeftReason.Remove (e.Id);
                }
                messageControl.SendMessage (Utility.GetMainChannel () as SocketTextChannel, leftMessage.Replace ("{USERNAME}", Utility.GetUserName (e)), true);
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
                if (before.Username != after.Username && (after as SocketGuildUser).Nickname == "") {
                    MentionNameChange (before as SocketGuildUser, after as SocketGuildUser);
                }

                return Task.CompletedTask;
            };

            discordClient.UserBanned += (e, guild) => {
                SocketChannel channel = Utility.GetMainChannel ();
                if (channel == null)
                    return Task.CompletedTask;

                string banMessage = Utility.SelectRandom (onUserBannedMessage);
                messageControl.SendMessage (channel as SocketTextChannel, banMessage.Replace ("{USERNAME}", Utility.GetUserName (e as SocketGuildUser)), true);

                return Task.CompletedTask;
            };

            discordClient.UserUnbanned += (e, guild) => {
                SocketChannel channel = Utility.GetMainChannel ();
                if (channel == null)
                    return Task.CompletedTask;

                string unbannedMessage = Utility.SelectRandom (onUserUnbannedMessage);
                messageControl.SendMessage (channel as SocketTextChannel, unbannedMessage.Replace ("{USERNAME}", Utility.GetUserName (e as SocketGuildUser)), true);

                return Task.CompletedTask;
            };

            discordClient.Ready += () => {
                Logging.Log (Logging.LogType.BOT, "Bot is ready and running!");
                return Task.CompletedTask;
            };

            string token = "";
            try {
                token = SerializationIO.LoadTextFile (dataPath + "bottoken" + gitHubIgnoreType)[0];
            }catch (Exception e) {
                Logging.Log (Logging.LogType.CRITICAL, "Bottoken not found, please create a file at <botroot>/data/bottoken.botproperty and insert bottoken there. " + e.Message);
            }

            Logging.Log (Logging.LogType.BOT, "Connecting to Discord..");
            await discordClient.LoginAsync (TokenType.Bot, token);
            await discordClient.StartAsync ();

            BotConfiguration.PostInit ();

            await Utility.AwaitFullBoot ();
            if (args.Length > 0 && args [ 0 ] == "true" && onPatchedAnnounceChannel != 0) {
                using (HttpClient client = new HttpClient ()) {
                    string changelog = await client.GetStringAsync (AutoPatcher.url + "changelog.txt");
                    string version = await client.GetStringAsync (AutoPatcher.url + "version.txt");
                    string total = $"Succesfully installed new patch, changelog for {version}:\n```{changelog}```";

                    SocketGuildChannel patchNotesChannel = Utility.GetServer ().GetChannel (onPatchedAnnounceChannel);
                    if (patchNotesChannel != null) {
                        messageControl.SendMessage (patchNotesChannel as ISocketMessageChannel, total, true);
                    }
                }
            }

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
            string changedMessage = Utility.SelectRandom (onUserChangedNameMessage);
            messageControl.SendMessage (channel, changedMessage.Replace ("{OLDNAME}", Utility.GetUserUpdateName (before, after, true)).Replace ("{NEWNAME}", Utility.GetUserUpdateName (before, after, false)), true);
        }

        private static bool hasBooted = false;
        public static bool FullyBooted () {
            if (hasBooted)
                return hasBooted;

            if (Utility.GetServer () != null) {
                if (Utility.GetServer ().Channels.Count != 0) { // Why this is neccesary is beyond me, but I'll take it.
                    hasBooted = true;
                    Logging.Log (Logging.LogType.BOT, "Bot has fully booted.");
                }
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

        public static void SetKickReason(ulong id, string reason) {
            automaticLeftReason.Add (id, reason);
        }

        public static Command FindCommand (string commandName) {
            for (int i = 0; i < commands.Length; i++) {
                if (commands[i].command.ToUpper () == commandName.ToUpper ())
                    return commands[i];
            }
            return null;
        }

        public static async Task<FoundCommandResult> FindAndExecuteCommand(SocketMessage e, string commandName, List<string> arguements, Command [ ] commandList, int depth, bool printMessage = false) {
            for (int i = 0; i < commandList.Length; i++) {
                if (commandList [ i ].command == commandName) {
                    if (arguements.Count > 0 && arguements [ 0 ] == "?") {
                        Command command = commandList [ i ];
                        messageControl.SendMessage (e, command.GetHelp (e), false);
                    } else {
                        FoundCommandResult result = new FoundCommandResult (await commandList [ i ].TryExecute (e as SocketUserMessage, depth, arguements.ToArray ()), commandList [ i ]);
                        if (printMessage && result != null) {
                            messageControl.SendMessage (e, result.result.message, result.command.allowInMain);
                        }

                        if (depth == 0)
                            CommandVariables.Clear (e.Id);

                        return result;
                    }
                }
            }

            return null;
        }

        public class FoundCommandResult {
            public Command.Result result;
            public Command command;

            public FoundCommandResult(Command.Result _result, Command _command) {
                result = _result;
                command = _command;
            }
        }

        public void FindPhraseAndRespond (SocketMessage e) {
            for (int i = 0; i < phrases.Length; i++) {
                if (phrases[i].CheckAndRespond (e))
                    return;
            }
        }
    }
}
