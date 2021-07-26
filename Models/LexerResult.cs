using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;


namespace Lexical
{
    class LexerNode<T> : TokenDefinition<T> where T : Enum
    {
        public string Substring { get; set; }

        public LexerNode(string substring, T token, Regex pattern) : base(token, pattern)
        {
            Substring = substring;
        }
    }

    class LexerProcessExpressionResult<T> where T : Enum
    {

        public string Text { get; }
        public bool Success { get; }
        public IEnumerable<LexerNode<T>> Nodes { get; }
        public string ErrorMessage { get; }

        public LexerProcessExpressionResult(
            string text,
            bool success,
            IEnumerable<LexerNode<T>> nodes = null,
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