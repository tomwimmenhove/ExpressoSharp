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
                var epVarA = new ExpressoVariable<int>("a", "2 * 6");

                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();
                var calc1 = ExpressoCompiler.CompileExpression<Func<NonNativeTypeTest, double>>(
                    "a = x.X * 21 * a", new ExpressoVariable[] { epVarA }, "x");
                sw.Stop();

                Console.WriteLine($"First compilation took {sw.Elapsed}");

                Console.WriteLine($"a = {(int) epVarA.Value}");
                epVarA.Value = 2;
                Console.WriteLine($"a = {(int) epVarA.Value}");

                Console.WriteLine(calc1(new NonNativeTypeTest(2)));
                Console.WriteLine($"a = {(int) epVarA.Value}");

                return;

                sw.Reset();
                sw.Start();
                var calc2 = ExpressoCompiler.CompileExpression<Func<double, NonNativeTypeTest>>(
                    "new Expresso.NonNativeTypeTest((int) x * 21)", "x");
                sw.Stop();

                var multi = ExpressoCompiler.CompileExpressions(                    
                    ExpressoMethod.Create<Func<NonNativeTypeTest, double>>(
                        "x.X * 21", "x"),
                    ExpressoMethod.Create<Func<double, NonNativeTypeTest>>(
                        "new Expresso.NonNativeTypeTest((int) x * 21)", "x")
                );

                Console.WriteLine($"Second compilation took {sw.Elapsed}");

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
