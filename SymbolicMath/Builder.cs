using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SymbolicMath
{
    public static class ExpressionHelper
    {
        public static Expression sin(Expression e)
        {
            return e.Sin();
        }

        public static Expression cos(Expression e)
        {
            return e.Cos();
        }

        public static Expression tan(Expression e)
        {
            return e.Tan();
        }

        public static Expression ln(Expression e)
        {
            return e.Log();
        }

        public static Expression e(Expression e)
        {
            return e.Exp();
        }

        public static Expression mul(params Expression[] args)
        {
            return mul(args.ToList());
        }

        public static Expression mul(List<Expression> args)
        {
            Expression total = args[0];
            for (int i = 1; i < args.Count; i++)
            {
                total = total.Mul(args[i]);
            }
            return total;
        }

        public static Expression sum(List<Expression> args)
        {
            Expression total = args[0];
            for (int i = 1; i < args.Count; i++)
            {
                total = total.Add(args[i]);
            }
            return total;
        }
    }
}
