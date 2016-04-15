using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SymbolicMath;
using static SymbolicMath.ExpressionHelper;
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

            public override Expression Derivative(string variable)
            {
                return this;
            }

            public override double Evaluate(Dictionary<string, double> context)
            {
                return 0;
            }

            public override Expression With(Dictionary<string, double> values)
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
        }

        DummyExpression otherTerms { get; } = new DummyExpression();
        Simplifier simplifier { get; } = new Simplifier();
        Variable x = "x";
        Constant I = 1;

        [TestMethod]
        public void Order()
        {
            Expression @in = x + 1;
            Expression @out = 1 + x;
            Assert.AreEqual(@out, simplifier.Simplify(@in));

            @in = otherTerms + 1;
            @out = 1 + otherTerms;
            Assert.AreEqual(@out, simplifier.Simplify(@in));

            @in = otherTerms + x;
            @out = x + otherTerms;
            Assert.AreEqual(@out, simplifier.Simplify(@in));

            @out = 1 + (x + otherTerms);
            {
                @in = otherTerms + x + 1;
                Assert.AreEqual(@out, simplifier.Simplify(@in));
                @in = otherTerms + 1 + x;
                Assert.AreEqual(@out, simplifier.Simplify(@in));
            }

            @out = 2 + (x + otherTerms);
            {
                @in = otherTerms + x + 1 + 1;
                Assert.AreEqual(@out, simplifier.Simplify(@in));
                @in = otherTerms + 1 * 2 + x;
                Assert.AreEqual(@out, simplifier.Simplify(@in));
            }

            @out = x + ln(x);
            @in = x + ln(x);
            Assert.AreEqual(@out, simplifier.Simplify(@in));

            @out = x + ln(x);
            @in = ln(x) + x;
            Assert.AreEqual(@out, simplifier.Simplify(@in));

            @out = 1 + (x + (ln(x) + otherTerms));
            {
                @in = otherTerms + x + 1 + ln(x);
                Assert.AreEqual(@out, simplifier.Simplify(@in));
                @in = otherTerms + 1 + ln(x) + x;
                Assert.AreEqual(@out, simplifier.Simplify(@in));
                @in = 1 + ln(x) + otherTerms + x;
                Assert.AreEqual(@out, simplifier.Simplify(@in));
            }

            @out = (I / 3) + (x + (ln(x) + otherTerms));
            {
                @in = otherTerms + x + 1 * (I / 3) + ln(x);
                Assert.AreEqual(@out, simplifier.Simplify(@in));
                @in = otherTerms + I / 3 + ln(x) + x;
                Assert.AreEqual(@out, simplifier.Simplify(@in));
                @in = (3 * I) / 9 + ln(x) + otherTerms + x;
                Assert.AreEqual(@out, simplifier.Simplify(@in));
            }

            @out = (I / 3) + (x + (sin(x) + otherTerms));
            {
                @in = otherTerms + x + 1 * (I / 3) + sin(x);
                Assert.AreEqual(@out, simplifier.Simplify(@in));
                @in = otherTerms + I / 3 + sin(x) + x;
                Assert.AreEqual(@out, simplifier.Simplify(@in));
                @in = (3 * I) / 9 + sin(x) + otherTerms + x;
                Assert.AreEqual(@out, simplifier.Simplify(@in));
            }

            @out = (con(2) / 3) + (x + (sin(x) + otherTerms));
            {
                @in = otherTerms + x + 2 * (I / 3) + sin(x);
                Assert.AreEqual(@out, simplifier.Simplify(@in));
                @in = otherTerms + I / 3 + I / 3 + sin(x) + x;
                Assert.AreEqual(@out, simplifier.Simplify(@in));
                @in = (6 * I) / 9 + sin(x) + otherTerms + x;
                Assert.AreEqual(@out, simplifier.Simplify(@in));
            }
        }

        [TestMethod]
        public void Simplification()
        {
            Expression x = var("x");
            Expression e = (x + (1 + x));
            Expression I = 1;
            Expression II = 2;
            Simplifier simplifier = new Simplifier();

            Assert.AreEqual((1 + (2 * x)), simplifier.Simplify(e));

            Assert.AreEqual(5 + x, simplifier.Simplify(1 + ((1 + ((x + 1) + 1)) + 1)));

            Assert.AreEqual(2 + ((I / 5) * x), simplifier.Simplify(x / 5 + 1 + I));

            Assert.AreEqual(II / 5, simplifier.Simplify((I + I) / 5));

            //Constant is always on the left
            Assert.AreEqual(x - 5, simplifier.Simplify(x - (1 + 2 + 2)));

            Assert.AreEqual(5 - x, simplifier.Simplify((1 + 2 + 2) - x));

            Assert.AreEqual(5 - x, simplifier.Simplify((ln(1) + 1 + 2 + 2) - x));

            Assert.AreEqual(-x, simplifier.Simplify((ln(1) + 1 + 2 + 2) * ln(1) - x));

            Assert.AreEqual(con(0), simplifier.Simplify(((ln(1) + 1 + 2 + 2) - x) * 0));

            Assert.AreEqual(-x, simplifier.Simplify(((ln(1) + 1 + 2 + 2) * 0 - x) * 1));

            Assert.AreEqual(-x, simplifier.Simplify(((ln(1) + 1 + 2 + 2) * 0 - x) / 1));

            Assert.AreEqual(x, simplifier.Simplify(x / 1));

            Assert.AreEqual(2 * x, simplifier.Simplify(x * (II / I)));

            Assert.AreEqual(x, simplifier.Simplify(x / 2 * 2));

            Assert.AreEqual((4 * x), simplifier.Simplify(x + x + x + x));

            Assert.AreEqual((6 * otherTerms), simplifier.Simplify((otherTerms + otherTerms) * 3));

            Assert.AreEqual((0), simplifier.Simplify(x + x - (x + x)));

            Assert.AreEqual((x), simplifier.Simplify(x + x - x));

            Assert.AreEqual((6 * otherTerms), simplifier.Simplify((otherTerms + otherTerms) * 4 - otherTerms - otherTerms));

            Assert.AreEqual(-(6 * otherTerms), simplifier.Simplify((otherTerms + otherTerms) * (-3)));
        }
    }
}
