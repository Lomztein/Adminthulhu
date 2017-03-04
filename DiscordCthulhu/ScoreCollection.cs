using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordCthulhu {
    public class ScoreCollection {

        public Dictionary<string, int> scores;

        public async Task ChangeScore (string user, int number) {
            if (scores.ContainsKey (user)) {
                scores[user] += number;
            }else {
                scores.Add (user, number);
            }
            await Save();
        }

        public int GetScore (string user) {
            if (scores.ContainsKey (user)) {
                return scores[user];
            }else {
                return 0;
            }
        }

        // Totally didn't copy paste this part from AliasCollection.cs.
        // Might be good to create a generalized version of load instead.
        public async Task Save () {
            await SerializationIO.SaveObjectToFile (Program.dataPath + "scores.dat", scores);
        }

        public static async Task<Dictionary<string, int>> Load () {
            Dictionary<string, int> collection = await SerializationIO.LoadObjectFromFile<Dictionary<string, int>> (Program.dataPath + "scores" + Program.gitHubIgnoreType);
            if (collection == null) {
                return new Dictionary<string, int> ();
            } else {
                return collection;
            }
        }
    }
}
