

using System;
using System.Collections.Generic;
using System.Linq;

namespace Lexical
{
    public class Lexer<T> where T : Enum
    {
        private IEnumerable<LexerToken<T>> Tokens;

        public Lexer(LexerDefinition<T> tokens)
        {
            Tokens = tokens.MapTokenDefinitionEnumerable();
        }

        public LexerProcessExpressionResult<T> ProcessExpression(string text, T ignoreToken = default)
        {
            int position = 0;
            string textRemaining = text ?? "";
            var nodes = new List<LexerNode<T>>();

            while (textRemaining.Length > 0)
            {
                var match = Tokens
                    .Select((t, idx) => new { Definition = t, MatchLength = t.MatchLength(textRemaining) })
                    .FirstOrDefault(t => t.MatchLength > 0);

                if (match == null)
                {
                    return new LexerProcessExpressionResult<T>(text, false, errorMessage: $"Invalid expression at position {position}: {text}");
                }

                var matchLength = match.MatchLength;
                string substring = textRemaining[..matchLength];

                position += matchLength;
                textRemaining = textRemaining[matchLength..];

                if (ignoreToken.Equals(match.Definition.Token)) continue;

                nodes.Add(new LexerNode<T>(substring, match.Definition.Token, match.Definition.Pattern));
            }

            return new LexerProcessExpressionResult<T>(text, true, nodes: nodes);
        }
    }
}