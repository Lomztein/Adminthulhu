using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Rest;

namespace Adminthulhu
{
    public class AutoCommands
    {
        public enum Event {
            Null = -1, JoinedVoice, LeftVoice, EventStarted, EventEnded, MessageRecieved, MessageDeleted, MessageEdited, UserJoined, UserLeft, UserBirthday, HourPassed, DayPassed, UserStricken, UserStrikeRaised, Count
        }
        public static string [ ] args = new string [ ] {
            "User ID, Voice channel ID", "User ID, Voice channel ID", "Event name", "Event name", "Message content, Channel ID", "Channel ID", "New message content, Channel ID", "User ID", "User ID", "User ID", "Time", "Time", "User ID, Time", "User ID"
        };

        public static Dictionary<Event, AutocEvent> autocEvents = new Dictionary<Event, AutocEvent> ();
        public static string saveDataPath;

        public static void Initialize() {
            saveDataPath = Program.dataPath + "autocommands" + Program.gitHubIgnoreType;
            LoadData ();

            for (int i = 0; i < (int)Event.Count; i++) {
                AddEventIfNonExisting ((Event)i);
            }
            SaveData ();
        }

        private static void AddEventIfNonExisting (Event eve) {
            if (!autocEvents.ContainsKey (eve))
                autocEvents.Add (eve, new AutocEvent ());
            else if (autocEvents [ eve ].autocs == null)
                autocEvents [ eve ] = new AutocEvent ();
        }

        public static void RunEvent(Event eve, params string[] arguments) {
            autocEvents [ eve ].Run (arguments);
        }

        public static AutocEvent GetEvent(Event eve) {
            return autocEvents [ eve ];
        }

        public static bool AddChain(Event eve, Autoc autoc) {
            if (Utility.GetServer ().GetTextChannel (autoc.executingChannelID) != null) {
                autocEvents [ eve ].autocs.Add (autoc);
                SaveData ();
                return true;
            }
            return false;
        }

        public static bool RemoveChain(Event eve, string name) {
            SoftStringComparer softie = new SoftStringComparer ();
            IEnumerable<Autoc> toRemove = autocEvents [ eve ].autocs.Where (x => softie.Equals (name, x.name));
            if (toRemove.Count () > 0) {
                autocEvents [ eve ].autocs.RemoveAll (x => toRemove.Contains (x));
                SaveData ();
                return true;
            }
            return false;
        }

        private static void LoadData() {
            autocEvents = SerializationIO.LoadObjectFromFile<Dictionary<Event, AutocEvent>> (saveDataPath);
            if (autocEvents == null)
                autocEvents = new Dictionary<Event, AutocEvent> ();
        }

        private static void SaveData() {
            SerializationIO.SaveObjectToFile (saveDataPath, autocEvents, true, false);
        }

        public static Event StringToEvent(string input) {
            SoftStringComparer softie = new SoftStringComparer ();
            for (int i = 0; i < (int)Event.Count; i++) {
                if (softie.Equals (((Event)i).ToString (), input))
                    return (Event)i;
            }
            return Event.Null;
        }

        public class Autoc { // Short for automatic command :D
            public string name;
            public ulong executingChannelID;
            public string commandChain;

            public Autoc(string _name, ulong id, string command) {
                name = _name;
                executingChannelID = id;
                commandChain = command;
            }
        }

        public class AutocEvent {

            public List<Autoc> autocs = new List<Autoc> ();

            public async void Run(params string[] arguments) {

                foreach (Autoc a in autocs) {
                    SocketTextChannel channel = Utility.GetServer ().GetTextChannel (a.executingChannelID);
                    string cmd = a.commandChain;
                    for (int i = 0; i < arguments.Length; i++) {
                        cmd = cmd.Replace ("{arg" + i + "}", arguments [ i ]);
                    }
                    RestUserMessage message = await Program.messageControl.AsyncSend (channel, cmd, true);
                }
            }

        }
    }

    public class AutocCommandSet : CommandSet {

        public AutocCommandSet() {
            command = "autoc";
            shortHelp = "Automatically executed commands.";
            isAdminOnly = true;
            catagory = Category.Advanced;

            commandsInSet = new Command [ ] {

            };
        }

        public override void Initialize() {
            base.Initialize ();

            List<Add.Base> adds = new List<Add.Base> ();
            List<Remove.Base> removes = new List<Remove.Base> ();
            List<List.Base> lists = new List<List.Base> ();

            for (int i = 0; i < (int)AutoCommands.Event.Count; i++) {
                AutoCommands.Event eve = ((AutoCommands.Event)i);
                string name = eve.ToString ().ToLower ();
                adds.Add (new Add.Base (name,eve));
                removes.Add (new Remove.Base (name,eve));
                lists.Add (new List.Base (name, eve));
            }
            Add addSet = new Add ();
            Remove removeSet = new Remove ();
            List listSet = new List ();

            AddProceduralCommands (addSet);
            AddProceduralCommands (removeSet);
            AddProceduralCommands (listSet);

            addSet.AddProceduralCommands (adds.ToArray ());
            removeSet.AddProceduralCommands (removes.ToArray ());
            listSet.AddProceduralCommands (lists.ToArray ());
            // This could all be done in a loop I guess.
        }

        public class Add : CommandSet {

            public Add() {
                command = "add";
                shortHelp = "Add a chain to an autoc event.";
            }


            public class Base : Command {

                AutoCommands.Event eve;

                public Base(string name, AutoCommands.Event _eve) {
                    command = name;
                    shortHelp = "Add a chain to " + name;
                    eve = _eve;

                    AddOverload (typeof (AutoCommands.Autoc), "Autoc event arguments: " + AutoCommands.args[(int)eve]);
                    //AddOverload (typeof (AutoCommands.Autoc), "Add an already existing command chain to the " + name + " autoc event.");
                }

                public Task<Result> Execute(SocketUserMessage e, string name, ulong executeChannelID, string commandChain) {
                    if (commandChain.Length > 1 && commandChain [ 1 ].IsTrigger ()) {
                        AutoCommands.Autoc autoc = new AutoCommands.Autoc (name, executeChannelID, commandChain.Substring (1, commandChain.Length - 2));
                        if (AutoCommands.AddChain (eve, autoc)) {
                            return TaskResult (autoc, "Succesfully added new chain to autoc event.");
                        } else {
                            return TaskResult (null, "Failed to add new chain to autoc event - channel not existing.");
                        }
                    }
                    return TaskResult (null, "Failed to add new chain to autoc event - Command chain invalid.");
                }
            }
        }

        public class Remove : CommandSet {

            public Remove() {
                command = "remove";
                shortHelp = "Remove a chain from an autoc event.";
            }

            public class Base : Command {

                public AutoCommands.Event eve;

                public Base(string name, AutoCommands.Event _eve) {
                    command = name;
                    shortHelp = "Remove chain from " + name;
                    eve = _eve;

                    AddOverload (typeof (bool), "Remove a chain command from the " + name + " autoc event, given by name.");
                }

                public Task<Result> Execute(SocketUserMessage e, string name) {
                    if (AutoCommands.RemoveChain (eve, name)) {
                        return TaskResult (true, "Succesfully removed chain **" + name + " from " + eve.ToString () + ".");
                    } else {
                        return TaskResult (false, "Failed to remove chain from event - Chain **" + name + "** not found in **" + eve.ToString () + "**.");
                    }
                }
            }
        }

        public class List : CommandSet {
            public List() {
                command = "list";
                shortHelp = "List all chains in an autoc event.";
            }

            public class Base : Command {

                public AutoCommands.Event eve;

                public Base(string name, AutoCommands.Event _eve) {
                    command = name;
                    shortHelp = "List chains in " + name;
                    eve = _eve;

                    AddOverload (typeof (string), "Lists all chains in the " + name + " autoc event.");
                }

                public Task<Result> Execute(SocketUserMessage e) {
                    string message = "```";
                    foreach (AutoCommands.Autoc autoc in AutoCommands.GetEvent (eve).autocs) {
                        SocketTextChannel channel = Utility.GetServer ().GetTextChannel (autoc.executingChannelID);
                        message = Utility.UniformStrings (autoc.name + ", " + channel.Name, autoc.commandChain, " -> ");
                    }
                    message += "```";
                    return TaskResult (message, message);
                }
            }
        }
    }
}