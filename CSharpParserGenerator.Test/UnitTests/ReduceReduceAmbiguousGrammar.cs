using System;
using System.Collections.Generic;
using Xunit;

namespace CSharpParserGenerator.Test.Parsers.ReduceReduceAmbiguousGrammar
{
    public class ReduceReduceAmbiguousGrammar
    {

        [Fact]
        public void ReduceReduceAmbiguousGrammarTest()
        {
            var exceptionDetails = Assert.Throws<InvalidOperationException>(() => CompileParser());
            Assert.Matches("(?i).*(Reduce/Reduce).*", exceptionDetails.Message);
        }

        private Parser<ELang> CompileParser()
        {
            var tokens = new LexerDefinition<ELang>(new Dictionary<ELang, TokenRegex>
            {
                [ELang.Ignore] = "[ \\n]+",
                [ELang.a] = "a",
                [ELang.b] = "b",
                [ELang.c] = "c",
                [ELang.d] = "d",
            });
            var lexer = new Lexer<ELang>(tokens, ELang.Ignore);

            // S -> A
            // A -> B a b
            // A -> C a c
            // B -> D
            // C -> D
            // D -> d
            var rules = new GrammarRules<ELang>(new Dictionary<ELang, Token[][]>()
            {
                [ELang.S] = new Token[][]
                    {
                    new Token[] { ELang.A }
                },
                [ELang.A] = new Token[][]
                    {
                    new Token[] { ELang.B, ELang.a, ELang.b },
                    new Token[] { ELang.C, ELang.a, ELang.c }
                },
                [ELang.B] = new Token[][]
                    {
                    new Token[] { ELang.D }
                },
                [ELang.C] = new Token[][]
                    {
                    new Token[] { ELang.D }
                },
                [ELang.D] = new Token[][]
                    {
                    new Token[] { ELang.d }
                }
            });

            return new ParserGenerator<ELang>(lexer, rules).CompileParser();
        }

        private enum ELang
        {
            S, A, B, C, D,

            a, b, c, d, e, Ignore
        }
    }
}
