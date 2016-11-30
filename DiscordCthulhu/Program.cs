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
            new CEndTheWorld ()
        };

        public static Phrase[] phrases = new Phrase[] {
            new Phrase ("Neat!", "", 100, "Very!"),
            new Phrase ("", "Nyx", 1, "*Allegedly...*"),
            new Phrase ("", "Peacekeeper", 2, "*It's always crits..*"),
            new Phrase ("wow!", "Gizmo Gizmo", 100, "INSANE AIR TIME!"),
            new Phrase ("Thx fam", "", 100, "No props. We Gucci."),
            new Phrase ("Fuck yis", "Lomztein", 100, "Fuck no.")
        };

        public static string dataPath = "";
        public static AliasCollection aliasCollection = null;
        public static ScoreCollection scoreCollection = new ScoreCollection ();
        public static PlayerGroups playerGroups;
        public static MessageControl messageControl = null;
        public static string commandSettingsDirectory = "Command Settings/";
        public static string gitHubIgnoreType = ".botproperty";

        static void Main ( string[] args ) => new Program ().Start (args);

        private DiscordClient discordClient;
        public static Dictionary<ulong, string> voiceChannelNames = new Dictionary<ulong, string>();

        public void Start (string[] args) {

            aliasCollection = AliasCollection.Load ();
            scoreCollection.scores = ScoreCollection.Load ();
            playerGroups = PlayerGroups.Load ();

            InitializeDirectories ();
            InitializeData ();

            discordClient = new DiscordClient ();
            messageControl = new MessageControl();

            // Linux specific test
            if (args.Length > 0) {
                dataPath = args[0];
            }else {
                dataPath = System.IO.Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly ().GetName ().CodeBase);
                dataPath = dataPath.Substring (dataPath.IndexOf ('\\') + 1);
                dataPath += "\\";
            }

            InitializeCommands ();

            discordClient.MessageReceived += async ( s, e ) => {

                Console.WriteLine (GetChannelName (e) + " says: " + e.Message.Text);
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
                                Console.WriteLine("Removed!");
                                messageControl.RemoveMessageTimer(messageTimer);
                                break;
                            }
                        }
                    }
                }

                FindPhraseAndRespond (e);
            };

            discordClient.UserJoined += ( s, e ) => {
                messageControl.SendMessage (e.Server.FindChannels("main").ToArray()[0], "**" + e.User.Name + "** has joined this server. Bid them welcome or murder them in cold blood, it's really up to you.");
            };

            discordClient.UserUpdated += ( s, e ) => {
                UpdateVoiceChannel (e.Before.VoiceChannel);
                UpdateVoiceChannel (e.After.VoiceChannel);

                Channel[] array = e.Server.FindChannels ("main").ToArray ();
                if (array.Length == 0)
                    return;

                Channel channel = array[0];
                if (e.Before.Name != e.After.Name) {
                    messageControl.SendMessage (channel, "**"+ GetUserUpdateName (e, true) + "** has changed their name to **" + e.After.Name + "**");
                }

                if (e.Before.Nickname != e.After.Nickname) {
                    Console.WriteLine (":" + GetUserUpdateName (e, true) + ":");
                    messageControl.SendMessage (channel, "**" + GetUserUpdateName (e, true) + "** has changed their nickname to **" + GetUserUpdateName (e, false) + "**");
                }
            };

            Console.WriteLine (dataPath);
            string token = SerializationIO.LoadTextFile (dataPath + "bottoken" + gitHubIgnoreType)[0];
            Console.WriteLine ("Connecting using token: " + token);

            discordClient.ExecuteAndWait (async () => {
                await discordClient.Connect (token, TokenType.Bot);
            });
        }

        public string GetUserName (User user) {
            if (user.Nickname.Length == 0)
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

        private void InitializeData () {
            voiceChannelNames.Add (250545007797207040, "Radical Red");
            voiceChannelNames.Add (250545037790674944, "Beautiful Blue");
            voiceChannelNames.Add (250722068272775178, "Indigo International");
            voiceChannelNames.Add (243685884723986432, "Funkthulhu's Funky Cave");
            voiceChannelNames.Add (169127187897778178, "Corner of Shame");
        }

        private void UpdateVoiceChannel ( Channel voice ) {
            if (voice != null) {

                Dictionary<Game, int> numPlayers = new Dictionary<Game, int> ();
                foreach (User user in voice.Users) {

                    if (user.CurrentGame.HasValue) {
                        if (numPlayers.ContainsKey (user.CurrentGame.Value)) {
                            numPlayers[user.CurrentGame.Value]++;
                        }else {
                            numPlayers.Add (user.CurrentGame.Value, 1);
                        }
                    }

                }

                int highest = int.MinValue;
                Game highestGame = new Game ("");

                for (int i = 0; i < numPlayers.Count; i++) {
                    KeyValuePair<Game, int> value = numPlayers.ElementAt (i);

                    if (value.Value > highest) {
                        highest = value.Value;
                        highestGame = value.Key;
                    }
                }

                Console.WriteLine (highestGame.Name);

                if (highestGame.Name != "") {
                    voice.Edit (voiceChannelNames[voice.Id] + " - " + highestGame.Name);
                } else {
                    voice.Edit (voiceChannelNames[voice.Id]);
                }
            }
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
            if (!Directory.Exists (dataPath + commandSettingsDirectory))
                Directory.CreateDirectory (dataPath + commandSettingsDirectory);
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

        public void FindPhraseAndRespond ( MessageEventArgs e ) {
            for (int i = 0; i < phrases.Length; i++) {
                phrases[i].CheckAndRespond (e);
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
