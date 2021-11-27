using System;
using ExpressoSharp;
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
            var v1 = new ExpressoProperty<string>("s");
            var fn1 = ExpressoCompiler.CompileExpression<Func<string, string>>("s = a", new[] { v1 }, "a");
            var s = fn1("Hello world");
            Assert.AreEqual(s, "Hello world");
            Assert.AreEqual(v1.Value, "Hello world");

            var fn2 = ExpressoCompiler.CompileExpression<Func<bool>>("s == \"Hello world\"", new[] { v1 });
            Assert.IsTrue(fn2());

            v1.Value = "Bye";

            var fn3 = ExpressoCompiler.CompileExpression<Func<bool>>("s == \"Bye\"", new[] { v1 });
            Assert.IsTrue(fn3());

            var v2 = new ExpressoProperty<string>("s", "test");
            Assert.AreEqual(v2.Value, "test");

            var fn4 = ExpressoCompiler.CompileExpression<Func<string>>("s", new[] { v2 });
            Assert.AreEqual(fn4(), "test");
        }

        [TestMethod]
        public void DynamicTest()
        {
            var v1 = new ExpressoProperty<dynamic>(true, "s", "test");
            var fn1 = ExpressoCompiler.CompileExpression<Func<bool>>("s.Length == 4", new[] { v1 });
            Assert.IsTrue(fn1());

            var v2 = new ExpressoProperty<dynamic>(false, "s", "test");
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

            var v3 = new ExpressoProperty<dynamic>(true, "s", 42);

            var fn4 = ExpressoCompiler.CompileExpression<Func<Type>>("s.GetType()", new[] { v3 });
            Assert.AreEqual(fn4(), typeof(int));

            v3.Value = 42f;
            Assert.AreEqual(fn4(), typeof(float));
        }

        [TestMethod]
        public void SharedVariableTest()
        {
            var v = new ExpressoProperty<string>("s", "test");
            var fn1 = ExpressoCompiler.CompileExpression<Func<string>>("s", new[] { v });
            var fn2 = ExpressoCompiler.CompileExpression<Func<string>>("s", new[] { v });
            var fn3 = ExpressoCompiler.CompileExpression<Action<string>>("s = x", new[] { v }, "x");

            Assert.AreEqual(v.Value, "test");
            Assert.AreEqual(fn1(), "test");
            Assert.AreEqual(fn2(), "test");

            v.Value = "hello";
            Assert.AreEqual(v.Value, "hello");
            Assert.AreEqual(fn1(), "hello");
            Assert.AreEqual(fn2(), "hello");

            fn3("hi");
            Assert.AreEqual(v.Value, "hi");
            Assert.AreEqual(fn1(), "hi");
            Assert.AreEqual(fn2(), "hi");
        }

        [TestMethod]
        public void FieldTest()
        {
            var v = new ExpressoField<string>("s", "\"test\"");

            var method1 = ExpressoMethod.Create<Func<string>>("s");
            var method2 = ExpressoMethod.Create<Action<string>>("s = x", "x");

            var funcs = ExpressoCompiler.CompileExpressions(new[] { v }, method1, method2);
            
            var fn1 = (Func<string>) funcs[0];
            var fn2 = (Action<string>) funcs[1];

            Assert.AreEqual(fn1(), "test");

            fn2("hello");

            Assert.AreEqual(fn1(), "hello");
        }

        [TestMethod]
        public void DynamicFieldTest()
        {
            var v = new ExpressoField<dynamic>(true, "s", "\"1234\"");

            var method1 = ExpressoMethod.Create<Func<dynamic>>("s.Length", true);
            var method2 = ExpressoMethod.Create<Action<dynamic>>("s = x", "x");

            var funcs = ExpressoCompiler.CompileExpressions(new[] { v }, method1, method2);
            
            var fn1 = (Func<dynamic>) funcs[0];
            var fn2 = (Action<dynamic>) funcs[1];

            Assert.AreEqual(fn1(), 4);

            fn2("12345");

            Assert.AreEqual(fn1(), 5);
        }
    }
}
