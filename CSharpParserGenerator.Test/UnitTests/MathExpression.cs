using System;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace CSharpParserGenerator.Test.Parsers.MathExpression
{
    public class MathExpression
    {
        [Fact]
        public void ProccessLexerText()
        {
            var lexer = GetLexer();
            var items = new List<(string expression, bool success, int nodesCount)>
            {
                ("2*3", true, 4),
                ("2 * (3+ 6)", true, 8),
                ("a", false, 0),
                (null, true, 1)
            };

            foreach (var item in items)
            {
                var result = lexer.ProcessExpression(item.expression);
                Assert.Equal(result.Text, item.expression);

                if (item.success)
                {
                    Assert.True(result.Success);
                    Assert.Equal(result.Nodes.Count(), item.nodesCount);
                }
                else
                {
                    Assert.False(result.Success);
                    Assert.True(!string.IsNullOrWhiteSpace(result.ErrorMessage));
                }
            }

        }

        [Theory]
        [InlineData("3 + 4*5 -2.0", true, 21)]
        [InlineData("3 + 4*5 - ", false, 0)]
        [InlineData("", false, 0)]
        [InlineData("2 ^ ( 2 + 3 )", true, 32)]
        public void ParserTests(string expression, bool success, int resultValue)
        {
            var parser = CompileParser();
            var result = parser.Parse<double>(expression);

            if (success)
            {
                Assert.True(result.Success);
                Assert.Equal(resultValue, result.Value);
            }
            else
            {
                Assert.False(result.Success);
                Assert.True(result.Errors.Any());
            }
        }

        [Fact]
        public void ParserCastTest()
        {
            var parser = CompileParser();

            // Object result
            var objectResult = parser.Parse<object>("3 + 4.0 * 0.5 - 2");
            Assert.True(objectResult.Success);
            Assert.Equal(3, Convert.ToDouble(objectResult.Value));

            // Double? result
            var doubleResult = parser.Parse<Nullable<double>>("(12 / 2 ^ 2) + 12");
            Assert.True(doubleResult.Success);
            Assert.Equal(15, doubleResult.Value);

            // String result
            var stringResult = parser.Parse<string>("150 - 100 / 2");
            Assert.True(doubleResult.Success);
            Assert.Equal("100", stringResult.Value);

            // Invalid result
            var exceptionDetails = Assert.Throws<InvalidOperationException>(() => parser.Parse<List<double>>("2 * 2"));
            Assert.Matches("(?i).*Invalid ParseResult value type.*", exceptionDetails.Message);
        }

        private Lexer<EMathLang> GetLexer()
        {
            var tokens = new LexerDefinition<EMathLang>(new Dictionary<EMathLang, TokenRegex>
            {
                [EMathLang.Ignore] = "[ \\n]+",
                [EMathLang.LParenthesis] = "\\(",
                [EMathLang.RParenthesis] = "\\)",
                [EMathLang.Plus] = "\\+",
                [EMathLang.Pow] = "(\\*\\*|\\^)",
                [EMathLang.Mul] = "\\*",
                [EMathLang.Div] = "/",
                [EMathLang.Sub] = "-",
                [EMathLang.Number] = "[-+]?\\d*(\\.\\d+)?",
            });
            return new Lexer<EMathLang>(tokens, EMathLang.Ignore);
        }

        private Parser<EMathLang> CompileParser()
        {
            // A' -> A
            // A -> A + M
            // A -> A - M
            // A -> M
            // M -> M * E
            // M -> M / E
            // M -> E
            // E -> E ^ T
            // E -> T
            // T -> ( A )
            // T -> number
            var rules = new SyntaxDefinition<EMathLang>(new Dictionary<EMathLang, DefinitionRules>()
            {
                [EMathLang.A] = new DefinitionRules
                    {
                        new List<Token> { EMathLang.A, EMathLang.Plus, EMathLang.M, new Op(o => { o[0] += o[2]; }) },
                        new List<Token> { EMathLang.A, EMathLang.Sub, EMathLang.M, new Op(o => { o[0] -= o[2]; }) },
                        new List<Token> { EMathLang.M }
                    },
                [EMathLang.M] = new DefinitionRules
                    {
                        new List<Token> { EMathLang.M, EMathLang.Mul, EMathLang.E, new Op(o => { o[0] *= o[2]; }) },
                        new List<Token> { EMathLang.M, EMathLang.Div, EMathLang.E, new Op(o => { o[0] /= o[2]; }) },
                        new List<Token> { EMathLang.E }
                    },
                [EMathLang.E] = new DefinitionRules
                    {
                        new List<Token> { EMathLang.E, EMathLang.Pow, EMathLang.T, new Op(o => { o[0] = Math.Pow(o[0], o[2]); }) },
                        new List<Token> { EMathLang.T }
                    },
                [EMathLang.T] = new DefinitionRules
                    {
                        new List<Token> { EMathLang.LParenthesis, EMathLang.A, EMathLang.RParenthesis, new Op(o => {o[0] = o[1]; }) },
                        new List<Token> { EMathLang.Number, new Op(o => o[0] = Convert.ToDouble(o[0])) }
                    }
            });

            return new ParserGenerator<EMathLang>(GetLexer(), rules).CompileParser();
        }

        private enum EMathLang
        {
            A, E, M, T,

            Ignore, Pow, Mul, Sub, Plus, Div, LParenthesis, RParenthesis, Number
        }
    }
}
