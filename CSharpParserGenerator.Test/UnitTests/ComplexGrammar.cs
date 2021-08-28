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
    /// <br/>   * This grammar can be summarized in the following regular expression (spaces are ignored): "(ab?c?d*;)+"
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
        public void ParserTests(string input, string[] expected)
        {
            var parser = CompileParser();
            var result = parser.Parse<string[]>(input);

            Assert.True(result.Success);
            Assert.True(result.Value.SequenceEqual(expected));
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
                [ELang.e] = "e",
                [ELang.semicolon] = ";"
            });
            var lexer = new Lexer<ELang>(tokens, ELang.Ignore);

            // R -> S
            // S -> A S
            // S -> A
            //
            // A -> a B C D E ;
            // A -> a B D E;
            //
            // B -> b
            // B ->
            // 
            // C -> c
            //
            // D -> d D
            // D ->
            //
            // E -> e
            // E ->
            var rules = new SyntaxDefinition<ELang>(new Dictionary<ELang, DefinitionRules>()
            {
                [ELang.R] = new DefinitionRules
                {
                    new List<Token> { ELang.S, new Op(o => o[0] = (o[1] as IEnumerable<string>).ToArray()) }
                },
                [ELang.S] = new DefinitionRules
                {
                    new List<Token> { ELang.A, ELang.S, new Op(o => o[0] = ConcatItems(o[1], o[2])) },
                    new List<Token> { ELang.A }
                },
                [ELang.A] = new DefinitionRules
                {
                    new List<Token> { ELang.a, ELang.B, ELang.C, ELang.D, ELang.E, ELang.semicolon, new Op(o => o[0] = ConcatItems("a", o[2], o[3], $"d{o[4]}", o[5], ";")) },
                    new List<Token> { ELang.a, ELang.B, new Op(o => o[3] = "!c" ), ELang.D, ELang.E, ELang.semicolon, new Op(o => o[0] = ConcatItems("a", o[2], o[3], $"d{o[4]}", o[5], ";")) }
                },
                [ELang.B] = new DefinitionRules
                {
                    new List<Token> { ELang.b },
                    new List<Token> { new Op(o => o[0] = "!b" ) }
                },
                [ELang.C] = new DefinitionRules
                {
                    new List<Token> { ELang.c }
                },
                [ELang.D] = new DefinitionRules
                {
                    new List<Token> { ELang.d, ELang.D, new Op(o => o[0] = 1 + o[2] ) },
                    new List<Token> { new Op(o => o[0] = 0 ) }
                },
                [ELang.E] = new DefinitionRules
                {
                    new List<Token> { ELang.e },
                    new List<Token> { new Op(o => o[0] = "!e" ) }
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

            a, b, c, d, e, semicolon, Ignore
        }
    }
}
