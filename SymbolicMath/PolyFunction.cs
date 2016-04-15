using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SymbolicMath
{
    public abstract class PolyFunction : Expression, IEnumerable<Expression>
    {
        private Expression[] Args { get; }

        public override int Complexity { get; }

        public override int Height { get; }

        public override bool IsConstant { get; }

        public override int Size { get; }

        private double? mValue;

        public override double Value
        {
            get
            {
                if (!IsConstant)
                {
                    throw new InvalidOperationException($"{GetType().Name} is not a constant function");
                }
                else
                {
                    return mValue.Value;
                }
            }
        }

        public PolyFunction(params Expression[] args)
        {
            Args = args;
            int complexity = 0;
            int size = 0;
            int height = 0;
            bool isConstant = true;
            foreach (Expression e in Args)
            {
                complexity += e.Complexity + 1;
                size += e.Size + 1;
                height = Math.Max(height, e.Height);
                isConstant &= e.IsConstant;
            }
            Complexity = complexity;
            Size = size;
            Height = height;
            IsConstant = isConstant;
            mValue = null;
        }

        protected void SetValue(double value)
        {
            if (mValue.HasValue)
            {
                throw new InvalidOperationException("Can only set the memorized value once");
            }
            mValue = value;
        }

        protected Expression[] ArgsWith(Dictionary<string, double> values)
        {
            Expression[] newArgs = new Expression[Args.Length];
            for (int i = 0; i < Args.Length; ++i)
            {
                newArgs[i] = Args[i].With(values);
            }
            return newArgs;
        }

        public abstract PolyFunction With(int index, Expression e);

        public abstract PolyFunction With(Expression[] newArgs);

        protected Expression[] ArgsWith(int index, Expression e)
        {
            Expression[] newArgs = new Expression[Args.Length];
            for (int i = 0; i < Args.Length; ++i)
            {
                if (i != index)
                {
                    newArgs[i] = Args[i];
                }
                else
                {
                    newArgs[i] = e;
                }
            }
            return newArgs;
        }

        protected Expression[] CopyArgs()
        {
            Expression[] newArgs = new Expression[Args.Length];
            for (int i = 0; i < Args.Length; ++i)
            {
                newArgs[i] = Args[i];
            }
            return newArgs;
        }

        public IEnumerator<Expression> GetEnumerator()
        {
            return Args.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Args.GetEnumerator();
        }

        public Expression this[int index]
        {
            get { return Args[index]; }
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType().Equals(this.GetType()))
            {
                PolyFunction that = obj as PolyFunction;
                if (that.Args.Length == this.Args.Length)
                {
                    bool[] used = new bool[Args.Length];
                    foreach (Expression e in this)
                    {
                        bool found = false;
                        for (int i = 0; i < Args.Length; ++i)
                        {
                            if (!used[i] && that.Args[i].Equals(e))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            return false;
        }
    }

    public class Sum : PolyFunction
    {
        public Sum(params Expression[] args) : base(args)
        {
            if (IsConstant)
            {
                double value = 0;
                foreach (Expression e in args)
                {
                    value += e.Value;
                }
                base.SetValue(value);
            }
        }

        public override Expression Derivative(string variable)
        {
            Expression[] newArgs = CopyArgs();
            for (int i = 0; i < newArgs.Length; ++i)
            {
                newArgs[i] = newArgs[i].Derivative(variable);
            }
            return new Sum(newArgs);
        }

        public override double Evaluate(Dictionary<string, double> context)
        {
            double value = 0;
            foreach (Expression e in this)
            {
                value += e.Evaluate(context);
            }
            return value;
        }

        public override PolyFunction With(Expression[] newArgs)
        {
            return new Sum(newArgs);
        }

        public override Expression With(Dictionary<string, double> values)
        {
            Expression[] newArgs = base.ArgsWith(values);
            return new Sum(newArgs);
        }

        public override PolyFunction With(int index, Expression e)
        {
            Expression[] newArgs = base.ArgsWith(index, e);
            return new Sum(newArgs);
        }
    }
}
