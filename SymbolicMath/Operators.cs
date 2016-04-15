using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SymbolicMath.ExpressionHelper;

namespace SymbolicMath
{
    /// <summary>
    /// An operator has two arguments (Expressions) and combines them in some way.
    /// </summary>
    public abstract class Operator : Expression
    {
        public Expression Left { get; }

        public Expression Right { get; }

        public override bool IsConstant { get { return Left.IsConstant && Right.IsConstant; } }

        public override int Height { get { return Math.Max(Left.Height, Right.Height) + 1; } }

        public override int Size { get { return Left.Size + Right.Size + 1; } }

        public override int Complexity { get { return Left.Complexity + Right.Complexity + 1; } }

        public abstract bool Commutative { get; }

        public abstract bool Associative { get; }

        public Operator(Expression left, Expression right)
        {
            Left = left;
            Right = right;
        }

        public T GetLeft<T>() where T : Expression
        {
            return (Left as T);
        }

        public T GetRight<T>() where T : Expression
        {
            return (Right as T);
        }

        public abstract Operator WithArgs(Expression left, Expression right);

        public override bool Equals(object obj)
        {
            return (obj.GetType() == this.GetType()) &&
                (
                    (obj as Operator).Left.Equals(this.Left) &&
                    (obj as Operator).Right.Equals(this.Right)
                );
        }
    }

    public class Add : Operator
    {
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
                    return Left.Value + Right.Value;
                }
            }
        }

        public override bool Commutative { get { return true; } }

        public override bool Associative { get { return true; } }

        public Add(Expression left, Expression right) : base(left, right) { }

        public override Expression Derivative(string variable)
        {
            return Left.Derivative(variable) + Right.Derivative(variable);
        }

        public override double Evaluate(Dictionary<string, double> context)
        {
            return Left.Evaluate(context) + Right.Evaluate(context);
        }

        public override Operator WithArgs(Expression left, Expression right)
        {
            return new Add(left, right);
        }

        public override string ToString()
        {
            return $"({Left.ToString()} + {Right.ToString()})";
        }

        public override bool Equals(object obj)
        {
            return (obj.GetType() == this.GetType()) &&
                (
                    (
                        (obj as Operator).Left.Equals(this.Left) &&
                        (obj as Operator).Right.Equals(this.Right)
                    ) ||
                    (
                        (obj as Operator).Left.Equals(this.Right) &&
                        (obj as Operator).Right.Equals(this.Left)
                    )
                );
        }
    }

    public class Sub : Operator
    {
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
                    return Left.Value - Right.Value;
                }
            }
        }

        public override bool Commutative { get { return false; } }

        public override bool Associative { get { return false; } }

        public Sub(Expression left, Expression right) : base(left, right) { }

        public override Expression Derivative(string variable)
        {
            return Left.Derivative(variable) - Right.Derivative(variable);
        }

        public override double Evaluate(Dictionary<string, double> context)
        {
            return Left.Evaluate(context) - Right.Evaluate(context);
        }

        public override Operator WithArgs(Expression left, Expression right)
        {
            return new Sub(left, right);
        }

        public override string ToString()
        {
            return $"({Left.ToString()} - {Right.ToString()})";
        }
    }

    public class Mul : Operator
    {
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
                    return Left.Value * Right.Value;
                }
            }
        }

        public override bool Commutative { get { return true; } }

        public override bool Associative { get { return true; } }

        public Mul(Expression left, Expression right) : base(left, right) { }

        public override Expression Derivative(string variable)
        {
            Expression u = Left;
            Expression v = Right;
            Expression du = u.Derivative(variable);
            Expression dv = v.Derivative(variable);

            return v * du + u * dv;
        }

        public override double Evaluate(Dictionary<string, double> context)
        {
            return Left.Evaluate(context) * Right.Evaluate(context);
        }

        public override Operator WithArgs(Expression left, Expression right)
        {
            return new Mul(left, right);
        }
        
        public override string ToString()
        {
            return $"({Left.ToString()} * {Right.ToString()})";
        }

        public override bool Equals(object obj)
        {
            return (obj.GetType() == this.GetType()) &&
                (
                    (
                        (obj as Operator).Left.Equals(this.Left) &&
                        (obj as Operator).Right.Equals(this.Right)
                    ) ||
                    (
                        (obj as Operator).Left.Equals(this.Right) &&
                        (obj as Operator).Right.Equals(this.Left)
                    )
                );
        }
    }

    public class Div : Operator
    {
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
                    return Left.Value / Right.Value;
                }
            }
        }

        public override bool Commutative { get { return false; } }

        public override bool Associative { get { return false; } }

        public Div(Expression left, Expression right) : base(left, right) { }

        public override Expression Derivative(string variable)
        {
            Expression u = Left;
            Expression v = Right;
            Expression du = u.Derivative(variable);
            Expression dv = v.Derivative(variable);

            return (v * du - u * dv) / (v * v);
        }

        public override double Evaluate(Dictionary<string, double> context)
        {
            return Left.Evaluate(context) / Right.Evaluate(context);
        }

        public override Operator WithArgs(Expression left, Expression right)
        {
            return new Div(left, right);
        }

        public override string ToString()
        {
            return $"({Left.ToString()} / {Right.ToString()})";
        }
    }

    public class Pow : Operator
    {
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
                    return Math.Pow(Left.Value, Right.Value);
                }
            }
        }

        public override bool Commutative { get { return false; } }

        public override bool Associative { get { return false; } }

        public Pow(Expression left, Expression right) : base(left, right) { }

        public override Expression Derivative(string variable)
        {
            if (!(Right is Constant) && !(Left is Constant))
            {
                Expression u = Left;
                Expression v = Right;
                Expression du = u.Derivative(variable);
                Expression dv = v.Derivative(variable);

                return (new Pow(u, v - 1)) * (v * du + u * ln(u) * dv);
            }
            else if ((Right is Constant) && !(Left is Constant))
            {
                Expression u = Left;
                Constant n = Right as Constant;
                Expression du = u.Derivative(variable);

                return n * (new Pow(u, n - 1)) * du;
            }
            else if (!(Right is Constant) && (Left is Constant))
            {
                Constant n = Left as Constant;
                Expression u = Right;
                Expression du = u.Derivative(variable);

                return Math.Log(n.Value) * (new Pow(n, u)) * du;
            }
            else
            //((Right is Constant) && (Left is Constant))
            {
                Constant n = Left as Constant;
                Constant exp = Right as Constant;

                return Math.Pow(n.Value, exp.Value);
            }
        }

        public override double Evaluate(Dictionary<string, double> context)
        {
            return Math.Pow(Left.Evaluate(context), Right.Evaluate(context));
        }

        public override Operator WithArgs(Expression left, Expression right)
        {
            return new Pow(left, right);
        }

        public override string ToString()
        {
            return $"({Left.ToString()} ^ {Right.ToString()})";
        }
    }
}
