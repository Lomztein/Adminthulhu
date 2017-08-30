using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

// I highly advice against reading this. It is incredibly ugly and unoptimized.
namespace Adminthulhu {

    public class VoiceCommands : CommandSet {
        public VoiceCommands () {
            command = "voice";
            shortHelp = "Voice command set.";
            commandsInSet = new Command[] { new CLock (), new CUnlock (), new CInvite (), new CMembers (), new CKick (), new CCallVoiceChannel (), new CLooking (), new CFull (), new CSetDesired (), new CCustomName (), new CCreate () };
            catagory = Category.Utility;
        }
    }

    class CLock : Command {

        public CLock () {
            command = "lock";
            shortHelp = "Lock voice channel.";
            overloads.Add (new Overload (typeof (Voice.VoiceChannel), "Locks your current voice channel."));
        }

        public Task<Result> Execute ( SocketUserMessage e) {
                SocketGuildChannel channel = (e.Author as SocketGuildUser).VoiceChannel;
                if (channel != null) {
                    Voice.VoiceChannel vc = Voice.allVoiceChannels[channel.Id];
                    if (!vc.IsLocked ()) {

                        if (!vc.lockable) {
                            return TaskResult (vc, "Error - cannot lock this channel due to *reasons*.");
                        }

                        vc.Lock (e.Author.Id, true);

                        return TaskResult (vc, "Succesfully locked voice channel **" + vc.GetName () + "**.");
                    }

                    return TaskResult (vc, "Error - voice channel **" + vc.GetName () + "** already locked.");
                }

                return TaskResult (null, "Failed to lock channel, are you even in one?");
        }
    }

    class CUnlock : Command {

        public CUnlock () {
            command = "unlock";
            shortHelp = "Unlock voice channel.";
            overloads.Add (new Overload (typeof (Voice.VoiceChannel), "Unlocks your current voice channel, if locked."));
        }

        public Task<Result> Execute ( SocketUserMessage e) {

                SocketGuildChannel channel = (e.Author as SocketGuildUser).VoiceChannel;
                if (channel != null) {
                    Voice.VoiceChannel vc = Voice.allVoiceChannels[channel.Id];
                    if (vc.IsLocked ()) {

                        if (vc.lockerID == e.Author.Id) {
                            vc.Unlock (true);
                            return TaskResult (vc, "Succesfully unlocked voice channel **" + vc.GetName () + "**.");

                        }

                        return TaskResult (vc, "Only the person who locked this channel can do that, which is " + vc.GetLocker ().Mention);

                    }

                    return TaskResult (vc, "Failed to unlock voice channel **" + vc.GetName () + "** - It is not unlocked.");
                }

                return TaskResult (null, "Failed to unlock channel, are you even in one?");
            }
        }

    class CCreate : Command {

        public CCreate() {
            command = "create";
            shortHelp = "Create temporary voice channel.";
            overloads.Add (new Overload (typeof (Voice.VoiceChannel), "Creates a temporary channel by name for whatever you need."));
        }

        public async Task<Result> Execute (SocketUserMessage e, string name, string timespan) {
            TimeSpan timeSpan;
            if (Utility.TryParseSimpleTimespan (timespan, out timeSpan)) {
                Voice.VoiceChannel channel = await Voice.CreateTemporaryChannel (name, timeSpan);
                return new Result(channel, "Succesfully created voice channel by name **" + name + "**!");
            } else {
                return new Result(null, "Failed to create voice channel - TimeSpan could not be parsed.");
            }
        }
    }

    class CInvite : Command {

        public CInvite () {
            command = "invite";
            shortHelp = "Invite user.";
            overloads.Add (new Overload (typeof (SocketGuildUser), "Invites a user by name, into your current locked channel."));
        }

        public Task<Result> Execute ( SocketUserMessage e, string name) {
                SocketGuildChannel channel = (e.Author as SocketGuildUser).VoiceChannel;
                SocketGuildUser user = Utility.FindUserByName ((e.Channel as SocketGuildChannel).Guild, name);
                if (user == null) {
                    return TaskResult (null, "Failed to invite - User **" + name + "** couldn't be found on this server.");

                }

                if (channel != null) {
                    Voice.VoiceChannel vc = Voice.allVoiceChannels[channel.Id];
                    if (vc.IsLocked ()) {
                        vc.InviteUser (e.Author as SocketGuildUser, user);
                        return TaskResult (user, "User **" + Utility.GetUserName (user) + "** succesfully invited.");

                    }

                    return TaskResult (user, "The channel isn't locked, but I'm sure " + Utility.GetUserName (user) + " would love to join anyways.");
                }

                return TaskResult(null, "Failed to invite, are you even in a channel?");
            }
        }

    class CMembers : Command {

        public CMembers() {
            command = "members";
            shortHelp = "Member list.";
            overloads.Add (new Overload (typeof (SocketGuildUser [ ]), "Display list of allowed members in your current locked voice channel."));
        }

        public Task Execute(SocketUserMessage e) {
            SocketGuildChannel channel = (e.Author as SocketGuildUser).VoiceChannel;

            if (channel != null) {
                Voice.VoiceChannel vc = Voice.allVoiceChannels [ channel.Id ];
                if (vc.IsLocked ()) {
                    List<SocketGuildUser> foundUsers = new List<SocketGuildUser> ();
                    string reply = "```\n";
                    foreach (ulong user in vc.allowedUsers) {
                        SocketGuildUser u = Utility.GetServer ().GetUser (user);
                        foundUsers.Add (u);
                        reply += Utility.GetUserName (u) + "\n";
                    }
                    reply += "```";
                    return TaskResult (foundUsers.ToArray (), "Users allowed on your locked channel:\n" + reply);
                }

                return TaskResult (null, "Error - The channel isn't locked.");

            }
            return TaskResult (null, "Error - Are you even in a channel?");
        }
    }

    class CKick : Command {

        public CKick() {
            command = "kick";
            shortHelp = "Kick member.";
            overloads.Add (new Overload (typeof (bool), "Kicks member by name, from your currently locked voice channel. You must be locker."));
        }

        public Task<Result> Execute(SocketUserMessage e, string username) {

            SocketGuildChannel channel = (e.Author as SocketGuildUser).VoiceChannel;
            SocketGuildUser user = Utility.FindUserByName ((e.Channel as SocketGuildChannel).Guild, username);
            if (user == null) {
                return TaskResult (false, "Error - User not found.");
            }

            if (channel != null) {
                Voice.VoiceChannel vc = Voice.allVoiceChannels [ channel.Id ];
                if (vc.IsLocked ()) {

                    if (vc.lockerID == e.Author.Id) {
                        if (user.GuildPermissions.ManageChannels) {
                            return TaskResult (false, "Nice try, but you can't kick admins >:D.");

                        }
                        vc.Kick (user);
                        Program.messageControl.SendMessage (user, "Sorry man, but you have been kicked from voice channel **" + vc.GetName () + "**.");
                        return TaskResult (true, "User **" + Utility.GetUserName (user) + "** succesfully kicked.");
                    }

                    return TaskResult (false, "Only the person who locked this channel can do that, which is " + vc.GetLocker ().Mention);
                }

                return TaskResult (false, "Error - The channel isn't locked.");
            }

            return TaskResult(false, "Error - Are you even in a channel?");
        }
    }

    public class CLooking : Command {
        public CLooking() {
            command = "looking";
            shortHelp = "Toogle looking.";
            overloads.Add (new Overload (typeof (bool), "Toggles a tag which informs the world that you're looking for players."));
        }

        public Task<Result> Execute(SocketUserMessage e) {
            if ((e.Author as SocketGuildUser).VoiceChannel != null) {
                Voice.allVoiceChannels [ (e.Author as SocketGuildUser).VoiceChannel.Id ].ToggleStatus (Voice.VoiceChannel.VoiceChannelStatus.Looking);
                return TaskResult (Voice.allVoiceChannels [ (e.Author as SocketGuildUser).VoiceChannel.Id ].status == Voice.VoiceChannel.VoiceChannelStatus.Looking, "Succesfully toggled \"Looking for players\" tag.");
            } else {
                return TaskResult (false, "Error - You have to be in a channel.");
            }
        }
    }

    public class CFull : Command {
        public CFull() {
            command = "full";
            shortHelp = "Toogle full.";
            overloads.Add (new Overload (typeof (bool), "Toggles a tag which informs the world that you're full for players."));
        }

        public Task<Result> Execute(SocketUserMessage e) {
            if ((e.Author as SocketGuildUser).VoiceChannel != null) {
                Voice.allVoiceChannels [ (e.Author as SocketGuildUser).VoiceChannel.Id ].ToggleStatus (Voice.VoiceChannel.VoiceChannelStatus.Full);
                return TaskResult (Voice.allVoiceChannels [ (e.Author as SocketGuildUser).VoiceChannel.Id ].status == Voice.VoiceChannel.VoiceChannelStatus.Full, "Succesfully toggled \"Full of players\" tag.");
            }
            return TaskResult (false, "Error - You have to be in a channel.");
        }
    }

    public class CCustomName : Command {
        public CCustomName() {
            command = "name";
            shortHelp = "Set channel name.";
            overloads.Add (new Overload (typeof (bool), "Sets a custom suffix name on this channel, input \"reset\" to reset name."));
        }

        public Task<Result> Execute(SocketUserMessage e, string name) {
            if ((e.Author as SocketGuildUser).VoiceChannel != null) {
                if (name.ToLower () == "reset") {
                    Voice.allVoiceChannels [ (e.Author as SocketGuildUser).VoiceChannel.Id ].SetCustomName ("", true);
                    return TaskResult (true, "Succesfully reset channel name.");
                } else {
                    Voice.allVoiceChannels [ (e.Author as SocketGuildUser).VoiceChannel.Id ].SetCustomName (name, true);
                    return TaskResult (true, "Succesfully set custom name to **" + name + "**.");
                }
            } else {
                return TaskResult (false, "Error - You are not in a voice channel.");
            }
        }
    }

    public class CSetDesired : Command {
        public CSetDesired() {
            command = "desired";
            shortHelp = "Set desired members.";
            overloads.Add (new Overload (typeof (ulong), "Sets a desired amount of people in your current channel."));
        }

        public Task<Result> Execute(SocketUserMessage e, uint amount) {
            if ((e.Author as SocketGuildUser).VoiceChannel != null) {
                Voice.allVoiceChannels [ (e.Author as SocketGuildUser).VoiceChannel.Id ].SetDesiredMembers (amount);
                return TaskResult (amount, "Succesfully set desired amount of players to " + amount + ".");
            } else {
                return TaskResult (0, "Error - You have to be in a channel.");
            }

        }
    }
}