/* This file is part of Expresso
 *
 * Copyright (c) 2021 Tom Wimmenhove. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for details.
 */

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
            var dynamicPropertyOptions = new ExpressoPropertyOptions() { IsDynamic = true };

            var v1 = new ExpressoProperty<dynamic>(dynamicPropertyOptions, "s", "test");
            var fn1 = ExpressoCompiler.CompileExpression<Func<bool>>("s.Length == 4", new[] { v1 });
            Assert.IsTrue(fn1());

            var v2 = new ExpressoProperty<dynamic>("s", "test");
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

            var v3 = new ExpressoProperty<dynamic>(dynamicPropertyOptions, "s", 42);

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

            var method1 = new ExpressoMethod<Func<string>>("s");
            var method2 = new ExpressoMethod<Action<string>>("s = x", "x");

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
            var dynamicFieldOptions = new ExpressoFieldOptions() { IsDynamic = true };
            var dynamicMethodOptions = new ExpressoMethodOptions() { ReturnsDynamic = true, DefaultParameterOptions = new ExpressoParameterOptions { IsDynamic = true } };

            var v = new ExpressoField<dynamic>(dynamicFieldOptions, "s", "\"1234\"");

            var method1 = new ExpressoMethod<Func<dynamic>>(dynamicMethodOptions, "s.Length");
            var method2 = new ExpressoMethod<Action<dynamic>>("s = x", "x");

            var funcs = ExpressoCompiler.CompileExpressions(new[] { v }, method1, method2);
            
            var fn1 = (Func<dynamic>) funcs[0];
            var fn2 = (Action<dynamic>) funcs[1];

            Assert.AreEqual(fn1(), 4);

            fn2("12345");

            Assert.AreEqual(fn1(), 5);
        }

        [TestMethod]
        public void ForceDoubleTest()
        {
            var options = new ExpressoMethodOptions() { ReturnsDynamic = true };

            var fn1 = ExpressoCompiler.CompileExpression<Func<dynamic>>(options, "12");
            Assert.AreEqual(fn1().GetType(), typeof(int));

            options.ForceNumericDouble = true;

            var fn2 = ExpressoCompiler.CompileExpression<Func<dynamic>>(options, "12");
            Assert.AreEqual(fn2().GetType(), typeof(double));
        }


        [TestMethod]
        public void SecurityTestAllowNoneSimpleReturn()
        {
            var options = new ExpressoMethodOptions() { ExpressoSecurityAccess = eExpressoSecurityAccess.None };  
            ExpressoCompiler.CompileExpression<Func<double, double>>(options, "x", "x");
        }

        [TestMethod]
        public void SecurityTestAllowAll()
        {
            var options = new ExpressoMethodOptions() { ExpressoSecurityAccess = eExpressoSecurityAccess.AllowAll };  
            ExpressoCompiler.CompileExpression<Func<double, double>>(options, "x", "x")(42.42);
            ExpressoCompiler.CompileExpression<Func<double, double>>(options, "Abs(x)", "x")(42.42);
            ExpressoCompiler.CompileExpression<Func<string, int>>(options, "s.Length", "s")("hello");
            ExpressoCompiler.CompileExpression<Func<double, Type>>(options, "x.GetType()", "x")(42.42);
        }


        [TestMethod, ExpectedException(typeof(SecurityException))]
        public void SecurityTestAllowNoneMethodsMathMethod()
        {
            var options = new ExpressoMethodOptions() { ExpressoSecurityAccess = eExpressoSecurityAccess.None };  
            ExpressoCompiler.CompileExpression<Func<double, double>>(options, "Abs(x)", "x")(42.42);
        }

        [TestMethod, ExpectedException(typeof(SecurityException))]
        public void SecurityTestAllowNoneMethodsMemberAccess()
        {
            var options = new ExpressoMethodOptions() { ExpressoSecurityAccess = eExpressoSecurityAccess.None };  
            ExpressoCompiler.CompileExpression<Func<string, int>>(options, "s.Length", "s")("hello");
        }

        [TestMethod, ExpectedException(typeof(SecurityException))]
        public void SecurityTestAllowNoneMethodsMemberInvocation()
        {
            var options = new ExpressoMethodOptions() { ExpressoSecurityAccess = eExpressoSecurityAccess.None };  
            ExpressoCompiler.CompileExpression<Func<double, Type>>(options, "x.GetType()", "x")(42.42);
        }


        [TestMethod]
        public void SecurityTestAllowMathMethodsMathMethod()
        {
            var options = new ExpressoMethodOptions() { ExpressoSecurityAccess = eExpressoSecurityAccess.AllowMathMethods };  
            ExpressoCompiler.CompileExpression<Func<double, double>>(options, "Abs(x)", "x")(42.42);
        }

        [TestMethod, ExpectedException(typeof(SecurityException))]
        public void SecurityTestAllowMathMethodsMemberAccess()
        {
            var options = new ExpressoMethodOptions() { ExpressoSecurityAccess = eExpressoSecurityAccess.AllowMathMethods };  
            ExpressoCompiler.CompileExpression<Func<string, int>>(options, "s.Length", "s")("hello");
        }

        [TestMethod, ExpectedException(typeof(SecurityException))]
        public void SecurityTestAllowMathMethodsMemberInvocation()
        {
            var options = new ExpressoMethodOptions() { ExpressoSecurityAccess = eExpressoSecurityAccess.AllowMathMethods };  
            ExpressoCompiler.CompileExpression<Func<double, Type>>(options, "x.GetType()", "x")(42.42);
        }


        [TestMethod, ExpectedException(typeof(SecurityException))]
        public void SecurityTestAllowMemberAccessMathMethod()
        {
            var options = new ExpressoMethodOptions() { ExpressoSecurityAccess = eExpressoSecurityAccess.AllowMemberAccess };  
            ExpressoCompiler.CompileExpression<Func<double, double>>(options, "Abs(x)", "x")(42.42);
        }

        [TestMethod]
        public void SecurityTestAllowMemberAccessMemberAccess()
        {
            var options = new ExpressoMethodOptions() { ExpressoSecurityAccess = eExpressoSecurityAccess.AllowMemberAccess };  
            ExpressoCompiler.CompileExpression<Func<string, int>>(options, "s.Length", "s")("hello");
        }

        [TestMethod, ExpectedException(typeof(SecurityException))]
        public void SecurityTestAllowMemberAccessnMemberInvocation()
        {
            var options = new ExpressoMethodOptions() { ExpressoSecurityAccess = eExpressoSecurityAccess.AllowMemberAccess };  
            ExpressoCompiler.CompileExpression<Func<double, Type>>(options, "x.GetType()", "x")(42.42);
        }


        [TestMethod, ExpectedException(typeof(SecurityException))]
        public void SecurityTestAllowMemberInvokationMathMethod()
        {
            var options = new ExpressoMethodOptions() { ExpressoSecurityAccess = eExpressoSecurityAccess.AllowMemberAccess | eExpressoSecurityAccess.AllowMemberInvokation };  
            ExpressoCompiler.CompileExpression<Func<double, double>>(options, "Abs(x)", "x")(42.42);
        }

        [TestMethod]
        public void SecurityTestAllowMemberInvokationMemberAccess()
        {
            var options = new ExpressoMethodOptions() { ExpressoSecurityAccess = eExpressoSecurityAccess.AllowMemberAccess | eExpressoSecurityAccess.AllowMemberInvokation };  
            ExpressoCompiler.CompileExpression<Func<string, int>>(options, "s.Length", "s")("hello");
        }
        
        [TestMethod]
        public void SecurityTestAllowMemberInvokationMemberInvocation()
        {
            var options = new ExpressoMethodOptions() { ExpressoSecurityAccess = eExpressoSecurityAccess.AllowMemberAccess | eExpressoSecurityAccess.AllowMemberInvokation };  
            ExpressoCompiler.CompileExpression<Func<double, Type>>(options, "x.GetType()", "x")(42.42);
        }
    }
}
