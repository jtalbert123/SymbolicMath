using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SymMathTests
{
    public static class MyAssert
    {
        public static void Throws<T>(Action func) where T : Exception
        {
            var exceptionThrown = false;
            try
            {
                func.Invoke();
            }
            catch (T)
            {
                exceptionThrown = true;
            }

            if (!exceptionThrown)
            {
                throw new AssertFailedException(
                    string.Format("An exception of type {0} was expected, but not thrown", typeof(T))
                    );
            }
        }

        public static void Throws<T, TResult>(Func<TResult> func) where T : Exception
        {
            var exceptionThrown = false;
            try
            {
                func.Invoke();
            }
            catch (T)
            {
                exceptionThrown = true;
            }

            if (!exceptionThrown)
            {
                throw new AssertFailedException(
                    string.Format("An exception of type {0} was expected, but not thrown", typeof(T))
                    );
            }
        }
    }
}
