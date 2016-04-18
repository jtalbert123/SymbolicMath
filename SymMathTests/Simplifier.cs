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
        private class DummyExpression : Expression
        {
            public override int Complexity { get { return 100; } }

            public override int Height { get { return 100; } }

            public override bool IsConstant { get { return false; } }

            public override int Size { get { return 100; } }

            public override double Value { get { throw new InvalidOperationException(); } }

            public override Expression Derivative(Variable variable)
            {
                return this;
            }

            public override double Evaluate(IReadOnlyDictionary<Variable, double> context)
            {
                return 0;
            }

            public override Expression With(IReadOnlyDictionary<Variable, Expression> values)
            {
                return this;
            }

            public override bool Equals(object obj)
            {
                return obj is DummyExpression;
            }

            public override string ToString()
            {
                return "Complex";
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }

        static DummyExpression otherTerms { get; } = new DummyExpression();
        static ISimplifier simplifier { get; } = new Simplifier();
        static Variable x = "x";
        static Variable y = "y";
        static Expression I = 1;
        static Expression II = 2;

        [TestMethod]
        public void Order()
        {
            Expression @in = "x + 1";
            Expression @out = "1 + x";
            Assert.AreEqual(@out, simplifier.Simplify(@in));

            @in = otherTerms + 1;
            @out = 1 + otherTerms;
            Assert.AreEqual(@out, simplifier.Simplify(@in));

            @in = otherTerms + x;
            @out = x + otherTerms;
            Assert.AreEqual(@out, simplifier.Simplify(@in));

            @out = 1 + x + otherTerms;
            {
                @in = otherTerms + x + 1;
                Assert.AreEqual(@out, simplifier.Simplify(@in));
                @in = otherTerms + 1 + x;
                Assert.AreEqual(@out, simplifier.Simplify(@in));
            }

            @out = x + ln(x);
            @in = x + ln(x);
            Assert.AreEqual(@out, simplifier.Simplify(@in));

            @out = x + ln(x);
            @in = ln(x) + x;
            Assert.AreEqual(@out, simplifier.Simplify(@in));

            @out = 1 + x + ln(x) + otherTerms;
            {
                @in = otherTerms + x + 1 + ln(x);
                Assert.AreEqual(@out, simplifier.Simplify(@in));
                @in = otherTerms + 1 + ln(x) + x;
                Assert.AreEqual(@out, simplifier.Simplify(@in));
                @in = 1 + ln(x) + otherTerms + x;
                Assert.AreEqual(@out, simplifier.Simplify(@in));
            }

            @out = (I / 3) + x + ln(x) + otherTerms;
            {
                @in = otherTerms + x + (I / 3) + ln(x);
                Assert.AreEqual(@out, simplifier.Simplify(@in));
                @in = otherTerms + I / 3 + ln(x) + x;
                Assert.AreEqual(@out, simplifier.Simplify(@in));
            }

            @out = (I / 3) + x + sin(x) + otherTerms;
            {
                @in = otherTerms + x + (I / 3) + sin(x);
                Assert.AreEqual(@out, simplifier.Simplify(@in));
                @in = otherTerms + I / 3 + sin(x) + x;
                Assert.AreEqual(@out, simplifier.Simplify(@in));
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
                Assert.AreEqual(@out, simplifier.Simplify(@in));
                @in = (I + I) / 3;
                Assert.AreEqual(@out, simplifier.Simplify(@in));

                @in = 2 * (I / 3);
                Assert.AreEqual(@out, simplifier.Simplify(@in));
            }
        }

        [TestMethod]
        public void Simplification()
        {
            Assert.AreEqual((1 + (2 * x)), simplifier.Simplify(x + (1 + x)));

            Assert.AreEqual(5 + x, simplifier.Simplify(1 + ((1 + ((x + 1) + 1)) + 1)));

            Assert.AreEqual(Parse("2+1/5*x"), simplifier.Simplify(x / 5 + 1 + I));

            Assert.AreEqual(II / 5, simplifier.Simplify((I + I) / 5));

            //Constant is always on the left
            Assert.AreEqual(-5 + x, simplifier.Simplify(x - (1 + 2 + 2)));

            Assert.AreEqual(5 + -x, simplifier.Simplify((1 + 2 + 2) - x));

            Assert.AreEqual(5 + -x, simplifier.Simplify((ln(1) + 1 + 2 + 2) - x));

            Assert.AreEqual(-x, simplifier.Simplify((ln(1) + 1 + 2 + 2) * ln(1) - x));

            Assert.AreEqual(0, simplifier.Simplify(((ln(1) + 1 + 2 + 2) - x) * 0));

            Assert.AreEqual(x, simplifier.Simplify(((ln(1) + 1 + 2 + 2) * 0 + x) * 1));

            Assert.AreEqual(-x, simplifier.Simplify(5 * 0 - x));

            Assert.AreEqual(-x, simplifier.Simplify(((ln(1) + 1 + 2 + 2) * 0 - x) / 1));

            Assert.AreEqual(x, simplifier.Simplify(x / 1));

            Assert.AreEqual(2 * x, simplifier.Simplify(x * (II / I)));

            Assert.AreEqual(x, simplifier.Simplify(x / 2 * 2));

            Assert.AreEqual(4 * x, simplifier.Simplify(x + x + x + x));

            Assert.AreEqual(6 * otherTerms, simplifier.Simplify((otherTerms + otherTerms) * 3));

            Assert.AreEqual(3 * x, simplifier.Simplify(x + 2 * x));
        }

        [TestMethod]
        public void SimplificationWithSub()
        {
            Assert.AreEqual(0, simplifier.Simplify(x + x - (x + x)));

            Assert.AreEqual(x, simplifier.Simplify(x + x - x));

            Assert.AreEqual(6 * otherTerms, simplifier.Simplify((otherTerms + otherTerms) * 4 - otherTerms - otherTerms));

            Assert.AreEqual(-6 * otherTerms, simplifier.Simplify((otherTerms + otherTerms) * (-3)));

            Assert.AreEqual(-2 * ln(x) + otherTerms, simplifier.Simplify(otherTerms - ln(x) - ln(x)));

            Assert.AreEqual(-2 * ln(x), simplifier.Simplify((otherTerms + otherTerms) * (-3) + (otherTerms + otherTerms) * (3) - ln(x) - ln(x)));

            Assert.AreEqual(2, simplifier.Simplify(5 * x - 5 * x + 2));

            Assert.AreEqual(7, simplifier.Simplify(7 - 5 * x + 5 * x));
        }

        [TestMethod]
        public void Multivariate()
        {
            Assert.AreEqual(x + y, simplifier.Simplify(x + y));
            Assert.AreEqual(x + y, simplifier.Simplify(y + x));
            Assert.AreEqual(y + 2 * x, simplifier.Simplify(y + x + x));
            Assert.AreEqual(y, simplifier.Simplify(y + x - x));
            Assert.AreEqual(0, simplifier.Simplify(y + x - x - y));
            Assert.AreEqual(0, simplifier.Simplify(y - y + x - x));
            Assert.AreEqual(0, simplifier.Simplify(y + x - y - x));
            Assert.AreEqual("z" + otherTerms, simplifier.Simplify(y + x - y - x + "z" + otherTerms));

            Expression z = "z";
            Assert.AreEqual("5 * z", simplifier.Simplify(z + 3 * z - 2 * z + z + 2 * z + y - x - y + x));
            Assert.AreEqual(4 + 14 * y + 3 * x, simplifier.Simplify(5 * y + 4 + 3 * x + 5 * y + 4 * y));
        }

        [TestMethod]
        public void Exponential_Logs()
        {
            Assert.AreEqual(otherTerms, simplifier.Simplify(ln(e(otherTerms))));

            Assert.AreEqual(1 + otherTerms, simplifier.Simplify(ln(e(otherTerms + 1))));

            Assert.AreEqual(ln(otherTerms), simplifier.Simplify(ln(e(ln(otherTerms)))));

            Assert.AreEqual(ln(5), simplifier.Simplify(ln(e(ln(5)))));
        }
    }
}
