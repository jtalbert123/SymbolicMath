using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SymbolicMath;
using SymbolicMath.Simplification;

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

            Simplifier simplifier = new Simplifier();
            Expression x = "x", y = "y";
            Console.WriteLine(simplifier.Simplify(new Pow(new Exp(x), 2).Derivative("x")));
        }
    }
}
