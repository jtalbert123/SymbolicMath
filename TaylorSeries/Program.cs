using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SymbolicMath;
//using static SymbolicMath.ExpressionHelper;
using SymbolicMath.Simplification;

namespace TaylorSeries
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            int terms = 15;
            ISimplifier simplifier = new Simplifier();
            Expression target = "(sin(x)*cos(x))";
            Expression series = "c0";
            for (int i = 1; i < terms; i++)
            {
                series = series.Add($"c{i}*x^{i}");
            }
            Dictionary<Variable, Expression> reduceX = new Dictionary<Variable, Expression>() { ["x"] = 0 };
            Dictionary<Variable, double> evalX = new Dictionary<Variable, double>() { ["x"] = 0 };
            Dictionary<Variable, Expression> coeffecients = new Dictionary<Variable, Expression>() { };
            Expression derivative = series;
            Expression targetDerivative = target;
            for (int i = 0; i < terms; i++)
            {
                Expression toSolve = simplifier.Simplify(derivative.With(reduceX));
                //toSolve is (n * var)

                Product left;
                Constant n = null;
                Variable v = null;
                if (toSolve is Product)
                {
                    left = toSolve as Product;
                    n = left.Arguments[0] as Constant;
                    v = left.Arguments[1] as Variable;
                } else if (toSolve is Variable)
                {
                    n = (Constant)1;
                    v = toSolve as Variable;
                }
                //(n*v) = derivative @ x=0
                double derivativeVal = targetDerivative.Evaluate(evalX);
                Expression value = derivativeVal / n;
                coeffecients.Add(v, value);

                derivative = simplifier.Simplify(derivative.Derivative("x"));
                targetDerivative = simplifier.Simplify(targetDerivative.Derivative("x"));
            }
            series = simplifier.Simplify(series.With(coeffecients));
            System.Windows.Clipboard.SetText(series.ToString(), System.Windows.TextDataFormat.Text);
            Console.WriteLine(series);
            Console.ReadKey();
        }
    }
}
