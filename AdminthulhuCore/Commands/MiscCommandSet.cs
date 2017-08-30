using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Adminthulhu
{
    public class MiscCommandSet : CommandSet
    {
        public MiscCommandSet() {
            command = "misc";
            shortHelp = "Miscellaneous advanced commands.";
            catagory = Category.Advanced;

            commandsInSet = new Command [ ] {
                new AtIndex (), new IndexOf (), new GetVariable (),
            };
        }

        public class AtIndex : Command {
            public AtIndex() {
                command = "atindex";
                shortHelp = "Get object at index from array.";
                AddOverload (typeof (object), "Get an object from an array at the given index.");
                catagory = Category.Advanced;
            }

            public Task<Result> Execute(SocketUserMessage e, object [ ] array, int index) {
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

            public Task<Result> Execute(SocketUserMessage e, object [ ] array, object obj) {
                return TaskResult (array.ToList ().IndexOf (obj), "");
            }
        }

        public class GetVariable : Command {
            public GetVariable() {
                command = "getvariable";
                shortHelp = "Get a variable.";
                AddOverload (typeof (object), "Get the given variable from the given object, if it exists.");
                catagory = Category.Advanced;
            }

            public Task<Result> Execute(SocketUserMessage e, object obj, string name) {
                try {
                return TaskResult (Utility.GetVariable (obj, name), "");
                } catch (Exception exc) {
                    return TaskResult (exc.Message, "");
                }
            }
        }
    }
}
