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
            new CShowAlias (), new CClearAliasses ()
        };

        public static string dataPath = "C:/Users/Lomztein/Source/Repos/DiscordCthulhuBot/DiscordCthulhu/";
        public static AliasCollection aliasCollection = null;

        static void Main ( string[] args ) => new Program ().Start ();

        private DiscordClient discordClient;

        public void Start () {
            aliasCollection = AliasCollection.Load ();
            discordClient = new DiscordClient ();

            discordClient.MessageReceived += async ( s, e ) => {

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

            };

            discordClient.ExecuteAndWait (async () => {
                await discordClient.Connect ("MTg5MTM0OTYzNjM4MTQwOTMw.Cjzmkw.VhiecwYf4QH1lHrtTo5JjKZGfZw");
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
    }
}
