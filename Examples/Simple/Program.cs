/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */

using System;
using ExpressoSharp;

namespace Simple
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                /* We can use ExpressoCompiler.CompileExpression<T>() in order to compile an expression into
                 * a funciton with one single line */
                var calc1 = ExpressoCompiler.CompileExpression<Func<double, double>>("x * 21", "x");

                /* The same function will also accept custom types as parameter or return value. Any necessary
                 * assemblies will automatically be referenced */
                var calc2 = ExpressoCompiler.CompileExpression<Func<NonNativeTypeTest, double>>("x.X * 21", "x");

                Console.WriteLine(calc1(1));
                Console.WriteLine(calc2(new NonNativeTypeTest(2)));

                /* We can also compile multiple expressions simultaniously. This will result in an array of delegates. */
                var multi = ExpressoCompiler.CompileExpressions(                    
                    new ExpressoMethod<Func<double, double>>("x * 21", "x"),
                    new ExpressoMethod<Func<NonNativeTypeTest, double>>("x.X * 21", "x")
                );

                /* We can simply cast the delecations to Func<,> types and then cann them as if they're normal functions. */
                calc1 = (Func<double, double>) multi[0];
                calc2 = (Func<NonNativeTypeTest, double>) multi[1];

                Console.WriteLine(calc1(3));
                Console.WriteLine(calc2(new NonNativeTypeTest(4)));
            }
            catch (ParserException e)
            {
                Console.Error.WriteLine($"Parse erro: {e.Message}");
            }
            catch (CompilerException e)
            {
                Console.Error.WriteLine($"Compile error: {e.Message}");
            }

            Console.WriteLine("Press any key to return");
            Console.ReadKey();
        }
    }
}
