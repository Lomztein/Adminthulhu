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
            requiredPermission = Permissions.Type.UseAdvancedCommands;

            commandsInSet = new Command [ ] {
                new GetVariable (),
            };
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
