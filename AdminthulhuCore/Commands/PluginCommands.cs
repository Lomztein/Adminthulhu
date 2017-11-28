using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using Discord.WebSocket;
using Discord;

namespace Adminthulhu
{
    public static class PluginCommands
    {
        public class Plugin {
            public PluginCommand[] pluginCommands;
            public PluginSet [ ] pluginSets;
        }

        public class PluginCommand : Command {

            public string set;

            public string chain;
            public string returnTypeName;
            public Parameter [ ] parameters;

            public async Task<Result> Execute(SocketUserMessage e, params string[] arguments) {
                for (int i = 0; i < arguments.Length; i++) {
                    CommandVariables.Set (e.Id, "arg" + i, arguments [ i ], true);
                }

                var result = await Program.FindAndExecuteCommand (e, chain, Program.commands, 0, true, true);
                if (result != null)
                    return result.result;
                return new Result ("", null);
            }

            public override (Type [ ] types, string [ ] names, string returnType) GetDescriptiveOverloadParameters(int overloadIndex) {
                List<Type> types = new List<Type> ();
                List<string> names = new List<string> ();

                for (int i = 0; i < parameters.Length; i++) {
                    types.Add (Type.GetType (parameters [ i ].typeName));
                    names.Add (parameters [ i ].paramName);
                }

                return (types.ToArray (), names.ToArray (), returnTypeName);
            }

            public class Parameter {
                public string typeName;
                public string paramName;
                public bool allowParams;
            }
        }

        public class PluginSet : CommandSet {

            public string set;
            public PluginCommand pluginCommandsInSet;

            public PluginSet(string _set) {
                set = _set;
            }
        }
    }
}
