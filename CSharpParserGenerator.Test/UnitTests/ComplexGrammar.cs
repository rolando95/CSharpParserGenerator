using System;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace CSharpParserGenerator.Test.Parsers.ComplexGrammar
{

    /// <summary>
    /// This grammar is designed to be complex enough to test parser. 
    /// 
    /// <br/>The idea is to cover the following validations:
    /// <br/>   * Multiple semantic actions per production rule
    /// <br/>   * Empty production rules with semantic actions
    /// <br/> How this grammar is supposed to work?
    /// <br/>   * This grammar can be summarized in the following regular expression (spaces are ignored): "(ab?c?d*e?;)+"
    /// <br/>   * According to the input string, the result must be:
    /// <br/>       - A string list where each item is represented with the transformation of each character
    /// <br/>       - For each character except for the 'd': The same character if it appears, !'char' if it is absent. 
    /// <br/>       - For the 'd' character: it is replaced by a single 'd' accompanied by the number of times it is repeated.
    /// <br/>   * A few examples:
    /// <br/><example> "a b c d e; "        returns     { "a", "b", "c", "d1", "e", ";" } </example>
    /// <br/><example> "a c d d d d ; "     returns     { "a", "!b", "c", "d4", "!e", ";" } </example>
    /// <br/><example> "a b e ; a d d d ; " returns     { "a",  "b", "!c", "d0", "e", ";", "a", "!b", "!c", "d3", "!e", ";" } </example>
    /// <br/> Some considerations:
    /// <br/>   * The way syntax and semantic actions were defined was 'unnecessarily' complex with the aim of testing edge cases in CSharpParserGenerator.
    /// </summary>
    public class ComplexGrammarTests
    {

        [Theory]
        [InlineData("a b c d e ; ", new[] { "a", "b", "c", "d1", "e", ";" })]
        [InlineData("a c d d d d ; ", new[] { "a", "!b", "c", "d4", "!e", ";" })]
        [InlineData("a b e ; a d d d ; ", new[] { "a", "b", "!c", "d0", "e", ";", "a", "!b", "!c", "d3", "!e", ";" })]
        [InlineData("a ; a e ; ", new[] { "a", "!b", "!c", "d0", "!e", ";", "a", "!b", "!c", "d0", "e", ";" })]
        public void ParserTests(string input, string[] expected)
        {
            var parser = CompileParser();
            var result = parser.Parse<string[]>(input);

            Assert.True(result.Success);
            Assert.Equal(expected, result.Value);
        }

        private Parser<ELang> CompileParser()
        {
            var tokens = new LexerDefinition<ELang>(new Dictionary<ELang, TokenRegex>
            {
                [ELang.Ignore] = "[ \\n]+",
                [ELang.a] = "a",
                [ELang.semicolon] = ";"
            });

            var lexer = new Lexer<ELang>(tokens, ELang.Ignore);

            // R -> S
            // S -> A S
            // S -> A
            // A -> a B C D E ;
            // A -> a B D E ;
            // B -> b
            // B ->
            // C -> c
            // D -> d D
            // D ->
            // E -> e
            // E ->
            var rules = new GrammarRules<ELang>(new Dictionary<ELang, Token[][]>()
            {
                [ELang.R] = new Token[][]
                {
                    new Token[] { ELang.S, new Op(o => o[0] = (o[0] as IEnumerable<string>).ToArray()) }
                },
                [ELang.S] = new Token[][]
                {
                    new Token[] { ELang.A, ELang.S, new Op(o => o[0] = ConcatItems(o[0], o[1])) },
                    new Token[] { ELang.A }
                },
                [ELang.A] = new Token[][]
                {
                    new Token[] { "a", ELang.B, ELang.C, ELang.D, ELang.E, ELang.semicolon, new Op(o => o[0] = ConcatItems(o[0], o[1], o[2], $"d{o[3]}", (o[4] ?? "!e"), o[5])) },
                    new Token[] { ELang.a, ELang.B, new Op(o => o[2] = "!c"), ELang.D, ELang.E, ELang.semicolon, new Op(o => o[0] = ConcatItems(o[0], o[1], o[2], $"d{o[3]}", (o[4] ?? "!e"), ";")) }
                },
                [ELang.B] = new Token[][]
                {
                    new Token[] { "b" },
                    new Token[] { new Op(o => o[0] = "!b") }
                },
                [ELang.C] = new Token[][]
                {
                    new Token[] { "c" }
                },
                [ELang.D] = new Token[][]
                {
                    new Token[] { "d", ELang.D, new Op(o => o[0] = 1 + o[1]) },
                    new Token[] { new Op(o => o[0] = 0) }
                },
                [ELang.E] = new Token[][]
                {
                    new Token[] { "e" },
                    new Token[0]
                },
            });
            return new ParserGenerator<ELang>(lexer, rules).CompileParser();
        }

        private IEnumerable<string> ConcatItems(params object[] @params)
        {
            return @params.Aggregate((a, b) =>
            {
                IEnumerable<string> lhs = a is IEnumerable<string> ? a as IEnumerable<string> : new List<string>() { $"{a}" };
                IEnumerable<string> rhs = b is IEnumerable<string> ? b as IEnumerable<string> : new List<string>() { $"{b}" };
                return lhs.Concat(rhs);
            }) as IEnumerable<string>;
        }

        private enum ELang
        {
            R, S, A, B, C, D, E,

            a, semicolon, Ignore
        }
    }
}
