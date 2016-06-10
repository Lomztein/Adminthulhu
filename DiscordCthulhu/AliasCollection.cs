using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;



namespace DiscordCthulhu {

    [Serializable]
    class AliasCollection {

        public List<User> users;

        public AliasCollection () {
            users = new List<User> ();
        }

        [Serializable]
        public class User {

            public string discordAlias;
            public List<string> aliasses;

            public User (string name, string first) {
                aliasses = new List<string> ();
                discordAlias = name;
                aliasses.Add (first);
            }
        }

        public void Save () {
            SerializationIO.SaveObjectToFile (Program.dataPath + "aliasses.dat", this);
        }

        public static AliasCollection Load () {
            AliasCollection collection = SerializationIO.LoadObjectFromFile<AliasCollection> (Program.dataPath + "aliasses.dat");
            if (collection == null) {
                return new AliasCollection ();
            } else {
                return collection;
            }
        }

        public User FindUserByAlias (string alias) {
            for (int i = 0; i < users.Count; i++) {
                if (users[i].discordAlias.ToLower () == alias.ToLower ()) {
                    return users[i];
                }

                for (int j = 0; j < users[i].aliasses.Count; j++) {
                    if (users[i].aliasses[j].ToLower () == alias.ToLower ()) {
                        return users[i];
                    }
                }
            }

            return null;
        }

        public bool AddAlias (string username, string alias) {
            User user = FindUserByAlias (username);
            if (user == null) {
                users.Add (new User (username, alias));
                Save ();
                return true;
            }
            if (user.aliasses.Contains (alias)) {
                return false;
            } else {
                user.aliasses.Add (alias);
                Save ();
                return true;
            }
        }

        public bool RemoveAlias (string username, string alias) {
            User user = FindUserByAlias (username);
            if (user == null) {
                return false;
            }
            if (user.aliasses.Contains (alias)) {
                user.aliasses.Remove (alias);
                Save ();
                return true;
            } else {
                return false;
            }
        }
    }
}
