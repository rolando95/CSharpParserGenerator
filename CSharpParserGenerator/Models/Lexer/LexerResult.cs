using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;


namespace CSharpParserGenerator
{
    [DebuggerDisplay("{StringToken}")]
    public class LexerNode<ELang> : LexerToken<ELang> where ELang : Enum
    {
        public string Substring { get; set; }
        public int Position { get; set; }

        public LexerNode(string substring, int position, Token<ELang> token, Regex pattern) : base(token, pattern)
        {
            Substring = substring;
            Position = position;
        }

        public static LexerNode<ELang> EndLexerNode(int position = 0) => new LexerNode<ELang>("EOF", position, Token<ELang>.EndToken, null);

        // Only to display in the debbuger
        [DebuggerBrowsable(DebuggerBrowsableState.Never), ExcludeFromCodeCoverage]
        public string StringToken => $"({Token.Symbol}) {Substring}";
    }

    public class LexerProcessExpressionResult<ELang> where ELang : Enum
    {
        public string Text { get; }
        public bool Success { get; }
        public IEnumerable<LexerNode<ELang>> Nodes { get; }
        public string ErrorMessage { get; }

        public LexerProcessExpressionResult(
            string text,
            bool success,
            List<LexerNode<ELang>> nodes = null,
            string errorMessage = null
        )
        {
            Text = text;
            Success = success;
            Nodes = nodes;
            ErrorMessage = errorMessage;
        }
    }
}