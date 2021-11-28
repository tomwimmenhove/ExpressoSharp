/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ExpressoSharp;

namespace Calculator
{
    public class Calc<T>
    {
        /* A regular expression that matches variable assignments */
        private static Regex regex = new Regex(@"^\s*([a-zA-Z_$][a-zA-Z_$0-9]*)\s*\=\s*(.*)$", RegexOptions.Compiled);

        /* A list of variables used */
        private List<ExpressoProperty<T>> _variables = new List<ExpressoProperty<T>>();

        private bool _isDynamic;
        private Dictionary<string, Func<bool>> _commands;

        public Calc(bool isDynamic)
        {
            _commands = new Dictionary<string, Func<bool>>(StringComparer.OrdinalIgnoreCase)
            {
                { "help", Help },
                { "show", ShowVariables },
                { "clear", Clear },
                { "exit", Exit },
                { "quit", Exit },
            };

            _isDynamic = isDynamic;

            Console.Write("Initializing...");
            ExpressoCompiler.Prime();
            Console.WriteLine("Done");
        }

        private bool ShowVariables()
        {
            foreach (var variable in _variables)
            {
                Console.WriteLine($"{variable.Name, -20}{variable.Value.GetType(), -20}= {variable.Value}");
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

        private bool Clear()
        {
            _variables.Clear();
            return true;
        }

        private bool Exit() => false;

        public void Run()
        {
            var methodOptions = new ExpressoMethodOptions
            {
                ForceNumericDouble = true,
                ReturnsDynamic = _isDynamic,
                DefaultParameterOptions = new ExpressoParameterOptions
                {
                    IsDynamic = _isDynamic
                }
            };
            var propertyOptions = new ExpressoPropertyOptions { IsDynamic = _isDynamic };

            methodOptions.ExpressoSecurityAccess = eExpressoSecurityAccess.AllowMathMethods;

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
                    assignTo = match.Groups[1].ToString().Trim();
                    expression = match.Groups[2].ToString().Trim();

                    /* Delete this variable */
                    if (expression.Length == 0)
                    {
                        _variables.RemoveAll(x => x.Name == assignTo);
                        continue;
                    }
                }

                Func<T> func;
                try
                {
                    /* Compile the expression */
                    func = ExpressoCompiler.CompileExpression<Func<T>>(methodOptions, expression, _variables.ToArray());
                }
                catch (ParserException e)
                {
                    Console.Error.WriteLine($"Parse error: {e.Message}");
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

                T result;
                try
                {
                    /* Run the compiled function and get the result */
                    result = func();
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Runtime exception: {e.Message}");
                    continue;
                }

                /* If it was an assignment, find the variable and store the result */
                if (assignTo != null)
                {
                    var variable = _variables.FirstOrDefault(x => x.Name == assignTo);
                    /* If the variable already exists; assign the result. */
                    if (variable != null)
                    {
                        variable.Value = result;
                    }
                    /* If the variable doesn't already exist, add it with the result as it's initial value. */
                    else
                    {
                        _variables.Add(new ExpressoProperty<T>(propertyOptions, assignTo, result));
                    }
                }

                /* Print the result */
                Console.WriteLine(result);
            }
        }
    }
}
