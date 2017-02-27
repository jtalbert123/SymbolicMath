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
    /// Normalizes and simplifies <see cref="Expression"/>s bu repeated applying rules to them.
    /// </summary>
    /// <remarks>
    /// Simplify(e) will be mathematically equivalent to e.
    /// In most cases, the result of Simplify(e) should have a lower height, size, and/or complexity value than e, 
    /// However some expressions may be expanded. Generally less complex expressions should appear on the left after simplification.
    /// </remarks>
    public class Simplifier : ISimplifier
    {
        public static ISimplifier Instance { get; } = new Simplifier();

        public List<IRule> Pre { get; }
        public List<IRule> Processors { get; }
        public List<IRule> Post { get; }

        private Dictionary<Expression, Expression> PreCache;
        private Dictionary<Expression, Expression> ProcessingCache;
        private Dictionary<Expression, Expression> PostCache;

        /// <summary>
        /// The memorization is most effective for common terms with high complexity,
        /// terms that are too complex tend to not be common, so we filter them out.
        /// </summary>
        private const int memorizationComplexityCap = 100;
        
        /// <summary>
        /// The memorization is most effective for common terms with high complexity,
        /// terms that are too simple provide minimal benifit so we filter them out.
        /// </summary>
        private const int memorizationComplexityMin = 5;

        internal Simplifier()
        {
            Pre = new List<IRule>()
            {
                Rules.ReOrder.ReOrderPoly,
                Rules.ReOrder.ReOrderOp,
            };
            Processors = new List<IRule>()
            {
                Rules.ReOrder.ReOrderPoly,
                Rules.ReOrder.ReOrderOp,
                Rules.Combine.LiteralSum,
                Rules.Combine.LiteralProduct,
                Rules.Combine.SumLike,
                Rules.Combine.ProdLike,
                Rules.ReOrder.CleanNegs,
                Rules.Identities.Add0,
                Rules.Identities.Mul0,
                Rules.Identities.Mul1,
                Rules.Identities.Pow1,
                Rules.Identities.Pow0,
                Rules.ReOrder.LevelProduct,
            };
            Post = new List<IRule>()
            {
                Rules.ReOrder.FixNegs,
                Rules.ReOrder.DivisionToEnd,
            };

            PreCache = new Dictionary<Expression, Expression>();
            ProcessingCache = new Dictionary<Expression, Expression>();
            PostCache = new Dictionary<Expression, Expression>();
        }

        Expression ISimplifier.Simplify(Expression e)
        {
            Expression simplified = ReWrite(e);
            simplified = Process(simplified);
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
                var terms = new List<Expression>(fn.Arguments.Count);
                foreach (Expression term in fn.Arguments)
                {
                    terms.Add(ApplyRules(term, Rules, memory));
                }
                simplified = fn.With(terms);
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
                if (simplified.Complexity < memorizationComplexityCap && simplified.Complexity > memorizationComplexityMin)
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

    /// <summary>
    /// A rule that matches against a specific type. Helps do a bit of filtering before the rule executes the provided delegate.
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
            else
            {
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
                });

            public static IRule DivisionToEnd { get; } = new TypeRule<Product>(
                delegate (Product prod)
                {
                    bool sorted = true;
                    bool hitDivision = false;
                    foreach (Expression e in prod)
                    {
                        if (e is Invert)
                        {
                            hitDivision = true;
                        }
                        else
                        {
                            if (hitDivision)
                            {
                                sorted = false;
                                break;
                            }
                        }
                    }
                    if (!sorted)
                    {
                        List<Expression> newTerms = prod.CopyArgs();
                        newTerms.Sort((a, b) =>
                        {
                            if (a is Invert && b is Invert)
                            {
                                return 0;
                            }
                            else if (a is Invert)
                            {
                                return 1;
                            }
                            else if (b is Invert)
                            {
                                return -1;
                            }
                            else
                            {
                                return 0;
                            }
                        });
                        return prod.With(newTerms);
                    }
                    return null;
                });

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
                });

            public static IRule CleanNegs { get; } = new TypeRule<Product>(
                delegate (Product set)
                {
                    var terms = new List<Expression>(set.Arguments.Count);
                    bool negative = false;
                    int flipped = 0;
                    bool hasConstant = false;
                    bool constantWasNeg = false;
                    int constantIndex = 0;
                    foreach (Expression term in set)
                    {
                        if (term is Negative)
                        {
                            terms.Add((term as Negative).Argument);
                            negative = !negative;
                            flipped++;
                            if ((term as Negative).Argument is Constant)
                            {
                                hasConstant = true;
                                constantWasNeg = true;
                            }
                        }
                        else
                        {
                            terms.Add(term);
                            if (term is Constant)
                            {
                                hasConstant = true;
                            }
                        }
                        if (!hasConstant)
                        {
                            constantIndex++;
                        }
                    }
                    if (negative)
                    {
                        if (!hasConstant)
                        {
                            terms[0] = terms[0].Neg();
                        }
                        else
                        {
                            terms[constantIndex] = terms[constantIndex].Neg();
                        }
                    }
                    if (flipped > 1)
                    {
                        return new Product(terms);
                    }
                    else if (flipped == 1 && hasConstant && !constantWasNeg)
                    {
                        return new Product(terms);
                    }
                    return null;
                });

            public static IRule FixNegs { get; } = new TypeRule<Product>(
                delegate (Product set)
                {
                    if (set.Arguments.Count == 2)
                    {
                        if (set.Arguments[0].Equals(new Constant(1).Neg()))
                        {
                            return set.Arguments[1].Neg();
                        }
                    }
                    return null;
                });

            public static IRule LevelProduct { get; } = new TypeRule<Product>(
                delegate (Product set)
                {
                    var terms = new List<Expression>();
                    bool changed = false;
                    foreach (Expression term in set)
                    {
                        if (term is Product)
                        {
                            terms.AddRange((term as Product).Arguments);
                            changed = true;
                        }
                        else
                        {
                            terms.Add(term);
                        }
                    }
                    if (changed)
                    {
                        return set.With(terms);
                    }
                    return null;
                });
        }

        public static class Identities
        {
            public static IRule Mul0 { get; } = new TypeRule<Product>(
                delegate (Product set)
                {
                    foreach (Expression term in set)
                    {
                        if (term.Equals(new Constant(0)))
                        {
                            return new Constant(0);
                        }
                        else if (term.Equals(new Negative(new Constant(0))))
                        {
                            return new Constant(0);
                        }
                    }
                    return null;
                });

            public static IRule Mul1 { get; } = new TypeRule<Product>(
                delegate (Product set)
                {
                    var terms = new List<Expression>();
                    foreach (Expression term in set)
                    {
                        if (!term.Equals(new Constant(1)))
                        {
                            terms.Add(term);
                        }
                    }
                    if (terms.Count < set.Arguments.Count)
                    {
                        if (terms.Count == 0)
                        {
                            return new Constant(0);
                        }
                        else if (terms.Count == 1)
                        {
                            return terms[0];
                        }
                        else
                        {
                            return mul(terms);
                        }
                    }
                    return null;
                });

            public static IRule Pow1 { get; } = new TypeRule<Power>(
                delegate (Power set)
                {
                    if (set.Right is Constant && set.Right.Value == 1)
                    {
                        return set.Left;
                    }
                    return null;
                });

            public static IRule Pow0 { get; } = new TypeRule<Power>(
                delegate (Power set)
                {
                    if (set.Right is Constant && set.Right.Value == 0)
                    {
                        return new Constant(1);
                    }
                    else if (set.Left is Constant && set.Left.Value == 0)
                    {
                        return new Constant(0);
                    }
                    return null;
                });

            public static IRule Add0 { get; } = new TypeRule<Sum>(
                delegate (Sum set)
                {
                    var terms = new List<Expression>();
                    foreach (Expression term in set)
                    {
                        if (!term.Equals(new Constant(0)))
                        {
                            terms.Add(term);
                        }
                    }
                    if (terms.Count < set.Arguments.Count)
                    {
                        if (terms.Count == 0)
                        {
                            return new Constant(0);
                        }
                        else if (terms.Count == 1)
                        {
                            return terms[0];
                        }
                        else
                        {
                            return sum(terms);
                        }
                    }
                    return null;
                });
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
                });

            public static IRule LiteralProduct { get; } = new TypeRule<Product>(
                delegate (Product e)
                {
                    double value = 1;
                    int literalsFound = 0;
                    List<Expression> newTerms = new List<Expression>(e.Arguments.Count);
                    foreach (Expression term in e)
                    {
                        if (term is Constant)
                        {
                            literalsFound++;
                            value *= term.Value;
                        }
                        else if (term is Negative && (term as Negative).Argument is Constant)
                        {
                            literalsFound++;
                            value *= term.Value;
                        }
                        else if (term is Invert && (term as Invert).Argument is Constant)
                        {
                            Invert inv = term as Invert;
                            int gcd = GCD(value, inv.Argument.Value);
                            if (gcd != 1)
                            {
                                value /= gcd;
                                inv = new Invert(inv.Argument.Value / gcd);
                            }
                            if (inv.Value != 1)
                            {
                                newTerms.Add(inv);
                            }
                        }
                        else
                        {
                            newTerms.Add(term);
                        }
                    }
                    if (literalsFound > 1)
                    {
                        newTerms.Insert(0, value);
                        return new Product(newTerms);
                    }
                    return null;
                });

            public static IRule SumLike { get; } = new TypeRule<Sum>(
                delegate (Sum sum)
                {
                    Dictionary<Expression, double> uniqueTerms = new Dictionary<Expression, double>();
                    bool changed = false;
                    foreach (Expression e in sum)
                    {
                        var term = e;
                        double multiplier = 1;
                        if (e is Negative)
                        {
                            multiplier = -1;
                            term = (e as Negative).Argument;
                        }
                        if (term is Product)
                        {
                            Product prod = term as Product;
                            if (prod.Arguments.Count >= 2 && prod.Arguments[0].IsConstant)
                            {
                                var coeffecient = prod.Arguments[0];
                                if (coeffecient is Constant)
                                {
                                    multiplier *= coeffecient.Value;
                                    term = mul(prod.Skip(1).ToList());
                                }
                                else if (coeffecient is Negative && (coeffecient as Negative).Argument is Constant)
                                {
                                    multiplier *= -(coeffecient as Negative).Argument.Value;
                                    term = mul(prod.Skip(1).ToList());
                                }
                            }
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
                            double multiplier = uniqueTerms[e];
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
                });

            public static IRule ProdLike { get; } = new TypeRule<Product>(
                delegate (Product sum)
                {
                    Dictionary<Expression, Expression> uniqueTerms = new Dictionary<Expression, Expression>();
                    bool changed = false;
                    foreach (Expression e in sum)
                    {
                        var term = e;
                        Expression exponent = 1;
                        if (e is Invert)
                        {
                            exponent = -1;
                            term = (e as Invert).Argument;
                        }
                        else if (e is Power)
                        {
                            Power pow = e as Power;
                            exponent = pow.Right;
                            term = pow.Left;
                        }
                        if (uniqueTerms.ContainsKey(term))
                        {
                            changed = true;
                            uniqueTerms[term] += exponent;
                        }
                        else
                        {
                            uniqueTerms.Add(term, exponent);
                        }
                    }
                    if (changed)
                    {
                        var terms = new List<Expression>();
                        foreach (var e in uniqueTerms.Keys)
                        {
                            Expression exponent = uniqueTerms[e];
                            if (exponent.IsConstant && exponent.Value == 0)
                            {
                                // do not add the term
                            }
                            else if (exponent.IsConstant && exponent.Value == 1)
                            {
                                terms.Add(e);
                            }
                            else if (exponent.IsConstant && exponent.Value == -1)
                            {
                                terms.Add(e.Inv());
                            }
                            else
                            {
                                terms.Add(e.Pow(exponent));
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
                            return new Product(terms);
                        }
                    }
                    return null;
                });
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

        public static Expression Simplify(this Expression e)
        {
            return Simplifier.Instance.Simplify(e);
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
                else if (a is Variable && b is Variable)
                {
                    return (a as Variable).Name.CompareTo((b as Variable).Name);
                }
                return 0;
            };
    }
}
