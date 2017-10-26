using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Adminthulhu
{

    public class VariableCommandSet : CommandSet {
        public VariableCommandSet() {
            command = "var";
            shortHelp = "Set relating to variables";
            catagory = Category.Advanced;
            requiredPermission = Permissions.Type.UseAdvancedCommands;

            commandsInSet = new Command [ ] {
                new SetL (), new SetP (), new SetG (),
                new GetL (), new GetP (), new GetG (),
                new DelL (), new DelP (), new DelG (),
                new ArraySet  (),
            };
        }

        public class ArraySet : CommandSet {
            public ArraySet() {
                command = "array";
                shortHelp = "Set for manipulating arrays.";

                commandsInSet = new Command [ ] {
                    new Create (), new Add (), new Remove (), new Count (), new IndexOf (), new AtIndex (),
                };
            }

            public class Create : Command {
                public Create() {
                    command = "create";
                    shortHelp = "Create an array.";

                    AddOverload (typeof (object), "Create an array containing the given objects.");
                }

                public Task<Result> Execute(SocketUserMessage e, params object [ ] toAdd) {
                    object [ ] array = new object [ toAdd.Length ];
                    for (int i = 0; i < toAdd.Length; i++) {
                        array [ i ] = toAdd [ i ];
                    }
                    return TaskResult (array, "");
                }
            }

            public class Add : Command {
                public Add() {
                    command = "add";
                    shortHelp = "Add to an array.";
                    AddOverload (typeof (object), "Add the given objects to a given array and returns it.");
                }

                public Task<Result> Execute(SocketUserMessage e, object [ ] array, params object [ ] toAdd) {
                    List<object> arr = array.ToList ();
                    foreach (object obj in toAdd) {
                        arr.Add (obj);
                    }
                    return TaskResult (arr.ToArray (), "");
                }
            }

            public class Remove : Command {
                public Remove() {
                    command = "remove";
                    shortHelp = "Remove from an array.";

                    AddOverload (typeof (object), "Removes the given object from an array.");
                }

                public Task<Result> Execute(SocketUserMessage e, object [ ] array, params object [ ] toRemove) {
                    List<object> arr = array.ToList ();
                    foreach (object obj in toRemove) {
                        arr.Remove (obj);
                    }
                    return TaskResult (arr.ToArray (), "");
                }
            }

            public class Count : Command {
                public Count() {
                    command = "count";
                    shortHelp = "Count elements in an array.";

                    AddOverload (typeof (int), "Returns the amount of objects that are in an array.");
                }

                public Task<Result> Execute(SocketUserMessage e, object [ ] array) {
                    return TaskResult (array.Length, $"Array count: {array.Length}");
                }
            }


            public class AtIndex : Command {
                public AtIndex() {
                    command = "atindex";
                    shortHelp = "Get object at index from array.";
                    AddOverload (typeof (object), "Get an object from an array at the given index.");
                    catagory = Category.Advanced;
                }

                public Task<Result> Execute(SocketUserMessage e, int index, params object [ ] array) {
                    if (array != null && array.Length > 0 && (index < 0 || index >= array.Length)) {
                        return TaskResult (null, "Error - Index null or out of range.");
                    } else {
                        return TaskResult (array [ index ], array [ index ].ToString ());
                    }
                }
            }

            public class IndexOf : Command {
                public IndexOf() {
                    command = "indexof";
                    shortHelp = "Get the index of object array.";
                    AddOverload (typeof (object), "Get the index of a given object within a given array. Returns -1 if not there.");
                    catagory = Category.Advanced;
                }

                public Task<Result> Execute(SocketUserMessage e, object obj, params object [ ] array) {
                    return TaskResult (array.ToList ().IndexOf (obj), "");
                }
            }
        }

        public class SetL : Command {
            public SetL() {
                command = "setl";
                shortHelp = "Set local variable.";

                AddOverload (typeof (object), "Set a variable in the local scope, only accessable from current command chain.");
            }

            public Task<Result> Execute(SocketUserMessage e, string name, object variable) {
                try {
                    CommandVariables.Set (e.Id, name, variable, false);
                } catch (ArgumentException exc) {
                    return TaskResult (exc.Message, exc.Message);
                }
                return TaskResult (null, "");
            }
        }

        public class SetP : Command {
            public SetP() {
                command = "setp";
                shortHelp = "Set personal variable.";

                AddOverload (typeof (object), "Set a variable in the personal scope, only accessable for current user.");
            }

            public Task<Result> Execute(SocketUserMessage e, string name, object variable) {
                try {
                    CommandVariables.Set (e.Author.Id, name, variable, false);
                } catch (ArgumentException exc) {
                    return TaskResult (exc.Message, exc.Message);
                }
                return TaskResult (null, "");
            }
        }

        public class SetG : Command {
            public SetG() {
                command = "setg";
                shortHelp = "Set global variable.";

                AddOverload (typeof (object), "Set a variable in the global scope, accessable for the entire Discord server.");
                isAdminOnly = true;
            }

            public Task<Result> Execute(SocketUserMessage e, string name, object variable) {
                try {
                    CommandVariables.Set (0, name, variable, false);
                } catch (ArgumentException exc) {
                    return TaskResult (exc.Message, exc.Message);
                }
                return TaskResult (null, "");
            }
        }

        public class GetL : Command {
            public GetL() {
                command = "getl";
                shortHelp = "Get local variable.";

                AddOverload (typeof (object), "Get a variable in the local scope.");
            }

            public Task<Result> Execute(SocketUserMessage e, string name) {
                 return TaskResult (CommandVariables.Get (e.Id, name), "");
            }
        }

        public class GetP : Command {
            public GetP() {
                command = "getp";
                shortHelp = "Get personal variable.";

                AddOverload (typeof (object), "Get a variable in the personal scope.");
            }

            public Task<Result> Execute(SocketUserMessage e, string name) {
                return TaskResult (CommandVariables.Get (e.Author.Id, name), "");
            }
        }

        public class GetG : Command {
            public GetG() {
                command = "getg";
                shortHelp = "Get global variable.";

                AddOverload (typeof (object), "Get a variable in the global scope.");
            }

            public Task<Result> Execute(SocketUserMessage e, string name) {
                return TaskResult (CommandVariables.Get (0, name), "");
            }
        }

        public class DelL : Command {
            public DelL() {
                command = "dell";
                shortHelp = "Delete local variable.";
                AddOverload (typeof (bool), "Delete a variable in the local scope.");
            }

            public Task<Result> Execute(SocketUserMessage e, string name) {
                return TaskResult (CommandVariables.Delete (e.Id, name), "");
            }
        }

        public class DelP : Command {
            public DelP() {
                command = "delp";
                shortHelp = "Delete personal variable.";
                AddOverload (typeof (bool), "Delete a variable in the personal scope.");
            }

            public Task<Result> Execute(SocketUserMessage e, string name) {
                return TaskResult (CommandVariables.Delete (e.Author.Id, name), "");
            }
        }

        public class DelG : Command {
            public DelG() {
                command = "delg";
                shortHelp = "Delete global variable.";
                AddOverload (typeof (bool), "Delete a variable in the global scope.");

                isAdminOnly = true;
            }

            public Task<Result> Execute(SocketUserMessage e, string name) {
                return TaskResult (CommandVariables.Delete (e.Author.Id, name), "");
            }
        }
    }

    public static class CommandVariables {

        public static Dictionary<ulong, Dictionary<string, object>> variables = new Dictionary<ulong, Dictionary<string, object>> (); // Nested dictionaries at compile time? That ain't confusing.
        public static string [ ] reservedNames = new string [ ] {
            "arg", "for",
        };

        public static object Get(ulong ID, string name) {
            if (variables.ContainsKey (ID)) {
                if (variables [ ID ].ContainsKey (name)) {
                    return variables [ ID ] [ name ];
                }
            }
            return null;
        }

        public static void Set(ulong ID, string name, object obj, bool allowReserved) {
            SoftStringComparer softie = new SoftStringComparer ();
            if (!allowReserved && reservedNames.Any (x => softie.Equals (x, name))) {
                throw new ArgumentException ($"Cannot use name {name}, as it is reserved.");
            }

            if (!variables.ContainsKey (ID)) {
                variables.Add (ID, new Dictionary<string, object> ());
            }
            if (variables [ ID ].ContainsKey (name)) {
                variables [ ID ] [ name ] = obj;
            } else {
                variables [ ID ].Add (name, obj);
            }
        }

        public static bool Delete(ulong ID, string name) {
            if (variables.ContainsKey (ID)) {
                if (variables [ ID ].ContainsKey (name)) {
                    variables [ ID ].Remove (name);
                    return true;
                }
            }
            return false;
        }

        public static bool Clear(ulong ID) {
            if (variables.ContainsKey (ID)) {
                variables.Remove (ID);
                return true;
            }
            return false;
        }
    }
}
