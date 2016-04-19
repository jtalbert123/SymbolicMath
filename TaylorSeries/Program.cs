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
            int terms = 20;
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
            Expression derivative = target;
            double factorial = 1;
            for (int i = 0; i < terms; i++)
            {
                Variable v = $"c{i}";
                //(factorial*v) = derivative @ x=0
                double derivativeVal = derivative.Evaluate(evalX);
                Expression value = derivativeVal / factorial;
                coeffecients.Add(v, value);

                factorial *= i + 1;
                derivative = simplifier.Simplify(derivative.Derivative("x"));
            }
            series = series.With(coeffecients);
            series = simplifier.Simplify(series);
            System.Windows.Clipboard.SetText(series.ToString());
            Console.WriteLine(series);
            Console.ReadKey();
        }
    }
}
