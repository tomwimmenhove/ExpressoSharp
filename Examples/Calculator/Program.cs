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
            var regex = new Regex(@"^\s*([a-zA-Z_$][a-zA-Z_$0-9]*)\s\=.*", RegexOptions.Compiled);
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

                var match = regex.Match(line);
                if (match.Success)
                {
                    var name = match.Groups[1].ToString();

                    if (!variables.Any(x => x.Name == name))
                    {
                        variables.Add(new ExpressoVariable<double>(name));
                    }
                }

                try
                {
                    var f = ExpressoCompiler.CompileExpression<Func<double>>(line, variables.ToArray());
                    var result = f();

                    Console.WriteLine(result);

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
