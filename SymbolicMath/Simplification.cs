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
                Rules.ReWrite.AddFold,
                Rules.Constants.Exact,
                Rules.Constants.Mul_aDivb,
                Rules.Identites.Add0,
                Rules.Identites.Mul0,
                Rules.Identites.Mul1,
                Rules.Identites.Div1,
                Rules.Identites.DivSelf,
                Rules.Identites.AddSelf,
                Rules.Identites.MulNeg,
                Rules.Identites.LnExp
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

                        if (right.GetType() == top.GetType())
                        {
                            Operator oRight = right as Operator;
                            if (left.Complexity > oRight.Left.Complexity && (left.IsConstant == oRight.Left.IsConstant || (!left.IsConstant && oRight.Left.IsConstant)))
                            {
                                return top.With(oRight.Left, oRight.With(left, oRight.Right));
                            }
                            if (!left.IsConstant && oRight.Left.IsConstant)
                            {
                                return top.With(oRight.Left, oRight.With(left, oRight.Right));
                            }
                        }
                        else if (left.GetType() == top.GetType())
                        {
                            Operator oLeft = left as Operator;
                            if (!left.IsConstant)
                            {
                                //((a b) c)->(a (b c))
                                return top.With(oLeft.Left, oLeft.With(oLeft.Right, right));
                            }
                        }
                        if (left.Complexity > right.Complexity && (left.IsConstant == right.IsConstant || (!left.IsConstant && right.IsConstant)))
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
                            if (top.Left.IsConstant && top.Right.GetType().Equals(top.GetType()))
                            {
                                Constant cLeft = top.Left as Constant;
                                Operator oRight = top.Right as Operator;

                                if (oRight.Left.IsConstant && !(oRight.Right.IsConstant))
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
                                if (top.Left is Constant && top.Left.Value < 0)
                                {
                                    top = top.With(new Neg(-top.Left.Value), top.Right);
                                }
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
                    else if (e is Function)
                    {
                        Function top = e as Function;
                        Expression arg = top.Argument;
                        if (e is Neg)
                        {
                            if (arg is Mul)
                            {
                                Mul product = arg as Mul;
                                if (product.Left.IsConstant)
                                {
                                    return product.With(-product.Left, product.Right);
                                }
                            }
                            else if (arg is Constant)
                            {
                                return -arg.Value;
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

            public static Rule AddFold { get; } = new SimpleDelegateRule(
                delegate (Expression e)
                {
                    if (e is Add)
                    {
                        Add top = e as Add;
                        if (top.Right is Mul)
                        {
                            Mul right = top.Right as Mul;
                            if (top.Left.Equals(right.Right))
                            {
                                return new Mul(1 + right.Left, top.Left);
                            }
                        }
                        else if (top.Right is Neg)
                        {// (a + (-b))
                            Neg nRight = top.Right as Neg;
                            if (top.Left is Mul)
                            {//(a*b + (-c))
                                Mul left = top.Left as Mul;
                                if (left.Right.Equals(nRight.Argument))
                                {//(a*b + (-b))
                                    return left.With(left.Left + (-1), left.Right);
                                }
                            }
                        }
                        else if (top.Right is Add)
                        {//(a + (b + c))
                            Add right = top.Right as Add;
                            if (top.Left.Equals(right.Left))
                            {//(a + (a + b))
                                return top.With(2 * top.Left, right.Right);
                            }
                        }
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

            public static Rule AddSelf { get; } = new SimpleDelegateRule(
                delegate (Expression e)
                {
                    Add top = e as Add;
                    if (top != null)
                    {
                        if (top.Right.Equals(top.Left))
                        {
                            return 2 * top.Left;
                        }
                        else if (top.Right is Neg)
                        {
                            Neg right = top.Right as Neg;
                            if (top.Left.Equals(right.Argument))
                            {
                                return 0;
                            }
                        }
                    }
                    return null;
                }, 90);

            public static Rule MulNeg { get; } = new SimpleDelegateRule(
                delegate (Expression e)
                {
                    Mul top = e as Mul;
                    if (top != null)
                    {
                        Neg nLeft = top.Left as Neg;
                        Neg nRight = top.Right as Neg;
                        if (nLeft != null && nRight != null)
                        {//((-a) * (-b))->(a * b)
                            return top.With(nRight.Argument, nLeft.Argument);
                        }
                        else if (nLeft == null && nRight != null)
                        {
                            return new Neg(top.With(top.Left, nRight.Argument));
                        }
                        else if (nLeft != null && nRight == null)
                        {
                            return new Neg(top.With(nLeft.Argument, top.Right));
                        }
                    }
                    return null;
                }, 90);

            public static Rule LnExp { get; } = new SimpleDelegateRule(
                delegate (Expression e)
                {
                    if (e is Function)
                    {
                        Function f = e as Function;
                        if (e is Log)
                        {
                            if (f.Argument is Exp)
                            {
                                return (f.Argument as Function).Argument;
                            }
                        }
                        else if (e is Exp)
                        {
                            if (f.Argument is Log)
                            {
                                return (f.Argument as Function).Argument;
                            }
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
                                    {// (a / b) with b | a
                                        return e.Value;
                                    }
                                    else if (top.Right.Value / top.Left.Value % 1 == 0 && top.Left.Value != 1)
                                    {// (a / b) with a | b
                                        return new Div(1, top.Right.Value / top.Left.Value);
                                    }
                                    else if (top.Right.Value.IsInt() && top.Left.Value.IsInt())
                                    { // (int / int)
                                        double gcd = GCD(top.Left.Value, top.Right.Value);
                                        if (gcd > 1)
                                        {
                                            return new Div(top.Left.Value / gcd, top.Right.Value / gcd);
                                        }
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

            public static Rule Mul_aDivb { get; } = new SimpleDelegateRule(
                delegate (Expression e)
                {
                    if (e.IsConstant)
                    {
                        if (e is Mul)
                        {
                            Operator top = e as Operator;
                            if (top.Left is Constant && top.Right is Div)
                            {
                                Div right = top.Right as Div;
                                return right.With(top.Left * right.Left, right.Right);
                            }
                        }
                    }
                    return null;
                }, 90);

            //public static Rule All { get; } = new SimpleDelegateRule(e => (Exact.Transform(e) == null) ? Mul_aDivb.Transform(e) : Exact.Transform(e));
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
    }
}
