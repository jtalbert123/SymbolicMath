using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SymbolicMath;
using static SymbolicMath.ExpressionHelper;
using static SymbolicMath.Infix;
using SymbolicMath.Simplification;
using System.Collections.Generic;

namespace SymMathTests
{
    [TestClass]
    public class SimplifierTests
    {

        static Expression otherTerms { get; } = "(o^o^o^o^o^o^o^o^o^o^o^o^o^o^o)";
        static Variable x = "x";
        static Variable y = "y";
        static Expression I = 1;
        static Expression II = 2;

        [TestMethod]
        public void Order()
        {
            Expression @in = "x + 1";
            Expression @out = "1 + x";
            Assert.AreEqual(@out, @in.Simplify());

            @in = otherTerms + 1;
            @out = 1 + otherTerms;
            Assert.AreEqual(@out, @in.Simplify());

            @in = otherTerms + x;
            @out = x + otherTerms;
            Assert.AreEqual(@out, @in.Simplify());

            @out = 1 + x + otherTerms;
            {
                @in = otherTerms + x + 1;
                Assert.AreEqual(@out, @in.Simplify());
                @in = otherTerms + 1 + x;
                Assert.AreEqual(@out, @in.Simplify());
            }

            @out = x + ln(x);
            @in = x + ln(x);
            Assert.AreEqual(@out, @in.Simplify());

            @out = x + ln(x);
            @in = ln(x) + x;
            Assert.AreEqual(@out, @in.Simplify());

            @out = 1 + x + ln(x) + otherTerms;
            {
                @in = otherTerms + x + 1 + ln(x);
                Assert.AreEqual(@out, @in.Simplify());
                @in = otherTerms + 1 + ln(x) + x;
                Assert.AreEqual(@out, @in.Simplify());
                @in = 1 + ln(x) + otherTerms + x;
                Assert.AreEqual(@out, @in.Simplify());
            }

            @out = (I / 3) + x + ln(x) + otherTerms;
            {
                @in = otherTerms + x + (I / 3) + ln(x);
                Assert.AreEqual(@out, @in.Simplify());
                @in = otherTerms + I / 3 + ln(x) + x;
                Assert.AreEqual(@out, @in.Simplify());
            }

            @out = (I / 3) + x + sin(x) + otherTerms;
            {
                @in = otherTerms + x + (I / 3) + sin(x);
                Assert.AreEqual(@out, @in.Simplify());
                @in = otherTerms + I / 3 + sin(x) + x;
                Assert.AreEqual(@out, @in.Simplify());
            }
        }

        [TestMethod]
        public void Constants()
        {
            Expression @out;
            Expression @in;
            @out = Parse("2/3");
            {
                @in = (II / 3);
                Assert.AreEqual(@out, @in.Simplify());
                @in = (I + I) / 3;
                Assert.AreEqual(@out, @in.Simplify());

                @in = 2 * (I / 3);
                Assert.AreEqual(@out, @in.Simplify());
            }
        }

        [TestMethod]
        public void Simplification()
        {
            Assert.AreEqual((1 + (2 * x)), (x + (1 + x)).Simplify());

            Assert.AreEqual(5 + x, (1 + ((1 + ((x + 1) + 1)) + 1)).Simplify());

            Assert.AreEqual(Parse("2+x/5"), (x / 5 + 1 + I).Simplify());

            Assert.AreEqual(II / 5, ((I + I) / 5).Simplify());

            //Constant is always on the left
            Assert.AreEqual(-5 + x, (x - (1 + 2 + 2)).Simplify());

            Assert.AreEqual(5 + -x, ((1 + 2 + 2) - x).Simplify());

            Assert.AreEqual(5 + -x, ((ln(1) + 1 + 2 + 2) - x).Simplify());

            Assert.AreEqual(-x, ((ln(1) + 1 + 2 + 2) * ln(1) - x).Simplify());

            Assert.AreEqual(0, (((ln(1) + 1 + 2 + 2) - x) * 0).Simplify());

            Assert.AreEqual(x, (((ln(1) + 1 + 2 + 2) * 0 + x) * 1).Simplify());

            Assert.AreEqual(-x, (5 * 0 - x).Simplify());

            Assert.AreEqual(-x, (((ln(1) + 1 + 2 + 2) * 0 - x) / 1).Simplify());

            Assert.AreEqual(x, (x / 1).Simplify());

            Assert.AreEqual(2 * x, (x * (II / I)).Simplify());

            Assert.AreEqual(x, (x / 2 * 2).Simplify());

            Assert.AreEqual(4 * x, (x + x + x + x).Simplify());

            Assert.AreEqual(6 * otherTerms, ((otherTerms + otherTerms) * 3).Simplify());

            Assert.AreEqual(3 * x, (x + 2 * x).Simplify());

            Assert.AreEqual(1 / x, ((x ^ 2) / (x ^ 3)).Simplify());

            Assert.AreEqual((y ^ x)/x, ((x ^ 2) * (y^(x-1)) / (x ^ 3) / (y^-1)).Simplify());
        }

        [TestMethod]
        public void SimplificationWithSub()
        {
            Assert.AreEqual(0, (x + x - (x + x)).Simplify());

            Assert.AreEqual(x, (x + x - x).Simplify());

            Assert.AreEqual(0, (x - x + x - x).Simplify());

            Assert.AreEqual(6 * otherTerms, ((otherTerms + otherTerms) * 4 - otherTerms - otherTerms).Simplify());

            Assert.AreEqual(-6 * otherTerms, ((otherTerms + otherTerms) * (-3)).Simplify());

            Assert.AreEqual(-2 * ln(x) + otherTerms, (otherTerms - ln(x) - ln(x)).Simplify());

            Assert.AreEqual(-2 * ln(x), ((otherTerms + otherTerms) * (-3) + (otherTerms + otherTerms) * (3) - ln(x) - ln(x)).Simplify());

            Assert.AreEqual(2, (5 * x - 5 * x + 2).Simplify());

            Assert.AreEqual(7, (7 - 5 * x + 5 * x).Simplify());
        }

        [TestMethod]
        public void Multivariate()
        {
            Assert.AreEqual(x + y, (x + y).Simplify());
            Assert.AreEqual(y + 2 * x, (y + x + x).Simplify());
            Assert.AreEqual(y, (y + x - x).Simplify());
            Assert.AreEqual(0, (y + x - x - y).Simplify());
            Assert.AreEqual(0, (y - y + x - x).Simplify());
            Assert.AreEqual(0, (y + x - y - x).Simplify());
            Assert.AreEqual("z" + otherTerms, (y + x - y - x + "z" + otherTerms).Simplify());

            Expression z = "z";
            Assert.AreEqual("5 * z", (z + 3 * z - 2 * z + z + 2 * z + y - x - y + x).Simplify());
            Assert.AreEqual(4 + 14 * y + 3 * x, (5 * y + 4 + 3 * x + 5 * y + 4 * y).Simplify());
        }

        [TestMethod]
        public void Exponential_Logs()
        {
            Assert.AreEqual(otherTerms, (ln(e(otherTerms))).Simplify());

            Assert.AreEqual(1 + otherTerms, (ln(e(otherTerms + 1))).Simplify());

            Assert.AreEqual(ln(otherTerms), (ln(e(ln(otherTerms)))).Simplify());

            Assert.AreEqual(ln(5), (ln(e(ln(5)))).Simplify());
        }

        [TestMethod]
        public void Debug()
        {
            Assert.AreEqual(x, (x / 2 * 2).Simplify());
        }
    }
}
