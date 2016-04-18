using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SymbolicMath;
using SymbolicMath.Simplification;
using static SymbolicMath.ExpressionHelper;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SymMathTests
{
    [TestClass]
    public class Variables
    {

        [TestMethod]
        public void Construction()
        {
            var names = new[] { "x", "X", "y", "Y", "z", "Z", "daysPerYear", "gallons" };
            foreach (var name in names)
            {
                Expression e = name;
                Assert.IsInstanceOfType(e, typeof(Variable));
                Assert.AreEqual(name, e.ToString());
            }
        }

        [TestMethod]
        public void Combination()
        {
            var names = new[] { "x", "X", "y", "Y", "z", "Z", "daysPerYear", "gallons" };
            foreach (var name1 in names)
            {
                Expression A = name1;
                foreach (var name2 in names)
                {
                    Expression B = name2;
                    Assert.AreEqual($"({name1} + {name2})", (A + B).ToString());
                    Assert.AreEqual($"({name1} - {name2})", (A - B).ToString());
                    Assert.AreEqual($"({name1} * {name2})", (A * B).ToString());
                    Assert.AreEqual($"({name1} / {name2})", (A / B).ToString());
                }
            }
        }

        [TestMethod]
        public void Derivation()
        {
            var names = new[] { "x", "X", "y", "Y", "z", "Z", "daysPerYear", "gallons" };
            foreach (var name1 in names)
            {
                Expression A = name1;
                Assert.AreEqual(1, A.Derivative(name1));
                Assert.AreEqual(0, A.Derivative(name1 + "_"));
            }
        }

        [TestMethod]
        public void Evaluation()
        {
            Variable x = "x";
            Dictionary<Variable, double> values = new Dictionary<Variable, double>() { [x] = 100.45 };
            Assert.AreEqual(100.45, x.Evaluate(values));
            Assert.AreEqual(200.9, (x + x).Evaluate(values));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void NonConstantValue()
        {
            Expression x = "x";
            Assert.AreEqual(0, x.Value);
        }
    }
}
