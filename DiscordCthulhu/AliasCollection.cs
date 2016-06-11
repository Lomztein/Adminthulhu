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

        public List<User> FindUsersByAlias (string alias) {
            List<User> foundUsers = new List<User>();
            for (int i = 0; i < users.Count; i++) {

                if (users[i].discordAlias.ToLower () == alias.ToLower ()) {

                    if (!foundUsers.Contains (users[i])) {
                        foundUsers.Add (users[i]);
                    }
                }

                for (int j = 0; j < users[i].aliasses.Count; j++) {
                    if (users[i].aliasses[j].ToLower () == alias.ToLower ()) {

                        if (!foundUsers.Contains (users[i])) {
                            foundUsers.Add (users[i]);
                        }
                    }
                }

            }

            if (foundUsers.Count == 0) {
                foundUsers.Add (null);
            }

            return foundUsers;
        }

        public bool AddAlias (string username, string alias) {
            User user = FindUsersByAlias (username)[0];
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
            User user = FindUsersByAlias (username)[0];
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

        public bool RemoveUser (User user) {
            if (!users.Contains (user))
                return false;
            users.Remove (user);
            Save ();
            return true;
        }
    }
}
