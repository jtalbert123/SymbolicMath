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

    [TestMethod]
    public void Functions()
    {
        Assert.AreEqual(sin(x), Infix.Parse("sin(x)"));
        Assert.AreEqual(cos(x), Infix.Parse("cos(x)"));
        Assert.AreEqual(tan(x), Infix.Parse("tan(x)"));
        Assert.AreEqual(ln(x), Infix.Parse("ln(x)"));
        Assert.AreEqual(ln(x), Infix.Parse("log(x)"));
        Assert.AreEqual(e(x), Infix.Parse("e(x)"));
        Assert.AreEqual(e(x), Infix.Parse("e^(x)"));
        Assert.AreEqual(-x, Infix.Parse("n(x)"));
    }

    [TestMethod]
    public void FunctionsOfExpressions()
    {
        Assert.AreEqual(sin(x+3), Infix.Parse("sin(x+3)"));
        Assert.AreEqual(cos(x/y), Infix.Parse("cos(x/y)"));
        Assert.AreEqual(tan(5*x), Infix.Parse("tan(5*x)"));
        Assert.AreEqual(ln(x^2), Infix.Parse("ln(x^2)"));
        Assert.AreEqual(ln(x-y/3), Infix.Parse("log(x-y/3)"));
        Assert.AreEqual(e(x)*ln(x), Infix.Parse("e(x)*ln(x)"));
        Assert.AreEqual(e(x*ln(x)), Infix.Parse("e^(x*ln(x))"));
    }
}