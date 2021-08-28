using System;
using System.Collections.Generic;
using Xunit;

namespace CSharpParserGenerator.Test.Parsers.ShiftReduceAmbiguousGrammar
{
    public class ShiftReduceAmbiguousGrammar
    {
        [Fact]
        public void ShiftReduceAmbiguousGrammarTest()
        {
            var exceptionDetails = Assert.Throws<InvalidOperationException>(() => CompileParser());
            Assert.Matches("(?i).*(Reduce/Shift|Shift/Reduce).*", exceptionDetails.Message);
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

            // S' -> S
            // S -> c B b A
            // S -> c A
            // A -> a
            // A -> ''
            // B -> a
            // B -> a D
            // D -> b
            var rules = new SyntaxDefinition<ELang>(new Dictionary<ELang, DefinitionRules>()
            {
                [ELang.S] = new DefinitionRules
                    {
                        new List<Token> { ELang.c, ELang.B, ELang.b, ELang.A},
                        new List<Token> { ELang.c, ELang.A }
                    },
                [ELang.A] = new DefinitionRules
                    {
                        new List<Token> { ELang.a },
                        new List<Token> { }
                    },
                [ELang.B] = new DefinitionRules
                    {
                        new List<Token> { ELang.a },
                        new List<Token> { ELang.a, ELang.D }
                    },
                [ELang.D] = new DefinitionRules
                    {
                        new List<Token> { ELang.b }
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
