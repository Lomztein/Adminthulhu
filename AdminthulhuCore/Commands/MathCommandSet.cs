using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Adminthulhu
{
    public class MathCommandSet : CommandSet
    {
        public MathCommandSet() {
            command = "math";
            shortHelp = "Math related commands. Works with floating point numbers.";
            catagory = Category.Advanced;

            commandsInSet = new Command [ ] {
                new Add (), new Subtract (), new Multiply (), new Divide (), new Pow (), new Log (), new Mod (), new Sin (), new Cos (), new Tan (),
            };
        }

        public class Add : Command {
            public Add() {
                command = "add";
                shortHelp = "Add numbers.";

                AddOverload (typeof (double), "Add two numbers together.");
                AddOverload (typeof (double), "Get the sum of an array of numbers.");
            }

            public Task<Result> Execute(SocketUserMessage e, double num1, double num2) {
                return TaskResult (num1 + num2, $"{num1} + {num2} = {num1 + num2}");
            }

            public Task<Result> Execute(SocketUserMessage e, double[] numbers) {
                return TaskResult (numbers.Sum (), "");
            }
        }

        public class Subtract : Command {

            public Subtract() {
                command = "subtract";
                shortHelp = "Subtract numbers.";

                AddOverload (typeof (double), "Subtract num2 from num1.");
            }

            public Task<Result> Execute(SocketUserMessage e, double num1, double num2) {
                return TaskResult (num1 - num2, $"{num1} - {num2} = {num1 - num2}");
            }
        }

        public class Multiply : Command {

            public Multiply() {
                command = "multiply";
                shortHelp = "Multiply numbers.";

                AddOverload (typeof (double), "Mutliply two numbers.");
            }

            public Task<Result> Execute(SocketUserMessage e, double num1, double num2) {
                return TaskResult (num1 * num2, $"{num1} * {num2} = {num1 * num2}");
            }
        }

        public class Divide : Command {

            public Divide() {
                command = "divide";
                shortHelp = "Divide numbers.";

                AddOverload (typeof (double), "Divide num1 with num2.");
            }

            public Task<Result> Execute(SocketUserMessage e, double num1, double num2) {
                return TaskResult (num1 / num2, $"{num1} / {num2} = {num1 / num2}");
            }
        }

        public class Pow : Command {

            public Pow() {
                command = "pow";
                shortHelp = "Get the power.";

                AddOverload (typeof (double), "Get the num1 to the power of num2.");
            }

            public Task<Result> Execute(SocketUserMessage e, double num1, double num2) {
                return TaskResult (Math.Pow (num1, num2), $"{num1}^{num2} = {Math.Pow (num1, num2)}");
            }
        }

        public class Log : Command {

            public Log() {
                command = "log";
                shortHelp = "Returns logs.";

                AddOverload (typeof (double), "Get the natural logarithm of the given number.");
                AddOverload (typeof (double), "Get the logarithm of the given number in a specific base.");
            }

            public Task<Result> Execute(SocketUserMessage e, double num) {
                return TaskResult (Math.Log (num), $"Log({num}) = {Math.Log (num)}");
            }

            public Task<Result> Execute(SocketUserMessage e, double num, double logBase) {
                return TaskResult (Math.Log (num, logBase), $"Log{logBase}({num}) = {Math.Log (num, logBase)}");
            }
        }

        public class Mod : Command {

            public Mod() {
                command = "mod";
                shortHelp = "Returns modulus.";

                AddOverload (typeof (double), "Get the remainder of num1 / num2.");
            }

            public Task<Result> Execute(SocketUserMessage e, double num1, double num2) {
                return TaskResult (num1 % num2, $"{num1} % {num2} = {num1 % num2}");
            }
        }

        public class Sin : Command {

            public Sin() {
                command = "sin";
                shortHelp = "Anger the gods.";

                AddOverload (typeof (double), "Get the sin of the given angle in radians.");
            }

            public Task<Result> Execute(SocketUserMessage e, double num) {
                return TaskResult (Math.Sin (num), $"SIN ({num}) = {Math.Sin (num)}");
            }
        }

        public class Cos : Command {

            public Cos() {
                command = "cos";
                shortHelp = "Returns cosine.";

                AddOverload (typeof (double), "Get the cos of the given angle in radians.");
            }

            public Task<Result> Execute(SocketUserMessage e, double num) {
                return TaskResult (Math.Cos (num), $"COS ({num}) = {Math.Cos (num)}");
            }
        }

        public class Tan : Command {

            public Tan() {
                command = "tan";
                shortHelp = "Get ready for summer.";

                AddOverload (typeof (double), "Get the tan of the given angle in radians.");
            }

            public Task<Result> Execute(SocketUserMessage e, double num) {
                return TaskResult (Math.Tan (num), $"TAN ({num}) = {Math.Tan (num)}");
            }
        }
    }
}
