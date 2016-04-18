﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SymbolicMath
{
    public static class Infix
    {
        private static string operators = "+-*/^()";
        private static List<string> functions = new List<string>() { "ln", "log", "e", "e^", "sin", "cos", "tan", "n" };
        // a single operator, a legal identifier, or a number
        private static Regex token = new Regex($@"({functions.Aggregate((f, str) => str + "|" + f)}|[+\-*/()]|(?:(?<!e)\^)|(?:[a-zA-Z_][0-9a-zA-Z_]*)|[0-9]+(?:\.[0-9]+)?)");

        public static Expression Parse(string expression)
        {
            MatchCollection tokens = token.Matches(expression);
            List<string> tokenList = new List<string>(tokens.Count);
            foreach (Match token in tokens)
            {
                tokenList.Add(token.ToString());
            }
            string[] postfix = InfixToPostfix(tokenList.ToArray());
            Stack<Expression> stack = new Stack<Expression>();
            Expression result = 0;
            for (int i = 0; i < postfix.Length; i++)
            {
                double constant;
                if (operators.Contains(postfix[i]))
                {
                    // it is an operator
                    switch (postfix[i])
                    {
                        default:
                            throw new Exception("error on term " + postfix[i]);
                        case "+":
                            Expression right = stack.Pop();
                            Expression left = stack.Pop();
                            result = left + right;
                            break;
                        case "-":
                            right = stack.Pop();
                            left = stack.Pop();
                            result = left - right;
                            break;
                        case "*":
                            right = stack.Pop();
                            left = stack.Pop();
                            result = left * right;
                            break;
                        case "/":
                            right = stack.Pop();
                            left = stack.Pop();
                            result = left / right;
                            break;
                        case "^":
                            right = stack.Pop();
                            left = stack.Pop();
                            result = left ^ right;
                            break;
                    }
                }
                else if (functions.Contains(postfix[i]))
                {
                    switch (postfix[i])
                    {
                        default:
                            throw new Exception("error on term " + postfix[i]);
                        case "e":
                        case "e^":
                            result = stack.Pop().Exp();
                            break;
                        case "ln":
                        case "log":
                            result = stack.Pop().Log();
                            break;
                        case "n":
                            result = stack.Pop().Neg();
                            break;
                        case "sin":
                            result = stack.Pop().Sin();
                            break;
                        case "cos":
                            result = stack.Pop().Cos();
                            break;
                        case "tan":
                            result = stack.Pop().Tan();
                            break;
                    }
                }
                else if (double.TryParse(postfix[i], out constant))
                {
                    result = constant;
                }
                else
                {
                    result = postfix[i];
                }
                stack.Push(result);
            }

            return stack.Pop();
        }

        public static string[] InfixToPostfix(string[] infixArray)
        {
            var stack = new Stack<string>();
            var postfix = new Stack<string>();

            string st;
            for (int i = 0; i < infixArray.Length; i++)
            {
                if (!(operators.Contains(infixArray[i])) && !(functions.Contains(infixArray[i])))
                {
                    postfix.Push(infixArray[i]);
                }
                else
                {
                    if (infixArray[i].Equals("("))
                    {
                        stack.Push("(");
                    }
                    else if (infixArray[i].Equals(")"))
                    {
                        st = stack.Pop();
                        while (!(st.Equals("(")))
                        {
                            postfix.Push(st);
                            st = stack.Pop();
                        }
                    }
                    else
                    {
                        while (stack.Count > 0)
                        {
                            st = stack.Pop();
                            if (Priority(st) >= Priority(infixArray[i]))
                            {
                                postfix.Push(st);
                            }
                            else
                            {
                                stack.Push(st);
                                break;
                            }
                        }
                        stack.Push(infixArray[i]);
                    }
                }
            }
            while (stack.Count > 0)
            {
                postfix.Push(stack.Pop());
            }

            return postfix.Reverse().ToArray();
        }

        private static int Priority(string token)
        {
            switch (token)
            {
                default:
                    break;
                case "+":
                case "-":
                    return 2;
                case "*":
                case "/":
                    return 3;
                case "^":
                    return 4;
            }
            if (functions.Contains(token))
            {
                return 10;
            }
            return 0;
        }

        public static Expression ToExpression(this string representation)
        {
            return Parse(representation);
        }
    }
}