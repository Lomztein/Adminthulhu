using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using System.IO;

namespace DiscordCthulhu {
    class Program {

        public static char commandChar = '!';

        public static Command[] commands = new Command[] {
            new CCommandList (), new CRollTheDice (), new CCallVoiceChannel (), new CSetColor (),
            new CFlipCoin (), new CRandomGame (), new CQuote (), new CSetCommand ()
        };

        public static Phrase[] phrases = new Phrase[] {
            new Phrase ("Neat!", "", 100, "Very!"),
            new Phrase ("", "Nyx", 1, "*Allegedly...*"),
            new Phrase ("", "Peacekeeper", 2, "*It's always crits..*"),
            new Phrase ("wow!", "Gizmo Gizmo", 100, "INSANE AIR TIME!"),
            new Phrase ("Thx fam", "", 100, "No props. We Gucci.")
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

        public void Start (string[] args) {

            aliasCollection = AliasCollection.Load ();
            scoreCollection.scores = ScoreCollection.Load ();
            playerGroups = PlayerGroups.Load ();

            InitializeDirectories ();

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

                Console.WriteLine (e.Server.Name + "/" + e.Channel.Name + "/" + e.User.Name + " says: " + e.Message.Text);
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

            Console.WriteLine (dataPath);
            string token = SerializationIO.LoadTextFile (dataPath + "bottoken" + gitHubIgnoreType)[0];
            Console.WriteLine ("Connecting using token: " + token);

            discordClient.ExecuteAndWait (async () => {
                await discordClient.Connect (token, TokenType.Bot);
            });
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

        public void FindAndExecuteCommand (MessageEventArgs e, string commandName, List<string> arguements) {
            for (int i = 0; i < commands.Length; i++) {
                if (commands[i].command == commandName) {
                    commands[i].ExecuteCommand (e, arguements);
                }
            }
        }

        public void FindPhraseAndRespond (MessageEventArgs e) {
            for (int i = 0; i < phrases.Length; i++) {
                phrases[i].CheckAndRespond (e);
            }
        }
    }
}
