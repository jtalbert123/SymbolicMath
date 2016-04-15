using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SymbolicMath.ExpressionHelper;

namespace SymbolicMath
{
    public abstract class Function : Expression
    {
        public Expression Argument { get; }

        public override bool IsConstant { get { return Argument.IsConstant; } }

        public override int Height { get { return Argument.Height + 1; } }

        public override int Size { get { return Argument.Size + 1; } }

        public Function(Expression arg) : base()
        {
            Argument = arg;
        }

        public abstract Function WithArg(Expression arg);

        public override bool Equals(object obj)
        {
            return (obj.GetType() == this.GetType()) && (obj as Function).Argument.Equals(this.Argument);
        }
    }

    public class Neg : Function
    {
        public Neg(Expression arg) : base(arg) { }

        public override Expression Derivative(string variable)
        {
            return -Argument.Derivative(variable);
        }

        public override double Evaluate(Dictionary<string, double> context)
        {
            return -Argument.Evaluate(context);
        }

        public override Function WithArg(Expression arg)
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
        public Exp(Expression arg) : base(arg) { }

        public override Expression Derivative(string variable)
        {
            return this * Argument.Derivative(variable);
        }

        public override double Evaluate(Dictionary<string, double> context)
        {
            return Math.Exp(Argument.Evaluate(context));
        }

        public override Function WithArg(Expression arg)
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
        public Log(Expression arg) : base(arg) { }

        public override Expression Derivative(string variable)
        {
            return Argument.Derivative(variable) / Argument;
        }

        public override double Evaluate(Dictionary<string, double> context)
        {
            return Math.Log(Argument.Evaluate(context));
        }

        public override Function WithArg(Expression arg)
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
        public Sin(Expression arg) : base(arg) { }

        public override Expression Derivative(string variable)
        {
            return new Cos(Argument) * Argument.Derivative(variable);
        }

        public override double Evaluate(Dictionary<string, double> context)
        {
            return Math.Sin(Argument.Evaluate(context));
        }

        public override Function WithArg(Expression arg)
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
        public Cos(Expression arg) : base(arg) { }

        public override Expression Derivative(string variable)
        {
            return -(new Sin(Argument)) * Argument.Derivative(variable);
        }

        public override double Evaluate(Dictionary<string, double> context)
        {
            return Math.Cos(Argument.Evaluate(context));
        }

        public override Function WithArg(Expression arg)
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
        public Tan(Expression arg) : base(arg) { }

        public override Expression Derivative(string variable)
        {
            Expression sec = 1 / (new Cos(Argument));
            return sec * sec * Argument.Derivative(variable);
        }

        public override double Evaluate(Dictionary<string, double> context)
        {
            return Math.Tan(Argument.Evaluate(context));
        }

        public override Function WithArg(Expression arg)
        {
            return new Tan(arg);
        }

        public override string ToString()
        {
            return $"tan({Argument})";
        }
    }
}
