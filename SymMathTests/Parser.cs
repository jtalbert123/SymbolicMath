using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SymbolicMath;
using static SymbolicMath.ExpressionHelper;
using SymbolicMath.Simplification;
using static SymbolicMath.Infix;

[TestClass]
public class ParserTests
{
    static ISimplifier simplifier { get; } = new Simplifier();
    static Variable x = "x";
    static Variable y = "y";
    static Expression I = 1;
    static Expression II = 2;

    [ClassInitialize]
    public static void SetUp(TestContext context)
    {
        Assert.AreEqual(0, Parse("0"));
    }

    [TestMethod]
    public void Constants()
    {
        Assert.AreEqual(I + I, Parse("1 + 1"));
        Assert.AreEqual(I + I / II, Parse("1 + 1/2"));
        Assert.AreEqual(I * 7 + 3 / II, Parse("1*7 + 3/2"));
        Assert.AreEqual(I + I + I + I + I + I + I, Parse("1 + 1 + 1 + 1 + 1 + 1 + 1"));
        Assert.AreEqual(I * I * I * II * I * I * I, Parse("1 * 1 * 1 * 2 * 1 * 1 * 1"));
        Assert.AreEqual((I + .125).Value, Parse("1.125"));
    }

    [TestMethod]
    public void Variables()
    {
        Assert.AreEqual(x, Parse("x"));
        Assert.AreEqual(x + x, Parse("x + x"));
        Assert.AreEqual(2 * x, Parse("2*x"));
        Assert.AreEqual(2 * x + y, Parse("2*x + y"));
        Assert.AreEqual(2 * x + y + "otherStuff", Parse("2*x + y + otherStuff"));
    }

    [TestMethod]
    public void Functions()
    {
        Assert.AreEqual(sin(x), Parse("sin(x)"));
        Assert.AreEqual(cos(x), Parse("cos(x)"));
        Assert.AreEqual(tan(x), Parse("tan(x)"));
        Assert.AreEqual(ln(x), Parse("ln(x)"));
        Assert.AreEqual(ln(x), Parse("log(x)"));
        Assert.AreEqual(e(x), Parse("e(x)"));
        Assert.AreEqual(e(x), Parse("e^(x)"));
        Assert.AreEqual(-x, Parse("n(x)"));
        Assert.AreEqual(-x, Parse("~x"));
    }

    [TestMethod]
    public void FunctionsOfExpressions()
    {
        Assert.AreEqual(sin(x+3), Parse("sin(x+3)"));
        Assert.AreEqual(cos(x/y), Parse("cos(x/y)"));
        Assert.AreEqual(tan(5*x), Parse("tan(5*x)"));
        Assert.AreEqual(ln(x^2), Parse("ln(x^2)"));
        Assert.AreEqual(ln(x-y/3), Parse("log(x-y/3)"));
        Assert.AreEqual(e(x)*ln(x), Parse("e(x)*ln(x)"));
        Assert.AreEqual(e(x*ln(x)), Parse("e^(x*ln(x))"));
    }
}