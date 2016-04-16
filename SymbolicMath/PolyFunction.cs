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

        private readonly double m_value;

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
                    return m_value;
                }
            }
        }
        public abstract bool Associative { get; }
        public abstract bool Commutitive { get; }

        public IReadOnlyList<Expression> Arguments { get; }

        protected PolyFunction(IList<Expression> args, double mValue)
        {
            m_value = mValue;

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

        public override Expression With(Dictionary<string, double> values)
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
        public abstract Expression With(Expression tail);

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
            hashCode = Arguments.Fold((Expression e, int hash) => hash ^ e.GetHashCode());
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
        public override bool Commutitive { get; } = true;

        public Sum(IList<Expression> args) : base(args, args.Fold((Expression e, double sum) => sum + (e.IsConstant ? e.Value : 0))) { }

        public Sum(params Expression[] args) : this(args.ToList()) { }

        public override Expression Derivative(string variable)
        {
            List<Expression> terms = new List<Expression>(Arguments.Count);
            foreach (Expression e in Arguments)
            {
                terms.Add(e.Derivative(variable));
            }
            return With(terms);
        }

        public override double Evaluate(Dictionary<string, double> context)
        {
            return Arguments.Fold((Expression e, double sum) => sum += e.Evaluate(context));
        }

        public override Expression With(Expression tail)
        {
            List<Expression> args = CopyArgs();
            args.Add(tail);
            return this.With(args);
        }

        public override Expression With(List<Expression> args)
        {
            return new Sum(args);
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder("(");
            bool first = true;
            foreach (Expression e in Arguments)
            {
                if (!first)
                {
                    if (e is Neg)
                    {
                        result.Append(" - ");
                    }
                    else
                    {
                        result.Append(" + ");
                    }
                }

                if (e is Neg)
                {
                    result.Append((e as Neg).Argument.ToString());
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

        public static Expression operator +(Sum left, Expression newTerm)
        {
            if (newTerm is Sum)
            {
                return merge(left, newTerm as Sum);
            }
            else
            {
                return left.With(newTerm);
            }
        }

        public static Expression operator +(Expression newTerm, Sum right)
        {
            if (newTerm is Sum)
            {
                return merge(newTerm as Sum, right);
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
        public static TResult Fold<TSource, TResult>(this IEnumerable<TSource> seq, Func<TSource, TResult, TResult> evaluator)
        {
            TResult result = default(TResult);
            foreach (TSource e in seq)
            {
                result = evaluator(e, result);
            }
            return result;
        }
    }
}