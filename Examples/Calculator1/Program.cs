/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */

using System;
using ExpressoSharp;

namespace Calculator1
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new ExpressoMethodOptions { ForceNumericDouble = true };
            
            while (true)
            {
                /* Read a simple mathematical expression from the command line */
                Console.Write("> ");
                var expression = Console.ReadLine();
                if (expression == null)
                {
                    break;
                }
                if (expression.Length == 0)
                {
                    continue;
                }

                try
                {
                    /* Compile the expression */
                    var func = ExpressoCompiler.CompileExpression<Func<double>>(options, expression);

                    /* And call it */
                    var result = func();

                    /* Print the result */
                    Console.WriteLine(result);
                }
                /* Catch anything that might've gone wrong during parsing... */
                catch (ExpressoParserException e)
                {
                    Console.Error.WriteLine($"Parse erro: {e.Message}");
                    continue;
                }
                /* ... during compilation... */
                catch (ExpressoCompilerException e)
                {
                    Console.Error.WriteLine($"Compile error: {e.Message}");
                    continue;
                }
                /* ... or during execution */
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Unknown error: {e.Message}");
                    continue;
                }
            }
        }
    }
}
