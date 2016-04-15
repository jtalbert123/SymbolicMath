using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SymbolicMath.ExpressionHelper;

namespace SymbolicMath
{
    /// <summary>
    /// The generic case of an Expression, definnes the interface and some shared data.
    /// Expressions should be Pure (similar to immutable)
    /// </summary>
    [System.Diagnostics.Contracts.Pure]
    public abstract class Expression
    {
        /// <summary>
        /// Evaluate using the context provided at construction.
        /// </summary>
        /// <returns></returns>
        public double Evaluate()
        {
            return Evaluate(new Dictionary<string, double>());
        }

        public abstract bool IsConstant { get; }

        public abstract int Height { get; }

        public abstract int Size { get; }

        /// <summary>
        /// Evaluate using the given <see cref="ExpressionContext"/>
        /// </summary>
        /// <param name="context">the context to evaluate within</param>
        /// <returns></returns>
        public abstract double Evaluate(Dictionary<string, double> context);

        /// <summary>
        /// Find the symbolic derivative of the expression with respect to the given variable
        /// </summary>
        /// <param name="variable">the domain variable for this derivation</param>
        /// <returns></returns>
        public abstract Expression Derivative(string variable);

        #region operators

        public static Expression operator -(Expression arg) { return new Neg(arg); }

        public static Expression operator +(Expression left, Expression right) { return new Add(left, right); }

        public static Expression operator -(Expression left, Expression right) { return new Sub(left, right); }

        public static Expression operator *(Expression left, Expression right) { return new Mul(left, right); }

        public static Expression operator /(Expression left, Expression right) { return new Div(left, right); }

        public static Expression operator ^(Expression left, Expression right) { return new Pow(left, right); }

        #endregion

        #region conversions

        public static implicit operator Expression(string name) { return new Variable(name); }

        public static implicit operator Expression(double value) { return new Constant(value); }

        #endregion
    }

    public class Variable : Expression
    {
        public string Name { get; }

        public override bool IsConstant { get { return false; } }

        public override int Height { get { return 1; } }

        public override int Size { get { return 1; } }

        public Variable(string name)
        {
            Name = name;
        }

        public override Expression Derivative(string variable)
        {
            return Name.Equals(variable) ? 1 : 0;
        }

        public override double Evaluate(Dictionary<string, double> context)
        {
            return context[Name];
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            return (obj is Variable) && (obj as Variable).Name.Equals(Name);
        }
    }

    public class Constant : Expression
    {
        public double Value { get; }

        public override bool IsConstant { get { return true; } }

        public override int Height { get { return 1; } }

        public override int Size { get { return 1; } }

        public Constant(double value)
        {
            Value = value;
        }

        public override Expression Derivative(string variable)
        {
            return 0;
        }

        public override double Evaluate(Dictionary<string, double> context)
        {
            return Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override bool Equals(object obj)
        {
            return (obj is Constant) && (obj as Constant).Value.Equals(Value);
        }
    }

    public static class ExpressionHelper
    {
        public static Expression ln(Expression e)
        {
            return new Log(e);
        }

        public static Expression e(Expression e)
        {
            return new Exp(e);
        }

        public static Expression sin(Expression e)
        {
            return new Sin(e);
        }

        public static Expression cos(Expression e)
        {
            return new Cos(e);
        }

        public static Expression tan(Expression e)
        {
            return new Tan(e);
        }
    }
}