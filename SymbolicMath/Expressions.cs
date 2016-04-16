﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SymbolicMath.ExpressionHelper;

namespace SymbolicMath
{
    /// <summary>
    /// The generic case of an Expression, defines the interface and some shared data.
    /// Expressions are Immutable, and thus thread safe.
    /// </summary>
    [System.Diagnostics.Contracts.Pure]
    public abstract class Expression
    {

        /// <summary>
        /// If this flag is set, then the value of this <see cref="Expression"/> will never change, 
        /// also the <see cref="Value"/> property will be defined and usable.
        /// </summary>
        public abstract bool IsConstant { get; }

        /// <summary>
        /// The constant value of the function. Should only be called if IsConstant is set.
        /// </summary>
        /// <remarks>
        /// If the expression is not constant this will throw an <see cref="InvalidOperationException"/>.
        /// The value returned may not be exact, and so the expression should be simplified even if it is constant if precision is needed.
        /// eg. The expression (3 * (1/3)) is constant, but it's Value is not exactly 1 (it's .999999...).
        /// </remarks>
        public abstract double Value { get; }

        /// <summary>
        /// The maximum number of nodes from this <see cref="Expression"/> to a leaf (variable or constant).
        /// eg (3 * (1/3)).Height is 3 (*, /, 1) or (*, /, 3)
        /// </summary>
        public abstract int Height { get; }

        /// <summary>
        /// The number of nodes in a <see cref="Expression"/> eg. (3 * (1/3)).Size is 5
        /// </summary>
        public abstract int Size { get; }

        /// <summary>
        /// A measure of the mathematical complexity of an <see cref="Expression"/>, 
        /// constants do not contribute, but constant expressions do.
        /// eg. (3 * (1/3)).Complexity is 2, but (x * (1/x)).Complexity is 4. 
        /// Used by simplification to reorder expressions.
        /// </summary>
        public abstract int Complexity { get; }

        /// <summary>
        /// Evaluate using the given set of variable values. May throw an excpetion if not all values are provided.
        /// </summary>
        /// <param name="context">the context to evaluate within</param>
        /// <returns></returns>
        public abstract double Evaluate(Dictionary<string, double> context);

        /// <summary>
        /// Substitutes the given values for any matching variable names, use to partially evaluate an expession.
        /// </summary>
        /// <param name="values">the values of some collection of variables</param>
        /// <returns></returns>
        public abstract Expression With(Dictionary<string, double> values);

        /// <summary>
        /// Find the symbolic derivative of the expression with respect to the given variable
        /// </summary>
        /// <param name="variable">the domain variable for this derivation</param>
        /// <returns></returns>
        public abstract Expression Derivative(string variable);

        /// <summary>
        /// Find the symbolic derivative of the expression with respect to the given variable
        /// </summary>
        /// <param name="variable">the domain variable for this derivation</param>
        /// <returns></returns>
        public Expression Derivative(Variable var)
        {
            return Derivative(var.Name);
        }

        public override int GetHashCode()
        {
            return GetType().GetHashCode();
        }

        #region operators

        public static Expression operator -(Expression arg) { return new Neg(arg); }

        public static Expression operator +(Expression left, Expression right)
        {
            if (left is Sum && right is Sum)
            {
                return merge(left as Sum, right as Sum);
            }
            else if (left is Sum)
            {
                return (left as Sum).With(right);
            }
            else if (right is Sum)
            {
                Sum set = right as Sum;
                var terms = set.CopyArgs();
                terms.Insert(0, left);
                return set.With(terms);
            }
            else {
                return new Sum(left, right);
            }
        }

        public static Expression operator -(Expression left, Expression right)
        {
            if (right is Sum)
            {
                Sum rSet = right as Sum;
                var terms = new List<Expression>();
                // create a set with negated terms
                foreach (Expression e in rSet)
                {
                    if (e is Neg)
                    {
                        terms.Add((e as Neg).Argument);
                    } else
                    {
                        terms.Add(-e);
                    }
                }
                return left + rSet.With(terms);
            }
            else {
                return left + (-right);
            }
        }

        public static Expression operator *(Expression left, Expression right) { return new Mul(left, right); }

        public static Expression operator /(Expression left, Expression right) { return new Div(left, right); }

        public static Expression operator ^(Expression left, Expression right) { return new Pow(left, right); }

        #endregion

        #region conversions

        public static implicit operator Expression(string name) { return new Variable(name); }

        public static implicit operator Expression(double value) { return new Constant(value); }

        #endregion
    }

    /// <summary>
    /// An elementary variable. Each variable has a name.
    /// The <see cref="Value"/> property always throws an exception
    /// </summary>
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
            if (name == null)
            {
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

        public override Expression With(Dictionary<string, double> values)
        {
            if (values.ContainsKey(Name))
            {
                return values[Name];
            }
            return this;
        }

        public Expression With(double value)
        {
            return value;
        }

        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object obj)
        {
            return (obj is Variable) && (obj as Variable).Name.Equals(Name);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ base.GetHashCode();
        }

        public static implicit operator Variable(string name) { return new Variable(name); }
    }

    /// <summary>
    /// A constant represents a numeric literal, and has no children.
    /// </summary>
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

        public override Expression With(Dictionary<string, double> values)
        {
            return this;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override bool Equals(object obj)
        {
            return (obj is Constant) && (obj as Constant).Value.Equals(Value);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode() ^ base.GetHashCode();
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
        public static Neg neg(Expression expr)
        {
            return new Neg(expr);
        }

        public static Expression sum(params Expression[] terms)
        {
            if (terms.Length == 0)
            {
                return 0;
            }
            else if (terms.Length == 1)
            {
                return terms[0];
            }
            List<Expression> args = new List<Expression>(terms.Length);
            foreach (Expression e in terms)
            {
                args.Add(e);
            }
            return new Sum(args);
        }

        public static Expression sum(List<Expression> terms)
        {
            if (terms.Count == 0)
            {
                return 0;
            }
            else if (terms.Count == 1)
            {
                return terms[0];
            }
            List<Expression> args = new List<Expression>(terms.Count);
            foreach (Expression e in terms)
            {
                args.Add(e);
            }
            return new Sum(args);
        }

        public static Sum merge(Sum left, Sum right)
        {
            return new Sum(left.Concat(right).ToList());
        }
    }
}