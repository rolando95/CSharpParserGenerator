using System;
using CSharpParserGenerator;
using System.Collections.Generic;

namespace MathExpression
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = CompileParser();

            Console.Write("Write math expression: ");
            string expression = Console.ReadLine();

            var result = parser.Parse<double>(expression);

            if (result.Success)
            {
                Console.WriteLine($"Result: {result.Value}");
            }
            else
            {
                Console.WriteLine("Some errors have been detected:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine(error.Description);
                }
            }
        }

        public enum ELang
        {
            A, E, M, T,

            Ignore, Pow, Mul, Sub, Plus, Div, LParenthesis, RParenthesis, Number
        }

        public static Parser<ELang> CompileParser()
        {
            var tokens = new LexerDefinition<ELang>(new Dictionary<ELang, TokenRegex>
            {
                [ELang.Ignore] = "[ \\n]+",
                [ELang.LParenthesis] = "\\(",
                [ELang.RParenthesis] = "\\)",
                [ELang.Plus] = "\\+",
                [ELang.Pow] = "(\\*\\*|\\^)",
                [ELang.Mul] = "\\*",
                [ELang.Div] = "/",
                [ELang.Sub] = "-",
                [ELang.Number] = "[-+]?\\d*(\\.\\d+)?",
            });

            // A' -> A
            // A -> A + M
            // A -> A - M
            // A -> M
            // 
            // M -> M * E
            // M -> M / E
            // M -> E
            // 
            // E -> E ^ T
            // E -> T
            // 
            // T -> ( A )
            // T -> number
            var rules = new GrammarRules<ELang>(new Dictionary<ELang, Token[][]>()
            {
                [ELang.A] = new Token[][]
                    {
                        new Token[] { ELang.A, ELang.Plus, ELang.M, new Op(o => { o[0] += o[2]; }) },
                        new Token[] { ELang.A, ELang.Sub, ELang.M, new Op(o => { o[0] -= o[2]; }) },
                        new Token[] { ELang.M }
                    },
                [ELang.M] = new Token[][]
                    {
                        new Token[] { ELang.M, ELang.Mul, ELang.E, new Op(o => { o[0] *= o[2]; }) },
                        new Token[] { ELang.M, ELang.Div, ELang.E, new Op(o => { o[0] /= o[2]; }) },
                        new Token[] { ELang.E }
                    },
                [ELang.E] = new Token[][]
                    {
                        new Token[] { ELang.E, ELang.Pow, ELang.T, new Op(o => { o[0] = Math.Pow(o[0], o[2]); }) },
                        new Token[] { ELang.T }
                    },
                [ELang.T] = new Token[][]
                    {
                        new Token[] { ELang.LParenthesis, ELang.A, ELang.RParenthesis, new Op(o => {o[0] = o[1]; }) },
                        new Token[] { ELang.Number, new Op(o => o[0] = Convert.ToDouble(o[0])) }
                    }
            });

            var lexer = new Lexer<ELang>(tokens, ELang.Ignore);
            return new ParserGenerator<ELang>(lexer, rules).CompileParser();
        }
    }
}
