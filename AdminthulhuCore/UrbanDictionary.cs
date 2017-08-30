using Discord;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json.Linq;
using Discord.WebSocket;

namespace Adminthulhu
{
    public class UrbanDictionary {

        public const string URL = "http://api.urbandictionary.com/v0/define?term={word}";

        public static async Task<Embed> GetDefinition(string word) {
            using (TextReader reader = await Utility.DoJSONRequestAsync (URL.Replace ("{word}", word))) {
                JObject json = JObject.Parse (reader.ReadToEnd ());
                Definition response = new Definition (json);

                try {
                    if (response.success) {
                        EmbedBuilder builder = new EmbedBuilder ()
                            .WithTitle ($"Top definition of {response.word}")
                            .WithUrl (response.permalink)
                            .WithColor (new Color (0, 0, 128))
                            .WithDescription (response.definition)
                            .WithFooter ($"Defined by {response.author}. Souce: www.urbandictionary.com");
                        if (response.example.Length > 0)
                            builder.AddField("Example", response.example);

                            builder.AddField ("Votes", response.thumbsUp.ToString () + "↑ / " + response.thumbsDown.ToString() + "↓");

                        if (response.soundUrl.Length > 0)
                            builder.AddField ("Sound", response.soundUrl.Singlify ("\n", 3));
                        if (response.tags.Length > 0)
                            builder.AddField ("Tags", response.tags.Singlify (", "));

                        return builder.Build ();
                    } else {
                        return new EmbedBuilder ().WithTitle ($"No definitons for {word} found.").WithColor (new Color (0, 0, 128)).Build ();
                    }
                } catch (Exception e) {
                    Logging.Log (Logging.LogType.EXCEPTION, e.Message + " - " + e.StackTrace);
                    return new EmbedBuilder ().WithTitle ($"Error - {e.Message}").Build ();
                }
            }
        }

        public class Definition {

            public string word;
            public string definition;
            public string example;
            public string author;
            public string permalink;
            public bool success;

            public string [ ] tags;
            public string [ ] soundUrl;

            public int thumbsUp;
            public int thumbsDown;

            public Definition(JObject jObject) {
                success = jObject [ "result_type" ].ToObject<string>() != "no_results";

                if (success) {
                    tags = jObject [ "tags" ].ToObject<string [ ]> ();
                    soundUrl = jObject [ "sounds" ].ToObject<string [ ]> ();
                    JObject first = (jObject [ "list" ] as JArray) [ 0 ] as JObject;

                    word = first [ "word" ].ToObject<string> ();
                    definition = first [ "definition" ].ToObject<string> ();
                    example = first [ "example" ].ToObject<string> ();
                    author = first [ "author" ].ToObject<string> ();
                    permalink = first [ "permalink" ].ToObject<string> ();

                    thumbsUp = first [ "thumbs_up" ].ToObject<int> ();
                    thumbsDown = first [ "thumbs_down" ].ToObject<int> ();
                }
            }
        }
    }

    public class CUrbanDictionary : Command {
        public CUrbanDictionary() {
            command = "define";
            shortHelp = "Get a definition of a word.";
            catagory = Category.Fun;

            AddOverload (typeof (Embed), "Get the definition of a given word, from the very best sources on the internet!");
        }

        public async Task<Result> Execute(SocketUserMessage e, string word) {
            Embed embed = await UrbanDictionary.GetDefinition (word);
            Program.messageControl.SendEmbed (e.Channel as ITextChannel, embed);
            return new Result (embed, "");
        }
    }
}
