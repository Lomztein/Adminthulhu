﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

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

        public async Task<bool> CreateGroup (SocketMessage e, string groupName) {
            SocketGuild guild = (e.Channel as SocketGuildChannel).Guild;
            if (!groups.ContainsKey (guild.Name))
                groups.Add (guild.Name, new List<Group> ());

            if (FindGroupByName (guild.Name, groupName) == null) {
                groups[guild.Name].Add (new Group (groupName, e.Author.Mention));
                await Save();
                return true;
            }

            return false;
        }

        public async Task<bool> JoinGroup (SocketMessage e, string groupName) {
            SocketGuild guild = (e.Channel as SocketGuildChannel).Guild;
            Group group = FindGroupByName (guild.Name, groupName);
            if (!IsUserMember (group, e.Author.Mention) && group != null) {
                group.AddMember (e.Author.Mention);
                await Save();
                return true;
            }

            return false;
        }

        public async Task<bool> LeaveGroup (SocketMessage e, string groupName) {
            SocketGuild guild = (e.Channel as SocketGuildChannel).Guild;
            Group group = FindGroupByName (guild.Name, groupName);
            if (IsUserMember (group, e.Author.Mention)) {
                group.RemoveMember (e.Author.Mention);

                if (group.memberMentions.Count == 0)
                    groups[guild.Name].Remove (group);

                await Save();
                return true;
            }

            return false;
        }

        public bool IsUserMember (Group g, string userMention) {
            if (g == null)
                return false;

            return g.memberMentions.Contains (userMention);
        }

        public static async Task<PlayerGroups> Load () {
            PlayerGroups groups = await SerializationIO.LoadObjectFromFile<PlayerGroups> (Program.dataPath + "groups" + Program.gitHubIgnoreType);

            if (groups != null)
                return groups;
            else
                return new PlayerGroups ();

        }

        public async Task Save () {
            await SerializationIO.SaveObjectToFile (Program.dataPath + "groups" + Program.gitHubIgnoreType, this);
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
