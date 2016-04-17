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
    }
}
