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
            longHelp = "A set of commands specifically for voice channels.";
            commandsInSet = new Command[] { new CLock (), new CUnlock (), new CInvite (), new CMembers (), new CKick (), new CCallVoiceChannel (), new CLooking (), new CFull (), new CSetDesired (), new CCustomName (), new CCreate () };
            catagory = Catagory.Utility;
        }
    }
    class CLock : Command {

        public CLock () {
            command = "lock";
            shortHelp = "Lock voice channel.";
            longHelp = "locks your current voice channel.";
            argumentNumber = 0;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                SocketGuildChannel channel = (e.Author as SocketGuildUser).VoiceChannel;
                if (channel != null) {
                    AutomatedVoiceChannels.VoiceChannel vc = AutomatedVoiceChannels.allVoiceChannels[channel.Id];
                    if (!vc.IsLocked ()) {

                        if (!vc.lockable) {
                            Program.messageControl.SendMessage (e.Channel, "Error - cannot lock this channel due to *reasons*.", false);
                            return Task.CompletedTask;
                        }

                        vc.Lock (e.Author as SocketGuildUser, true);

                        Program.messageControl.SendMessage (e.Channel, "Succesfully locked voice channel **" + vc.name + "**.", false);
                        return Task.CompletedTask;
                    }

                    Program.messageControl.SendMessage (e.Channel, "Error - voice channel **" + vc.name + "** already locked.", false);
                    return Task.CompletedTask;
                }

                Program.messageControl.SendMessage (e.Channel, "Failed to lock channel, are you even in one?", false);
            }
            return Task.CompletedTask;
        }
    }

    class CUnlock : Command {

        public CUnlock () {
            command = "unlock";
            shortHelp = "Unlock voice channel.";
            longHelp = "unlocks your current voice channel if locked.";
            argumentNumber = 0;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                SocketGuildChannel channel = (e.Author as SocketGuildUser).VoiceChannel;
                if (channel != null) {
                    AutomatedVoiceChannels.VoiceChannel vc = AutomatedVoiceChannels.allVoiceChannels[channel.Id];
                    if (vc.IsLocked ()) {

                        if (vc.lockerID == e.Author.Id) {
                            vc.Unlock (true);
                            Program.messageControl.SendMessage (e.Channel, "Succesfully unlocked voice channel **" + vc.name + "**.", false);
                            return Task.CompletedTask;

                        }

                        Program.messageControl.SendMessage (e.Channel, "Only the person who locked this channel can do that, which is " + vc.GetLocker ().Mention, false);
                        return Task.CompletedTask;

                    }

                    Program.messageControl.SendMessage (e.Channel, "Failed to unlock voice channel **" + vc.name + "** - It is not unlocked.", false);
                    return Task.CompletedTask;

                }

                Program.messageControl.SendMessage (e.Channel, "Failed to unlock channel, are you even in one?", false);
            }
            return Task.CompletedTask;
        }
    }

    class CCreate : Command {

        public CCreate() {
            command = "create";
            shortHelp = "Create temporary voice channel.";
            longHelp = "Creates a temporary voice channel for whatever you need.";
            argumentNumber = 2;
        }

        public override Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                TimeSpan timeSpan;
                if (Utility.TryParseSimpleTimespan (arguments [ 1 ], out timeSpan)) {
                    AutomatedVoiceChannels.CreateTemporaryChannel (arguments [ 0 ], timeSpan);
                    Program.messageControl.SendMessage (e, "Succesfully created voice channel by name **" + arguments[0] + "**!", false);
                } else {
                    Program.messageControl.SendMessage (e, "Failed to create voice channel - TimeSpan could not be parsed.", false);
                }
            }
            return Task.CompletedTask;
        }
    }

    class CInvite : Command {

        public CInvite () {
            command = "invite";
            shortHelp = "Invite user.";
            argHelp = "<username>";
            longHelp = "Invites " + argHelp + " your current locked voice channel.";
            argumentNumber = 1;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                SocketGuildChannel channel = (e.Author as SocketGuildUser).VoiceChannel;
                SocketGuildUser user = Utility.FindUserByName ((e.Channel as SocketGuildChannel).Guild, arguments[0]);
                if (user == null) {
                    Program.messageControl.SendMessage (e.Channel, "Failed to invite - User **" + arguments[0] + "** couldn't be found on this server.", false);
                    return Task.CompletedTask;

                }

                if (channel != null) {
                    AutomatedVoiceChannels.VoiceChannel vc = AutomatedVoiceChannels.allVoiceChannels[channel.Id];
                    if (vc.IsLocked ()) {
                        vc.InviteUser (e.Author as SocketGuildUser, user);
                        Program.messageControl.SendMessage (e.Channel, "User **" + Utility.GetUserName (user) + "** succesfully invited.", false);
                        return Task.CompletedTask;

                    }

                    Program.messageControl.SendMessage (e.Channel, "The channel isn't locked, but I'm sure " + Utility.GetUserName (user) + " would love to join anyways.", false);
                    return Task.CompletedTask;

                }

                Program.messageControl.SendMessage (e.Channel, "Failed to invite, are you even in a channel?", false);
            }
            return Task.CompletedTask;
        }
    }

    class CMembers : Command {

        public CMembers () {
            command = "members";
            shortHelp = "Member list.";
            longHelp = "Display list of allowed members of your locked voice channel.";
            argumentNumber = 0;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                SocketGuildChannel channel = (e.Author as SocketGuildUser).VoiceChannel;

                if (channel != null) {
                    AutomatedVoiceChannels.VoiceChannel vc = AutomatedVoiceChannels.allVoiceChannels[channel.Id];
                    if (vc.IsLocked ()) {
                        string reply = "```\n";
                        foreach (ulong user in vc.allowedUsers) {
                            //reply += Utility.GetUserName (Utility.GetServer ().GetUser (user)) + "\n";
                        }
                        reply += "```";
                        Program.messageControl.SendMessage (e.Channel, "Users allowed on your locked channel:\n" + reply, false);
                        return Task.CompletedTask;

                    }

                    Program.messageControl.SendMessage (e.Channel, "Error - The channel isn't locked.", false);
                    return Task.CompletedTask;

                }

                Program.messageControl.SendMessage (e.Channel, "Error - Are you even in a channel?", false);
            }
            return Task.CompletedTask;
        }
    }

    class CKick : Command {

        public CKick () {
            command = "kick";
            shortHelp = "Kick member.";
            argHelp = "<username>";
            longHelp = "Kicks member from your locked voice channel. You must be the one who locked.";
            argumentNumber = 1;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                SocketGuildChannel channel = (e.Author as SocketGuildUser).VoiceChannel;
                SocketGuildUser user = Utility.FindUserByName ((e.Channel as SocketGuildChannel).Guild, arguments[0]);
                if (user == null) {
                    Program.messageControl.SendMessage (e.Channel, "Error - User not found.", false);
                    return Task.CompletedTask;
                }

                if (channel != null) {
                    AutomatedVoiceChannels.VoiceChannel vc = AutomatedVoiceChannels.allVoiceChannels[channel.Id];
                    if (vc.IsLocked ()) {

                        if (vc.lockerID == e.Author.Id) {
                            if (user.GuildPermissions.ManageChannels) {
                                Program.messageControl.SendMessage (e.Channel, "Nice try, but you can't kick admins >:D.", false);
                                return Task.CompletedTask;

                            }
                            vc.Kick (user);
                            Program.messageControl.SendMessage (e.Channel, "User **" + Utility.GetUserName (user) + "** succesfully kicked.", false);
                            Program.messageControl.SendMessage (user, "Sorry man, but you have been kicked from voice channel **" + vc.name + "**.");
                            return Task.CompletedTask;

                        }

                        Program.messageControl.SendMessage (e.Channel, "Only the person who locked this channel can do that, which is " + vc.GetLocker ().Mention, false);
                        return Task.CompletedTask;

                    }

                    Program.messageControl.SendMessage (e.Channel, "Error - The channel isn't locked.", false);
                    return Task.CompletedTask;

                }

                Program.messageControl.SendMessage (e.Channel, "Error - Are you even in a channel?", false);
            }
            return Task.CompletedTask;
        }
    }

    public class CLooking : Command {
        public CLooking() {
            command = "looking";
            shortHelp = "Toogle looking.";
            longHelp = "Toggles a tag which informs the world you're looking for players.";
            argumentNumber = 0;
        }

        public override Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                if ((e.Author as SocketGuildUser).VoiceChannel != null) {
                    AutomatedVoiceChannels.allVoiceChannels [ (e.Author as SocketGuildUser).VoiceChannel.Id ].ToggleStatus (AutomatedVoiceChannels.VoiceChannel.VoiceChannelStatus.Looking);
                    Program.messageControl.SendMessage (e, "Succesfully toggled \"Looking for players\" tag.", false);
                } else {
                    Program.messageControl.SendMessage (e, "Error - You have to be in a channel.", false);
                }
            }

            return Task.CompletedTask;
        }
    }

    public class CFull : Command {
        public CFull() {
            command = "full";
            shortHelp = "Toogle full.";
            longHelp = "Toggles a tag which informs the world you're full of players.";
            argumentNumber = 0;
        }

        public override Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                if ((e.Author as SocketGuildUser).VoiceChannel != null) {
                AutomatedVoiceChannels.allVoiceChannels [ (e.Author as SocketGuildUser).VoiceChannel.Id ].ToggleStatus (AutomatedVoiceChannels.VoiceChannel.VoiceChannelStatus.Full);
                    Program.messageControl.SendMessage (e, "Succesfully toggled \"Full of players\" tag.", false);
                }
            } else {
                Program.messageControl.SendMessage (e, "Error - You have to be in a channel.", false);
            }
            return Task.CompletedTask;
        }
    }

    public class CCustomName : Command {
        public CCustomName() {
            command = "name";
            shortHelp = "Set channel name.";
            longHelp = "Sets a custom game name to this channel. Input \"reset\" to reset.";
            argumentNumber = 1;
        }

        public override Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                if ((e.Author as SocketGuildUser).VoiceChannel != null) {
                    if (arguments [ 0 ].ToLower () == "reset") {
                        AutomatedVoiceChannels.allVoiceChannels [ (e.Author as SocketGuildUser).VoiceChannel.Id ].SetCustomName ("", true);
                        Program.messageControl.SendMessage (e, "Succesfully reset channel name.", false);
                    } else {
                        AutomatedVoiceChannels.allVoiceChannels [ (e.Author as SocketGuildUser).VoiceChannel.Id ].SetCustomName (arguments[0], true);
                        Program.messageControl.SendMessage (e, "Succesfully set custom name to **" + arguments[0] + "**.", false);
                    }
                }
            } else {
                Program.messageControl.SendMessage (e, "Error - You have to be in a channel.", false);
            }
            return Task.CompletedTask;
        }
    }

    public class CSetDesired : Command {
        public CSetDesired() {
            command = "desired";
            shortHelp = "Set desired members.";
            longHelp = "Sets a desired amount of people in this channel.";
            argumentNumber = 1;
        }

        public override Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                if ((e.Author as SocketGuildUser).VoiceChannel != null) {
                    uint parse;
                    if (uint.TryParse (arguments [ 0 ], out parse)) {
                        AutomatedVoiceChannels.allVoiceChannels [ (e.Author as SocketGuildUser).VoiceChannel.Id ].SetDesiredMembers (parse);
                        Program.messageControl.SendMessage (e, "Succesfully set desired amount of players to " + parse + ".", false);
                    } else {
                        Program.messageControl.SendMessage (e, "Failed to set desired amount of players to " + arguments [ 0 ] + " - could not parse.", false);
                    }
                }
            } else {
                Program.messageControl.SendMessage (e, "Error - You have to be in a channel.", false);
            }

            return Task.CompletedTask;
        }
    }
}