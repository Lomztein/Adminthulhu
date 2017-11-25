using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Discord;
using Discord.WebSocket;
using System.Reflection;

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
            percentageForEventActive = BotConfiguration.GetSetting ("Events.PersentageForEventActive", this, percentageForEventActive);
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
                    if (now > e.time) {
                        StartEvent (e);
                    }
                }
            }

            foreach (Event e in ongoingEvents) {
                if (e.time.Add (e.duration) < DateTime.Now && e.eventState != Event.EventState.Ended) {
                    e.eventState = Event.EventState.Ended;
                    AutoCommands.RunEvent (AutoCommands.Event.EventEnded, e.name);
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
            Logging.Log (Logging.LogType.BOT, "Starting event: " + startingEvent.name + "!");
            startingEvent.eventState = Event.EventState.InProgress;
            AutoCommands.RunEvent (AutoCommands.Event.EventStarted, startingEvent.name);

            if (startingEvent.eventMemberIDs.Count != 0) {
                string mentions = "";
                foreach (ulong id in startingEvent.eventMemberIDs) {
                    SocketGuildUser user = Utility.GetServer ().GetUser (id);
                    if (user != null) {
                       mentions += ", " + user.Mention;
                    }
                }

                if (startingEvent.eventMemberIDs.Count > 0) {
                    string members;
                    Embed eventEmbed = ConstructEmbed (startingEvent, out members);
                    Program.messageControl.SendEmbed (Utility.GetMainChannel () as SocketTextChannel, eventEmbed,  members);
                    Voice.CreateTemporaryChannel (startingEvent.name, startingEvent.duration);
                    ongoingEvents.Add (startingEvent);
                } else {
                    try {

                    Program.messageControl.SendMessage (
                        Utility.GetMainChannel () as SocketTextChannel,
                        "Event **" + startingEvent.name + "** cancelled, since no one showed up. :(", true);
                    startingEvent.eventState = Event.EventState.Ended;
                    }catch (Exception e) {
                        Logging.Log (e);    
                    }
                }

                if (startingEvent.repeatTime.Ticks != 0) {
                    Event evt = CreateAndReturnEvent (startingEvent.name, startingEvent.time.Add (startingEvent.repeatTime), startingEvent.duration, startingEvent.hostID, startingEvent.iconUrl, startingEvent.description, startingEvent.repeatTime);
                    evt.eventMemberIDs = startingEvent.eventMemberIDs;
                    toAdd.Add (evt);
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
            .WithTitle ("Event announcement for " + evt.name + "!")
            .WithDescription (evt.description)
            .WithColor (CSetColor.GetUserColor (evt.hostID).Color)
            .WithTimestamp (evt.time)
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
                if (e.name.ToLower () == eventName.ToLower ())
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

        public static Event CreateEvent (string eventName, DateTime date, TimeSpan duration, ulong hostID, string iconUrl, string description, TimeSpan repeatTime) {
            Event eve = CreateAndReturnEvent (eventName, date, duration, hostID, iconUrl, description, repeatTime);
            upcomingEvents.Add (eve);
            SaveEvents ();
            return eve;
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

            [Editable] public string name;
            [Editable] public string description;
            [Editable] public DateTime time;
            [Editable] public ulong hostID;
            [Editable] public string iconUrl;
            [Editable] public TimeSpan duration;
            [Editable] public TimeSpan repeatTime;

            public enum EventState { Awaiting, InProgress, Ended }
            public EventState eventState = EventState.Awaiting;

            public Dictionary<ulong, DateTime> lastRemind;

            public List<ulong> eventMemberIDs = new List<ulong> ();

            public Event() { }

            public Event(string name, DateTime time, TimeSpan _duration, ulong _hostID, string _iconUrl, string description, TimeSpan _repeatTime) {
                this.name = name;
                this.time = time;
                duration = _duration;
                hostID = _hostID;
                iconUrl = _iconUrl;
                repeatTime = _repeatTime;

                this.description = description;

                eventState = EventState.Awaiting;
            }

            [AttributeUsage (AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
            public class EditableAttribute : Attribute { }

        }
    }

    public class EventCommands : CommandSet {
        public EventCommands () {
            command = "event";
            shortHelp = "Event command set.";
            commandsInSet = new Command[] { new CCreateEvent (), new CCancelEvent (), new CJoinEvent (), new CLeaveEvent (), new CEventList (), new CEventMembers (), new CListEventGames (), new GetEvent () };
            catagory = Category.Utility;
        }

        public override void Initialize() {
            base.Initialize ();

            EditCommandSet editCommandSet = new EditCommandSet ();
            AddProceduralCommands (editCommandSet);

            Type type = typeof (DiscordEvents.Event);
            FieldInfo [ ] info = type.GetFields ();
            List<FieldInfo> editables = info.Where (x => x.GetCustomAttribute (typeof (DiscordEvents.Event.EditableAttribute)) != null).ToList ();

            List<Command> newCommands = new List<Command> ();
            Command baseCommand = new CEditEventBase ();

            foreach (FieldInfo editable in editables) {

                Command newCommand = baseCommand.CloneCommand ();
                (newCommand as CEditEventBase).editInfo = editable;
                newCommand.command = editable.Name;

                newCommands.Add (newCommand);
            }

            editCommandSet.AddProceduralCommands (newCommands.ToArray ());
            // Lets see if this shit works.. It does! A bit funky, but it works!
        }

        public class EditCommandSet : CommandSet {
            public EditCommandSet() {
                command = "edit";
                shortHelp = "Event edit command set.";
                commandsInSet = new Command [ 0 ];
                catagory = Category.Utility;
            }
        }
    }


    public class CEditEventBase : Command {
        public FieldInfo editInfo;

        public CEditEventBase() {
            command = "";
            shortHelp = "<eventname>;<newvalue>";
            overloads.Add (new Overload (typeof (DiscordEvents.Event), $"Edit variable {command} for <event> to <newvalue>"));
        }

        public async Task<Result> Execute(SocketUserMessage e, string eventName, string newValue) {
            DiscordEvents.Event eve = DiscordEvents.FindEvent (eventName);
            if (eve != null) {
                try {
                    object nValue = Convert.ChangeType (newValue, editInfo.FieldType);
                    editInfo.SetValue (eve, nValue);
                    DiscordEvents.SaveEvents ();
                    return new Result (nValue, $"Succesfully edited event {eve.name}'s variable {command} to {nValue}.");

                } catch (Exception exception) {
                    return new Result (null, "Error - " + exception.Message);
                }
            } else {
                return new Result (null, "Error - Events " + eventName + " not found.");
            }
        }
    }

    // Move this command to a seperate file later, this is just for ease of writing.
    public class CCreateEvent : Command {
        public CCreateEvent() {
            command = "create";
            shortHelp = "Create a new event.";
            requiredPermission = Permissions.Type.CreateEvents;
            overloads.Add (new Overload (typeof (DiscordEvents.Event), "Creates a new event on this server. Uses a questionnaire for input."));
            overloads.Add (new Overload (typeof (DiscordEvents.Event), "Creates a new event on this server by name and date."));
            overloads.Add (new Overload (typeof (DiscordEvents.Event), "Creates a new event on this server by name, date and description."));
            overloads.Add (new Overload (typeof (DiscordEvents.Event), "Creates a new event on this server by name, date, description and duration."));
            overloads.Add (new Overload (typeof (DiscordEvents.Event), "Creates a new event on this server by name, date, description, duration and repeat time."));
            overloads.Add (new Overload (typeof (DiscordEvents.Event), "Creates a new event on this server by name, date, description, duration, repeat time and an image url."));
        }

        public async Task<Result> Execute (SocketUserMessage e) {

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
                return await Execute (e, results [ 0 ] as string, parse, results [ 2 ] as string, results [ 3 ] as string, results [ 4 ] as string, results [ 5 ] as string);
            }
            return new Result (null, "Failed to create event - something went wrong.");
        }

        public Task<Result> Execute(SocketUserMessage e, string eventName, DateTime eventDate) {
            return Execute (e, eventName, eventDate, "");
        }

        public Task<Result> Execute(SocketUserMessage e, string eventName, DateTime eventDate, string eventDescription) {
            return Execute (e, eventName, eventDate, eventDescription, "1h");
        }

        public Task<Result> Execute(SocketUserMessage e, string eventName, DateTime eventDate, string eventDescription, string eventDuration) {
            return Execute (e, eventName, eventDate, eventDescription, eventDuration, "0h");
        }

        public Task<Result> Execute(SocketUserMessage e, string eventName, DateTime eventDate, string eventDescription, string eventDuration, string eventRepeatTime) {
            return Execute (e, eventName, eventDate, eventDescription, eventDuration, eventRepeatTime, "");
        }

        public Task<Result> Execute(SocketUserMessage e, string eventName, DateTime eventDate, string eventDescription, string eventDuration, string eventRepeatTime, string eventUrl) {

            if (Utility.TryParseSimpleTimespan (eventDuration as string, out TimeSpan duration) &&
                Utility.TryParseSimpleTimespan (eventRepeatTime, out TimeSpan repeat)) {

                DiscordEvents.Event eve = DiscordEvents.CreateEvent (eventName, eventDate, duration, e.Author.Id, eventUrl, eventDescription, repeat);
                DiscordEvents.JoinEvent (e.Author.Id, eventName);
                return TaskResult (eve, "Succesfully created event **" + eventName + "** at " + eventDate);
            } else {
                return TaskResult (null, "Failed to create event, could not parse time.");
            }
        }
    }

    public class CCancelEvent : Command {
        public CCancelEvent () {
            command = "cancel";
            shortHelp = "Cancel event.";
            isAdminOnly = true;

            overloads.Add (new Overload (typeof (DiscordEvents.Event), "Cancels event by name."));
        }

        public Task<Result> Execute(SocketUserMessage message, string eventname) {
            DiscordEvents.Event eve = DiscordEvents.FindEvent (eventname);

            if (eve == null) {
                return TaskResult (null, "Unable to cancel event - event by name **" + eventname + "** not found.");
            } else if (eve.hostID != message.Author.Id) {
                return TaskResult (null, "Unable to cancel event **" + eve.name + "**, you are not the host.");
            } else {
                DiscordEvents.upcomingEvents.Remove (eve);
                DiscordEvents.SaveEvents ();

                return TaskResult (eve, "Event **" + eve.name + "** has been cancelled.");
            }
        }
    }

    public class CJoinEvent : Command {
        public CJoinEvent () {
            command = "join";
            shortHelp = "Join event.";
            overloads.Add (new Overload (typeof (DiscordEvents.Event), "Joins event by name."));
            overloads.Add (new Overload (typeof (void), "Joins the given event."));
        }

        public Task<Result> Execute(SocketUserMessage e, string eventname) {
            DiscordEvents.Event eve = DiscordEvents.FindEvent (eventname);

            if (eve != null) {
                return Execute (e, eve);
            } else {
                return TaskResult (null, "Failed to join event - event by name **" + eventname+ "** could not be found.");
            }
        }

        public Task<Result> Execute(SocketUserMessage e, DiscordEvents.Event eve) {
            if (eve.eventMemberIDs.Contains (e.Author.Id)) {
                return TaskResult (null, "Failed to join event **" + eve.name + "** - You are already a member.");
            } else {
                eve.eventMemberIDs.Add (e.Author.Id);
                DiscordEvents.SaveEvents ();
                return TaskResult (null, "Succesfully joined event **" + eve.name + "**.");
            }
        }
    }

    public class CLeaveEvent : Command {
        public CLeaveEvent () {
            command = "leave";
            shortHelp = "Leave event.";
            overloads.Add (new Overload (typeof (bool), "Leaves upcoming event by name."));
        }

        public Task<Result> Execute(SocketUserMessage e, string eventname) {
            DiscordEvents.Event eve = DiscordEvents.FindEvent (eventname);

            if (eve != null) {
                return Execute (e, eve);
            } else {
                return TaskResult (false, "Failed to leave event - event by name **" + eve.name + "** could not be found.");
            }
        }

        public Task<Result> Execute(SocketUserMessage e, DiscordEvents.Event eve) {
            if (eve.eventMemberIDs.Contains (e.Author.Id)) {

                eve.eventMemberIDs.Remove (e.Author.Id);
                DiscordEvents.SaveEvents ();
                return TaskResult (null, "Succesfully joined event **" + eve.name + "**.");
            } else {
                return TaskResult (null, "Failed to leave event **" + eve.name + "**. You are not a member of it.");
            }
        }
    }

    public class CEventList : Command {
        public CEventList () {
            command = "list";
            shortHelp = "List events.";
            overloads.Add (new Overload (typeof (DiscordEvents.Event[]), "List all upcoming events."));
        }

        public Task<Result> Execute(SocketUserMessage e) {
            string combinedEvents = "Upcoming events are: ```";
            if (DiscordEvents.upcomingEvents.Count != 0) {
                foreach (DiscordEvents.Event eve in DiscordEvents.upcomingEvents) {
                    combinedEvents += "\n" + eve.name + " - " + eve.time + " - " + eve.description;
                }
            } else {
                combinedEvents += "Nothing, add something! :D";
            }

            combinedEvents += "```";
            return TaskResult (DiscordEvents.upcomingEvents.ToArray (), combinedEvents);
        }
    }

    public class CEventMembers : Command {
        public CEventMembers () {
            command = "members";
            shortHelp = "List members.";
            overloads.Add (new Overload (typeof (SocketGuildUser[]), "List all members in event an event by name."));
        }

        public Task<Result> Execute(SocketUserMessage e, string eventName) {
            return Execute (e, DiscordEvents.FindEvent (eventName));
        }

        public Task<Result> Execute(SocketUserMessage e, DiscordEvents.Event eve) {
            DiscordEvents.Event locEvent = eve;

            if (locEvent != null) {
                List<SocketGuildUser> users = new List<SocketGuildUser> ();

                string combinedMembers = "Event members are: ```";
                if (DiscordEvents.upcomingEvents.Count != 0) {
                    foreach (ulong user in locEvent.eventMemberIDs) {
                        SocketGuildUser found = Utility.GetServer ().GetUser (user);
                        users.Add (found);
                        combinedMembers += "\n" + Utility.GetUserName (found);
                    }
                } else {
                    combinedMembers += "Nobody, why don't you join? :D";
                }

                combinedMembers += "```";
                return TaskResult (locEvent, combinedMembers);
            } else {
                return TaskResult (null, "Failed to show event member list - event **" + locEvent.name + "** not found.");
            }
        }
    }


    public class GetEvent : Command {

        public GetEvent() {
            command = "get";
            shortHelp = "Get an event object.";
            AddOverload (typeof (DiscordEvents.Event), "Get an event object by the given name.");

            catagory = Category.Advanced;
            requiredPermission = Permissions.Type.UseAdvancedCommands;
        }

        public Task<Result> Execute(SocketUserMessage e, string eventName) {
            DiscordEvents.Event eve = DiscordEvents.FindEvent (eventName);
            return TaskResult (eve, eve.name);
        }

    }
}
