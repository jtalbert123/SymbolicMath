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
                Rules.ReWrite.MakeCommutitave,
                Rules.ReWrite.ExtractNegs
            };
            Processors = new List<Rule>()
            {
                Rules.ReWrite.LeftToRight,
                Rules.ReWrite.GroupConstants,
                Rules.ReWrite.ExtractConstants,
                Rules.Constants.Exact,
                Rules.Identites.Add0,
                Rules.Identites.Mul0,
                Rules.Identites.Mul1,
                Rules.Identites.Div1
            };
            Post = new List<Rule>()
            {
                Rules.ReWrite.UnMakeCommutitave
            };
        }

        public Expression Simplify(Expression e)
        {
            Expression simplified = ReWrite(e);
            simplified = Process(simplified);
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

                    simplified = ApplyRules(simplified, Rules);
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

    public class SimpleDelegateRule : DelegateRule
    {
        public SimpleDelegateRule(Func<Expression, Expression> transformer, int priority = 1) : base(e => transformer(e) != null, transformer, priority) {}
    }

    public static class Rules
    {
        public static class ReWrite
        {
            /// <summary>
            /// Moves constants to the left: Smaller Literals->Larger Literals->Constants->Expressions 
            /// </summary>
            public static Rule LeftToRight { get; } = new SimpleDelegateRule(
                delegate (Expression e)
                {
                    Operator top = e as Operator;
                    if (top != null && top.Commutative)
                    {
                        Expression left = top.Left;
                        Expression right = top.Right;

                        if (left.Complexity > right.Complexity)
                        {
                            return top.With(right, left);
                        }
                        else if (left.Complexity == right.Complexity)
                        {
                            if (left.IsConstant && right.IsConstant)
                            {
                                if (left.Value > right.Value)
                                {
                                    return top.With(right, left);
                                }
                            }
                            else if (right.IsConstant)
                            {
                                return top.With(right, left);
                            }
                        }
                    }
                    return null;
                }, 100);

            public static Rule GroupConstants { get; } = new SimpleDelegateRule(
                delegate (Expression e)
                {
                    if (e is Operator)
                    {
                        Operator top = e as Operator;
                        if (top.Commutative)
                        {
                            if (top.Left is Constant && top.Right.GetType().Equals(top.GetType()))
                            {
                                Constant cLeft = top.Left as Constant;
                                Operator oRight = top.Right as Operator;
                                if (oRight.Left is Constant && !(oRight.Right is Constant))
                                {
                                    return top.With(oRight.With(top.Left, oRight.Left), oRight.Right);
                                }
                            }
                        }
                    }
                    return null;
                }, 95);

            public static Rule ExtractConstants { get; } = new SimpleDelegateRule(
                delegate (Expression e)
                {
                    if (e is Operator)
                    {
                        Operator top = e as Operator;
                        if (top.Commutative)
                        {
                            if (top.Left is Constant && top.Right.GetType().Equals(top.GetType()))
                            {
                                Constant cLeft = top.Left as Constant;
                                Operator oRight = top.Right as Operator;
                                if (oRight.Left is Constant && !(oRight.Right is Constant))
                                {
                                    return top.With(oRight.With(top.Left, oRight.Left), oRight.Right);
                                }
                            }
                            else if (top.Right.GetType().Equals(top.GetType()))
                            {
                                Operator oRight = top.Right as Operator;
                                if (oRight.Left is Constant)
                                {
                                    return top.With(oRight.Left, oRight.With(top.Left, oRight.Right));
                                }
                            }
                        }
                    }
                    return null;
                }, 95);

            public static Rule MakeCommutitave { get; } = new SimpleDelegateRule(
                delegate (Expression e)
                {
                    if (e is Operator)
                    {
                        Operator top = e as Operator;
                        if (!top.Commutative)
                        {
                            if (e is Sub)
                            {
                                return new Add(top.Left, new Neg(top.Right));
                            }
                            else if (e is Div)
                            {
                                if (!(e as Div).Left.Equals(con(1)))
                                {
                                    return new Mul(top.Left, 1 / top.Right);
                                }
                            }
                        }
                    }
                    return null;
                }, 95);

            public static Rule UnMakeCommutitave { get; } = new SimpleDelegateRule(
                delegate (Expression e)
                {
                    if (e is Operator)
                    {
                        Operator top = e as Operator;
                        if (top.Commutative)
                        {
                            if (e is Add)
                            {
                                Neg left = top.Left as Neg;
                                Neg right = top.Right as Neg;
                                if (right != null && left != null)
                                {
                                    return new Neg(new Add(left.Argument, right.Argument));
                                }
                                if (right != null)
                                {
                                    return new Sub(top.Left, right.Argument);
                                }
                                else if (left != null)
                                {
                                    return new Sub(top.Right, left.Argument);
                                }
                            }
                            else if (e is Mul)
                            {
                                if (top.Right is Div)
                                {
                                    Div right = top.Right as Div;
                                    if (right.Left.Equals(new Constant(1)))
                                    {
                                        return new Div(top.Left, right.Right);
                                    }
                                }
                            }
                        }
                    }
                    return null;
                }, 95);

            public static Rule ExtractNegs { get; } = new SimpleDelegateRule(
                delegate (Expression e)
                {
                    Constant cons = e as Constant;
                    if (cons != null && e.Value < 0)
                    {
                        return new Neg(new Constant(Math.Abs(cons.Value)));
                    }
                    return null;
                }, 90);
        }

        public static class Identites
        {
            public static Rule Add0 { get; } = new SimpleDelegateRule(
                delegate (Expression e)
                {
                    Add top = e as Add;
                    if (top != null)
                    {
                        if (top.Left.Equals(con(0)))
                        {
                            return (top.Right);
                        }
                        else if (top.Right.Equals(con(0)))
                        {
                            return (top.Left);
                        }
                    }
                    return null;
                }, 90);

            public static Rule Mul0 { get; } = new SimpleDelegateRule(
                delegate (Expression e)
                {
                    Mul top = e as Mul;
                    if (top != null)
                    {
                        if (top.Left.Equals(con(0)))
                        {
                            return 0;
                        }
                        else if (top.Right.Equals(con(0)))
                        {
                            return 0;
                        }
                    }
                    return null;
                }, 90);

            public static Rule Mul1 { get; } = new SimpleDelegateRule(
                delegate (Expression e)
                {
                    Mul top = e as Mul;
                    if (top != null)
                    {
                        if (top.Left.Equals(con(1)))
                        {
                            return (top.Right);
                        }
                        else if (top.Right.Equals(con(1)))
                        {
                            return (top.Left);
                        }
                    }
                    return null;
                }, 90);

            public static Rule Div1 { get; } = new SimpleDelegateRule(
                delegate (Expression e)
                {
                    Div top = e as Div;
                    if (top != null)
                    {
                        if (top.Right.Equals(con(1)))
                        {
                            return (top.Left);
                        }
                    }
                    return null;
                }, 90);

            public static Rule DivSelf { get; } = new SimpleDelegateRule(
                delegate (Expression e)
                {
                    Div top = e as Div;
                    if (top != null)
                    {
                        if (top.Right.Equals(top.Left))
                        {
                            return 1;
                        }
                    }
                    return null;
                }, 90);
        }

        public static class Constants
        {
            /// <summary>
            /// Evaluate constant Expressions that will not inherienly lose precision (will not evaluate 1/3 to .333333333)
            /// </summary>
            public static Rule Exact { get; } = new SimpleDelegateRule(
                delegate (Expression e)
                {
                    if (e.IsConstant)
                    {
                        if (e is Operator)
                        {
                            Operator top = e as Operator;
                            if (top.Left is Constant && top.Right is Constant)
                            {
                                if (e is Add || e is Sub || e is Mul)
                                {
                                    return e.Value;
                                }
                                else if (e is Div)
                                {
                                    if (top.Left.Value % top.Right.Value == 0)
                                    {
                                        return e.Value;
                                    }
                                }
                                else if (e is Pow)
                                {
                                    if (top.Left.Value % 1 == 0 && top.Right.Value % 1 == 0)
                                    {
                                        return e.Value;
                                    }
                                }
                            }
                        }
                        else if (e is Function)
                        {
                            Function top = e as Function;
                            if (top.Argument is Constant)
                            {
                                if (e is Neg)
                                {
                                    //return e.Value;
                                    return null;
                                }
                                else if (e is Log && top.Argument.Value == 1)
                                {
                                    return e.Value;
                                }
                            }
                        }
                    }
                    return null;
                }, 90);
        }

        public static bool Matches(this Expression e, Rule rule, out int priority)
        {
            priority = rule.Match(e);
            return priority >= 0;
        }

        public static bool Matches(this Expression e, Rule rule)
        {
            return rule.Match(e) >= 0;
        }
    }
}
