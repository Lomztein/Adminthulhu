using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Adminthulhu
{
    public class CCreateBook : Command
    {
        public CCreateBook() {
            command = "createbook";
            shortHelp = "Create a book message.";
            longHelp = "Construct a message consisting of multiple pages.";
            argumentNumber = 0;
            catagory = Catagory.Utility;
        }

        public override async Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                List<object> results = await MessageControl.CreateQuestionnaire (e.Author.Id, e.Channel, new MessageControl.QE ("Page #", typeof (string [])));
                if (results.Count > 0) {
                    object [ ] obj = results [ 0 ] as object [ ];
                    string [ ] str = new string [obj.Length];
                    for (int i = 0; i < obj.Length; i++) {
                        str [ i ] = obj [ i ].ToString ();
                    }
                    Program.messageControl.SendBookMessage (e.Channel, str, true);
                }
            }
        }
    }
}
