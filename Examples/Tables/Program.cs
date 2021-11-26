using System;
using ExpressoSharp;

namespace Tables
{
    class Program
    {
        static double AskNumber(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                var s = Console.ReadLine();
                if (double.TryParse(s, out var number))
                {
                    return number;
                }
                Console.Error.WriteLine($"\"{s}\" is not a valid floating point number");
            }
        }

        static Func<double, double> AskExpression(string prompt)
        {
            Console.WriteLine(prompt);

            while (true)
            {
                Console.Write("y = ");

                var expression = Console.ReadLine();
                try
                {
                    /* Compile the expression */
                    var func = ExpressoCompiler.CompileExpression<Func<double, double>>(expression, "x");

                    return func;
                }
                catch (ParserException e)
                {
                    Console.Error.WriteLine($"Parse erro: {e.Message}");
                    continue;
                }
                catch (CompilerException e)
                {
                    Console.Error.WriteLine($"Compile error: {e.Message}");
                    continue;
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Unknown error: {e.Message}");
                    continue;
                }
            }
        }

        static void Main(string[] args)
        {
            var func = AskExpression("Please enter an expression that uses th variable 'x' (I.E \"12 * x\" or \"Sqrt(x)\")");
            var from = AskNumber("Start x at   : ");
            var to   = AskNumber("Stop x at    : ");
            var inc  = AskNumber("In steps of  : ");
            
            for (var x = from; x <= to; x += inc)
            {
                try
                {
                     var y = func(x);
                     Console.WriteLine($"f({x}) = {y}");
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Runtime error: {e.Message}");
                    break;
                }
            }

            Console.WriteLine("Press any key to return");
            Console.ReadKey();
        }
    }
}
