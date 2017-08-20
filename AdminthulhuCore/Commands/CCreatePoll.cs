using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Adminthulhu
{
    public class CCreatePoll : Command
    {
        public CCreatePoll() {
            command = "createpoll";
            shortHelp = "Create a poll.";
            longHelp = "Constructs a poll of various options where people can answer.";
            argumentNumber = 0;
            catagory = Catagory.Utility;
        }

        public override async Task ExecuteCommand(SocketUserMessage e, List<string> arguments) {
            base.ExecuteCommand (e, arguments);
            if (AllowExecution (e, arguments)) {
                List<Object> results = await MessageControl.CreateQuestionnaire (e.Author.Id, e.Channel,
                    new MessageControl.QE ("Give poll name.", typeof (string)),
                    new MessageControl.QE ("Give poll end date.", typeof (DateTime)),
                    new MessageControl.QE ("Give poll max votes per person.", typeof (int)),
                    new MessageControl.QE ("Give poll option #", typeof (string [ ])));

                object [ ] obj = results [ 3 ] as object [ ];
                string [ ] options = new string [obj.Length ];
                for (int i = 0; i < obj.Length; i++) {
                    options [ i ] = obj [ i ] as string;
                }

                try {
                await MessageControl.CreatePoll (new MessageControl.Poll ((string)results[0], e.Channel.Id, 0, DateTime.Parse (results [ 1 ].ToString ()), (int)results [ 2 ], null, options));
                } catch (Exception exc) {
                    Logging.DebugLog (Logging.LogType.EXCEPTION, exc.Message + " - " + exc.StackTrace);
                }
            }
        }
    }
}
