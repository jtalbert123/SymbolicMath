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

        public abstract bool IsConstant { get; }

        /// <summary>
        /// Returns the constant value of the function.
        /// Should only be called if IsConstant returns true.
        /// </summary>
        public abstract double Value { get; }

        public abstract int Height { get; }

        public abstract int Size { get; }

        public abstract int Complexity { get; }

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

        public override double Value { get { throw new InvalidOperationException("Variable.Value is not defined"); } }

        public override int Height { get { return 1; } }

        public override int Size { get { return 1; } }

        public override int Complexity { get { return 1; } }

        public Variable(string name)
        {
            if (name == null) {
                throw new ArgumentNullException("Variable names cannot be null");
            }
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

        public static implicit operator Variable(string name) { return new Variable(name); }
    }

    public class Constant : Expression
    {
        public override bool IsConstant { get { return true; } }

        public override double Value { get; }

        public override int Height { get { return 1; } }

        public override int Size { get { return 1; } }

        public override int Complexity { get { return 0; } }

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

        public static implicit operator Constant(double value) { return new Constant(value); }
    }

    public static class ExpressionHelper
    {
        public static Log ln(Expression e)
        {
            return new Log(e);
        }

        public static Exp e(Expression e)
        {
            return new Exp(e);
        }

        public static Sin sin(Expression e)
        {
            return new Sin(e);
        }

        public static Cos cos(Expression e)
        {
            return new Cos(e);
        }

        public static Tan tan(Expression e)
        {
            return new Tan(e);
        }

        public static Variable var(string name)
        {
            return new Variable(name);
        }

        public static Constant con(double val)
        {
            return new Constant(val);
        }
    }
}