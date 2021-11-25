using System;
using Expresso;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void SimpleTest()
        {
            Assert.AreEqual(ExpressoCompiler.CompileExpression<Func<int>>("21")(), 21);
            Assert.AreEqual(ExpressoCompiler.CompileExpression<Func<int, int>>("x * 2", "x")(21), 42);
        }

        [TestMethod]
        public void ArgumentFailTest()
        {
            try
            {
                ExpressoCompiler.CompileExpression<Action<int>>("x", "a", "b")(42);
                Assert.Fail();
            }
            catch (ArgumentException)
            {
            }
        }

        [TestMethod]
        public void ParseFailTest()
        {
            try
            {
                ExpressoCompiler.CompileExpression<Action>("3 3")();
                Assert.Fail();
            }
            catch (ParserException)
            {
            }
        }


        [TestMethod]
        public void CompileFailTest()
        {
            try
            {
                ExpressoCompiler.CompileExpression<Func<int>>("x")();
                Assert.Fail();
            }
            catch (CompilerException)
            {
            }
        }

        [TestMethod]
        public void ParameterTest()
        {
            Assert.AreEqual(ExpressoCompiler.CompileExpression<Func<int>>("21")(), 21);

            Assert.AreEqual(ExpressoCompiler.CompileExpression<Func<int, int>>("x * 2", "x")(21), 42);
        }

        [TestMethod]
        public void VariableTest()
        {
            var v1 = ExpressoVariable.Create<string>("s");

            try
            {
                /* Should throw because it's not initiallized */
                v1.Value = "hello";
                Assert.Fail();
            }
            catch (NotSupportedException)
            {
            }

            var fn1 = ExpressoCompiler.CompileExpression<Func<string, string>>("s = a", new[] { v1 }, "a");
            var s = fn1("Hello world");
            Assert.AreEqual(s, "Hello world");
            Assert.AreEqual(v1.Value, "Hello world");

            var fn2 = ExpressoCompiler.CompileExpression<Func<bool>>("s == \"Hello world\"", new[] { v1 });
            Assert.IsTrue(fn2());

            v1.Value = "Bye";

            var fn3 = ExpressoCompiler.CompileExpression<Func<bool>>("s == \"Bye\"", new[] { v1 });
            Assert.IsTrue(fn3());

            var v2 = ExpressoVariable.Create<string>("s", "test");
            Assert.AreEqual(v2.Value, "test");

            var fn4 = ExpressoCompiler.CompileExpression<Func<string>>("s", new[] { v2 });
            Assert.AreEqual(fn4(), "test");
        }

        [TestMethod]
        public void DynamicTest()
        {
            var v1 = ExpressoVariable.Create<dynamic>(true, "s", "test");
            var fn1 = ExpressoCompiler.CompileExpression<Func<bool>>("s.Length == 4", new[] { v1 });
            Assert.IsTrue(fn1());

            var v2 = ExpressoVariable.Create<dynamic>(false, "s", "test");
            try
            {
                ExpressoCompiler.CompileExpression<Func<int>>("s.Length", new[] { v2 });
                Assert.Fail();
            }
            catch (CompilerException)
            {
            }

            var fn3 = ExpressoCompiler.CompileExpression<Func<dynamic>>("\"test\"");
            Assert.AreEqual(fn3().Length, 4);

            var v3 = ExpressoVariable.Create<dynamic>(true, "s", 42);

            var fn4 = ExpressoCompiler.CompileExpression<Func<Type>>("s.GetType()", new[] { v3 });
            Assert.AreEqual(fn4(), typeof(int));

            v3.Value = 42f;
            Assert.AreEqual(fn4(), typeof(float));
        }
    }
}
