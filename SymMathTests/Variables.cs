using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SymbolicMath;
using SymbolicMath.Simplification;
using static SymbolicMath.ExpressionHelper;

namespace SymMathTests
{
    [TestClass]
    public class Variables
    {

        [TestMethod]
        public void Comstruction()
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
                Assert.AreEqual(new Constant(1), A.Derivative(name1));
                Assert.AreEqual(new Constant(0), A.Derivative(name1 + "_"));
            }
        }

        [TestMethod]
        public void Simplification()
        {
            Expression x = var("x");
            Expression e = (x + (1 + x));
            Assert.IsTrue(e.Matches(Rules.ReWrite.ExtractConstants));
            Assert.AreEqual((1 + (x + x)), Rules.ReWrite.ExtractConstants.Transform(e));
            Assert.AreEqual((1 + (x + x)), new Simplifier().Simplify(e));

            new Simplifier().Simplify(x - 1);
        }
    }
}
