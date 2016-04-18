using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SymbolicMath;
using static SymbolicMath.ExpressionHelper;
using SymbolicMath.Simplification;
using System.Collections.Generic;

[TestClass]
public class ParserTests
{
    static ISimplifier simplifier { get; } = new Simplifier();
    static Variable x = "x";
    static Variable y = "y";
    static Expression I = 1;
    static Expression II = 2;

    [TestMethod]
    public void Constants()
    {
        Assert.AreEqual(I + I, Infix.Parse("1 + 1"));
        Assert.AreEqual(I + I / II, Infix.Parse("1 + 1/2"));
        Assert.AreEqual(I * 7 + 3 / II, Infix.Parse("1*7 + 3/2"));
        Assert.AreEqual(I + I + I + I + I + I + I, Infix.Parse("1 + 1 + 1 + 1 + 1 + 1 + 1"));
        Assert.AreEqual(I * I * I * II * I * I * I, Infix.Parse("1 * 1 * 1 * 2 * 1 * 1 * 1"));
        Assert.AreEqual((I + .125).Value, Infix.Parse("1.125"));
    }

    [TestMethod]
    public void Variables()
    {
        Assert.AreEqual(x, Infix.Parse("x"));
        Assert.AreEqual(x + x, Infix.Parse("x + x"));
        Assert.AreEqual(2 * x, Infix.Parse("2*x"));
        Assert.AreEqual(2 * x + y, Infix.Parse("2*x + y"));
        Assert.AreEqual(2 * x + y + "otherStuff", Infix.Parse("2*x + y + otherStuff"));
    }
}