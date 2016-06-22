using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace DiscordCthulhu {

    [Serializable]
    public class PlayerGroups {

        public Dictionary<string, List<Group>> groups = new Dictionary<string, List<Group>>();

        //public List<Invite> currentInvites = new List<Invite>();
        //public List<Invite> currentRequests = new List<Invite>();

        public Group FindGroupByName (string server, string name) {

            List<Group> locGroups = groups[server];

            for (int i = 0; i < locGroups.Count; i++) {
                for (int j = 0; j < locGroups.Count; j++) {
                    if (locGroups[i].groupName.ToUpper () == name.ToUpper ()) {
                        return locGroups[i];
                    }
                }
            }

            return null;
        }

        public bool CreateGroup (MessageEventArgs e, string groupName) {
            if (!groups.ContainsKey (e.Server.Name))
                groups.Add (e.Server.Name, new List<Group> ());

            if (FindGroupByName (e.Server.Name, groupName) == null) {
                groups[e.Server.Name].Add (new Group (groupName, e.User.Mention));
                Save ();
                return true;
            }

            return false;
        }

        public bool JoinGroup (MessageEventArgs e, string groupName) {
            Group group = FindGroupByName (e.Server.Name, groupName);
            if (!IsUserMember (group, e.User.Mention) && group != null) {
                group.AddMember (e.User.Mention);
                Save ();
                return true;
            }

            return false;
        }

        public bool LeaveGroup (MessageEventArgs e, string groupName) {
            Group group = FindGroupByName (e.Server.Name, groupName);
            if (IsUserMember (group, e.User.Mention)) {
                group.RemoveMember (e.User.Mention);

                if (group.memberMentions.Count == 0)
                    groups[e.Server.Name].Remove (group);

                Save ();
                return true;
            }

            return false;
        }

        public bool IsUserMember (Group g, string userMention) {
            if (g == null)
                return false;

            return g.memberMentions.Contains (userMention);
        }

        public static PlayerGroups Load () {
            PlayerGroups groups = SerializationIO.LoadObjectFromFile<PlayerGroups> (Program.dataPath + "groups.dat");

            if (groups != null)
                return groups;
            else
                return new PlayerGroups ();

        }

        public void Save () {
            SerializationIO.SaveObjectToFile (Program.dataPath + "groups.dat", this);
        }

        [Serializable]
        public class Group {

            public string groupName;
            public List<string> memberMentions;

            public Group ( string name, string leader ) {
                groupName = name;
                memberMentions = new List<string> ();
                memberMentions.Add (leader);
            }

            public void AddMember (string newMember) {
                if (!memberMentions.Contains (newMember)) {
                    memberMentions.Add (newMember);
                }
            }

            public void RemoveMember (string member) {
                memberMentions.Remove (member);
            }
        }

        public class Invite {

            public string acceptorMention;
            public string userMention;
            public Group group;

            public Invite (string a, string n, Group g) {
                acceptorMention = a;
                userMention = n;
                group = g;
            }
        }
    }
}
