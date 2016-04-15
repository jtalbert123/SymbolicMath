using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SymbolicMath.ExpressionHelper;

namespace SymbolicMath.Simplification
{
    public class Simplifier
    {
        public List<Rule> Pre { get; }
        public List<Rule> Processors { get; }
        public List<Rule> Post { get; }

        /// <summary>
        /// There should not be any subset of the rules {R0, R1, R2, ..., Rn} such that R0(R1(R2(...Rn(e)...))).Matches(R0) for any <see cref="Expression"/> e
        /// </summary>
        /// <param name="rules"></param>
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
                Rules.Constants.Exact
            };
            Post = new List<Rule>()
            {
                Rules.ReWrite.UnMakeCommutitave
            };
        }

        /// <summary>
        /// Applies the <see cref="Rule"/> with the highest priority to the expression and rematches on the rules until it does not match any.
        /// This method reauires that no rules 'conflict' or create a loop:
        /// <para>
        /// For every rule A and B in the set, there should not exist a rule B such that B(e).Matches(A) and A(e).Matches(B) for any <see cref="Expression"/> e.
        /// More generally: there should not be any subset of the rules {R0, R1, R2, ..., Rn} such that R0(R1(R2(...Rn(e)...))).Matches(R0) for any <see cref="Expression"/> e
        /// </para>
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
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

    public static class Rules
    {
        public static class ReWrite
        {
            /// <summary>
            /// Moves constants to the left: Smaller Literals->Larger Literals->Constants->Expressions 
            /// </summary>
            public static Rule LeftToRight { get; } = new DelegateRule(
                delegate (Expression e)
                {
                    Operator top = e as Operator;
                    if (top != null && top.Commutative)
                    {
                        Expression left = top.Left;
                        Expression right = top.Right;

                        if (left.Complexity > right.Complexity)
                        {
                            return true;
                        }
                        else if (left.Complexity == right.Complexity)
                        {
                            if (left.IsConstant && right.IsConstant)
                            {
                                if (left.Value > right.Value)
                                {
                                    return true;
                                }
                            }
                            else if (right.IsConstant)
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                },
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
                    return e;
                }, 100);

            public static Rule GroupConstants { get; } = new DelegateRule(
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
                                    return true;
                                    //return top.WithArgs(oRight.WithArgs(top.Left, oRight.Left), oRight.Right);
                                }
                            }
                        }
                    }
                    return false;
                },
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
                    return e;
                }, 95);

            public static Rule ExtractConstants { get; } = new DelegateRule(
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
                                    return true;
                                    //return top.WithArgs(oRight.WithArgs(top.Left, oRight.Left), oRight.Right);
                                }
                            }
                            else if (top.Right.GetType().Equals(top.GetType()))
                            {
                                Operator oRight = top.Right as Operator;
                                if (oRight.Left is Constant)
                                {
                                    return true;
                                    //return top.WithArgs(oRight.Left, oRight.WithArgs(top.Left, oRight.Right));
                                }
                            }
                        }
                    }
                    return false;
                },
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
                    return e;
                }, 95);

            public static Rule MakeCommutitave { get; } = new DelegateRule(
                delegate (Expression e)
                {
                    if (e is Operator)
                    {
                        Operator top = e as Operator;
                        if (!top.Commutative)
                        {
                            if (e is Sub)
                            {
                                return true;
                                // return new Add(top.Left, -top.Right);
                            }
                            else if (e is Div)
                            {
                                if (!(e as Div).Left.Equals(con(1)))
                                {
                                    return true;
                                    // return new Mul(top.Left, 1/top.Right);
                                }
                            }
                        }
                    }
                    return false;
                },
                delegate (Expression e)
                {
                    if (e is Operator)
                    {
                        Operator top = e as Operator;
                        if (!top.Commutative)
                        {
                            if (e is Sub)
                            {
                                return new Add(top.Left, -top.Right);
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
                    return e;
                }, 95);

            public static Rule UnMakeCommutitave { get; } = new DelegateRule(
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
                                    return true;
                                    // return new -Add(left.Argument, right.Argument);
                                }
                                if (right != null)
                                {
                                    return true;
                                    // return new Sub(top.Left, right.Argument);
                                }
                                else if (left != null)
                                {
                                    return true;
                                    // return new Sub(top.Right, left.Argument);
                                }
                            }
                            else if (e is Mul)
                            {
                                if (top.Right is Div)
                                {
                                    Div right = top.Right as Div;
                                    if (right.Left.Equals(new Constant(1)))
                                    {
                                        return true;
                                        // return new Div(top.Left, right.Right);
                                    }
                                }
                            }
                        }
                    }
                    return false;
                },
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
                                    return -new Add(left.Argument, right.Argument);
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
                    return e;
                }, 95);

            public static Rule ExtractNegs { get; } = new DelegateRule(
                delegate(Expression e)
                {
                    Constant cons = e as Constant;
                    if (cons != null && e.Value < 0)
                    {
                        return true;
                        //return -(new Constant(-cons.Value));
                    }
                    return false;
                },
                delegate (Expression e)
                {
                    Constant cons = e as Constant;
                    if (cons != null && e.Value < 0)
                    {
                        return -(new Constant(-cons.Value));
                    }
                    return e;
                },90);
        }

        public static class Constants
        {
            /// <summary>
            /// Evaluate constant Expressions that will not inherienly lose precision (will not evaluate 1/3 to .333333333)
            /// </summary>
            public static Rule Exact { get; } = new DelegateRule(
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
                                    return true;
                                    //return e.Evaluate();
                                }
                                else if (e is Div)
                                {
                                    if (top.Left.Value % top.Right.Value == 0)
                                    {
                                        return true;
                                        //return e.Evaluate();
                                    }
                                }
                                else if (e is Pow)
                                {
                                    if (top.Left.Value % 1 == 0 && top.Right.Value % 1 == 0)
                                    {
                                        return true;
                                        //return e.Evaluate();
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
                                    return false;
                                    //return e.Evaluate();
                                }
                                else if (e is Log && top.Argument.Value == 1)
                                {
                                    return true;
                                    //return e.Evaluate();
                                }
                            }
                        }
                    }
                    return false;
                },
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
                                    return e.Value;
                                }
                                else if (e is Log && top.Argument.Value == 1)
                                {
                                    return e.Value;
                                }
                            }
                        }
                    }
                    return e;
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
