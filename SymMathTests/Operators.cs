using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SymbolicMath;
using static SymbolicMath.ExpressionHelper;
using System.Collections.Generic;

namespace SymMathTests
{
    [TestClass]
    public class Operators
    {
        Random rand = new Random();
        double delta = 1e-5;

        [TestMethod]
        public void OfConstants()
        {
            double a = rand.NextDouble() / rand.NextDouble();
            double b = rand.NextDouble() / rand.NextDouble();
            Expression powxy = "x^y";

            Dictionary<Variable, Expression> xToA = new Dictionary<Variable, Expression>() {
                ["x"] = a,
                ["y"] = b
            };

            Assert.AreEqual(0, powxy.With(xToA).Derivative("a"));

            Assert.IsFalse((powxy as Operator).Associative);
            Assert.IsFalse((powxy as Operator).Commutative);

            Assert.AreEqual(pow(a, b), powxy.With(xToA));

            Assert.AreEqual($"({a} ^ {b})", powxy.With(xToA).ToString());

            Assert.AreEqual(Math.Pow(a, b), powxy.With(xToA).Value);

            Assert.AreEqual(Math.Pow(a, b), powxy.With(xToA).Evaluate(null));
        }

        [TestMethod]
        public void OfVariables()
        {
            double a = rand.NextDouble() / rand.NextDouble();
            double b = rand.NextDouble() / rand.NextDouble();
            Expression powxy = "x^y";

            MyAssert.Throws<InvalidOperationException, double>(() => powxy.Value);
        }
    }
}
