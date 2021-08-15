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
            Expression, Addition, Exponentiation, Multiplication, Term,

            Ignore, Pow, Mul, Sub, Plus, Div, LPar, RPar, Property
        }

        public static Parser<ELang> CompileParser()
        {
            var tokens = new LexerDefinition<ELang>
            {
                [ELang.Ignore] = "[ \\n]+",
                [ELang.LPar] = "\\(",
                [ELang.RPar] = "\\)",
                [ELang.Plus] = "\\+",
                [ELang.Pow] = "(\\*\\*|\\^)",
                [ELang.Mul] = "\\*",
                [ELang.Div] = "/",
                [ELang.Sub] = "-",
                [ELang.Property] = "[-+]?\\d*(\\.\\d+)?",
            };

            var rules = new SyntaxDefinition<ELang>(new Dictionary<ELang, DefinitionRules>()
            {
                [ELang.Expression] = new DefinitionRules
                    {
                        new List<Token> { ELang.Addition }
                    },
                [ELang.Addition] = new DefinitionRules
                    {
                        new List<Token> { ELang.Addition, ELang.Plus, ELang.Multiplication, new Op(o => { o[0] = o[1] + o[3]; }) },
                        new List<Token> { ELang.Addition, ELang.Sub, ELang.Multiplication, new Op(o => { o[0] = o[1] - o[3]; }) },
                        new List<Token> { ELang.Multiplication }
                    },
                [ELang.Multiplication] = new DefinitionRules
                    {
                        new List<Token> { ELang.Multiplication, ELang.Mul, ELang.Exponentiation, new Op(o => { o[0] = o[1] * o[3]; }) },
                        new List<Token> { ELang.Multiplication, ELang.Div, ELang.Exponentiation, new Op(o => { o[0] = o[1] / o[3]; }) },
                        new List<Token> { ELang.Exponentiation }
                    },
                [ELang.Exponentiation] = new DefinitionRules
                    {
                        new List<Token> { ELang.Exponentiation, ELang.Pow, ELang.Term, new Op(o => { o[0] = Math.Pow(o[1], o[3]); }) },
                        new List<Token> { ELang.Term }
                    },
                [ELang.Term] = new DefinitionRules
                    {
                        new List<Token> { ELang.LPar, ELang.Addition, ELang.RPar, new Op(o => {o[0] = o[2]; }) },
                        new List<Token> { ELang.Property, new Op(o => { o[0] = Convert.ToDouble(o[1]); }) }
                    }
            });

            var lexer = new Lexer<ELang>(tokens, ELang.Ignore);
            return new ParserGenerator<ELang>(lexer, rules).CompileParser();
        }
    }
}
