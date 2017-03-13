﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Discord;
using Discord.WebSocket;

namespace Adminthulhu {
    // Not to be confused with C# events, this class handles planned day events for Discord servers.
    public class AutomatedEventHandling : IClockable {

        public static Thread timeThread;
        public static List<Event> upcomingEvents = new List<Event>();

        public DateTime lastMesauredTime;
        public int checkDelay = 1000; // The time between each check in milliseconds.

        public static string eventFileName = "events";

        public static void SaveEvents () {
            SerializationIO.SaveObjectToFile (Program.dataPath + eventFileName + Program.gitHubIgnoreType, upcomingEvents);
        }

        public static void LoadEvents () {
            upcomingEvents = SerializationIO.LoadObjectFromFile<List<Event>> (Program.dataPath + eventFileName + Program.gitHubIgnoreType);
            if (upcomingEvents == null)
                upcomingEvents = new List<Event> ();
        }

        public Task Initialize (DateTime now) {
            LoadEvents ();
            return Task.CompletedTask;
        }

        public Task OnSecondPassed ( DateTime now ) {
            List<Event> toRemove = new List<Event> ();
            foreach (Event e in upcomingEvents) {
                if (e.eventState == Event.EventState.Awaiting) {
                    if (now > e.eventTime) {
                        StartEvent (e);

                        toRemove.Add (e);
                        SaveEvents ();
                    }

                    foreach (ulong userID in e.eventMemberIDs) {
                        if (e.eventTime.AddDays (UserSettings.GetSetting<int>(userID, "EventRemindTime, ", 2)) < now) {
                            DateTime remindTime = new DateTime (now.Year, now.Month, now.Day, 12, 00, 0);
                            if (remindTime < now && e.lastRemind[userID].Day != remindTime.Day) {
                                SendEventReminder (userID, e);
                            }
                        }
                    }
                }
            }

            foreach (Event r in toRemove)
                upcomingEvents.Remove (r);
            return Task.CompletedTask;
        }

        public static void SendEventReminder ( ulong userID, Event remindEvent ) {
            // Make sure discordClient is ready before sending any reminders.
            if (Program.discordClient != null && Program.GetServer () != null) {
                SocketGuildUser user = Program.GetServer ().GetUser (userID);
                Program.messageControl.SendMessage (user, "**REMINDER:** You have an upcoming event **" + remindEvent.eventName + "** on **" + Program.serverName + "** coming soon, this " + remindEvent.eventTime.DayOfWeek.ToString () + " at " + remindEvent.eventTime.ToString ());
                remindEvent.lastRemind[userID] = DateTime.Now;
            }
        }

        public static void StartEvent ( Event startingEvent ) {
            ChatLogger.Log ("Starting event: " + startingEvent.eventName + "!");
            startingEvent.eventState = Event.EventState.Ended;

            if (startingEvent.eventMemberIDs.Count != 0) {
                string mentions = "";
                foreach (ulong id in startingEvent.eventMemberIDs) {
                    SocketGuildUser user = Program.GetServer ().GetUser (id);
                    mentions += ", " + user.Mention;
                }
                string startText = "Hey" + mentions + "!\nWe're starting the **" + startingEvent.eventName + "** now!";
                if (startingEvent.eventDescription.Length != 0) {
                    startText += "\n`" + startingEvent.eventDescription + "`";
                }

                Program.messageControl.SendMessage (
                    Program.GetMainChannel (Program.GetServer ()) as SocketTextChannel,
                    startText);
            } else {
                Program.messageControl.SendMessage (
                    Program.GetMainChannel (Program.GetServer ()) as SocketTextChannel,
                    "Event **" + startingEvent.eventName + "** cancelled, since no one showed up. :(");
            }

        }

        public static Event FindEvent (string eventName) {
            foreach (Event e in upcomingEvents) {
                if (e.eventName.ToLower () == eventName.ToLower ())
                    return e;
            }
            return null;
        }

        public static void JoinEvent (ulong userID, string eventName) {
            Event e = FindEvent (eventName);
            e.eventMemberIDs.Add (userID);
            SaveEvents ();
        }

        public static void CreateEvent (string eventName, DateTime date, string description = null) {
            upcomingEvents.Add (new Event (eventName, date, description));
            SaveEvents ();
        }

        public Task OnMinutePassed ( DateTime time ) {
            return Task.CompletedTask;
        }

        public Task OnHourPassed ( DateTime time ) {
            SaveEvents ();
            return Task.CompletedTask;
        }

        public Task OnDayPassed ( DateTime time ) {
            return Task.CompletedTask;
        }

        public class Event {

            public string eventName;
            public string eventDescription;
            public DateTime eventTime;

            public enum EventState { Awaiting, InProgress, Ended }
            public EventState eventState = EventState.Awaiting;

            public Dictionary<ulong, DateTime> lastRemind;

            public List<ulong> eventMemberIDs = new List<ulong>();

            public Event (string name, DateTime time, string description = "") {
                eventName = name;
                eventTime = time;
                eventDescription = description;

                eventState = EventState.Awaiting;
            }

        }
    }

    public class EventCommands : CommandSet {
        public EventCommands () {
            command = "event";
            name = "Event Command Set";
            help = "A set of commands about events.";
            commandsInSet = new Command[] { new CCreateEvent (), new CCancelEvent (), new CEditEvent (), new CJoinEvent (), new CLeaveEvent (), new CEventList (), new CEventMembers (), new CEventVote (), new CRemoveVote () };
        }
    }

    // Move this command to a seperate file later, this is just for ease of writing.
    public class CCreateEvent : Command {
        public CCreateEvent () {
            command = "create";
            name = "Create a new event";
            argHelp = "<name>;<date>;<desc>";
            help = "Creates a new event on this server. Date format being D-M-Y H:M:S";
            argumentNumber = 3;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                DateTime parse;
                if (DateTime.TryParse (arguments[1], out parse)) {
                    AutomatedEventHandling.CreateEvent (arguments[0], parse, arguments[2]);
                    Program.messageControl.SendMessage (e, "Succesfully created event **" + arguments[0] + "** at " + parse.ToString ());
                }else {
                    Program.messageControl.SendMessage (e, "Failed to create event, could not parse time.");
                }
            }
            return Task.CompletedTask;
        }
    }

    public class CCancelEvent : Command {
        public CCancelEvent () {
            command = "cancel";
            name = "Cancel Event";
            argHelp = "<name>";
            help = "Cancels event " + argHelp;
            isAdminOnly = true;
            argumentNumber = 1;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                AutomatedEventHandling.Event eve = AutomatedEventHandling.FindEvent (arguments[0]);

                if (eve == null) {
                    Program.messageControl.SendMessage (e, "Unable to cancel event - event by name **" + arguments[0] + "** not found.");
                }else {
                    Program.messageControl.SendMessage (e, "Event **" + eve.eventName + "** has been cancelled.");
                    AutomatedEventHandling.upcomingEvents.Remove (eve);
                    AutomatedEventHandling.SaveEvents ();
                }
            }
            return Task.CompletedTask;
        }
    }

    public class CEditEvent : Command {
        public CEditEvent () {
            command = "edit";
            name = "Edit Event";
            argHelp = "<name>;<newname>;<newdesc>;<newtime>";
            help = "Edits event event <name>";
            isAdminOnly = true;
            argumentNumber = 4;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                AutomatedEventHandling.Event eve = AutomatedEventHandling.FindEvent (arguments[0]);

                if (eve == null) {
                    Program.messageControl.SendMessage (e, "Unable to edit event - event by name **" + arguments[0] + "** not found.");
                }else{
                    DateTime parse;
                    if (DateTime.TryParse (arguments[2], out parse)) {
                        eve.eventName = arguments[0];
                        eve.eventDescription = arguments[1];
                        eve.eventTime = parse;

                        Program.messageControl.SendMessage (e, "Event **" + eve.eventName + "** has been edited.");
                        AutomatedEventHandling.SaveEvents ();
                    } else {
                        Program.messageControl.SendMessage (e, "Failed to edit event, could not parse time.");
                    }
                }
            }
            return Task.CompletedTask;
        }
    }

    public class CJoinEvent : Command {
        public CJoinEvent () {
            command = "join";
            name = "Join Event";
            argHelp = "<name>";
            help = "Joins upcoming event " + argHelp;
            argumentNumber = 1;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                AutomatedEventHandling.Event eve = AutomatedEventHandling.FindEvent (arguments[0]);

                if (eve != null) {
                    eve.eventMemberIDs.Add (e.Author.Id);
                    AutomatedEventHandling.SaveEvents ();
                    Program.messageControl.SendMessage (e,"Succesfully joined event **" + eve.eventName + "**. You will be mentioned when it begins.");
                } else {
                    Program.messageControl.SendMessage (e, "Failed to join event - event by name **" + arguments[0] + "** could not be found.");
                }
            }
            return Task.CompletedTask;
        }
    }

    public class CLeaveEvent : Command {
        public CLeaveEvent () {
            command = "leave";
            name = "Leave Event";
            argHelp = "<name>";
            help = "Leaves upcoming event " + argHelp;
            argumentNumber = 1;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                AutomatedEventHandling.Event eve = AutomatedEventHandling.FindEvent (arguments[0]);

                if (eve != null) {
                    eve.eventMemberIDs.Remove (e.Author.Id);
                    AutomatedEventHandling.SaveEvents ();
                    Program.messageControl.SendMessage (e, "Succesfully left event **" + eve.eventName + "**. You will most certainly not be missed.");
                } else {
                    Program.messageControl.SendMessage (e, "Failed to leave event - event by name **" + arguments[0] + "** could not be found.");
                }
            }
            return Task.CompletedTask;
        }
    }

    public class CEventList : Command {
        public CEventList () {
            command = "list";
            name = "List Events";
            help = "List all upcoming events.";
            argumentNumber = 0;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                string combinedEvents = "Upcoming events are: ```";
                if (AutomatedEventHandling.upcomingEvents.Count != 0) {
                    foreach (AutomatedEventHandling.Event eve in AutomatedEventHandling.upcomingEvents) {
                        combinedEvents += "\n" + eve.eventName + " - " + eve.eventTime + " - " + eve.eventDescription;
                    }
                }else {
                    combinedEvents += "Nothing, add something! :D";
                }
                
                combinedEvents += "```";
                Program.messageControl.SendMessage (e, combinedEvents);
            }
            return Task.CompletedTask;
        }
    }

    public class CEventMembers : Command {
        public CEventMembers () {
            command = "members";
            name = "List Members";
            argHelp = "<name>";
            help = "List all members in event <name>.";
            argumentNumber = 1;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                AutomatedEventHandling.Event locEvent = AutomatedEventHandling.FindEvent (arguments[0]);
                if (locEvent != null) {

                    string combinedMembers = "Event members are: ```";
                    if (AutomatedEventHandling.upcomingEvents.Count != 0) {
                        foreach (ulong user in locEvent.eventMemberIDs) {
                            combinedMembers += "\n" + Program.GetUserName (Program.GetServer ().GetUser (user));
                        }
                    } else {
                        combinedMembers += "Nobody, why don't you join? :D";
                    }

                    combinedMembers += "```";
                    Program.messageControl.SendMessage (e, combinedMembers);
                }else {
                    Program.messageControl.SendMessage (e, "Failed to show event member list - event **" + arguments[0] + "** not found.");
                }
            }
            return Task.CompletedTask;
        }
    }
}