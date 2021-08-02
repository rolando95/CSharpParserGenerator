using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System;

namespace CSharpParserGenerator.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        enum ELangTokens
        {
            Ignore,

            LParenthesis,
            RParenthesis,

            And,
            Or,

            Eq,
            Gt,
            Lt,

            Number,
            String,
            Boolean,
            Property
        }
        public enum ELang
        {
            Ignore, S, A, B, C, E, R, T, F, Y,

            a, b, c, d,

            Pow, Mul, Sub, Plus, Div, Zero, One, Para, Parc, Property
        }

        [HttpGet]
        [Route("/lexer")]
        public IActionResult ProcessExpression([FromQuery] string expression)
        {
            var tokens = new LexerDefinition<ELangTokens>
            {
                [ELangTokens.Ignore] = "[ \\n]+",
                [ELangTokens.LParenthesis] = "\\(",
                [ELangTokens.RParenthesis] = "\\)",
                [ELangTokens.And] = "(?i)and",
                [ELangTokens.Or] = "(?i)or",
                [ELangTokens.Eq] = "(?i)eq",
                [ELangTokens.Gt] = "(?i)gt",
                [ELangTokens.Lt] = "(?i)lt",
                [ELangTokens.String] = "(\".*\"|'.*')",
                [ELangTokens.Number] = "[-+]?\\d*(\\.\\d+)?",
                [ELangTokens.Boolean] = "(true|false)",
                [ELangTokens.Property] = "[_a-zA-Z][_a-zA-Z\\d]*",
            };

            var lexer = new Lexer<ELangTokens>(tokens, ELangTokens.Ignore);
            var lexerResult = lexer.ProcessExpression(expression);

            if (!lexerResult.Success) return BadRequest(lexerResult);
            return Ok(lexerResult);
        }

        [HttpGet]
        [Route("/parser")]
        public IActionResult Parser([FromHeader] string expression)
        {
            /// You only need to run this code once to generate the interpreter

            var tokens = new LexerDefinition<ELang>
            {
                [ELang.Ignore] = "[ \\n]+",
                [ELang.Para] = "\\(",
                [ELang.Parc] = "\\)",
                [ELang.Plus] = "\\+",
                [ELang.Pow] = "(\\*\\*|\\^)",
                [ELang.Mul] = "\\*",
                [ELang.Div] = "/",
                [ELang.Sub] = "-",
                [ELang.Property] = "[-+]?\\d*(\\.\\d+)?",
            };


            // S -> E
            // E -> E + T
            // E -> T
            // T -> T * F
            // T -> F
            // F -> (E)
            // F -> id
            var rules = new SyntaxDefinition<ELang>(new Dictionary<ELang, DefinitionRules>()
            {
                [ELang.S] = new DefinitionRules
                    {
                        new List<Token> { ELang.E }
                    },
                [ELang.E] = new DefinitionRules
                    {
                        new List<Token> { ELang.E, ELang.Plus, ELang.T, new Op(o => { o[0] = o[1] + o[3]; }) },
                        new List<Token> { ELang.E, ELang.Sub, ELang.T, new Op(o => { o[0] = o[1] - o[3]; }) },
                        new List<Token> { ELang.T }
                    },
                [ELang.T] = new DefinitionRules
                    {
                        new List<Token> { ELang.T, ELang.Mul, ELang.C, new Op(o => { o[0] = o[1] * o[3]; }) },
                        new List<Token> { ELang.T, ELang.Div, ELang.C, new Op(o => { o[0] = o[1] / o[3]; }) },
                        new List<Token> { ELang.C }
                    },
                [ELang.C] = new DefinitionRules
                    {
                        new List<Token> { ELang.C, ELang.Pow, ELang.F, new Op(o => { o[0] = Math.Pow(o[1], o[3]); }) },
                        new List<Token> { ELang.F }
                    },
                [ELang.F] = new DefinitionRules
                    {
                        new List<Token> { ELang.Para, ELang.E, ELang.Parc, new Op(o => {o[0] = o[2]; }) },
                        new List<Token> { ELang.Property, new Op(o => { o[0] = Convert.ToDouble(o[1]); }) }
                    }
            });

            var lexer = new Lexer<ELang>(tokens, ELang.Ignore);
            Parser<ELang> parser = new ParserGenerator<ELang>(lexer, rules).CompileParser();


            var result = parser.Parse<double>(expression);
            if (!result.Success) return BadRequest(result);
            return Ok(result);

        }
    }
}
