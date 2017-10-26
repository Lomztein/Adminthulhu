using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Adminthulhu {
    public class CRollTheDice : Command {

        string [ ] dices = new string [ ] {
            "[ • ]", "[ : ]", "[•••]", "[: :]", "[:•:]", "[:::]",
            "[: :•:]", "[:•:•:]", "[:•:::]", "[:::::]",
            "[: :::•:]","[:•:::•:]", "[:•:::::]", "[:::::::]",
            "[: :::::•:]", "[:•:::::•:]", "[:•:::::::]", "[:::::::::]",
            "[: :::::::•:]", "[:•:::::::•:]", "[:•:::::::::]", "[:::::::::::]"
        };

        public CRollTheDice () {
            command = "rtd";
            shortHelp = "Roll the dice.";
            catagory = Category.Utility;
            AddOverload (typeof (int), "Rolls a die that returns a number between one and the given max number.");
            AddOverload (typeof (int), "Rolls a an amount of n-sided dice.");
        }

        public Task<Result> Execute(SocketUserMessage e, int maxnumber) {
            Random random = new Random ();
            int number = random.Next (0, maxnumber) + 1;
            return TaskResult (number, $"You rolled {number}!");
        }

        public Task<Result> Execute (SocketUserMessage e, int sides, int dice) {
            if (sides > dices.Length) {
                return TaskResult (0, "Sorry, but the available dices doesn't go beyond " + dices.Length + ".");
            }
            string message = string.Empty;
            Random random = new Random ();

            int [ ] results = new int [ dice ];
            for (int i = 0; i < dice; i++) {
                int res = random.Next (0, sides);
                message += dices [ res ];
                results [ i ] = res + 1;
            }

            return TaskResult (results.Sum (), $"{message} - a sum of {results.Sum ()}");
        }
    }
}
