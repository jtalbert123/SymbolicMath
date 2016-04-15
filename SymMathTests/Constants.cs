using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SymbolicMath;

namespace SymMathTests
{
    [TestClass]
    public class Constants
    {
        [TestMethod]
        public void Construction()
        {
            for (double i = -100; i < 100; ++i)
            {
                Expression @const = i;
                Assert.IsInstanceOfType(@const, typeof(Constant));
                Assert.AreEqual(i.ToString(), @const.ToString());
                Assert.AreEqual(new Constant(0), @const.Derivative(""));
            }
        }

        [TestMethod]
        public void Combination()
        {
            for (double a = -100; a < 100; ++a)
            {
                Expression A = a;
                for (double b = -100; b < 100; ++b)
                {
                    Expression B = b;
                    Assert.AreEqual(new Add(A, B), (A + B));
                    Assert.AreEqual(new Add(A, B), (B + A));
                    Assert.AreEqual(new Sub(A, B), (A - B));
                    Assert.AreEqual(new Mul(A, B), (A * B));
                    Assert.AreEqual(new Mul(A, B), (B * A));
                    Assert.AreEqual(new Div(A, B), (A / B));


                    Assert.AreEqual(a + b, (A + B).Evaluate());
                    Assert.AreEqual(a - b, (A - B).Evaluate());
                    Assert.AreEqual(a * b, (A * B).Evaluate());
                    Assert.AreEqual(a / b, (A / B).Evaluate());
                }
            }
        }

        [TestMethod]
        public void Derivation()
        {
            for (double i = -100; i < 100; ++i)
            {
                Expression @const = i;
                Assert.AreEqual(new Constant(0), @const.Derivative(""));
            }
        }
    }
}
