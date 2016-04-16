using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SymbolicMath.ExpressionHelper;

namespace SymbolicMath.Simplification
{
    /// <summary>
    /// A utility class to simplify expressions.
    /// </summary>
    /// <remarks>
    /// Simplify(e) will be mathematically equivalent to e.
    /// In most cases, the result of Simplify(e) should have a lower height, size, and/or complexity value than e, 
    /// However some expressions may be expanded. Generally less complex expressions should appear on the left after simplification.
    /// </remarks>
    public class Simplifier
    {
        public List<Rule> Pre { get; }
        public List<Rule> Processors { get; }
        public List<Rule> Post { get; }

        public Simplifier()
        {
            Pre = new List<Rule>()
            {
            };
            Processors = new List<Rule>()
            {
            };
            Post = new List<Rule>()
            {
            };
        }

        public Expression Simplify(Expression e)
        {
            Expression simplified = ReWrite(e);
            bool changed = false;
            do
            {
                Expression old = simplified;
                simplified = Process(simplified);
                changed = !old.Equals(simplified);
            } while (changed);
            simplified = Format(simplified);

            return simplified;
        }

        internal Expression ReWrite(Expression e)
        {
            return ApplyRules(e, Pre);
        }

        internal Expression Process(Expression e)
        {
            return ApplyRules(e, Processors);
        }

        internal Expression Format(Expression e)
        {
            return ApplyRules(e, Post);
        }

        private Expression ApplyRules(Expression e, List<Rule> Rules)
        {
            Expression simplified = e;
            if (simplified is Operator)
            {
                Operator op = simplified as Operator;
                simplified = op.With(ApplyRules(op.Left, Rules), ApplyRules(op.Right, Rules));
            }
            else if (simplified is Function)
            {
                Function fn = simplified as Function;
                simplified = fn.With(ApplyRules(fn.Argument, Rules));
            }
            else if (simplified is PolyFunction)
            {
                PolyFunction fn = simplified as PolyFunction;
                for (int i = 0; i < fn.Count; ++i)
                {
                    simplified = fn.With(i, ApplyRules(fn[i], Rules));
                    fn = simplified as PolyFunction;
                }
            }
            bool changed;
            do
            {
                changed = false;
                Rule highest = null;
                int priorityMax = -1;
                foreach (Rule rule in Rules)
                {
                    int priority;
                    if (simplified.Matches(rule, out priority) && priority > priorityMax)
                    {
                        priorityMax = priority;
                        highest = rule;
                    }
                }
                if (highest != null)
                {
                    simplified = highest.Transform(simplified);
                    changed = true;

                    //simplified = ApplyRules(simplified, Rules);
                }
            } while (changed);

            return simplified;
        }
    }

    public interface Rule
    {
        /// <summary>
        /// Returns the priority of the match, or a negative number if there was no match.
        /// High priority numbers take precedence than low priority numbers.
        /// The priority returned should represent the 'strength' of the simplification.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        int Match(Expression e);

        /// <summary>
        /// Contract: the provided expression must be a match for this rule -> the returned expression will be mathematically equivalent to the provided one.
        /// The provided expression will not be modified.
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        Expression Transform(Expression match);
    }

    public class DelegateRule : Rule
    {
        private Func<Expression, int> _matcher;
        private Func<Expression, Expression> _transform;

        public DelegateRule(Func<Expression, int> matcher, Func<Expression, Expression> transform)
        {
            _matcher = matcher;
            _transform = transform;
        }

        public DelegateRule(Func<Expression, bool> matcher, Func<Expression, Expression> transform, int priority = 1)
        {
            _matcher = ((Expression e) => matcher(e) ? priority : -priority);
            _transform = transform;
        }

        public int Match(Expression e)
        {
            return _matcher(e);
        }

        public Expression Transform(Expression match)
        {
            return _transform(match);
        }
    }

    /// <summary>
    /// Takes the transformation function and uses it to create a delegate rule.
    /// This reduces performance as the entire transformation is run for every match attempt,
    /// however, the increase in code simplicity provieds an acceptable trade-off for small functions.
    /// </summary>
    public class SimpleDelegateRule : DelegateRule
    {
        public SimpleDelegateRule(Func<Expression, Expression> transformer, int priority = 1) : base(e => transformer(e) != null, transformer, priority) { }
    }

    public static class Rules
    {
        public static bool Matches(this Expression e, Rule rule, out int priority)
        {
            priority = rule.Match(e);
            return priority >= 0;
        }

        public static bool Matches(this Expression e, Rule rule)
        {
            return rule.Match(e) >= 0;
        }

        private static bool IsInt(this double num)
        {
            return num % 1.0 == 0.0;
        }

        private static int GCD(double left, double right)
        {
            if (!left.IsInt() || !right.IsInt())
            {
                return 1;
            }

            int a = (int)left;
            int b = (int)right;
            int Remainder;

            while (b != 0)
            {
                Remainder = a % b;
                a = b;
                b = Remainder;
            }

            return a;
        }

        private static Comparison<Expression> ComplexityComaparator { get; } =
            delegate (Expression a, Expression b)
            {
                if (a is Constant ^ b is Constant)
                {// Constant on the left
                    return (a is Constant) ? -1 : 1;
                }
                else if (a.IsConstant ^ b.IsConstant)
                {// Constant on the left
                    return (a.IsConstant) ? -1 : 1;
                }
                else if (a.Complexity != b.Complexity)
                {// less complex on the left
                    return a.Complexity.CompareTo(b.Complexity);
                }
                else if (a.IsConstant && b.IsConstant)
                {// smaller value on the left
                    return a.Value.CompareTo(b.Value);
                }
                return 0;
            };
    }
}
