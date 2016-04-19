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
            int terms = 12;
            //Expression target = "sin(x) + cos(x/2)";
            //Expression target = "sin(cos(x))";
            //Expression target = "e^(x^2)";
            //Expression target = "e^(x^2-5)";
            //Expression target = "sin(x^2)";
            Expression target = "sin(x) + cos(x/2) + sin(cos(x)) + e^(x^2) + e^(x^2-5) + sin(x^2)";
            Expression series = "0";
            Dictionary<Variable, double> evalX = new Dictionary<Variable, double>() { ["x"] = 0 };
            Expression derivative = target;
            double factorial = 1;
            for (int i = 0; i < terms; i++)
            {
                //(factorial*v) = derivative @ x=0
                double derivativeVal = derivative.Evaluate(evalX);
                Expression value = derivativeVal / factorial;
                series += value * $"x^{i}";

                factorial *= i + 1;
                derivative = derivative.Derivative("x").Simplify();
            }
            series = series.Simplify();
            System.Windows.Clipboard.SetText(series.ToString().Replace("E", "*10^"));
            Console.WriteLine(series);
            Console.ReadKey();
            System.Windows.Clipboard.SetText((target - series).Simplify().ToString().Replace("E", "*10^"));
        }
    }
}
