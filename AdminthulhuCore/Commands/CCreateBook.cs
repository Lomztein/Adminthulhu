using Discord.Rest;
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
            catagory = Category.Utility;
            AddOverload (typeof (RestUserMessage), "Construct a message consisting of multiple pages.");
        }

        public async Task<Result> Execute(SocketUserMessage e) {
            List<object> results = await MessageControl.CreateQuestionnaire (e.Author.Id, e.Channel, new MessageControl.QE ("Page #", typeof (string [ ])));
            if (results.Count > 0) {
                object [ ] obj = results [ 0 ] as object [ ];
                string [ ] str = new string [ obj.Length ];
                for (int i = 0; i < obj.Length; i++) {
                    str [ i ] = obj [ i ].ToString ();
                }
                RestUserMessage message = await Program.messageControl.SendBookMessage (e.Channel, "", str, true);
                return new Result (message, "");
            }
            return new Result(null, "");
        }
    }
}
