using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SymbolicMath.ExpressionHelper;
using SymbolicMath.Extensions;

namespace SymbolicMath
{
    /// <summary>
    /// An abstract representation of a function with some number of arguments. 
    /// </summary>
    /// <remarks>
    /// There is no built in way to cap the number, but throwing an 
    /// <see cref="ArgumentException"/> exception is acceptable.
    /// the mValue property is left to the implementation to set due to the need to loop over all arguments in most cases.
    /// </remarks>
    internal abstract class PolyFunction : Expression, IEnumerable<Expression>
    {
        public override int Complexity { get; }

        public override int Height { get; }

        public override bool IsConstant { get; }

        public override int Size { get; }

        protected abstract double mValue { get; }

        public override double Value
        {
            get
            {
                if (!IsConstant)
                {
                    throw new InvalidOperationException("This Function is not constant");
                }
                else
                {
                    return mValue;
                }
            }
        }
        public abstract bool Associative { get; }
        public abstract bool Commutative { get; }

        public IReadOnlyList<Expression> Arguments { get; }

        protected PolyFunction(IList<Expression> args)
        {
            Complexity = 0;
            Height = 0;
            IsConstant = true;
            Size = 0;
            List<Expression> argsList = new List<Expression>(args.Count);
            foreach (Expression e in args)
            {
                Complexity += e.Complexity + 1;
                Height = Math.Max(Height, e.Height);
                IsConstant &= e.IsConstant;
                Size += e.Size + 1;
                argsList.Add(e);
            }
            ++Height;
            Arguments = argsList.AsReadOnly();
        }

        /// <summary>
        /// Creates a list of the arguments with the items from the actual arguments list, that can be modified.
        /// </summary>
        /// <returns></returns>
        public List<Expression> CopyArgs()
        {
            List<Expression> newArgs = new List<Expression>(Arguments.Count);
            foreach (Expression e in Arguments)
            {
                newArgs.Add(e);
            }
            return newArgs;
        }

        public override Expression With(IReadOnlyDictionary<Variable, Expression> values)
        {
            List<Expression> newArgs = CopyArgs();
            newArgs = newArgs.ConvertAll(x => x.With(values));
            return With(newArgs);
        }

        public Expression With(int index, Expression replacement)
        {
            if (index > Arguments.Count)
            {
                throw new ArgumentOutOfRangeException("Cannot replace an argument that does not exist");
            }
            else if (index == Arguments.Count)
            {
                return this.With(replacement);
            }
            else
            {
                var args = CopyArgs();
                args[index] = replacement;
                return With(args);
            }
        }

        public abstract Expression With(List<Expression> args);

        /// <summary>
        /// Adds the given expression to the end of this <see cref="PolyFunction"/>'s argument list.
        /// </summary>
        /// <param name="tail">the new expression</param>
        /// <returns></returns>
        public Expression With(Expression tail)
        {
            List<Expression> terms = CopyArgs();
            if (tail is Product)
            {
                foreach (Expression term in tail as Product)
                {
                    terms.Add(term);
                }
            }
            else {
                terms.Add(tail);
            }
            return new Product(terms);
        }

        public IEnumerator<Expression> GetEnumerator()
        {
            return Arguments.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Arguments.GetEnumerator();
        }

        public override int GetHashCode()
        {
            int hashCode = base.GetHashCode();
            hashCode = Arguments.Fold((Expression e, int hash) => hash ^ e.GetHashCode(), 0);
            return hashCode;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == this.GetType())
            {
                PolyFunction that = obj as PolyFunction;
                if (this.Arguments.Count == that.Arguments.Count)
                {
                    for (int i = 0; i < Arguments.Count; ++i)
                    {
                        if (!this.Arguments[i].Equals(that.Arguments[i]))
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Represents a series of terms summed together.
    /// </summary>
    /// <remarks>
    /// If a term is negative, then ToString prepends a - instead of a + in the series.
    /// </remarks>
    internal class Sum : PolyFunction
    {
        public override bool Associative { get; } = true;
        public override bool Commutative { get; } = true;

        protected override double mValue { get; }

        internal Sum(IList<Expression> args) : base(args)
        {
            mValue = 0;
            foreach (Expression e in args)
            {
                if (e.IsConstant)
                {
                    mValue += e.Value;
                }
            }
        }

        internal Sum(params Expression[] args) : this(args.ToList()) { }

        public override Expression Derivative(Variable variable)
        {
            List<Expression> terms = new List<Expression>(Arguments.Count);
            foreach (Expression e in Arguments)
            {
                terms.Add(e.Derivative(variable));
            }
            return sum(terms);
        }

        public override double Evaluate(IReadOnlyDictionary<Variable, double> context)
        {
            return Arguments.Fold((Expression e, double sum) => sum + e.Evaluate(context), 0);
        }

        public override Expression With(List<Expression> args)
        {
            if (args.Count == 1)
            {
                return args[0];
            }
            return sum(args);
        }

        public override Expression Add(Expression right)
        {
            var terms = this.CopyArgs();
            if (right is Sum)
            {
                terms.AddRange((right as Sum).Arguments);
            } else
            {
                terms.Add(right);
            }
            return new Sum(terms);
        }

        public override Expression Neg()
        {
            var terms = new List<Expression>(Arguments.Count);
            foreach (Expression e in this)
            {
                terms.Add(e.Neg());
            }
            return new Sum(terms);
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder("(");
            bool first = true;
            foreach (Expression e in Arguments)
            {
                if (first && e is Negative)
                {
                    result.Append("-");
                }
                if (!first)
                {
                    if (e is Negative)
                    {
                        result.Append(" - ");
                    }
                    else
                    {
                        result.Append(" + ");
                    }
                }

                if (e is Negative)
                {
                    result.Append((e as Negative).Argument.ToString());
                }
                else
                {
                    result.Append(e.ToString());
                }
                first = false;
            }
            result.Append(")");
            return result.ToString();
        }
    }

    /// <summary>
    /// A representation of a series of terms multiplied together.
    /// </summary>
    internal class Product : PolyFunction
    {
        public override bool Associative { get; } = true;
        public override bool Commutative { get; } = true;

        protected override double mValue { get; }

        internal Product(IList<Expression> args) : base(args)
        {
            mValue = args.Count > 0 ? 1 : 0;
            foreach (Expression e in args)
            {
                if (e.IsConstant)
                {
                    mValue *= e.Value;
                }
            }
        }

        internal Product(params Expression[] args) : this(args.ToList()) { }

        public override Expression Derivative(Variable variable)
        {
            if (Arguments.Count == 1)
            {
                return Arguments[0].Derivative(variable);
            } else if (Arguments.Count == 2)
            {
                var u = Arguments[0];
                var du = u.Derivative(variable);
                var v = Arguments[1];
                var dv = v.Derivative(variable);
                return v * du + u * dv;
            } else
            {
                var u = Arguments[0];
                var du = u.Derivative(variable);
                var v = new Product(Arguments.Skip(1).ToList());
                var dv = v.Derivative(variable);
                return v * du + u * dv;
            }
        }

        public override double Evaluate(IReadOnlyDictionary<Variable, double> context)
        {
            return Arguments.Fold((Expression e, double product) => product * e.Evaluate(context), 1);
        }

        public override Expression With(List<Expression> args)
        {
            if (args.Count == 1)
            {
                return args[0];
            }
            return mul(args);
        }

        public override Expression Mul(Expression right)
        {
            return this.With(right);
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder("(");
            bool first = true;
            foreach (Expression e in Arguments)
            {
                if (first && e is Invert)
                {
                    result.Append("1/");
                }
                if (!first)
                {
                    if (e is Invert)
                    {
                        result.Append(" / ");
                    }
                    else
                    {
                        result.Append(" * ");
                    }
                }

                if (e is Invert)
                {
                    result.Append((e as Invert).Argument.ToString());
                }
                else
                {
                    result.Append(e.ToString());
                }
                first = false;
            }
            result.Append(")");
            return result.ToString();
        }
    }

    namespace Extensions
    {
        internal static class FoldList
        {
            public static TResult Fold<TSource, TResult>(this IEnumerable<TSource> seq, Func<TSource, TResult, TResult> evaluator, TResult initial)
            {
                TResult result = initial;
                foreach (TSource e in seq)
                {
                    result = evaluator(e, result);
                }
                return result;
            }
        }
    }
}