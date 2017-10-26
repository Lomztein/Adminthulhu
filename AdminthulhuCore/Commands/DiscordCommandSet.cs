using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Adminthulhu
{
    public class DiscordCommandSet : CommandSet
    {
        public DiscordCommandSet() {
            command = "discord";
            shortHelp = "Commands about Discord. These only return objects.";
            catagory = Category.Advanced;
            requiredPermission = Permissions.Type.UseAdvancedCommands;

            commandsInSet = new Command [ ] {
                new UserSet (), new ChannelSet (), new RoleSet (), new ServerSet (),

                new Mention (), new ID (),
            };
        }

        public class UserSet : CommandSet {
            public UserSet() {
                command = "user";
                shortHelp = "User related commands.";

                commandsInSet = new Command [ ] {
                    new Find (), new Random (), new Online (),
                };
            }

            public class Find : Command {
                public Find() {
                    command = "find";
                    shortHelp = "Use this to find users.";

                    AddOverload (typeof (SocketGuildUser), "Find user by ID.");
                    AddOverload (typeof (SocketGuildUser), "Find user by name.");
                    AddOverload (typeof (SocketGuildUser[]), "Find users by role.");
                }

                public Task<Result> Execute(SocketUserMessage e, ulong id) {
                    return TaskResult (Utility.GetServer ().GetUser (id), "");
                }

                public Task<Result> Execute(SocketUserMessage e, string name) {
                    return TaskResult (Utility.FindUserByName (Utility.GetServer (), name), "");
                }

                public Task<Result> Execute(SocketUserMessage e, SocketRole role) {
                    return TaskResult (Utility.GetServer ().Users.Where (x => x.Roles.Contains (role)).ToArray (), "");
                }
            }

            public class Random : Command {
                public Random() {
                    command = "random";
                    shortHelp = "Get random user.";

                    AddOverload (typeof (SocketGuildUser), "Get a completely random online user from the server.");
                    AddOverload (typeof (SocketGuildUser), "Get a random online user who is a member of the given role.");
                    AddOverload (typeof (SocketGuildUser), "Get a random user from the given array of users.");
                }

                public Task<Result> Execute(SocketUserMessage e) {
                    System.Random random = new System.Random ();
                    IEnumerable<SocketGuildUser> users = Utility.GetServer ().Users.Where (x => x.Status == UserStatus.Online);
                    return TaskResult (users.ElementAt (random.Next (0, users.Count ())), "");
                }

                public Task<Result> Execute(SocketUserMessage e, SocketRole role) {
                    System.Random random = new System.Random ();
                    IEnumerable<SocketGuildUser> users = Utility.GetServer ().Users.Where (x => x.Roles.Contains (role)).Where (x => x.Status == UserStatus.Online);
                    return TaskResult (users.ElementAt (random.Next (0, users.Count ())), "");
                }

                public Task<Result> Execute(SocketUserMessage e, IUser [ ] users) {
                    System.Random random = new System.Random ();
                    return TaskResult (users.ElementAt (random.Next (0, users.Count ())), "");
                }
            }

            public class Online : Command {
                public Online() {
                    command = "online";
                    shortHelp = "Is user online.";

                    AddOverload (typeof (bool), "Returns true if the given user by name is online, false if not.");
                    AddOverload (typeof (bool), "Returns true if the given user is online, false if not.");
                }

                public Task<Result> Execute(SocketUserMessage e, string username) {
                    SocketGuildUser user = Utility.FindUserByName (Utility.GetServer (), username);
                    return Execute (e, user);
                }

                public Task<Result> Execute(SocketUserMessage e, IUser user) {
                    if (user != null) {
                        return TaskResult (user.Status == UserStatus.Online, "");
                    } else {
                        return TaskResult (false, "User not found.");
                    }
                }
            }
        }

        public class RoleSet : CommandSet {

            public RoleSet() {
                command = "role";
                shortHelp = "Role related commands.";

                commandsInSet = new Command [ ] {
                    new Find (), new Members (),
                };
            }

            public class Find : Command {
                public Find() {
                    command = "find";
                    shortHelp = "Find role.";

                    AddOverload (typeof (SocketRole), "Find role by given name.");
                    AddOverload (typeof (SocketRole), "Find role by given ID.");
                    AddOverload (typeof (SocketRole[]), "Get all roles of the given user.");
                }

                public Task<Result> Execute(SocketUserMessage e, string rolename) {
                    return TaskResult (Utility.GetServer ().Roles.Where (x => x.Name == rolename).FirstOrDefault (), "");
                }

                public Task<Result> Execute(SocketUserMessage e, ulong id) {
                    return TaskResult (Utility.GetServer ().GetRole (id), "");
                }

                public Task<Result> Execute(SocketUserMessage e, SocketGuildUser user) {
                    return TaskResult (user.Roles.ToArray (), "");
                }
            }

            public class Members : Command {
                public Members() {
                    command = "members";
                    shortHelp = "Get role members.";

                    AddOverload (typeof (SocketGuildUser), "");
                }

                public Task<Result> Execute(SocketUserMessage e, SocketRole role) {
                    return TaskResult (role.Members.ToArray (), "");
                }
            }
        }

        public class ChannelSet : CommandSet {

            public ChannelSet() {
                command = "channel";
                shortHelp = "Channel related commands.";

                commandsInSet = new Command [ ] {
                    new Find (), new Members (), new Name (), new Type (),
                };
            }

            public class Find : Command {
                public Find() {
                    command = "find";
                    shortHelp = "Find channel.";

                    AddOverload (typeof (SocketChannel), "");
                    AddOverload (typeof (SocketChannel), "");
                }

                public Task<Result> Execute(SocketUserMessage e, string name) {
                    SoftStringComparer comparer = new SoftStringComparer ();
                    return TaskResult (Utility.GetServer ().Channels.Where (x => comparer.Equals (x.Name, name)).FirstOrDefault (), "");
                }

                public Task<Result> Execute(SocketUserMessage e, ulong id) {
                    return TaskResult (Utility.GetServer ().GetChannel (id), "");
                }
            }

            public class Members : Command {
                public Members() {
                    command = "members";
                    shortHelp = "Get role members.";

                    AddOverload (typeof (SocketGuildUser[]), "");
                }

                public Task<Result> Execute(SocketUserMessage e, SocketChannel channel) {
                    return TaskResult (channel.Users.ToArray (), "");
                }
            }

            public class Name : Command {
                public Name() {
                    command = "name";
                    shortHelp = "Get channel name.";

                    AddOverload (typeof (string), "Get the name of the given channel");
                }

                public Task<Result> Execute(SocketUserMessage e, SocketGuildChannel channel) {
                    return TaskResult (channel.Name, "");
                }
            }

            public class Type : Command {
                public Type() {
                    command = "type";
                    shortHelp = "Get channel type.";

                    AddOverload (typeof (string), "Get the type of given channel, either \"TEXT\" or \"VOICE\".");
                }

                public Task<Result> Execute(SocketUserMessage e, SocketGuildChannel channel) {
                    return TaskResult (channel as SocketTextChannel == null ? "VOICE" : "TEXT", "");
                }
            }
        }

        public class ServerSet : CommandSet {

            public ServerSet() {
                command = "server";
                shortHelp = "Server related commands.";

                commandsInSet = new Command [ ] {
                    new Get (), new Name (),
                };
            }

            public class Get : Command {
                public Get() {
                    command = "get";
                    shortHelp = "Returns the server object.";
                }

                public Task<Result> Execute(SocketUserMessage e) {
                    return TaskResult (Utility.GetServer (), "");
                }
            }

            public class Name : Command {

                public Name() {
                    command = "name";
                    shortHelp = "Get server name.";
                }

                public Task<Result> Execute(SocketUserMessage e) {
                    return TaskResult (Utility.GetServer ().Name, "");
                }
            }
        }

        public class Mention : Command {
            public Mention() {
                command = "mention";
                shortHelp = "Mentions mentionable Discord objects.";
                catagory = Category.Utility;

                AddOverload (typeof (string), "Mention given object.");
                AddOverload (typeof (string), "Mention all given objects.");
            }

            public Task<Result> Execute(SocketUserMessage e, IMentionable mention) {
                return TaskResult (mention.Mention, mention.Mention);
            }

            public Task<Result> Execute(SocketUserMessage e, IMentionable[] mentionables) {
                string total = "";
                foreach (IMentionable mention in mentionables) {
                    total += mention.Mention;
                }
                return TaskResult (total, total);
            }
        }

        public class ID : Command {
            public ID() {
                command = "id";
                shortHelp = "Get the ID of given Discord object.";
            }

            public Task<Result> Execute(SocketUserMessage e, SocketEntity<ulong> obj) {
                return TaskResult (obj.Id, "");
            }
        }
    }
}
