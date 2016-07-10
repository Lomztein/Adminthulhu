using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {
    class Program {

        public static Command[] commands = new Command[] {
            new CCommandList (), new CRollTheDice (), new CCallVoiceChannel (), new CCreateInvite (),
            new CSetColor (), new CSetGame (), new CRemoveGame (), new CSetAlias (), new CRemoveAlias (),
            new CShowAlias (), new CClearAliasses (), new CFlipCoin (), new CRandomGame (), new CQuote (),
            new CChangeScore (), new CShowScore (), new CCreateGroup (), new CCallGroup (), new CShowGroups (),
            new CJoinGroup (), new CLeaveGroup (), new CCreateTTTGame (), new CMakeTTTMove ()
        };

        public static Phrase[] phrases = new Phrase[] {
            new Phrase ("Neat!", "", 100, "Very!"),
            new Phrase ("", "Nyx", 1, "*Allegedly...*"),
            new Phrase ("", "Peacekeeper", 2, "*It's always crits..*")
        };

        public static string dataPath = "";
        public static AliasCollection aliasCollection = null;
        public static ScoreCollection scoreCollection = new ScoreCollection ();
        public static PlayerGroups playerGroups;
        public static MessageControl messageControl = null;

        static void Main ( string[] args ) => new Program ().Start ();

        private DiscordClient discordClient;

        public void Start () {

            aliasCollection = AliasCollection.Load ();
            scoreCollection.scores = ScoreCollection.Load ();
            playerGroups = PlayerGroups.Load ();

            discordClient = new DiscordClient ();
            messageControl = new MessageControl();

            dataPath = System.IO.Path.GetDirectoryName (System.Reflection.Assembly.GetExecutingAssembly ().GetName ().CodeBase);
            dataPath = dataPath.Substring (dataPath.IndexOf ('\\') + 1);
            dataPath += "\\";

            discordClient.MessageReceived += async ( s, e ) => {

                Console.WriteLine (e.Server.Name + "/" + e.Channel.Name + "/" + e.User.Name + " says: " + e.Message.Text);
                if (!e.Message.IsAuthor && e.Message.Text.Length > 0 && e.Message.Text[0] == '!') {
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

            string token = SerializationIO.LoadTextFile (dataPath + "bottoken.txt")[0];
            Console.WriteLine ("Connecting using token: " + token);

            discordClient.ExecuteAndWait (async () => {
                await discordClient.Connect (token);
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

        public Task FindAndExecuteAsync ( MessageEventArgs e, string commandName, List<string> arguements ) {
            return Task.Run (() => FindAndExecuteCommand (e, commandName, arguements));
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
