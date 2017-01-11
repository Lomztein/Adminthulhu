using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

// I highly advice against reading this. It is incredibly ugly and unoptimized.
namespace DiscordCthulhu {

    public class VoiceCommands : CommandSet {
        public VoiceCommands () {
            Initialize ();
            command = "voice";
            name = "Voice Commands";
            help = "A set of commands specifically for voice channels.";
            commandsInSet = new Command[] { new CLock (), new CUnlock (), new CInvite (), new CMembers (), new CKick (), new CCallVoiceChannel () };
        }
    }
    class CLock : Command {

        public CLock () {
            Initialize ();
            command = "lock";
            name = "Lock Voice Channel";
            help = "locks your current voice channel.";
            argumentNumber = 0;
        }

        public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                Channel channel = e.User.VoiceChannel;
                if (channel != null) {
                    AutomatedVoiceChannels.VoiceChannel vc = AutomatedVoiceChannels.allVoiceChannels[channel.Id];
                    if (!vc.IsLocked ()) {

                        if (!vc.lockable) {
                            Program.messageControl.SendMessage (e.Channel, "Error - cannot lock this channel due to *reasons*.");
                            return;
                        }

                        vc.Lock (e.User, true);

                        Program.messageControl.SendMessage (e.Channel, "Succesfully locked voice channel **" + vc.name + "**.");
                        return;
                    }

                    Program.messageControl.SendMessage (e.Channel, "Error - voice channel **" + vc.name + "** already locked.");
                    return;
                }

                Program.messageControl.SendMessage (e.Channel, "Failed to lock channel, are you even in one?");
            }
        }
    }

    class CUnlock : Command {

        public CUnlock () {
            Initialize ();
            command = "unlock";
            name = "Unlock Voice Channel";
            help = "unlocks your current voice channel if locked.";
            argumentNumber = 0;
        }

        public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                Channel channel = e.User.VoiceChannel;
                if (channel != null) {
                    AutomatedVoiceChannels.VoiceChannel vc = AutomatedVoiceChannels.allVoiceChannels[channel.Id];
                    if (vc.IsLocked ()) {

                        if (vc.lockerID == e.User.Id) {
                            vc.Unlock (true);
                            Program.messageControl.SendMessage (e.Channel, "Succesfully unlocked voice channel **" + vc.name + "**.");
                            return;
                        }

                        Program.messageControl.SendMessage (e.Channel, "Only the person who locked this channel can do that, which is " + vc.GetLocker ().Mention);
                        return;
                    }

                    Program.messageControl.SendMessage (e.Channel, "Failed to unlock voice channel **" + vc.name + "** - It is not unlocked.");
                    return;
                }

                Program.messageControl.SendMessage (e.Channel, "Failed to unlock channel, are you even in one?");
            }
        }
    }

    class CInvite : Command {

        public CInvite () {
            Initialize ();
            command = "invite";
            name = "Invite User";
            argHelp = "<username>";
            help = "Invites " + argHelp + " your current locked voice channel.";
            argumentNumber = 1;
        }

        public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                Channel channel = e.User.VoiceChannel;
                User user = Program.FindUserByName (e.Server, arguments[0]);
                if (user == null) {
                    Program.messageControl.SendMessage (e.Channel, "Failed to invite - User **" + arguments[0] + "** couldn't be found on this server.");
                    return;
                }

                if (channel != null) {
                    AutomatedVoiceChannels.VoiceChannel vc = AutomatedVoiceChannels.allVoiceChannels[channel.Id];
                    if (vc.IsLocked ()) {
                        vc.InviteUser (e.User, user);
                        Program.messageControl.SendMessage (e.Channel, "User **" + Program.GetUserName (user) + "** succesfully invited.");
                        return;
                    }

                    Program.messageControl.SendMessage (e.Channel, "The channel isn't locked, but I'm sure " + Program.GetUserName (user) + " would love to join anyways.");
                    return;
                }

                Program.messageControl.SendMessage (e.Channel, "Failed to invite, are you even in a channel?");
            }
        }
    }

    class CMembers : Command {

        public CMembers () {
            Initialize ();
            command = "members";
            name = "Member List";
            help = "Display list of members of your locked voice channel.";
            argumentNumber = 0;
        }

        public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                Channel channel = e.User.VoiceChannel;

                if (channel != null) {
                    AutomatedVoiceChannels.VoiceChannel vc = AutomatedVoiceChannels.allVoiceChannels[channel.Id];
                    if (vc.IsLocked ()) {
                        string reply = "```\n";
                        foreach (ulong user in vc.allowedUsers) {
                            //reply += Program.GetUserName (Program.GetServer ().GetUser (user)) + "\n";
                        }
                        reply += "```";
                        Program.messageControl.SendMessage (e.Channel, "Users allowed on your locked channel:\n" + reply);
                        return;
                    }

                    Program.messageControl.SendMessage (e.Channel, "Error - The channel isn't locked.");
                    return;
                }

                Program.messageControl.SendMessage (e.Channel, "Error - Are you even in a channel?");
            }
        }
    }

    class CKick : Command {

        public CKick () {
            Initialize ();
            command = "kick";
            name = "Kick Member";
            argHelp = "<username>";
            help = "Kicks member from your locked voice channel. You must be the one who locked.";
            argumentNumber = 1;
        }

        public override void ExecuteCommand ( MessageEventArgs e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                Channel channel = e.User.VoiceChannel;
                User user = Program.FindUserByName (e.Server, arguments[0]);
                if (user == null) {
                    Program.messageControl.SendMessage (e.Channel, "Error - User not found.");
                    return;
                }

                if (channel != null) {
                    AutomatedVoiceChannels.VoiceChannel vc = AutomatedVoiceChannels.allVoiceChannels[channel.Id];
                    if (vc.IsLocked ()) {

                        if (vc.lockerID == e.User.Id) {
                            if (user.ServerPermissions.ManageChannels) {
                                Program.messageControl.SendMessage (e.Channel, "Nice try, but you can't kick admins >:D.");
                                return;
                            }
                            vc.Kick (user);
                            Program.messageControl.SendMessage (e.Channel, "User **" + Program.GetUserName (user) + "** succesfully kicked.");
                            Program.messageControl.SendMessage (user, "Sorry man, but you have been kicked from voice channel **" + vc.name + "**.");
                            return;
                        }

                        Program.messageControl.SendMessage (e.Channel, "Only the person who locked this channel can do that, which is " + vc.GetLocker ().Mention);
                        return;
                    }

                    Program.messageControl.SendMessage (e.Channel, "Error - The channel isn't locked.");
                    return;
                }

                Program.messageControl.SendMessage (e.Channel, "Error - Are you even in a channel?");
            }
        }
    }
}