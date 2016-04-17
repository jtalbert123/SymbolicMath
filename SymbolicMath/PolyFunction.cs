using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SymbolicMath.ExpressionHelper;

namespace SymbolicMath
{
    public abstract class PolyFunction : Expression, IEnumerable<Expression>
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
                throw new ArgumentOutOfRangeException("Cannot replace a summand that does not exist");
            }
            else if (index == Arguments.Count)
            {
                return this.With(replacement);
            }
            else
            {
                var args = CopyArgs();
                args[index] = replacement;
                return this.With(args);
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
            List<Expression> args = CopyArgs();
            args.Add(tail);
            return this.With(args);
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

    public class Sum : PolyFunction
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
            return With(terms);
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
            return new Sum(args);
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

    public class Product : PolyFunction
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
            throw new NotImplementedException();
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
            return new Product(args);
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

        public static Expression operator *(Product left, Expression newTerm)
        {
            if (newTerm is Product)
            {
                List<Expression> newArgs = new List<Expression>(left.Arguments.Count + (newTerm as Product).Arguments.Count);
                foreach (Expression e in left)
                {
                    newArgs.Add(e);
                }
                foreach (Expression e in (newTerm as Product))
                {
                    newArgs.Add(e);
                }
                return left.With(newArgs);
            }
            else
            {
                return left.With(newTerm);
            }
        }

        public static Expression operator *(Expression newTerm, Product right)
        {
            if (newTerm is Product)
            {
                List<Expression> newArgs = new List<Expression>(right.Arguments.Count + (newTerm as Product).Arguments.Count);
                foreach (Expression e in newTerm as Product)
                {
                    newArgs.Add(e);
                }
                foreach (Expression e in right)
                {
                    newArgs.Add(e);
                }
                return right.With(newArgs);
            }
            else
            {
                var args = right.CopyArgs();
                args.Insert(0, newTerm);
                return right.With(args);
            }
        }
    }

    static class FoldList
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