using System.Collections.Generic;
using Lexical;
using Microsoft.AspNetCore.Mvc;
using Syntax;

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
        public enum ELangRules
        {
            S, A, B, C, E, R, T, F, Y,

            a, b, c, d,

            Mul, Sub, Plus, Zero, One, Para, Parc, Property
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

            var lexer = new Lexer<ELangTokens>(tokens);
            var lexerResult = lexer.ProcessExpression(expression, ELangTokens.Ignore);

            if (!lexerResult.Success) return BadRequest(lexerResult);
            return Ok(lexerResult);
        }



        [HttpGet]
        [Route("/parser")]
        public IActionResult Parser()
        {
            var tokens = new LexerDefinition<ELangRules>
            {

            };

            // E -> T R
            // R -> + T R
            // R -> ''
            // T -> F Y
            // Y -> * F Y
            // Y -> ''
            // F -> ( E )
            // F -> i
            // var rules = new GrammarRules<ELangRules>
            // {
            //     [ELangRules.E] = new DefinitionRules
            //     {
            //         new List<Token> { ELangRules.T, ELangRules.R, new Op(o => { }) }
            //     },
            //     [ELangRules.R] = new DefinitionRules
            //     {
            //         new List<Token> { ELangRules.Plus, ELangRules.T, ELangRules.R },
            //         new List<Token> { }
            //     },
            //     [ELangRules.T] = new DefinitionRules
            //     {
            //         new List<Token> { ELangRules.F, ELangRules.Y },
            //     },
            //     [ELangRules.Y] = new DefinitionRules
            //     {
            //         new List<Token> { ELangRules.Mul, ELangRules.F, ELangRules.Y },
            //         new List<Token> { }
            //     },
            //     [ELangRules.F] = new DefinitionRules
            //     {
            //         new List<Token> { ELangRules.Para, ELangRules.E, ELangRules.Parc },
            //         new List<Token> { ELangRules.Property }
            //     }
            // };

            // S -> E
            // E -> E + T
            // E -> T
            // T -> T * F
            // T -> F
            // F -> (E)
            // F -> id
            // var rules = new SyntaxDefinition<ELangRules>(new Dictionary<ELangRules, DefinitionRules>()
            // {
            //     [ELangRules.S] = new DefinitionRules
            //         {
            //             new List<Token> { ELangRules.E, new Op(a => {}) }
            //         },
            //     [ELangRules.E] = new DefinitionRules
            //         {
            //             new List<Token> { ELangRules.E, new Op(a => {}), ELangRules.Plus, ELangRules.T },
            //             new List<Token> { ELangRules.T }
            //         },
            //     [ELangRules.T] = new DefinitionRules
            //         {
            //             new List<Token> { ELangRules.T, ELangRules.Mul, ELangRules.F },
            //             new List<Token> { ELangRules.F }
            //         },
            //     [ELangRules.F] = new DefinitionRules
            //         {
            //             new List<Token> { ELangRules.Para, ELangRules.E, ELangRules.Parc },
            //             new List<Token> { ELangRules.Property }
            //         }
            // });

            // S -> E
            // E -> E - B
            // E -> E + B
            // E -> B
            // B -> 0
            // B -> 1
            var rules = new SyntaxDefinition<ELangRules>(new Dictionary<ELangRules, DefinitionRules>()
            {
                [ELangRules.S] = new DefinitionRules
                {
                    new List<Token> { ELangRules.E, new Op(o => { }) }
                },
                [ELangRules.E] = new DefinitionRules
                {
                    new List<Token> { ELangRules.E, ELangRules.Sub, ELangRules.B, new Op(o => { }) },
                    new List<Token> { ELangRules.E, ELangRules.Plus, ELangRules.B, new Op(o => { }) },
                    new List<Token> { ELangRules.B }
                },
                [ELangRules.B] = new DefinitionRules
                {
                    new List<Token> { ELangRules.Zero, new Op(o => { }) },
                    new List<Token> { ELangRules.One, new Op(o => { }) },
                }
            });

            // S -> a B C d
            // B -> C B
            // B -> b
            // C -> c c
            // C ->  ''
            // var rules = new GrammarRules<ELangRules>
            // {
            //     [ELangRules.S] = new DefinitionRules
            //     {
            //         new List<Token> { ELangRules.a, ELangRules.B, ELangRules.C, ELangRules.d}
            //     },
            //     [ELangRules.B] = new DefinitionRules
            //     {

            //         new List<Token> { ELangRules.C, ELangRules.B },
            //         new List<Token> { ELangRules.b }
            //     },
            //     [ELangRules.C] = new DefinitionRules
            //     {
            //         new List<Token> { ELangRules.c, ELangRules.c },
            //         new List<Token> {  },
            //     }
            // };

            var lexer = new Lexer<ELangRules>(tokens);
            var parserGenerator = new ParserGenerator<ELangRules>(lexer, rules);
            parserGenerator.CompileParser();

            return Ok();
        }
    }
}
