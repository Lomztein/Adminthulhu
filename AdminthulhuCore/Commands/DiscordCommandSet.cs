using Discord;
using Discord.Rest;
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

                new Mention (), new ID (), new Delete (),
            };
        }

        public class UserSet : CommandSet {
            public UserSet() {
                command = "user";
                shortHelp = "User related commands.";

                commandsInSet = new Command [ ] {
                    new Find (), new Random (), new Name (), new Online (), new Kick (), new Nickname (), new AddRole (), new RemoveRole (), new DM (), new Move (), new SetVoice (), new IsStricken (),
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

                public Task<Result> Execute(SocketUserMessage e, params IUser [ ] users) {
                    System.Random random = new System.Random ();
                    return TaskResult (users.ElementAt (random.Next (0, users.Count ())), "");
                }
            }

            public class Name : Command {
                public Name() {
                    command = "name";
                    shortHelp = "Get user name.";

                    AddOverload (typeof (string), "Get the name of a user, nickname if there is one.");
                }

                public Task<Result> Execute(SocketUserMessage e, SocketGuildUser guildUser) {
                    string name = Utility.GetUserName (guildUser);
                    return TaskResult (name, name);
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

            public class Kick : Command {
                public Kick() {
                    command = "kick";
                    shortHelp = "Kick user. Requires \"Kick Members\" permission.";
                    isAdminOnly = true;

                    AddOverload (typeof (bool), "Kicks user for no given reason.");
                    AddOverload (typeof (bool), "Kicks user with a reason.");
                }

                public Task<Result> Execute(SocketUserMessage e, SocketGuildUser user) {
                    return Execute (e, user, "");
                }

                public async Task<Result> Execute(SocketUserMessage e, SocketGuildUser user, string reason) {
                    if (CanKick ()) {
                        await user.KickAsync (reason);
                        Program.SetKickReason (user.Id, "Kicked from server. " + reason);
                        return new Result (true, $"Succesfully kicked **{user.Username}** from the server.");
                    } else {
                        return new Result (true, $"Unable to kick - Bot does not have the correct permission.");
                    }
                }

                private bool CanKick() => Utility.GetServer ().GetUser (Program.discordClient.CurrentUser.Id).GuildPermissions.KickMembers;
            }

            public class Nickname : Command {
                public Nickname() {
                    command = "nickname";
                    shortHelp = "Set someones nickname.";
                    isAdminOnly = true;

                    AddOverload (typeof (bool), "Set the given users nickname to something new.");
                    AddOverload (typeof (bool), "Reset the given users nickname.");
                }

                public bool CanSet() {
                    return Utility.GetServer ().GetUser (Program.discordClient.CurrentUser.Id).GuildPermissions.ManageNicknames;
                }

                public async Task<Result> Execute(SocketUserMessage e, SocketGuildUser user, string nickname) {
                    if (CanSet ()) {
                        await user.ModifyAsync (delegate (GuildUserProperties properties) {
                            properties.Nickname = nickname;
                        });
                        if (nickname == string.Empty) {
                            return new Result (true, $"Succesfully reset **{user.Username}**'s nickname.");
                        }
                        return new Result (true, $"Succesfully set **{user.Username}**'s nickname to **{nickname}**.");
                    } else {
                        return new Result (true, $"Unable to set nickname - Bot does not have the correct permission.");
                    }
                }

                public Task<Result> Execute(SocketUserMessage e, SocketGuildUser user) {
                    return Execute (e, user, "");
                }
            }

            public class AddRole : Command {

                public AddRole() {
                    command = "addrole";
                    shortHelp = "Adds roles to someone.";
                    isAdminOnly = true;

                    AddOverload (typeof (bool), "Add all given roles to the given person.");
                }

                public async Task<Result> Execute(SocketUserMessage e, SocketGuildUser user, params SocketRole[] roles) {
                    if (CanAdd ()) {
                        foreach (SocketRole role in roles) {
                            Utility.SecureAddRole (user, role);
                        }
                        return new Result (true, $"Succesfully added **{roles.Length}** roles to **{Utility.GetUserName (user)}**!");
                    } else {
                        return new Result (true, $"Could not add roles - Bot does not have the correct permission.");
                    }
                }

                public bool CanAdd () => Utility.GetServer().GetUser(Program.discordClient.CurrentUser.Id).GuildPermissions.ManageRoles;
            }

            public class RemoveRole : Command {

                public RemoveRole() {
                    command = "removerole";
                    shortHelp = "Removes roles to someone.";
                    isAdminOnly = true;

                    AddOverload (typeof (bool), "Add all given roles to the given person.");
                }

                public async Task<Result> Execute(SocketUserMessage e, SocketGuildUser user, params SocketRole [ ] roles) {
                    if (CanAdd ()) {
                        foreach (SocketRole role in roles) {
                            Utility.SecureAddRole (user, role);
                        }
                        return new Result (true, $"Succesfully added **{roles.Length}** roles to **{Utility.GetUserName (user)}**!");
                    } else {
                        return new Result (true, $"Could not add roles - Bot does not have the correct permission.");
                    }
                }

                public bool CanAdd() => Utility.GetServer ().GetUser (Program.discordClient.CurrentUser.Id).GuildPermissions.ManageRoles;
            }

            public class DM : Command {
                public DM() {
                    command = "dm";
                    shortHelp = "DM's a person.";
                    isAdminOnly = true;

                    AddOverload (typeof (IUserMessage), "Sends a DM to the given person with the given text.");
                }

                public async Task<Result> Execute(SocketUserMessage e, SocketGuildUser user, string contents) {
                    IUserMessage message = await Program.messageControl.SendMessage (user, contents);
                    return new Result (message, "Succesfully send a DM.");
                }
            }

            public class Move : Command {
                public Move() {
                    command = "move";
                    shortHelp = "Move to a different voice channel.";
                    isAdminOnly = true;

                    AddOverload (typeof (SocketVoiceChannel), "Moves a user to a different voice channel. Must be in one to begin with.");
                }

                public bool CanMove () => Utility.GetServer ().GetUser (Program.discordClient.CurrentUser.Id).GuildPermissions.MoveMembers;

                public async Task<Result> Execute(SocketUserMessage e, SocketGuildUser user, SocketVoiceChannel newChannel) {
                    if (CanMove ()) {
                        if (user.VoiceChannel != null) {
                            SocketVoiceChannel previous = user.VoiceChannel;
                            user.ModifyAsync (delegate (GuildUserProperties properties) {
                                properties.Channel = newChannel;
                            });
                            return new Result (previous, $"Succesfully moved **{Utility.GetUserName (user)}** to channel **{newChannel.Name}**");
                        }
                        return new Result (null, $"Failed to move **{Utility.GetUserName (user)}**, since he isn't currently in voice.");
                    }
                    return new Result (null, $"Failed to move **{Utility.GetUserName (user)}** - Bot does not have correct permissions.");
                } 
            }

            public class SetVoice : Command {
                public SetVoice() {
                    command = "setvoice";
                    shortHelp = "Servermute or -deafen someone.";
                    isAdminOnly = true;

                    AddOverload (typeof (bool), "Set mute on the given person!");
                    AddOverload (typeof (bool), "Set mute or deafen on the given person, or both at once!");
                }

                // Optionables are neat, but they don't mesh particularily well with commands. Could perhaps use reflection.
                public Task<Result> Execute(SocketUserMessage e, SocketGuildUser user, bool mute) {
                    if (CanMute ()) {
                        user.ModifyAsync (delegate (GuildUserProperties properties) {
                            properties.Mute = mute;
                        });
                        return TaskResult (true, $"Succesfully changed voice status of **{Utility.GetUserName (user)}**.");
                    }
                    return TaskResult (true, $"Failed to change voice status of **{Utility.GetUserName (user)}** - Bot does not have correct permissions.");
                }

                public Task<Result> Execute(SocketUserMessage e, SocketGuildUser user, bool mute, bool deafen) {
                    if (CanMute () && CanDeafen ()) {
                        user.ModifyAsync (delegate (GuildUserProperties properties) {
                            properties.Mute = mute;
                            properties.Deaf = deafen;
                        });
                        return TaskResult (true, $"Succesfully changed voice status of **{Utility.GetUserName (user)}**.");
                    }
                    return TaskResult (true, $"Failed to change voice status of **{Utility.GetUserName (user)}** - Bot does not have correct permissions.");
                }

                public bool CanMute() => Utility.GetServer ().GetUser (Program.discordClient.CurrentUser.Id).GuildPermissions.MuteMembers; // I really need a wrapper function for this.
                public bool CanDeafen () => Utility.GetServer ().GetUser (Program.discordClient.CurrentUser.Id).GuildPermissions.DeafenMembers;
            }

            public class IsStricken : Command {
                public IsStricken() {
                    command = "stricken";
                    shortHelp = "Is user stricken?";

                    AddOverload (typeof (bool), "Returns true if given user is stricken, false otherwise.");
                }

                public Task<Result> Execute(SocketUserMessage e, SocketGuildUser user) => TaskResult (Strikes.IsStricken (user.Id), $"{Utility.GetUserName (user)} is stricken: {Strikes.IsStricken (user.Id)}");
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

                    AddOverload (typeof (SocketRole), "Find role by given ID.");
                    AddOverload (typeof (SocketRole), "Find role by given name.");
                    AddOverload (typeof (SocketRole[]), "Get all roles of the given user.");
                }

                public Task<Result> Execute(SocketUserMessage e, ulong id) {
                    return TaskResult (Utility.GetServer ().GetRole (id), "");
                }

                public Task<Result> Execute(SocketUserMessage e, string rolename) {
                    return TaskResult (Utility.GetServer ().Roles.Where (x => x.Name.ToUpper () == rolename.ToUpper ()).FirstOrDefault (), "");
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
                    new Find (), new Members (), new Name (), new Type (), new Create (),
                };
            }

            public class Find : Command {
                public Find() {
                    command = "find";
                    shortHelp = "Find channel.";

                    AddOverload (typeof (SocketChannel), "Find channel by given ID.");
                    AddOverload (typeof (SocketChannel), "Find channel by given name.");
                }

                public Task<Result> Execute(SocketUserMessage e, ulong id) {
                    return TaskResult (Utility.GetServer ().GetChannel (id), "");
                }

                public Task<Result> Execute(SocketUserMessage e, string name) {
                    SoftStringComparer comparer = new SoftStringComparer ();
                    return TaskResult (Utility.GetServer ().Channels.Where (x => comparer.Equals (x.Name, name)).FirstOrDefault (), "");
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

                    AddOverload (typeof (SocketGuildChannel), "Get the type of given channel, either \"TEXT\" or \"VOICE\".");
                }

                public Task<Result> Execute(SocketUserMessage e, SocketGuildChannel channel) {
                    if (channel is SocketVoiceChannel) {
                        return TaskResult (channel as SocketVoiceChannel, "VOICE");
                    } else if (channel is SocketTextChannel) {
                        return TaskResult (channel as SocketTextChannel, "TEXT");
                    }
                    return TaskResult ("CATEGORY", "CATEGORY");
                }
            }

            public class Create : Command {
                public Create() {
                    command = "create";
                    shortHelp = "Create new text channel.";

                    AddOverload (typeof (ITextChannel), "Create a new text channel with the given name.");
                    AddOverload (typeof (ITextChannel), "Create a new text channel with the given name and topic.");
                }

                public bool CanCreate() => Utility.GetServer ().GetUser (Program.discordClient.CurrentUser.Id).GuildPermissions.ManageChannels;

                public async Task<Result> Execute(SocketUserMessage e, string name, string topic) {
                    if (CanCreate ()) {
                        RestTextChannel channel = await Utility.GetServer ().CreateTextChannelAsync (name);
                        await channel.ModifyAsync (delegate (TextChannelProperties properties) {
                            properties.Topic = topic;
                        });
                        return new Result (channel, "Succesfully created a new text channel: " + channel.Mention);
                    } else {
                        return new Result (null, "Failed to create new channel - Bot does not have correct permissions.");
                    }
                }

                public async Task<Result> Execute(SocketUserMessage e, string name) {
                    if (CanCreate ()) {
                        RestTextChannel channel = await Utility.GetServer ().CreateTextChannelAsync (name);
                        return new Result (channel, "Succesfully created a new text channel: " + channel.Mention);
                    } else {
                        return new Result (null, "Failed to create new channel - Bot does not have correct permissions.");
                    }
                }
            }
        }

        public class ServerSet : CommandSet {

            public ServerSet() {
                command = "server";
                shortHelp = "Server related commands.";

                commandsInSet = new Command [ ] {
                    new Get (), new Name (), new Channels (), new Members (), new AFKChannel (),
                };
            }

            public class Get : Command {
                public Get() {
                    command = "get";
                    shortHelp = "Returns the server object.";

                    AddOverload (typeof (SocketGuild), "Returns the server object.");
                }

                public Task<Result> Execute(SocketUserMessage e) {
                    return TaskResult (Utility.GetServer (), "");
                }
            }

            public class Name : Command {

                public Name() {
                    command = "name";
                    shortHelp = "Get server name.";

                    AddOverload (typeof (string), "Returns the server name.");
                }

                public Task<Result> Execute(SocketUserMessage e) {
                    return TaskResult (Utility.GetServer ().Name, "");
                }
            }

            public class Channels : Command {
                public Channels() {
                    command = "channels";
                    shortHelp = "Get all channels.";

                    AddOverload (typeof (SocketGuildChannel[]), "Returns all channels on the server.");
                }

                public Task<Result> Execute(SocketUserMessage e) {
                    return TaskResult (Utility.GetServer ().Channels.ToArray (), "");
                }
            }

            public class Members : Command {
                public Members() {
                    command = "members";
                    shortHelp = "Get all members.";

                    AddOverload (typeof (SocketGuildUser [ ]), "Returns all members on the server.");
                }

                public Task<Result> Execute(SocketUserMessage e) {
                    return TaskResult (Utility.GetServer ().Users.ToArray (), "");
                }
            }

            public class AFKChannel : Command {
                public AFKChannel() {
                    command = "afkchannel";
                    shortHelp = "Get AFK Channel";

                    AddOverload (typeof (SocketVoiceChannel), "Get the AFK channel if there is one, returns null otherwise.");
                }

                public Task<Result> Execute(SocketUserMessage e) {
                    return TaskResult (Utility.GetServer ().AFKChannel, "");
                }
            }
        }

        public class Mention : Command {
            public Mention() {
                command = "mention";
                shortHelp = "Mentions mentionable Discord objects.";
                catagory = Category.Utility;

                AddOverload (typeof (string), "Mention all given objects.");
            }

            public Task<Result> Execute(SocketUserMessage e, params IMentionable[] mentionables) {
                string total = "";
                foreach (IMentionable mention in mentionables) {
                    total += mention.Mention + "\n";
                }
                return TaskResult (total, total);
            }
        }

        public class ID : Command {
            public ID() {
                command = "id";
                shortHelp = "Get the ID of given Discord object.";

                AddOverload (typeof (ulong), "Return the ID of the given object.");
            }

            public Task<Result> Execute(SocketUserMessage e, SocketEntity<ulong> obj) {
                return TaskResult (obj.Id, obj.Id.ToString ());
            }
        }

        public class Delete : Command {
            public Delete() {
                command = "delete";
                shortHelp = "Delete deletable Discord objects.";

                AddOverload (typeof (object), "Delete whatever deletable object is given.");
            }

            public async Task<Result> Execute(SocketUserMessage e, IDeletable deletable) {
                await deletable.DeleteAsync ();
                return new Result (null, "Succesfully deleted the given object.");
            }
        }
    }
}
