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
            var regex = new Regex(@"^\s*([a-zA-Z_$][a-zA-Z_$0-9]*)\s*\=\s*(.*)$", RegexOptions.Compiled);

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

                /* Check if the expression is a variable assignment. */
                var match = regex.Match(line);
                string assignTo = null;
                var expression = line;
                if (match.Success)
                {
                    assignTo   = match.Groups[1].ToString();
                    expression = match.Groups[2].ToString();
                }
                
                double result;
                try
                {
                    /* Compile the expression */
                    var f = ExpressoCompiler.CompileExpression<Func<double>>(expression, variables.ToArray());

                    /* Run the compiled function and get the result */
                    result = f();
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

                /* If it was an assignment, find the variable and store the result */
                if (assignTo != null)
                {
                    var variable = variables.FirstOrDefault(x => x.Name == assignTo);
                    /* If the variable already exists; assign the result. */
                    if (variable != null)
                    {
                        variable.Value = result;
                    }
                    /* If the variable doesn't already exist, add it with the result as it's initial value. */
                    else
                    {
                        variables.Add(ExpressoVariable.Create<double>(assignTo, result));
                    }
                }

                /* Print the result */
                Console.WriteLine(result);
            }
        }
    }
}
