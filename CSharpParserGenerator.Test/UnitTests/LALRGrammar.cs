using Xunit;
using System.Collections.Generic;

namespace CSharpParserGenerator.Test.Parsers.LALRGrammar
{
    public class LALRGrammar
    {
        [Fact]
        public void ParserCastTest()
        {
            var parser = CompileParser();

            // Object result
            var objectResult = parser.Parse<object>("d a");
            Assert.True(objectResult.Success);
        }

        private Lexer<ELang> GetLexer()
        {
            var tokens = new LexerDefinition<ELang>(new Dictionary<ELang, TokenRegex>
            {
                [ELang.Ignore] = "[ \\n]+",
                [ELang.a] = "a",
                [ELang.b] = "b",
                [ELang.c] = "c",
                [ELang.d] = "d",
            });
            return new Lexer<ELang>(tokens, ELang.Ignore);
        }

        private Parser<ELang> CompileParser()
        {
            // S' -> X
            // X -> M a
            // X -> b M c
            // X -> d c
            // X -> b d a
            // M -> d
            var rules = new GrammarRules<ELang>(new Dictionary<ELang, Token[][]>()
            {
                [ELang.X] = new Token[][]
                {
                    new Token[] { ELang.M, ELang.a },
                    new Token[] { ELang.b, ELang.M, ELang.c },
                    new Token[] { ELang.d, ELang.c },
                    new Token[] { ELang.b, ELang.d, ELang.a },
                },
                [ELang.M] = new Token[][]
                {
                    new Token[] { ELang.d }
                }
            });

            return new ParserGenerator<ELang>(GetLexer(), rules).CompileParser();
        }

        private enum ELang
        {
            X, M,

            a, b, c, d,
            Ignore
        }
    }
}
