using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SymbolicMath;
using System.Collections.Generic;
using static SymbolicMath.ExpressionHelper;

namespace SymMathTests
{
    [TestClass]
    public class Constants
    {
        Random rand = new Random();
        double delta = 1e-5;

        [TestMethod]
        public void Construction()
        {
            double a = rand.NextDouble() / rand.NextDouble();
            Console.WriteLine(a);
            Expression @const = a;
            Assert.IsInstanceOfType(@const, typeof(Constant));
            Assert.AreEqual(a.ToString(), @const.ToString());
            Assert.AreEqual(0, @const.Derivative(""));
        }

        [TestMethod]
        public void Combination()
        {
            double a = rand.NextDouble() / rand.NextDouble();
            double b = rand.NextDouble() / rand.NextDouble();
            Console.WriteLine(a);
            Console.WriteLine(b);
            Expression A = a;
            Expression B = b;
            Assert.AreEqual($"({A} + {B})", (A + B).ToString());
            Assert.AreEqual($"({A} - {B})", (A - B).ToString());
            Assert.AreEqual($"({A} * {B})", (A * B).ToString());
            Assert.AreEqual($"({A} / {B})", (A / B).ToString());
            Assert.AreEqual($"({A} ^ {B})", (A ^ B).ToString());


            Assert.AreEqual(a + b, (A + B).Value, delta);
            Assert.AreEqual(a - b, (A - B).Value, delta);
            Assert.AreEqual(a * b, (A * B).Value, delta);
            Assert.AreEqual(a / b, (A / B).Value, delta);
        }

        [TestMethod]
        public void Derivation()
        {
            double a = rand.NextDouble() / rand.NextDouble();
            Expression @const = a;
            Assert.AreEqual(0, @const.Derivative(""));

        }

        [TestMethod]
        public void Evaluation()
        {
            Expression I = 1;
            Expression II = 2;
            Expression I_5 = 1.5;
            Assert.AreEqual(4.5, (I + II + I_5).Value);
            Assert.AreEqual(4.5 * 1.5, ((I + II + I_5) * I_5).Value);
        }
    }
}
