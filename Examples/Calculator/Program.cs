using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Expresso;

namespace Calculator
{
    class Program
    {
        static void Main(string[] args)
        {
            /* A regular expression that matches variable assignments */
            var regex = new Regex(@"^\s*([a-zA-Z_$][a-zA-Z_$0-9]*)\s\=.*", RegexOptions.Compiled);

            /* A list of variables used */
            var variables = new List<ExpressoVariable<double>>();

            while (true)
            {
                Console.Write("> ");
                var line = Console.ReadLine();
                if (line == null)
                {
                    break;
                }

                line = line.Trim();
                if (line.Equals("quit", StringComparison.InvariantCultureIgnoreCase) ||
                    line.Equals("exit", StringComparison.InvariantCultureIgnoreCase))
                {
                    break;
                }

                /* Check if the expression contains a variable assignment. */
                var match = regex.Match(line);
                if (match.Success)
                {
                    var name = match.Groups[1].ToString();

                    /* If the variable doesn't already exist, add it */
                    if (!variables.Any(x => x.Name == name))
                    {
                        variables.Add(new ExpressoVariable<double>(name));
                    }
                }

                try
                {
                    /* Compile the expression */
                    var f = ExpressoCompiler.CompileExpression<Func<double>>(line, variables.ToArray());

                    /* Print the result */
                    Console.WriteLine(f());

                    /* Generate a new list of variables for the next compilation
                     * and initialize them with the value of the previous list */
                    variables = variables.Select(x => new ExpressoVariable<double>(x.Name, x.Value.ToString())).ToList();
                }
                catch (ParserException e)
                {
                    Console.Error.WriteLine($"Parse erro: {e.Message}");
                }
                catch (CompilerException e)
                {
                    Console.Error.WriteLine($"Compile error: {e.Message}");
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Unknown error: {e.Message}");
                }
            }
        }
    }
}
