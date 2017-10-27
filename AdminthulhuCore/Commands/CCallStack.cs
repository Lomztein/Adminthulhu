using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Adminthulhu
{
    public class CCallStack : Command
    {
        public CCallStack() {
            command = "callstack";
            catagory = Category.Advanced;
            shortHelp = "View command chain callstack.";
            AddOverload (typeof (string), "View the latest executed callstack.");
            AddOverload (typeof (string), "View the callstack for the command given by the command message ID");
        }

        public Task<Result> Execute(SocketUserMessage e) {
            return Execute (e, callstacks[0].chainID);
        }

        public Task<Result> Execute(SocketUserMessage e, ulong id) {
            Callstack callstack = callstacks.Find (x => x.chainID == id);
            if (callstack != null) {
                string message = "```";
                foreach (Callstack.Item item in callstack.items) {
                    string arguments = " ";
                    for (int i = 0; i < item.arguments.Count; i++) {
                        arguments += item.arguments[i] + (i == item.arguments.Count - 1 ? "" : "; ");
                    }

                    message += Utility.UniformStrings (item.command.helpPrefix + item.command.command + arguments, item.returnObj == null ? "null" : item.returnObj.ToString (), " -> ", 50) + "\n";
                }
                message += "```";
                return TaskResult (message, message);
            }
            return TaskResult (null, "Error - No callstack found for given ID.");
        }

        /*public async Task<Result> Execute(SocketUserMessage e, string cmd) {
            if (cmd.Length > 2 && cmd [ 0 ] == ('(') && cmd[1].IsTrigger ()) {
                string newCmd; // Well thats not confusing.
                List<string> args = Utility.ConstructArguments (GetParenthesesArgs (cmd), out newCmd);
                Program.FoundCommandResult result = await Program.FindAndExecuteCommand (e, newCmd.Substring (1), args, Program.commands, 0, false);
                return Execute (e, e.Id).Result;
            }
            return new Result (null, "Error - Input must be a command wrapped in parenthesis.");
        }*/
    }
}
