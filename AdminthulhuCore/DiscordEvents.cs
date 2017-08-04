using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Discord;
using Discord.WebSocket;

namespace Adminthulhu {
    // Not to be confused with C# events, this class handles planned day events for Discord servers.
    public class DiscordEvents : IClockable, IConfigurable {

        public static Thread timeThread;
        public static List<Event> upcomingEvents = new List<Event>();

        public DateTime lastMesauredTime;
        public int checkDelay = 1000; // The time between each check in milliseconds.

        public static string eventFileName = "events";
        public static List<Event> ongoingEvents = new List<Event> ();

        public static float percentageForEventActive = 0.75f;

        public void LoadConfiguration() {
            percentageForEventActive = BotConfiguration.GetSetting ("Events.PersentageForEventActive", "", percentageForEventActive);
        }

        public static void SaveEvents () {
            SerializationIO.SaveObjectToFile (Program.dataPath + eventFileName + Program.gitHubIgnoreType, upcomingEvents);
        }

        public static void LoadEvents () {
            upcomingEvents = SerializationIO.LoadObjectFromFile<List<Event>> (Program.dataPath + eventFileName + Program.gitHubIgnoreType);
            if (upcomingEvents == null)
                upcomingEvents = new List<Event> ();
        }

        public Task Initialize (DateTime now) {
            LoadConfiguration ();
            BotConfiguration.AddConfigurable (this);
            LoadEvents ();
            return Task.CompletedTask;
        }

        private static List<Event> toAdd;
        public Task OnSecondPassed(DateTime now) {
            List<Event> toRemoveUpcoming = new List<Event> ();
            List<Event> toRemoveOngoing = new List<Event> ();
            toAdd = new List<Event> ();
            foreach (Event e in upcomingEvents) {
                if (e.eventState == Event.EventState.Awaiting) {
                    if (now > e.eventTime) {
                        StartEvent (e);
                    }
                }
            }

            foreach (Event e in ongoingEvents) {
                if (e.eventTime.Add (e.duration) < DateTime.Now) {
                    e.eventState = Event.EventState.Ended;
                }
            }

            toRemoveUpcoming.AddRange (upcomingEvents.Where (x => x.eventState != Event.EventState.Awaiting));
            toRemoveOngoing.AddRange (ongoingEvents.Where (x => x.eventState == Event.EventState.Ended));
            bool doSave = toRemoveOngoing.Count + toRemoveUpcoming.Count + toAdd.Count > 0 ? true : false;

            foreach (Event r in toRemoveUpcoming)
                upcomingEvents.Remove (r);
            foreach (Event r in toRemoveOngoing)
                ongoingEvents.Remove (r);
            foreach (Event a in toAdd)
                upcomingEvents.Add (a);
            
            if (doSave)
                SaveEvents ();

            return Task.CompletedTask;
        }

        public static void StartEvent ( Event startingEvent ) {
            ChatLogger.Log ("Starting event: " + startingEvent.eventName + "!");
            startingEvent.eventState = Event.EventState.InProgress;

            if (startingEvent.eventMemberIDs.Count != 0) {
                string mentions = "";
                foreach (ulong id in startingEvent.eventMemberIDs) {
                    SocketGuildUser user = Utility.GetServer ().GetUser (id);
                    mentions += ", " + user.Mention;
                }

                if (startingEvent.eventMemberIDs.Count > 0) {
                    string members;
                    Embed eventEmbed = ConstructEmbed (startingEvent, out members);
                    Program.messageControl.SendEmbed (Utility.GetMainChannel () as ITextChannel, eventEmbed,  members);
                    AutomatedVoiceChannels.CreateTemporaryChannel (startingEvent.eventName, startingEvent.duration);
                    ongoingEvents.Add (startingEvent);
                } else {
                    Program.messageControl.SendMessage (
                        Utility.GetMainChannel () as SocketTextChannel,
                        "Event **" + startingEvent.eventName + "** cancelled, since no one showed up. :(", true);
                    startingEvent.eventState = Event.EventState.Ended;
                }

                if (startingEvent.repeatTime.Ticks != 0) {
                    toAdd.Add (CreateAndReturnEvent (startingEvent.eventName, startingEvent.eventTime.Add (startingEvent.repeatTime), startingEvent.duration, startingEvent.hostID, startingEvent.iconUrl, startingEvent.eventDescription, startingEvent.repeatTime));
                }
            }
        }

        public static bool ContainsEventMembers(out Event foundEvent, params SocketGuildUser[] users) {
            List<ulong> userIDs = new List<ulong> ();
            foreach (SocketUser user in users)
                userIDs.Add (user.Id);

            foreach (Event e in ongoingEvents) {
                int eventMembers = e.eventMemberIDs.Where (x => userIDs.Contains (x)).Count ();
                if (eventMembers >= e.eventMemberIDs.Count * percentageForEventActive) {
                    foundEvent = e;
                    return true;
                }
            }
            foundEvent = null;
            return false;
        }

        public static Embed ConstructEmbed(Event evt, out string memberString) {
            SocketGuildUser user = Utility.GetServer ().GetUser (evt.hostID);
            memberString = "Calling";
            foreach (ulong id in evt.eventMemberIDs) {
                memberString += " <@" + id+ ">";
            }
            memberString += "!";

            EmbedBuilder builder = new EmbedBuilder ()
            .WithTitle ("Event announcement for " + evt.eventName + "!")
            .WithDescription (evt.eventDescription)
            .WithColor (CSetColor.GetUserColor (evt.hostID).Color)
            .WithTimestamp (evt.eventTime)
            .WithThumbnailUrl (Uri.IsWellFormedUriString (evt.iconUrl, UriKind.Absolute) ? evt.iconUrl : "")
            .AddField ("Duration", evt.duration.ToString ())
            .WithAuthor (author => {
                author
           .WithName (Utility.GetUserName (user))
           .WithIconUrl (user.GetAvatarUrl ());

            });
            Embed embed = builder.Build ();
            return embed;
        }

        public static Event FindEvent (string eventName) {
            foreach (Event e in upcomingEvents) {
                if (e.eventName.ToLower () == eventName.ToLower ())
                    return e;
            }
            return null;
        }

        public static bool JoinEvent (ulong userID, string eventName) {
            Event e = FindEvent (eventName);
            if (e != null && !e.eventMemberIDs.Contains (userID)) {
                e.eventMemberIDs.Add (userID);
                SaveEvents ();
                return true;
            }
            return false;
        }

        public static bool LeaveEvent(ulong userID, string eventName) {
            Event e = FindEvent (eventName);
            if (e != null && e.eventMemberIDs.Contains (userID)) {
                e.eventMemberIDs.Remove (userID);
                SaveEvents ();
                return true;
            }
            return false;
        }

        public static Event CreateAndReturnEvent(string eventName, DateTime date, TimeSpan duration, ulong hostID, string iconUrl, string description, TimeSpan repeatTime) {
            return new Event (eventName, date, duration, hostID, iconUrl, description, repeatTime); // Basicly just a constructor wrapper at this point
        }

        public static void CreateEvent (string eventName, DateTime date, TimeSpan duration, ulong hostID, string iconUrl, string description, TimeSpan repeatTime) {
            upcomingEvents.Add (CreateAndReturnEvent (eventName, date, duration, hostID, iconUrl, description, repeatTime));
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
            public ulong hostID;
            public string iconUrl;
            public TimeSpan duration;
            public TimeSpan repeatTime;

            public enum EventState { Awaiting, InProgress, Ended }
            public EventState eventState = EventState.Awaiting;

            public Dictionary<ulong, DateTime> lastRemind;

            public List<ulong> eventMemberIDs = new List<ulong>();

            public Event (string name, DateTime time, TimeSpan _duration, ulong _hostID, string _iconUrl, string description, TimeSpan _repeatTime) {
                eventName = name;
                eventTime = time;
                duration = _duration;
                hostID = _hostID;
                iconUrl = _iconUrl;
                repeatTime = _repeatTime;

                eventDescription = description;

                eventState = EventState.Awaiting;
            }

        }
    }

    public class EventCommands : CommandSet {
        public EventCommands () {
            command = "event";
            shortHelp = "Event command set.";
            longHelp = "A set of commands about events.";
            commandsInSet = new Command[] { new CCreateEvent (), new CCancelEvent (), new CEditEvent (), new CJoinEvent (), new CLeaveEvent (), new CEventList (), new CEventMembers (), new CListEventGames () };
            catagory = Catagory.Utility;
        }
    }

    // Move this command to a seperate file later, this is just for ease of writing.
    public class CCreateEvent : Command {
        public CCreateEvent () {
            command = "create";
            shortHelp = "Create a new event.";
            longHelp = "Creates a new event on this server. Uses a questionnaire for input.";
            argumentNumber = 0;
        }

        public override async Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {

                TimeSpan repeat = new TimeSpan (0);
                TimeSpan duration = new TimeSpan (0);
                List<object> results = null;

                try {
                     results = await MessageControl.CreateQuestionnaire (e.Author.Id, e.Channel,
                        new MessageControl.QE ("Input event name.", typeof (string)),
                        new MessageControl.QE ("Input event date.", typeof (DateTime)),
                        new MessageControl.QE ("Input event duration.", typeof (string)),
                        new MessageControl.QE ("Input event URL.", typeof (string)),
                        new MessageControl.QE ("Input event description.", typeof (string)),
                        new MessageControl.QE ("Input event repeat time.", typeof (string)));
                } catch (Exception exception) {
                    Program.messageControl.SendMessage (e, exception.Message, false);
                }

                if (results != null) {
                    DateTime parse = DateTime.Parse (results [ 1 ].ToString ());
                    Utility.TryParseSimpleTimespan (results [ 5 ] as string, out repeat);
                    Utility.TryParseSimpleTimespan (results [ 2 ] as string, out duration);

                    DiscordEvents.CreateEvent (results [ 0 ] as string, parse, duration, e.Author.Id, results [ 3 ] as string, results [ 4 ] as string, repeat);
                    DiscordEvents.JoinEvent (e.Author.Id, results [ 0 ] as string);
                    Program.messageControl.SendMessage (e, "Succesfully created event **" + results[0] + "** at " + parse.ToString (), false);
                } else {
                    Program.messageControl.SendMessage (e, "Failed to create event, could not parse time.", false);
                }
            }
        }
    }

    public class CCancelEvent : Command {
        public CCancelEvent () {
            command = "cancel";
            shortHelp = "Cancel event.";
            argHelp = "<name>";
            longHelp = "Cancels event " + argHelp;
            isAdminOnly = true;
            argumentNumber = 1;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                DiscordEvents.Event eve = DiscordEvents.FindEvent (arguments[0]);

                if (eve == null) {
                    Program.messageControl.SendMessage (e, "Unable to cancel event - event by name **" + arguments [ 0 ] + "** not found.", false);
                } else if (eve.hostID != e.Author.Id) {
                    Program.messageControl.SendMessage (e, "Unable to cancel event **" + eve.eventName + "**, you are not the host.", false);
                } else {
                    Program.messageControl.SendMessage (e, "Event **" + eve.eventName + "** has been cancelled.",false);
                    DiscordEvents.upcomingEvents.Remove (eve);
                    DiscordEvents.SaveEvents ();
                }
            }
            return Task.CompletedTask;
        }
    }

    public class CEditEvent : Command {
        public CEditEvent () {
            command = "edit";
            shortHelp = "Edit event.";
            argHelp = "<name>;<newname>;<newdesc>;<newtime (d-m-y h:m:s)>";
            longHelp = "Edits event event <name>";
            isAdminOnly = true;
            argumentNumber = 4;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                DiscordEvents.Event eve = DiscordEvents.FindEvent (arguments[0]);

                if (eve == null) {
                    Program.messageControl.SendMessage (e, "Unable to edit event - event by name **" + arguments[0] + "** not found.", false);
                }else{
                    DateTime parse;
                    if (Utility.TryParseDatetime (arguments[2], e.Author.Id, out parse)) {
                        eve.eventName = arguments[0];
                        eve.eventDescription = arguments[1];
                        eve.eventTime = parse;

                        Program.messageControl.SendMessage (e, "Event **" + eve.eventName + "** has been edited.", false);
                        DiscordEvents.SaveEvents ();
                    } else {
                        Program.messageControl.SendMessage (e, "Failed to edit event, could not parse time.", false);
                    }
                }
            }
            return Task.CompletedTask;
        }
    }

    public class CJoinEvent : Command {
        public CJoinEvent () {
            command = "join";
            shortHelp = "Join event.";
            argHelp = "<name>";
            longHelp = "Joins upcoming event " + argHelp;
            argumentNumber = 1;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                DiscordEvents.Event eve = DiscordEvents.FindEvent (arguments[0]);

                if (eve != null) {
                    eve.eventMemberIDs.Add (e.Author.Id);
                    DiscordEvents.SaveEvents ();
                    Program.messageControl.SendMessage (e,"Succesfully joined event **" + eve.eventName + "**. You will be mentioned when it begins.", false);
                } else {
                    Program.messageControl.SendMessage (e, "Failed to join event - event by name **" + arguments[0] + "** could not be found.", false);
                }
            }
            return Task.CompletedTask;
        }
    }

    public class CLeaveEvent : Command {
        public CLeaveEvent () {
            command = "leave";
            shortHelp = "Leave event.";
            argHelp = "<name>";
            longHelp = "Leaves upcoming event " + argHelp;
            argumentNumber = 1;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                DiscordEvents.Event eve = DiscordEvents.FindEvent (arguments[0]);

                if (eve != null) {
                    eve.eventMemberIDs.Remove (e.Author.Id);
                    DiscordEvents.SaveEvents ();
                    Program.messageControl.SendMessage (e, "Succesfully left event **" + eve.eventName + "**. You will most certainly not be missed.", false);
                } else {
                    Program.messageControl.SendMessage (e, "Failed to leave event - event by name **" + arguments[0] + "** could not be found.", false);
                }
            }
            return Task.CompletedTask;
        }
    }

    public class CEventList : Command {
        public CEventList () {
            command = "list";
            shortHelp = "List events.";
            longHelp = "List all upcoming events.";
            argumentNumber = 0;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                string combinedEvents = "Upcoming events are: ```";
                if (DiscordEvents.upcomingEvents.Count != 0) {
                    foreach (DiscordEvents.Event eve in DiscordEvents.upcomingEvents) {
                        combinedEvents += "\n" + eve.eventName + " - " + eve.eventTime + " - " + eve.eventDescription;
                    }
                }else {
                    combinedEvents += "Nothing, add something! :D";
                }
                
                combinedEvents += "```";
                Program.messageControl.SendMessage (e, combinedEvents, false);
            }
            return Task.CompletedTask;
        }
    }

    public class CEventMembers : Command {
        public CEventMembers () {
            command = "members";
            shortHelp = "List members.";
            argHelp = "<name>";
            longHelp = "List all members in event <name>.";
            argumentNumber = 1;
        }

        public override Task ExecuteCommand ( SocketUserMessage e, List<string> arguments ) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                DiscordEvents.Event locEvent = DiscordEvents.FindEvent (arguments[0]);
                if (locEvent != null) {

                    string combinedMembers = "Event members are: ```";
                    if (DiscordEvents.upcomingEvents.Count != 0) {
                        foreach (ulong user in locEvent.eventMemberIDs) {
                            combinedMembers += "\n" + Utility.GetUserName (Utility.GetServer ().GetUser (user));
                        }
                    } else {
                        combinedMembers += "Nobody, why don't you join? :D";
                    }

                    combinedMembers += "```";
                    Program.messageControl.SendMessage (e, combinedMembers, false);
                }else {
                    Program.messageControl.SendMessage (e, "Failed to show event member list - event **" + arguments[0] + "** not found.", false);
                }
            }
            return Task.CompletedTask;
        }
    }
}
