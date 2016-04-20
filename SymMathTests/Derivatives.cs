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
            Assert.AreEqual(0, (~II).Derivative(x));
        }

        [TestMethod]
        public void Variables()
        {
            Assert.AreEqual(0, x.Derivative(y));
            Assert.AreEqual(0, y.Derivative(x));
            Assert.AreEqual(1, x.Derivative(x));
            Assert.AreEqual(1, y.Derivative(y));

            Assert.AreEqual(2 * x, (x * x).Derivative(x));
            Assert.AreEqual(0, (x * x).Derivative(y));

            Assert.AreEqual(2 * x * y, (x * x * y).Derivative(x));
            Assert.AreEqual(x ^ 2, (x * x * y).Derivative(y));

            Assert.AreEqual(1, (x * y).Derivative(y).Derivative(x));

            Assert.AreEqual(2, (2 * x * y).Derivative(y).Derivative(x));

            Assert.AreEqual(2 * x, ((x ^ 2) * y).Derivative(y).Derivative(x));
            Assert.AreEqual(4 * x * y, ((x ^ 2) * (y ^ 2)).Derivative(y).Derivative(x));
            Assert.AreEqual(2 * y, ((x ^ 2) * (y ^ 2) / x).Derivative(y).Derivative(x));

            Assert.AreEqual(4 * x * y, ((x ^ 2) * (y ^ 2)).Derivative(x).Derivative(y));
            Assert.AreEqual(2 * y, ((x ^ 2) * (y ^ 2) / x).Derivative(x).Derivative(y));

            Assert.AreEqual(-(y ^ -2), (x / y).Derivative(y).Derivative(x));
            Assert.AreEqual(-(y ^ -2), (x / y).Derivative(x).Derivative(y));
        }

        [TestMethod]
        public void Trig()
        {
            Assert.AreEqual(5 * cos(5 * x), sin(x * 5).Derivative(x));
            Assert.AreEqual(2 * x * cos(x ^ 2), sin(x * x).Derivative(x));

            Assert.AreEqual(-5 * sin(5 * x), cos(x * 5).Derivative(x));
            Assert.AreEqual(-2 * x * sin(x ^ 2), cos(x * x).Derivative(x));

            Assert.AreEqual(1 / (cos(x) ^ 2), tan(x).Derivative(x));
            Assert.AreEqual(2 * x / (cos(x ^ 2) ^ 2), tan(x ^ 2).Derivative(x));
        }

        [TestMethod]
        public void Exponential()
        {
            Assert.AreEqual(1 / x, ln(x).Derivative(x));
            Assert.AreEqual(2 / x, ln(x ^ 2).Derivative(x));

            Assert.AreEqual(e(x), e(x).Derivative(x));
            Assert.AreEqual(2 * x * e(x ^ 2), e(x ^ 2).Derivative(x));
            Assert.AreEqual(0, e(x ^ 2).Derivative(y));
            Assert.AreEqual(2 * x, e(ln(x ^ 2)).Derivative(x));

            Assert.AreEqual("ln(2) * 2 ^ x", (2 ^ x).Derivative(x));
            Assert.AreEqual("ln(y) * y ^ x", (y ^ x).Derivative(x));
        }
    }
}
