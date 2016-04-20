using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SymbolicMath;
using SymbolicMath.Simplification;
using System.Diagnostics;
using System.Collections.Generic;

namespace SymMathTests
{
    [TestClass]
    public class Benchmarks
    {
        [TestMethod]
        public void Simplifier()
        {
            Expression baseExp = "sin(x) + cos(x/2) + sin(cos(x)) + e^(x^2) + e^(x^2-5) + sin(x^2)";
            //Expression baseExp = "sin(x)";
            int iterations = 13;
            Expression last = baseExp;

            last = baseExp;
            var start = Process.GetCurrentProcess().TotalProcessorTime;
            for (int i = 0; i < iterations; i++)
            {
                last = last.Derivative("x");
            }
            var stop = Process.GetCurrentProcess().TotalProcessorTime;
            Console.WriteLine();
            Console.WriteLine($"Taking the first {iterations} derivatives of {baseExp} with respect to x with simplification");
            Console.WriteLine($"\ttakes {stop - start}");
            //Console.WriteLine($"{last}");
        }
    }
}
