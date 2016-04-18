using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public abstract double Evaluate(IReadOnlyDictionary<Variable, double> context);

        /// <summary>
        /// Substitutes the given values for any matching variable names, use to partially evaluate an expession.
        /// </summary>
        /// <param name="values">the values of some collection of variables</param>
        /// <returns></returns>
        public Expression With(IReadOnlyDictionary<Variable, double> values)
        {
            Dictionary<Variable, Expression> newValues = new Dictionary<Variable, Expression>(values.Count);
            foreach (KeyValuePair<Variable, double> replacement in values)
            {
                newValues.Add(replacement.Key, new Constant(replacement.Value));
            }
            return this.With(new ReadOnlyDictionary<Variable, Expression>(newValues));
        }

        /// <summary>
        /// Substitutes the given values for any matching variable names, use to partially evaluate an expession.
        /// </summary>
        /// <param name="values">the values of some collection of variables</param>
        /// <returns></returns>
        public abstract Expression With(IReadOnlyDictionary<Variable, Expression> values);

        /// <summary>
        /// Find the symbolic derivative of the expression with respect to the given variable
        /// </summary>
        /// <param name="variable">the domain variable for this derivation</param>
        /// <returns></returns>
        public abstract Expression Derivative(Variable variable);

        public override int GetHashCode()
        {
            return GetType().GetHashCode();
        }

        #region operators

        public virtual Expression Add(Expression right)
        {
            var terms = new List<Expression>();
            terms.Add(this);
            if (right is Sum)
            {
                terms.AddRange((right as Sum).Arguments);
            } else
            {
                terms.Add(right);
            }
            return new Sum(terms);
        }
        public virtual Expression Sub(Expression right)
        {
            return this + right.Neg();
        }
        public virtual Expression Neg()
        {
            return new Negative(this);
        }
        public virtual Expression Mul(Expression right)
        {
            if (right is Product)
            {
                var terms = new List<Expression>();
                terms.Add(this);
                terms.AddRange((right as Product).Arguments);
                return new Product(terms);
            }
            return new Product(this, right);
        }
        public virtual Expression Div(Expression right)
        {
            return this.Mul(right.Inv());
        }
        public virtual Expression Inv()
        {
            return new Invert(this);
        }
        public virtual Expression Pow(Expression right)
        {
            return new Pow(this, right);
        }
        public virtual Expression Exp()
        {
            return new Exponential(this);
        }
        public virtual Expression Log()
        {
            return new Logarithm(this);
        }
        public virtual Expression Sin()
        {
            return new Sine(this);
        }
        public virtual Expression Cos()
        {
            return new Cosine(this);
        }
        public virtual Expression Tan()
        {
            return new Tangent(this);
        }

        public static Expression operator +(Expression left, Expression right) { return left.Add(right); }
        public static Expression operator -(Expression left, Expression right) { return left.Sub(right); }
        public static Expression operator -(Expression left) { return left.Neg(); }
        public static Expression operator *(Expression left, Expression right) { return left.Mul(right); }
        public static Expression operator /(Expression left, Expression right) { return left.Div(right); }
        public static Expression operator ^(Expression left, Expression right) { return left.Pow(right); }

        #endregion

        #region conversions

        public static implicit operator Expression(string name) { return new Variable(name); }

        public static implicit operator Expression(double value)
        {
            if (value < 0)
            {
                return new Constant(-value).Neg();
            }
            else {
                return new Constant(value);
            }
        }

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

        public override Expression Derivative(Variable variable)
        {
            return this.Equals(variable) ? 1 : 0;
        }

        public override double Evaluate(IReadOnlyDictionary<Variable, double> context)
        {
            return context[this];
        }

        public override Expression With(IReadOnlyDictionary<Variable, Expression> values)
        {
            if (values.ContainsKey(this))
            {
                return values[this];
            }
            return this;
        }

        public Expression With(double value)
        {
            return new Constant(value);
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
        public override int Complexity { get { return 0; } }

        public override int Height { get { return 1; } }

        public override bool IsConstant { get { return true; } }

        public override int Size { get { return 1; } }

        public override double Value { get; }

        public Constant(double value)
        {
            Value = value;
        }

        public override Expression Derivative(Variable variable)
        {
            return 0;
        }

        public override double Evaluate(IReadOnlyDictionary<Variable, double> context)
        {
            return Value;
        }

        public override Expression Inv()
        {
            if ((1 / Value) % 1.0 == 0)
            {
                return 1 / Value;
            }
            else
            {
                return new Invert(this);
            }
        }

        public override Expression Div(Expression right)
        {
            if (Value == 1)
            {
                return right.Inv();
            }
            else {
                return base.Div(right);
            }
        }

        public override Expression Neg()
        {
            return new Negative(Value);
        }

        public override Expression Log()
        {
            if (Value == 1)
            {
                return new Constant(0);
            } else
            {
                return new Logarithm(this);
            }
        }

        public override Expression With(IReadOnlyDictionary<Variable, Expression> values)
        {
            return this;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override bool Equals(object obj)
        {
            return (obj is Constant) && (obj as Constant).Value == this.Value;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ Value.GetHashCode();
        }
    }
}