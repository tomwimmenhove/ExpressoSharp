using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Expresso;

namespace Calculator
{
    public class Calc<T>
    {
        /* A regular expression that matches variable assignments */
        private static Regex regex = new Regex(@"^\s*([a-zA-Z_$][a-zA-Z_$0-9]*)\s*\=\s*(.*)$", RegexOptions.Compiled);

        /* A list of variables used */
        private List<ExpressoVariable<T>> variables = new List<ExpressoVariable<T>>();

        private Dictionary<string, Func<bool>> _commands;

        public Calc()
        {
            _commands = new Dictionary<string, Func<bool>>(StringComparer.OrdinalIgnoreCase)
            {
                { "help", Help },
                { "show", ShowVariables },
                { "exit", Exit },
                { "quit", Exit },
            };
        }

        private bool ShowVariables()
        {
            foreach (var variable in variables)
            {
                Console.WriteLine($"{variable.Name} = {variable.Value}");
            }

            return true;
        }

        private bool Help()
        {
            Console.WriteLine("Available commands:");
            Console.WriteLine("    help      : You're looking at it.");
            Console.WriteLine("    show      : Show a list of current variables");
            Console.WriteLine("    exit/quit : Exit the program");

            return true;
        }

        private bool Exit() => false;

        public void Run()
        {
            while (true)
            {
                var line = ReadLine.Read("> ");
                if (line == null)
                {
                    break;
                }

                ReadLine.AddHistory(line);

                line = line.Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                if (_commands.TryGetValue(line, out var command))
                {
                    if (!command())
                    {
                        break;
                    }
                    continue;
                }

                /* Check if the expression is a variable assignment. */
                var match = regex.Match(line);
                string assignTo = null;
                var expression = line;
                if (match.Success)
                {
                    assignTo = match.Groups[1].ToString();
                    expression = match.Groups[2].ToString();
                }

                T result;
                try
                {
                    /* Compile the expression */
                    var f = ExpressoCompiler.CompileExpression<Func<T>>(expression, variables.ToArray());

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
                        variables.Add(ExpressoVariable.Create<T>(assignTo, result));
                    }
                }

                /* Print the result */
                Console.WriteLine(result);
            }
        }
    }
}
