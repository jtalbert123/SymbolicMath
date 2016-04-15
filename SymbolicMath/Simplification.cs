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
        public List<Rule> Rules { get; }

        public Simplifier() : this(
            Simplification.Rules.ReOrder.General,
            Simplification.Rules.ReOrder.GroupConstants,
            Simplification.Rules.Constants.Exact)
        { }

        /// <summary>
        /// There should not be any subset of the rules {R0, R1, R2, ..., Rn} such that R0(R1(R2(...Rn(e)...))).Matches(R0) for any <see cref="Expression"/> e
        /// </summary>
        /// <param name="rules"></param>
        public Simplifier(params Rule[] rules)
        {
            Rules = new List<Rule>(rules);
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
            Expression simplified = e;
            if (simplified is Operator)
            {
                Operator op = simplified as Operator;
                simplified = (Expression)Activator.CreateInstance(simplified.GetType(), Simplify(op.Left), Simplify(op.Right));
            }
            else if (simplified is Function)
            {
                Function fn = simplified as Function;
                simplified = (Expression)Activator.CreateInstance(simplified.GetType(), Simplify(fn.Argument));
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

                    simplified = Simplify(simplified);
                }
            } while (changed);

            return simplified;
        }
    }

    public abstract class Rule
    {
        /// <summary>
        /// Returns the priority of the match, or a negative number if there was no match.
        /// High priority numbers take precedence than low priority numbers.
        /// The priority returned should represent the 'strength' of the simplification.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public abstract int Match(Expression e);

        /// <summary>
        /// Contract: the provided expression must be a match for this rule -> the returned expression will be mathematically equivalent to the provided one.
        /// The provided expression will not be modified.
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        public abstract Expression Transform(Expression match);
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

        public override int Match(Expression e)
        {
            return _matcher(e);
        }

        public override Expression Transform(Expression match)
        {
            return _transform(match);
        }
    }

    public static class Rules
    {
        public static class ReOrder
        {
            /// <summary>
            /// Moves constants to the left: Smaller Literals->Larger Literals->Constants->Expressions 
            /// </summary>
            public static Rule General { get; } = new DelegateRule(
                delegate (Expression e)
                {
                    Operator top = e as Operator;
                    if (top != null && top.IsSymmetric)
                    {
                        Expression left = top.Left;
                        Expression right = top.Right;
                        if (left.IsConstant == right.IsConstant)
                        {// Simpler expressions on the left
                            if (left.Size > right.Size)
                            {
                                return true;
                                //return top.WithArgs(right, left);
                            }
                            else if (left.Size == right.Size)
                            {
                                if (left.Evaluate() > right.Evaluate())
                                {
                                    return true;
                                    //return top.WithArgs(right, left);
                                }
                            }
                        }
                        else if (!left.IsConstant && right.IsConstant)
                        { // Move the constant one to the left
                          // (f(x) op c)
                            return true;
                            // return top.WithArgs(right, left);
                        }
                        else if (left.IsConstant && right.IsConstant)
                        {
                            if (!(left is Constant) && (right is Constant))
                            { // (c op n)->(n op c)
                                return true;
                                // return top.WithArgs(right, left);
                            }
                            double leftVal = left.Evaluate();
                            double rightVal = left.Evaluate();
                            if (leftVal > rightVal)
                            {
                                return true;
                                // return top.WithArgs(right, left);
                            }
                        }
                    }
                    return false;
                },
                delegate (Expression e)
                {
                    Operator top = e as Operator;
                    if (top != null && top.IsSymmetric)
                    {
                        Expression left = top.Left;
                        Expression right = top.Right;
                        if (left.IsConstant == right.IsConstant)
                        {// Simpler expressions on the left
                            if (left.Size > right.Size)
                            {
                                return top.WithArgs(right, left);
                            }
                            else if (left.Size == right.Size)
                            {
                                if (left.Evaluate() > right.Evaluate())
                                {
                                    return top.WithArgs(right, left);
                                }
                            }
                        }
                        else if (!left.IsConstant && right.IsConstant)
                        { // Move the constant one to the left
                          // (f(x) op c)
                            return top.WithArgs(right, left);
                        }
                        else if (left.IsConstant && right.IsConstant)
                        {
                            if (!(left is Constant) && (right is Constant))
                            { // (c op n)->(n op c)
                                return top.WithArgs(right, left);
                            }
                            double leftVal = left.Evaluate();
                            double rightVal = left.Evaluate();
                            if (leftVal > rightVal)
                            {
                                return top.WithArgs(right, left);
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
                        if (top.IsSymmetric)
                        {
                            if (top.Left is Constant && top.Right.GetType().Equals(top.GetType()))
                            {
                                Constant cLeft = top.Left as Constant;
                                Operator oRight = top.Right as Operator;
                                if (oRight.Left is Constant && !(oRight is Constant))
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
                        if (top.IsSymmetric)
                        {
                            if (top.Left is Constant && top.Right.GetType().Equals(top.GetType()))
                            {
                                Constant cLeft = top.Left as Constant;
                                Operator oRight = top.Right as Operator;
                                if (oRight.Left is Constant && !(oRight.Right is Constant))
                                {
                                    return top.WithArgs(oRight.WithArgs(top.Left, oRight.Left), oRight.Right);
                                }
                            }
                        }
                    }
                    return e;
                }, 90);
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
                                } else if (e is Div)
                                {
                                    if (top.Left.Evaluate() % top.Right.Evaluate() == 0)
                                    {
                                        return true;
                                        //return e.Evaluate();
                                    }
                                }
                                else if (e is Pow)
                                {
                                    if (top.Left.Evaluate() % 1 == 0 &&  top.Right.Evaluate() % 1 == 0)
                                    {
                                        return true;
                                        //return e.Evaluate();
                                    }
                                }
                            }
                        } else if (e is Function)
                        {
                            Function top = e as Function;
                            if (top.Argument is Constant)
                            {
                                if (e is Neg)
                                {
                                    return true;
                                    //return e.Evaluate();
                                } else if (e is Log && top.Argument.Evaluate() == 1)
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
                                    return e.Evaluate();
                                }
                                else if (e is Div)
                                {
                                    if (top.Left.Evaluate() % top.Right.Evaluate() == 0)
                                    {
                                        return e.Evaluate();
                                    }
                                }
                                else if (e is Pow)
                                {
                                    if (top.Left.Evaluate() % 1 == 0 && top.Right.Evaluate() % 1 == 0)
                                    {
                                        return e.Evaluate();
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
                                    return e.Evaluate();
                                }
                                else if (e is Log && top.Argument.Evaluate() == 1)
                                {
                                    return e.Evaluate();
                                }
                            }
                        }
                    }
                    return e;
                }, 95);
        }

        public static bool Matches(this Expression e, Rule rule, out int priority)
        {
            priority = rule.Match(e);
            return priority >= 0;
        }
    }
}
