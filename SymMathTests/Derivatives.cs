using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SymbolicMath;
using SymbolicMath.Simplification;
using static SymbolicMath.ExpressionHelper;

namespace SymMathTests
{
    [TestClass]
    public class Derivatives
    {
        static ISimplifier simplifier { get; } = new Simplifier();
        static Variable x = "x";
        static Variable y = "y";
        static Expression I = 1;
        static Expression II = 2;

        [TestMethod]
        public void Constants()
        {
            Assert.AreEqual(0, I.Derivative(y));
            Assert.AreEqual(0, I.Derivative(x));
            Assert.AreEqual(0, II.Derivative(y));
            Assert.AreEqual(0, II.Derivative(x));
        }

        [TestMethod]
        public void Variables()
        {
            Assert.AreEqual(0, x.Derivative(y));
            Assert.AreEqual(0, y.Derivative(x));
            Assert.AreEqual(1, x.Derivative(x));
            Assert.AreEqual(1, y.Derivative(y));

            Assert.AreEqual(2 * x, simplifier.Simplify((x * x).Derivative(x)));
            Assert.AreEqual(0, simplifier.Simplify((x * x).Derivative(y)));

            Assert.AreEqual(2 * x * y, simplifier.Simplify((x * x * y).Derivative(x)));
            Assert.AreEqual(x ^ 2, simplifier.Simplify((x * x * y).Derivative(y)));

            Assert.AreEqual(1, simplifier.Simplify((x * y).Derivative(y).Derivative(x)));

            Assert.AreEqual(2, simplifier.Simplify((2 * x * y).Derivative(y).Derivative(x)));

            Assert.AreEqual(2 * x, simplifier.Simplify(((x ^ 2) * y).Derivative(y).Derivative(x)));
            Assert.AreEqual(4 * x * y, simplifier.Simplify(((x ^ 2) * (y ^ 2)).Derivative(y).Derivative(x)));
            Assert.AreEqual(2 * y, simplifier.Simplify(((x ^ 2) * (y ^ 2) / x).Derivative(y).Derivative(x)));

            Assert.AreEqual(4 * x * y, simplifier.Simplify(((x ^ 2) * (y ^ 2)).Derivative(x).Derivative(y)));
            Assert.AreEqual(2 * y, simplifier.Simplify(((x ^ 2) * (y ^ 2) / x).Derivative(x).Derivative(y)));

            Assert.AreEqual(-(y ^ -2), simplifier.Simplify((x / y).Derivative(y).Derivative(x)));
            Assert.AreEqual(-(y ^ -2), simplifier.Simplify((x / y).Derivative(x).Derivative(y)));
        }

        [TestMethod]
        public void Trig()
        {
            Assert.AreEqual(5 * cos(5 * x), simplifier.Simplify((sin(x * 5).Derivative(x))));
            Assert.AreEqual(2 * x * cos(x ^ 2), simplifier.Simplify((sin(x * x).Derivative(x))));

            Assert.AreEqual(-5 * sin(5 * x), simplifier.Simplify((cos(x * 5).Derivative(x))));
            Assert.AreEqual(-2 * x * sin(x ^ 2), simplifier.Simplify((cos(x * x).Derivative(x))));

            Assert.AreEqual(1 / (cos(x) ^ 2), simplifier.Simplify((tan(x).Derivative(x))));
            Assert.AreEqual(2 * x / (cos(x ^ 2) ^ 2), simplifier.Simplify((tan(x ^ 2).Derivative(x))));
        }

        [TestMethod]
        public void Exponential()
        {
            Assert.AreEqual(1 / x, simplifier.Simplify((ln(x)).Derivative(x)));
            Assert.AreEqual(2 / x, simplifier.Simplify((ln(x ^ 2)).Derivative(x)));

            Assert.AreEqual(e(x), simplifier.Simplify((e(x)).Derivative(x)));
            Assert.AreEqual(2 * x * e(x ^ 2), simplifier.Simplify((e(x ^ 2)).Derivative(x)));
            Assert.AreEqual(0, simplifier.Simplify((e(x ^ 2)).Derivative(y)));
            Assert.AreEqual(2 * x, simplifier.Simplify((e(ln(x ^ 2))).Derivative(x)));

            Assert.AreEqual("ln(2) * 2 ^ x", simplifier.Simplify((2 ^ x).Derivative(x)));
            // Assert.AreEqual("ln(y) * y ^ x", simplifier.Simplify((y ^ x).Derivative(x)));
            // Need a rule to get from (u * u^(v+n)) to (u^(v+n+1))
        }
    }
}
