using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SymbolicMath;
using static SymbolicMath.ExpressionHelper;
using System.Collections.Generic;

namespace SymMathTests
{
    [TestClass]
    public class Functions
    {
        Random rand = new Random();
        double delta = 1e-5;

        [TestMethod]
        public void OfConstants()
        {
            double a = rand.NextDouble() / rand.NextDouble();
            Expression expr = a;
            Expression sinx = "sin(x)";
            Expression cosx = "cos(x)";
            Expression tanx = "tan(x)";
            Expression ex = "e^(x)";
            Expression lnx = "ln(x)";
            Expression invx = "x";
            invx = invx.Inv();
            Dictionary<Variable, Expression> xToA = new Dictionary<Variable, Expression>() { ["x"] = a };
            Assert.AreEqual(sin(a), sinx.With(xToA));
            Assert.AreEqual(cos(a), cosx.With(xToA));
            Assert.AreEqual(tan(a), tanx.With(xToA));
            Assert.AreEqual(e(a), ex.With(xToA));
            Assert.AreEqual(ln(a), lnx.With(xToA));
            Assert.AreEqual(log(a), lnx.With(xToA));
            Assert.AreEqual(inv(a), invx.With(xToA));

            Assert.AreEqual($"sin({a})", sinx.With(xToA).ToString());
            Assert.AreEqual($"cos({a})", cosx.With(xToA).ToString());
            Assert.AreEqual($"tan({a})", tanx.With(xToA).ToString());
            Assert.AreEqual($"e^({a})", ex.With(xToA).ToString());
            Assert.AreEqual($"ln({a})", lnx.With(xToA).ToString());
            Assert.AreEqual($"(1/{a})", invx.With(xToA).ToString());

            Assert.AreEqual(Math.Sin(a), sinx.With(xToA).Value);
            Assert.AreEqual(Math.Cos(a), cosx.With(xToA).Value);
            Assert.AreEqual(Math.Tan(a), tanx.With(xToA).Value);
            Assert.AreEqual(Math.Exp(a), ex.With(xToA).Value);
            Assert.AreEqual(Math.Log(a), lnx.With(xToA).Value);
            Assert.AreEqual(1 / a, invx.With(xToA).Value);

            Assert.AreEqual(Math.Sin(a), sinx.With(xToA).Evaluate(null));
            Assert.AreEqual(Math.Cos(a), cosx.With(xToA).Evaluate(null));
            Assert.AreEqual(Math.Tan(a), tanx.With(xToA).Evaluate(null));
            Assert.AreEqual(Math.Exp(a), ex.With(xToA).Evaluate(null));
            Assert.AreEqual(Math.Log(a), lnx.With(xToA).Evaluate(null));
            Assert.AreEqual(1 / a, invx.With(xToA).Evaluate(null));
        }

        [TestMethod]
        public void OfVariables()
        {
            Expression a = "a";
            Expression sinx = "sin(x)";
            Expression cosx = "cos(x)";
            Expression tanx = "tan(x)";
            Expression ex = "e^(x)";
            Expression lnx = "ln(x)";
            Expression invx = "x";
            invx = invx.Inv();
            Dictionary<Variable, Expression> xToA = new Dictionary<Variable, Expression>() { ["x"] = a };
            Dictionary<Variable, double> xTo0 = new Dictionary<Variable, double>() { ["x"] = 0 };
            Assert.AreEqual(sin(a), sinx.With(xToA));
            Assert.AreEqual(cos(a), cosx.With(xToA));
            Assert.AreEqual(tan(a), tanx.With(xToA));
            Assert.AreEqual(e(a), ex.With(xToA));
            Assert.AreEqual(ln(a), lnx.With(xToA));
            Assert.AreEqual(log(a), lnx.With(xToA));
            Assert.AreEqual(inv(a), invx.With(xToA));

            Assert.AreEqual($"sin({a})", sinx.With(xToA).ToString());
            Assert.AreEqual($"cos({a})", cosx.With(xToA).ToString());
            Assert.AreEqual($"tan({a})", tanx.With(xToA).ToString());
            Assert.AreEqual($"e^({a})", ex.With(xToA).ToString());
            Assert.AreEqual($"ln({a})", lnx.With(xToA).ToString());
            Assert.AreEqual($"(1/{a})", invx.With(xToA).ToString());

            MyAssert.Throws<InvalidOperationException, double>(() => sinx.With(xToA).Value);
            MyAssert.Throws<InvalidOperationException, double>(() => cosx.With(xToA).Value);
            MyAssert.Throws<InvalidOperationException, double>(() => tanx.With(xToA).Value);
            MyAssert.Throws<InvalidOperationException, double>(() => ex.With(xToA).Value);
            MyAssert.Throws<InvalidOperationException, double>(() => lnx.With(xToA).Value);
            MyAssert.Throws<InvalidOperationException, double>(() => invx.With(xToA).Value);

            Assert.AreEqual(Math.Sin(0), sinx.Evaluate(xTo0));
            Assert.AreEqual(Math.Cos(0), cosx.Evaluate(xTo0));
            Assert.AreEqual(Math.Tan(0), tanx.Evaluate(xTo0));
            Assert.AreEqual(Math.Exp(0), ex.Evaluate(xTo0));
            Assert.AreEqual(Math.Log(0), lnx.Evaluate(xTo0));
            Assert.AreEqual(double.PositiveInfinity, invx.Evaluate(xTo0));
        }

        [TestMethod]
        public void Combinations()
        {
            Expression x = "x";
            Expression I = "1";
            Assert.AreEqual("x", x.Inv().Inv());
            Assert.AreEqual("x", -(-x));
            Assert.AreEqual("x", -x.Neg());
            Assert.AreEqual("x", ~x.Neg());
            Assert.AreEqual("x", ~-x);
            Assert.AreEqual("~x", ~x);
            Assert.AreEqual("x", ~~x);
            Assert.AreEqual("(-x)", x.Neg().ToString());

            Assert.AreEqual(-1, I.Neg().Evaluate(null));
        }

        [TestMethod]
        public void PolyFunctions()
        {
            Expression prod = "x*y*5";
            Expression sum = "x+y+5";
            Assert.AreEqual(mul("x", "y", 5), "x*y*5");
            var xToConst = new Dictionary<Variable, Expression>()
            {
                ["x"] = 1,
                ["y"] = 2
            };
            Assert.AreEqual(mul("1", "2", 5), prod.With(xToConst));
            Assert.AreEqual(0, prod.With(xToConst).Derivative("x"));
            Assert.AreEqual("5*y", prod.Derivative("x"));
            Assert.AreEqual("1", mul("x").Derivative("x"));
            Assert.AreEqual(0, sum.With(xToConst).Derivative("x"));
            Assert.AreEqual(1, sum.Derivative("x"));
            MyAssert.Throws<InvalidOperationException, double>(() => prod.Value);

            Assert.IsTrue((prod as PolyFunction).Associative);
            Assert.IsTrue((prod as PolyFunction).Commutative);

            Assert.IsTrue((sum as PolyFunction).Associative);
            Assert.IsTrue((sum as PolyFunction).Commutative);

            Assert.AreEqual("(1/5 * 3)", mul(inv(5), 3).ToString());
            Assert.AreEqual("(-5 + 3)", neg(5).Add(3).ToString());
        }
    }
}
