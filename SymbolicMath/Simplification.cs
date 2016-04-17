using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SymbolicMath.ExpressionHelper;

namespace SymbolicMath.Simplification
{

    public interface ISimplifier
    {
        Expression Simplify(Expression e);
        Expression Normalize(Expression e);
    }
    /// <summary>
    /// A utility class to simplify expressions.
    /// </summary>
    /// <remarks>
    /// Simplify(e) will be mathematically equivalent to e.
    /// In most cases, the result of Simplify(e) should have a lower height, size, and/or complexity value than e, 
    /// However some expressions may be expanded. Generally less complex expressions should appear on the left after simplification.
    /// </remarks>
    public class Simplifier : ISimplifier
    {
        public List<IRule> Pre { get; }
        public List<IRule> Processors { get; }
        public List<IRule> Post { get; }

        private Dictionary<Expression, Expression> PreCache;
        private Dictionary<Expression, Expression> ProcessingCache;
        private Dictionary<Expression, Expression> PostCache;

        public Simplifier()
        {
            Pre = new List<IRule>()
            {
                Rules.ReOrder.ReOrderPoly,
                Rules.ReOrder.ReOrderOp
            };
            Processors = new List<IRule>()
            {
                Rules.ReOrder.ReOrderPoly,
                Rules.ReOrder.ReOrderOp,
                Rules.Combine.LiteralSum,
                Rules.Combine.SumLike
            };
            Post = new List<IRule>()
            {
            };

            PreCache = new Dictionary<Expression, Expression>();
            ProcessingCache = new Dictionary<Expression, Expression>();
            PostCache = new Dictionary<Expression, Expression>();
        }

        Expression ISimplifier.Simplify(Expression e)
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

        Expression ISimplifier.Normalize(Expression e)
        {
            return ReWrite(e);
        }

        internal Expression ReWrite(Expression e)
        {
            return ApplyRules(e, Pre, PreCache);
        }

        internal Expression Process(Expression e)
        {
            return ApplyRules(e, Processors, ProcessingCache);
        }

        internal Expression Format(Expression e)
        {
            return ApplyRules(e, Post, PostCache);
        }

        private Expression ApplyRules(Expression e, List<IRule> Rules, Dictionary<Expression, Expression> memory)
        {
            Expression simplified = e;
            if (memory != null)
            {
                if (memory.ContainsKey(e))
                {
                    return memory[e];
                }
            }
            if (simplified is Operator)
            {
                Operator op = simplified as Operator;
                simplified = op.With(ApplyRules(op.Left, Rules, memory), ApplyRules(op.Right, Rules, memory));
            }
            else if (simplified is Function)
            {
                Function fn = simplified as Function;
                simplified = fn.With(ApplyRules(fn.Argument, Rules, memory));
            }
            else if (simplified is PolyFunction)
            {
                PolyFunction fn = simplified as PolyFunction;
                for (int i = 0; i < fn.Arguments.Count; ++i)
                {
                    simplified = fn.With(i, ApplyRules(fn.Arguments[i], Rules, memory));
                    fn = simplified as PolyFunction;
                }
            }
            bool changed;
            do
            {
                changed = false;
                IRule highest = null;
                int priorityMax = -1;
                foreach (IRule rule in Rules)
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

                    simplified = ApplyRules(simplified, Rules, memory);
                }
            } while (changed);

            if (memory != null)
            {
                if (memory.ContainsKey(e))
                {
                    Expression memorized = memory[e];
                    if (memorized.Complexity < simplified.Complexity)
                    {
                        simplified = memorized;
                    }
                    else if (simplified.Complexity < memorized.Complexity)
                    {
                        memory[e] = simplified;
                    }
                }
                else
                {
                    memory.Add(e, simplified);
                }
            }
            return simplified;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IRule
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
        /// Transform should not be called unless the prior call to this IRule object was the Match function.
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        Expression Transform(Expression match);
    }

    public class DelegateRule : IRule
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
    /// 
    /// </summary>
    public class TypeRule<T> : IRule where T : Expression
    {
        private Expression _transform;
        private T _transformed;
        private Func<T, Expression> _function;
        public int Priority { get; }

        public TypeRule(Func<T, Expression> transformer, int priority = 1)
        {
            _function = transformer;
            Priority = priority;
        }

        public int Match(Expression e)
        {
            if (e is T)
            {
                _transformed = e as T;
                _transform = _function(e as T);
                return _transform != null ? Priority : -Priority;
            }
            return -Priority;
        }

        public Expression Transform(Expression match)
        {
            if (_transformed.Equals(match))
            {
                System.Diagnostics.Debug.Assert(_transform != null);
                return _transform;
            }
            else {
                return match;
            }
        }
    }

    public static class Rules
    {
        public static class ReOrder
        {
            public static IRule ReOrderPoly { get; } = new TypeRule<PolyFunction>(
                delegate (PolyFunction poly)
                {
                    if (!poly.Commutative)
                    {
                        // Can only re-order commutative functions
                        return null;
                    }
                    bool sorted = true;
                    Expression last = poly.Arguments[0];
                    foreach (Expression e in poly)
                    {
                        if (ComplexityComaparator.Invoke(last, e) > 0)
                        {
                            sorted = false;
                            break;
                        }
                        last = e;
                    }
                    if (!sorted)
                    {
                        List<Expression> newTerms = poly.CopyArgs();
                        newTerms.Sort(ComplexityComaparator);
                        return poly.With(newTerms);
                    }
                    return null;
                }, 100);

            public static IRule ReOrderOp { get; } = new TypeRule<Operator>(
                delegate (Operator top)
                {
                    if (!top.Commutative)
                    {
                        // Can only re-order commutative operators
                        return null;
                    }
                    if (ComplexityComaparator.Invoke(top.Left, top.Right) > 0)
                    {
                        return top.With(top.Right, top.Left);
                    }
                    return null;
                }, 100);
        }

        public static class Combine
        {
            public static IRule LiteralSum { get; } = new TypeRule<Sum>(
                delegate (Sum e)
                {
                    double value = 0;
                    int literalsFound = 0;
                    List<Expression> newTerms = new List<Expression>(e.Arguments.Count);
                    foreach (Expression term in e)
                    {
                        if (term is Constant)
                        {
                            literalsFound++;
                            value += term.Value;
                        }
                        else if (term is Negative && (term as Negative).Argument is Constant)
                        {
                            literalsFound++;
                            value += term.Value;
                        }
                        else
                        {
                            newTerms.Add(term);
                        }
                    }
                    if (literalsFound > 1)
                    {
                        newTerms.Insert(0, value);
                        return e.With(newTerms);
                    }
                    return null;
                }, 50);

            public static IRule SumLike { get; } = new TypeRule<Sum>(
                delegate (Sum sum)
                {
                    Dictionary<Expression, int> uniqueTerms = new Dictionary<Expression, int>();
                    bool changed = false;
                    foreach (Expression e in sum)
                    {
                        var term = e;
                        int multiplier = 1;
                        if (e is Negative)
                        {
                            multiplier = -1;
                            term = (e as Negative).Argument;
                        }
                        if (uniqueTerms.ContainsKey(term))
                        {
                            changed = true;
                            uniqueTerms[term] += multiplier;
                        }
                        else
                        {
                            uniqueTerms.Add(term, multiplier);
                        }
                    }
                    if (changed)
                    {
                        var terms = new List<Expression>();
                        foreach (var e in uniqueTerms.Keys)
                        {
                            int multiplier = uniqueTerms[e];
                            if (multiplier > 0)
                            {
                                if (multiplier == 1)
                                {
                                    terms.Add(e);
                                }
                                else
                                {
                                    terms.Add(multiplier * e);
                                }
                            }
                            else if (multiplier < 0)
                            {
                                multiplier = -multiplier;
                                if (multiplier == 1)
                                {
                                    terms.Add(e.Neg());
                                }
                                else
                                {
                                    terms.Add(multiplier * e.Neg());
                                }
                            }
                        }
                        if (terms.Count == 0)
                        {
                            return 0;
                        }
                        else if (terms.Count == 1)
                        {
                            return terms[0];
                        }
                        else
                        {
                            return new Sum(terms);
                        }
                    }
                    return null;
                }, 50);
        }

        public static bool Matches(this Expression e, IRule rule, out int priority)
        {
            priority = rule.Match(e);
            return priority >= 0;
        }

        public static bool Matches(this Expression e, IRule rule)
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
