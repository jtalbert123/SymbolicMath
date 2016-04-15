using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SymbolicMath.ExpressionHelper;

namespace SymbolicMath
{
    /// <summary>
    /// A function is an <see cref="Expression"/> that has one argument.
    /// </summary>
    /// <remarks>
    /// When extending this class, remember to pass in the constant value to the constructor <see cref="Function(Expression, double)"/>.
    /// Also, implement the <see cref="Expression.Derivative(string)"/> and <see cref="Expression.Evaluate(Dictionary{string, double})"/> methods.
    /// </remarks>
    public abstract class Function : Expression
    {
        public Expression Argument { get; }

        public override bool IsConstant { get { return Argument.IsConstant; } }

        public override int Height { get { return Argument.Height + 1; } }

        public override int Size { get { return Argument.Size + 1; } }

        public override int Complexity { get { return Argument.Complexity + 1; } }

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

        public Function(Expression arg) : base()
        {
            if (arg == null)
            {
                throw new ArgumentNullException("Do not use null as an Expression");
            }
            Argument = arg;
        }

        protected Function(Expression arg, double value) : this(arg)
        {
            m_value = value;
        }

        public abstract Function With(Expression arg);

        public override Expression With(Dictionary<string, double> values)
        {
            return this.With(Argument.With(values));
        }

        public override bool Equals(object obj)
        {
            return (obj.GetType() == this.GetType()) && (obj as Function).Argument.Equals(this.Argument);
        }
    }

    public class Neg : Function
    {
        public Neg(Expression arg) : base(arg, (arg.IsConstant) ? -arg.Value : 0) { }

        public override Expression Derivative(string variable)
        {
            return -Argument.Derivative(variable);
        }

        public override double Evaluate(Dictionary<string, double> context)
        {
            return -Argument.Evaluate(context);
        }

        public override Function With(Expression arg)
        {
            return new Neg(arg);
        }

        public override string ToString()
        {
            return $"(-{Argument})";
        }
    }

    public class Exp : Function
    {
        public Exp(Expression arg) : base(arg, (arg.IsConstant) ? Math.Exp(arg.Value) : 0) { }

        public override Expression Derivative(string variable)
        {
            return this * Argument.Derivative(variable);
        }

        public override double Evaluate(Dictionary<string, double> context)
        {
            return Math.Exp(Argument.Evaluate(context));
        }

        public override Function With(Expression arg)
        {
            return new Exp(arg);
        }

        public override string ToString()
        {
            return $"e^({Argument})";
        }
    }

    public class Log : Function
    {
        public Log(Expression arg) : base(arg, (arg.IsConstant) ? Math.Log(arg.Value) : 0) { }

        public override Expression Derivative(string variable)
        {
            return Argument.Derivative(variable) / Argument;
        }

        public override double Evaluate(Dictionary<string, double> context)
        {
            return Math.Log(Argument.Evaluate(context));
        }

        public override Function With(Expression arg)
        {
            return new Log(arg);
        }

        public override string ToString()
        {
            return $"ln({Argument})";
        }
    }

    public class Sin : Function
    {
        public Sin(Expression arg) : base(arg, (arg.IsConstant) ? Math.Sin(arg.Value) : 0) { }

        public override Expression Derivative(string variable)
        {
            return new Cos(Argument) * Argument.Derivative(variable);
        }

        public override double Evaluate(Dictionary<string, double> context)
        {
            return Math.Sin(Argument.Evaluate(context));
        }

        public override Function With(Expression arg)
        {
            return new Sin(arg);
        }

        public override string ToString()
        {
            return $"sin({Argument})";
        }
    }

    public class Cos : Function
    {
        public Cos(Expression arg) : base(arg, (arg.IsConstant) ? Math.Cos(arg.Value) : 0) { }

        public override Expression Derivative(string variable)
        {
            return -(new Sin(Argument)) * Argument.Derivative(variable);
        }

        public override double Evaluate(Dictionary<string, double> context)
        {
            return Math.Cos(Argument.Evaluate(context));
        }

        public override Function With(Expression arg)
        {
            return new Cos(arg);
        }

        public override string ToString()
        {
            return $"cos({Argument})";
        }
    }

    public class Tan : Function
    {
        public Tan(Expression arg) : base(arg, (arg.IsConstant) ? Math.Tan(arg.Value) : 0) { }

        public override Expression Derivative(string variable)
        {
            Expression sec = 1 / (new Cos(Argument));
            return sec * sec * Argument.Derivative(variable);
        }

        public override double Evaluate(Dictionary<string, double> context)
        {
            return Math.Tan(Argument.Evaluate(context));
        }

        public override Function With(Expression arg)
        {
            return new Tan(arg);
        }

        public override string ToString()
        {
            return $"tan({Argument})";
        }
    }
}
