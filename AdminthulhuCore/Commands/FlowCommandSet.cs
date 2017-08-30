using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Adminthulhu
{
    public class FlowCommandSet : CommandSet
    {
        public FlowCommandSet() {
            command = "flow";
            shortHelp = "Commands controlling chain flow.";
            catagory = Category.Advanced;

            commandsInSet = new Command [ ] {
                new IsNull (), new If (), new Not (), new And (), new Or (), new For (), new Wait (),
            };
        }

        public class IsNull : Command {
            public IsNull() {
                command = "isnull";
                shortHelp = "Converts objects to booleans.";
                AddOverload (typeof (bool), "Returns true if the given object isn't null, false otherwise.");
            }

            public Task<Result> Execute(SocketUserMessage e, object obj) {
                return TaskResult (obj != null, "Object = null: " + (obj != null));
            }
        }

        public class If : Command {
            public If() {
                command = "if";
                shortHelp = "Control command flow.";
                AddOverload (typeof (object), "Returns the the given object if input boolean is true.");
                AddOverload (typeof (object), "Returns the first given object if input boolean is true, otherwise the second.");
            }

            public Task<Result> Execute(SocketUserMessage e, bool boolean, object obj) {
                return TaskResult (boolean ? obj : null, "");
            }

            public Task<Result> Execute(SocketUserMessage e, bool boolean, object obj1, object obj2) {
                return TaskResult (boolean ? obj1 : obj2, "");
            }
        }

        public class Not : Command {
            public Not() {
                command = "not";
                shortHelp = "Inverses booleans.";

                AddOverload (typeof (bool), "Inverses a single boolean object.");
                AddOverload (typeof (bool), "Inverses a list of boolean object indivdually.");
            }

            public Task<Result> Execute(SocketUserMessage e, bool boolean) {
                return TaskResult (!boolean, $"Not {boolean} = {!boolean}");
            }

            public Task<Result> Execute(SocketUserMessage e, bool[] booleans) {
                for (int i = 0; i < booleans.Length; i++) {
                    booleans[i] = !booleans[i];
                }
                return TaskResult (booleans, "");
            }
        }

        public class And : Command {
            public And() {
                command = "and";
                shortHelp = "Logic gate AND.";

                AddOverload (typeof (bool), "Compares two given booleans.");
                AddOverload (typeof (bool), "Compares an entire array of booleans.");
            }

            public Task<Result> Execute(SocketUserMessage e, bool bool1, bool bool2) {
                return TaskResult (bool1 && bool2, $"{bool1} AND {bool2} = {bool1 && bool2}");
            }

            public Task<Result> Execute(SocketUserMessage e, bool [ ] booleans) {
                return TaskResult (booleans.All (x => x), "");
            }
        }

        public class Or : Command {
            public Or() {
                command = "or";
                shortHelp = "Logic gate OR.";

                AddOverload (typeof (bool), "Compares two given booleans.");
                AddOverload (typeof (bool), "Compares an entire array of booleans.");
            }

            public Task<Result> Execute(SocketUserMessage e, bool bool1, bool bool2) {
                return TaskResult (bool1 || bool2, $"{bool1} OR {bool2} = {bool1 || bool2}");
            }

            public Task<Result> Execute(SocketUserMessage e, bool [ ] booleans) {
                return TaskResult (booleans.Any (x => x), "");
            }
        }

        public class For : Command {
            public For() {
                command = "for";
                shortHelp = "Loop given command a set times.";

                AddOverload (typeof (object), "Loop given command the given amount of times.");
            }

            public async Task<Result> Execute(SocketUserMessage e, int amount, string command) {
                if (command.Length > 1 && command [ 1 ].IsTrigger ()) {
                    string cmd;
                    List<string> args = Utility.ConstructArguments (GetParenthesesArgs (command), out cmd);
                    for (int i = 0; i < amount; i++) {
                        await Program.FindAndExecuteCommand (e, cmd.Substring (1), args, Program.commands);
                    }
                }
                return new Result (null, "");
            }
        }

        public class Wait : Command {
            public Wait() {
                command = "wait";
                shortHelp = "Halts command for a while.";

                AddOverload(typeof (object), "Wait the given amount of seconds, then return the given command.");
                AddOverload (typeof (object), "Wait the given amount of seconds, then return the given object.");
            }

            public async Task<Result> Execute(SocketUserMessage e, double seconds, string command) {
                if (command.Length > 1 && command [ 1 ].IsTrigger ()) {
                    return new Result (command.Substring (1, command.Length - 2), "");
                }
                return new Result (null, "");
            }

            public async Task<Result> Execute(SocketUserMessage e, double seconds, object obj) {
                await Task.Delay ((int)Math.Round (seconds * 1000));
                return new Result (obj, "");
            }
        }
    }
}
