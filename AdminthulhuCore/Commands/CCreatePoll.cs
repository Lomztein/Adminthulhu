using Discord;
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
            catagory = Category.Utility;

            AddOverload (typeof (MessageControl.Poll.PollOption), "Constructs a poll of various options where people can answer.");
        }

        public async Task<Result> Execute(SocketUserMessage e) {
            List<Object> results = await MessageControl.CreateQuestionnaire (e.Author.Id, e.Channel,
                new MessageControl.QE ("Give poll name.", typeof (string)),
                new MessageControl.QE ("Give poll end date.", typeof (DateTime)),
                new MessageControl.QE ("Give poll max votes per person.", typeof (int)),
                new MessageControl.QE ("Give poll option #", typeof (string [ ])));

            object [ ] obj = results [ 3 ] as object [ ];
            string [ ] options = new string [ obj.Length ];
            for (int i = 0; i < obj.Length; i++) {
                options [ i ] = obj [ i ] as string;
            }

            try {
                MessageControl.Poll poll = new MessageControl.Poll ((string)results [ 0 ], e.Channel.Id, 0, DateTime.Parse (results [ 1 ].ToString ()), (int)results [ 2 ], null, options);
                IMessage message = await MessageControl.CreatePoll (poll);
                await poll.AwaitEnd ();
                return new Result(poll.winner, poll.winner.name);
            } catch (Exception exc) {
                Logging.DebugLog (Logging.LogType.EXCEPTION, exc.Message + " - " + exc.StackTrace);
                return new Result(null, "");
            }
        }
    }
}
