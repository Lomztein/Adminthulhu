using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

// I highly advice against reading this. It is incredibly ugly and unoptimized.
namespace DiscordCthulhu {

    public class VoiceCommands : CommandSet {
        public VoiceCommands () {
            command = "voice";
            name = "Voice Commands";
            help = "A set of commands specifically for voice channels.";
            commandsInSet = new Command[] { new CLock (), new CUnlock (), new CInvite (), new CMembers (), new CKick (), new CCallVoiceChannel () };
        }
    }
    class CLock : Command {

        public CLock () {
            command = "lock";
            name = "Lock Voice Channel";
            help = "locks your current voice channel.";
            argumentNumber = 0;
        }

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            await base.ExecuteCommand (e, arguments);
            if (await AllowExecution (e, arguments)) {

                SocketGuildChannel channel = (e.Author as SocketGuildUser).VoiceChannel;
                if (channel != null) {
                    AutomatedVoiceChannels.VoiceChannel vc = AutomatedVoiceChannels.allVoiceChannels[channel.Id];
                    if (!vc.IsLocked ()) {

                        if (!vc.lockable) {
                            await Program.messageControl.SendMessage (e.Channel, "Error - cannot lock this channel due to *reasons*.");
                            return;
                        }

                        await vc.Lock (e.Author as SocketGuildUser, true);

                        await Program.messageControl.SendMessage (e.Channel, "Succesfully locked voice channel **" + vc.name + "**.");
                        return;
                    }

                    await Program.messageControl.SendMessage (e.Channel, "Error - voice channel **" + vc.name + "** already locked.");
                    return;
                }

                await Program.messageControl.SendMessage (e.Channel, "Failed to lock channel, are you even in one?");
            }
        }
    }

    class CUnlock : Command {

        public CUnlock () {
            command = "unlock";
            name = "Unlock Voice Channel";
            help = "unlocks your current voice channel if locked.";
            argumentNumber = 0;
        }

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            await base.ExecuteCommand (e, arguments);
            if (await AllowExecution (e, arguments)) {

                SocketGuildChannel channel = (e.Author as SocketGuildUser).VoiceChannel;
                if (channel != null) {
                    AutomatedVoiceChannels.VoiceChannel vc = AutomatedVoiceChannels.allVoiceChannels[channel.Id];
                    if (vc.IsLocked ()) {

                        if (vc.lockerID == e.Author.Id) {
                            await vc.Unlock (true);
                            await Program.messageControl.SendMessage (e.Channel, "Succesfully unlocked voice channel **" + vc.name + "**.");
                            return;
                        }

                        await Program.messageControl.SendMessage (e.Channel, "Only the person who locked this channel can do that, which is " + vc.GetLocker ().Mention);
                        return;
                    }

                    await Program.messageControl.SendMessage (e.Channel, "Failed to unlock voice channel **" + vc.name + "** - It is not unlocked.");
                    return;
                }

                await Program.messageControl.SendMessage (e.Channel, "Failed to unlock channel, are you even in one?");
            }
        }
    }

    class CInvite : Command {

        public CInvite () {
            command = "invite";
            name = "Invite User";
            argHelp = "<username>";
            help = "Invites " + argHelp + " your current locked voice channel.";
            argumentNumber = 1;
        }

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            await base.ExecuteCommand (e, arguments);
            if (await AllowExecution (e, arguments)) {

                SocketGuildChannel channel = (e.Author as SocketGuildUser).VoiceChannel;
                SocketGuildUser user = Program.FindUserByName ((e.Channel as SocketGuildChannel).Guild, arguments[0]);
                if (user == null) {
                    await Program.messageControl.SendMessage (e.Channel, "Failed to invite - User **" + arguments[0] + "** couldn't be found on this server.");
                    return;
                }

                if (channel != null) {
                    AutomatedVoiceChannels.VoiceChannel vc = AutomatedVoiceChannels.allVoiceChannels[channel.Id];
                    if (vc.IsLocked ()) {
                        await vc.InviteUser (e.Author as SocketGuildUser, user);
                        await Program.messageControl.SendMessage (e.Channel, "User **" + Program.GetUserName (user) + "** succesfully invited.");
                        return;
                    }

                    await Program.messageControl.SendMessage (e.Channel, "The channel isn't locked, but I'm sure " + Program.GetUserName (user) + " would love to join anyways.");
                    return;
                }

                await Program.messageControl.SendMessage (e.Channel, "Failed to invite, are you even in a channel?");
            }
        }
    }

    class CMembers : Command {

        public CMembers () {
            command = "members";
            name = "Member List";
            help = "Display list of members of your locked voice channel.";
            argumentNumber = 0;
        }

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            await base.ExecuteCommand (e, arguments);
            if (await AllowExecution (e, arguments)) {

                SocketGuildChannel channel = (e.Author as SocketGuildUser).VoiceChannel;

                if (channel != null) {
                    AutomatedVoiceChannels.VoiceChannel vc = AutomatedVoiceChannels.allVoiceChannels[channel.Id];
                    if (vc.IsLocked ()) {
                        string reply = "```\n";
                        foreach (ulong user in vc.allowedUsers) {
                            //reply += Program.GetUserName (Program.GetServer ().GetUser (user)) + "\n";
                        }
                        reply += "```";
                        await Program.messageControl.SendMessage (e.Channel, "Users allowed on your locked channel:\n" + reply);
                        return;
                    }

                    await Program.messageControl.SendMessage (e.Channel, "Error - The channel isn't locked.");
                    return;
                }

                await Program.messageControl.SendMessage (e.Channel, "Error - Are you even in a channel?");
            }
        }
    }

    class CKick : Command {

        public CKick () {
            command = "kick";
            name = "Kick Member";
            argHelp = "<username>";
            help = "Kicks member from your locked voice channel. You must be the one who locked.";
            argumentNumber = 1;
        }

        public override async Task ExecuteCommand ( SocketMessage e, List<string> arguments ) {
            await base.ExecuteCommand (e, arguments);
            if (await AllowExecution (e, arguments)) {

                SocketGuildChannel channel = (e.Author as SocketGuildUser).VoiceChannel;
                SocketGuildUser user = Program.FindUserByName ((e.Channel as SocketGuildChannel).Guild, arguments[0]);
                if (user == null) {
                    await Program.messageControl.SendMessage (e.Channel, "Error - User not found.");
                    return;
                }

                if (channel != null) {
                    AutomatedVoiceChannels.VoiceChannel vc = AutomatedVoiceChannels.allVoiceChannels[channel.Id];
                    if (vc.IsLocked ()) {

                        if (vc.lockerID == e.Author.Id) {
                            if (user.GuildPermissions.ManageChannels) {
                                await Program.messageControl.SendMessage (e.Channel, "Nice try, but you can't kick admins >:D.");
                                return;
                            }
                            vc.Kick (user);
                            await Program.messageControl.SendMessage (e.Channel, "User **" + Program.GetUserName (user) + "** succesfully kicked.");
                            await Program.messageControl.SendMessage (user, "Sorry man, but you have been kicked from voice channel **" + vc.name + "**.");
                            return;
                        }

                        await Program.messageControl.SendMessage (e.Channel, "Only the person who locked this channel can do that, which is " + vc.GetLocker ().Mention);
                        return;
                    }

                    await Program.messageControl.SendMessage (e.Channel, "Error - The channel isn't locked.");
                    return;
                }

                await Program.messageControl.SendMessage (e.Channel, "Error - Are you even in a channel?");
            }
        }
    }
}