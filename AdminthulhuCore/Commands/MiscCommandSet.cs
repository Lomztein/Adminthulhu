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
                new GetVariable (), new Log (),
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

        public class Log : Command {
            public Log() {
                command = "log";
                shortHelp = "Log something.";
                isAdminOnly = true;
                AddOverload (typeof (void), "Logs something to the bot logs.");
            }

            public Task<Result> Execute(SocketUserMessage e, string text) {
                Logging.Log (Logging.LogType.BOT, text);
                return TaskResult (null, "Log has been logged to the log.");
            }

            public Task<Result> Execute(SocketUserMessage e, int type, string text) {
                Logging.LogType logType = (Logging.LogType)type;
                Logging.Log (logType, text);
                return TaskResult (null, "Log has been logged to the log as type " + logType.ToString () + ".");
            }
        }
    }
}
