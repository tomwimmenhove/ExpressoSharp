using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Expresso
{
    public class NonNativeTypeTest
    {
        public int X { get; set; }

        public NonNativeTypeTest(int x)
        {
            X = x;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var calc1 = ExpressoCompiler.CompileExpression<Func<NonNativeTypeTest, double>>(
                    "a = x.X * 21 * a", "x");

                var calc2 = ExpressoCompiler.CompileExpression<Func<double, NonNativeTypeTest>>(
                    "new Expresso.NonNativeTypeTest((int) x * 21)", "x");

                var multi = ExpressoCompiler.CompileExpressions(                    
                    ExpressoMethod.Create<Func<NonNativeTypeTest, double>>(
                        "x.X * 21", "x"),
                    ExpressoMethod.Create<Func<double, NonNativeTypeTest>>(
                        "new Expresso.NonNativeTypeTest((int) x * 21)", "x")
                );

                Console.WriteLine(calc1(new NonNativeTypeTest(2)));
                Console.WriteLine(calc2(4).X);

                calc1 = (Func<NonNativeTypeTest, double>) multi[0];
                calc2 = (Func<double, NonNativeTypeTest>) multi[1];

                Console.WriteLine(calc1(new NonNativeTypeTest(2)));
                Console.WriteLine(calc2(4).X);
            }
            catch (ParserException e)
            {
                Console.Error.WriteLine($"Parse erro: {e.Message}");
            }
            catch (CompilerException e)
            {
                Console.Error.WriteLine($"Compile error: {e.Message}");
            }
        }
    }
}
