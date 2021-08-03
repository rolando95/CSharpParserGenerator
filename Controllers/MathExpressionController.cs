using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System;

namespace CSharpParserGenerator.Controllers
{
    [ApiController]
    [Route("[Controller]")]
    public class MathExpressionController : ControllerBase
    {

        public enum ELang
        {
            Expression, Addition, Exponentiation, Multiplication, Term,

            Ignore, Pow, Mul, Sub, Plus, Div, LPar, RPar, Property
        }


        [HttpGet]
        public IActionResult Parser([FromHeader] string expression)
        {
            /// You only need to run this code once to generate the parser. 
            /// We have left the configuration and instance of the parser on the controller to make the example easier to read.

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
            Parser<ELang> parser = new ParserGenerator<ELang>(lexer, rules).CompileParser();

            /// You can run this part as many times as you want. Only requires one instance of the parser.

            var result = parser.Parse<double>(expression);
            if (!result.Success) return BadRequest(result);
            return Ok(result);

        }
    }
}
