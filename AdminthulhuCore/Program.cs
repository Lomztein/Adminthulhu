using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using System.Net.Http;
using Discord.Rest;
using System.Globalization;

namespace Adminthulhu
{
    public class Program : IConfigurable {

        public static string commandTrigger = "!";
        public static string commandTriggerHidden = "/";

        public static Command [ ] commands = new Command [ ] {
            new CCommandList (), new CSetColor (), new CRollTheDice (),
            new CFlipCoin (), new CRandomGame (), new CQuote (), new CEmbolden (),
            new CShowHeaders (), new CKarma (), new CReport (),
            new VoiceCommands (), new EventCommands (), new UserSettingsCommands (), new HangmanCommands (),
            new GameCommands (), new CUrbanDictionary (), new CPrint (), new AdminCommandSet (),

            new DiscordCommandSet (), new MiscCommandSet (), new FlowCommandSet (), new MathCommandSet (), new VariableCommandSet (),
            new CommandChain.CustomCommandSet (), new AutocCommandSet (), new CCallStack (),
        };
        public static List<Command> quickCommands = new List<Command> ();

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
                new Program ().ErrorCatcher (args); // This was possibly one of my worst ideas ever, on top of that it doesn't even work.
        }

        public static DiscordSocketClient discordClient;

        private const int BOOT_WAIT_TIME = 5;
        private static DateTime bootedTime = new DateTime ();

        // SocketGuild data
        public static string mainTextChannelName = "";
        public static string dumpTextChannelName = "";
        public static string serverName = "";
        public static ulong serverID = 0;
        public static string cultureName = "en-US";

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
            mainTextChannelName = BotConfiguration.GetSetting ("Server.MainTextChannelName", this, "general");
            dumpTextChannelName = BotConfiguration.GetSetting ("Server.DumpTextChannelName", this, "dump");
            serverName = BotConfiguration.GetSetting ("Server.Name", this, "Discord Server");
            serverID = BotConfiguration.GetSetting<ulong> ("Server.ID", this, 0);
            cultureName = BotConfiguration.GetSetting ("Server.CultureName", this, cultureName);
            CultureInfo cultureInfo = new CultureInfo (cultureName);
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            onUserJoinMessage = BotConfiguration.GetSetting ("Server.Messages.OnUserJoin", this, onUserJoinMessage);
            onUserJoinFromInviteMessage = BotConfiguration.GetSetting ("Server.Messages.OnUserJoinFromInvite", this, onUserJoinFromInviteMessage);
            onUserLeaveMessage = BotConfiguration.GetSetting ("Server.Messages.OnUserLeave", this, onUserLeaveMessage);
            onUserBannedMessage = BotConfiguration.GetSetting ("Server.Messages.OnUserBanned", this, onUserBannedMessage);
            onUserUnbannedMessage = BotConfiguration.GetSetting ("Server.Messages.OnUserUnbanned", this, onUserUnbannedMessage);
            onUserChangedNameMessage = BotConfiguration.GetSetting ("Server.Messages.OnUserChangedName", this, onUserChangedNameMessage);
            onPatchedAnnounceChannel = BotConfiguration.GetSetting ("Server.Messages.OnPatchedAnnounceChannel", this, (ulong)0);

            commandTrigger = BotConfiguration.GetSetting ("Command.Trigger", this, "!");
            commandTriggerHidden = BotConfiguration.GetSetting ("Command.HiddenTrigger", this, "/");

            phrases = BotConfiguration.GetSetting ("Misc.ResponsePhrases", this, new Phrase [ ] { new Phrase ("Neat!", 0, 100, "Very!", 0, ""), new Phrase ("Neato", 0, 100, "", 0, "🌯") });
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
            AutoCommands.Initialize ();
            clock = new Clock ();

            InitializeData ();
            UserGameMonitor.Initialize ();

            bootedTime = DateTime.Now.AddSeconds (BOOT_WAIT_TIME);

            Logging.Log (Logging.LogType.BOT, "Setting up events..");
            discordClient.MessageReceived += async (e) => {

                Logging.Log (Logging.LogType.CHAT, Utility.GetChannelName (e) + " says: " + e.Content);

                bool hideTrigger = false;
                if (e.Content.Length > 0 && ContainsCommandTrigger (e.Content, out hideTrigger)) {
                    FindAndExecuteCommand (e, e.Content, commands, 0, true, true);  
                }

                if (e.Author.Id != discordClient.CurrentUser.Id) {
                    FindPhraseAndRespond (e);
                    AutoCommands.RunEvent (AutoCommands.Event.MessageRecieved, e.Content, e.Channel.Id.ToString ());
                }

                if (e.Content.Length > 0 && hideTrigger) {
                    e.DeleteAsync ();
                    allowedDeletedMessages.Add (e.Content);
                }
            };

            discordClient.MessageUpdated += async (cache, message, channel) => {
                Logging.Log (Logging.LogType.CHAT, "Message edited: " + Utility.GetChannelName (message as SocketMessage) + " " + message.Content);
                AutoCommands.RunEvent (AutoCommands.Event.MessageDeleted, message.Content, message.Channel.Id.ToString ());
            };

            discordClient.MessageDeleted += async (cache, channel) => {
                IMessage message = await cache.GetOrDownloadAsync ();
                Logging.Log (Logging.LogType.CHAT, "Message deleted: " + Utility.GetChannelName (channel as SocketGuildChannel));
                AutoCommands.RunEvent (AutoCommands.Event.MessageDeleted, channel.Id.ToString ());
            };

            discordClient.UserJoined += async (e) => {
                Younglings.OnUserJoined (e);
                RestInviteMetadata possibleInvite = await InviteHandler.FindInviter ();
                Logging.Log (Logging.LogType.BOT, "User " + e.Username + " joined.");
                AutoCommands.RunEvent (AutoCommands.Event.UserJoined, e.Id.ToString ());
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
                Logging.Log (Logging.LogType.BOT, "User " + e.Username + " left.");
                AutoCommands.RunEvent (AutoCommands.Event.UserLeft, e.Id.ToString ());
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

                if (before.VoiceChannel == null && after.VoiceChannel != null)
                    AutoCommands.RunEvent (AutoCommands.Event.JoinedVoice, user.Id.ToString (), after.VoiceChannel.Id.ToString ());
                if (before.VoiceChannel != null && after.VoiceChannel == null)
                    AutoCommands.RunEvent (AutoCommands.Event.LeftVoice, user.Id.ToString (), before.VoiceChannel.Id.ToString ());

                await Voice.OnUserUpdated (guild, before.VoiceChannel, after.VoiceChannel);
                return;
            };

            discordClient.GuildMemberUpdated += async (before, after) => {
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

            try {
                if (args.Length > 0 && args [ 0 ] == "true" && onPatchedAnnounceChannel != 0) {
                    using (HttpClient client = new HttpClient ()) {
                        string changelog = await client.GetStringAsync (AutoPatcher.url + "changelog.txt");
                        string version = await client.GetStringAsync (AutoPatcher.url + "version.txt");
                        string total = $"Succesfully installed new patch, changelog for {version}:\n{changelog}";

                        SocketGuildChannel patchNotesChannel = Utility.GetServer ().GetChannel (onPatchedAnnounceChannel);
                        if (patchNotesChannel != null) {
                            messageControl.SendMessage (patchNotesChannel as ISocketMessageChannel, total, true, "```");
                        }
                    }
                }
            } catch (Exception e) {
                Logging.Log (e);
            }

            BakeQuickCommands ();

            await Task.Delay (-1);
        }

        private void BakeQuickCommands() {
            List<Command> cmds = Command.RecursiveCacheCommands (commands.ToList ());
            quickCommands = cmds;
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

            if (serverID == 0 && discordClient.Guilds.Count == 1) {
                serverID = discordClient.Guilds.ElementAt (0).Id;
                serverName = Utility.GetServer ().Name;

                BotConfiguration.SetSetting ("Server.ID", serverID);
                BotConfiguration.SetSetting ("Server.Name", serverName);
                BotConfiguration.SaveSettings ();
            }

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

        public static async Task<FoundCommandResult> FindAndExecuteCommand (SocketMessage e, string fullCommand, Command [ ] commandList, int depth, bool printMessage, bool allowQuickCommands) {
            string cmd = "";
            List<string> arguments = Utility.ConstructArguments (fullCommand.Substring (1), out cmd);

            return await FindAndExecuteCommand (e, cmd, arguments, commandList, depth, printMessage, allowQuickCommands);
        }

        public static async Task<FoundCommandResult> FindAndExecuteCommand (SocketMessage e, string commandName, List<string> arguements, Command [ ] commandList, int depth, bool printMessage, bool allowQuickCommands) {
            for (int i = 0; i < commandList.Length; i++) {
                if (commandList [ i ].command == commandName) {
                    if (arguements.Count > 0 && arguements [ 0 ] == "?") {
                        Command command = commandList [ i ];
                        if (command is CommandSet) {
                            messageControl.SendMessage (e, command.GetHelp (e), command.allowInMain);
                        } else {
                            messageControl.SendEmbed (e.Channel, command.GetHelpEmbed (e, UserConfiguration.GetSetting<bool> (e.Author.Id, "AdvancedCommandsMode")));
                        }
                        return null;
                    } else {
                        FoundCommandResult result = new FoundCommandResult (await commandList [ i ].TryExecute (e as SocketUserMessage, depth, arguements.ToArray ()), commandList [ i ]);
                        if (result != null) {
                            if (printMessage)
                                messageControl.SendMessage (e, result.result.message, result.command.allowInMain);

                            if (depth == 0)
                                CommandVariables.Clear (e.Id);

                            return result;
                        }
                    }
                }
            }

            if (allowQuickCommands) {
                return await FindAndExecuteCommand (e, commandName, arguements, quickCommands.ToArray (), depth, printMessage, false);
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
