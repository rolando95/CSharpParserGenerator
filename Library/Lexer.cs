

using System;
using System.Collections.Generic;
using System.Linq;

namespace CSharpParserGenerator
{
    public class Lexer<ELang> where ELang : Enum
    {
        private IEnumerable<LexerToken<ELang>> Tokens;
        private ELang IgnoreToken;

        public Lexer(LexerDefinition<ELang> tokens, ELang ignoreToken = default)
        {
            Tokens = tokens.MapTokenDefinitionEnumerable();
            IgnoreToken = ignoreToken;
        }

        public LexerProcessExpressionResult<ELang> ProcessExpression(string text)
        {
            int position = 0;
            string textRemaining = text ?? "";
            var nodes = new List<LexerNode<ELang>>();

            while (textRemaining.Length > 0)
            {
                var oldPosition = position;

                var match = Tokens
                    .Select((t, idx) => new { Definition = t, MatchLength = t.MatchLength(textRemaining) })
                    .FirstOrDefault(t => t.MatchLength > 0);

                if (match == null)
                {
                    return new LexerProcessExpressionResult<ELang>(text, false, errorMessage: $"Invalid expression at position {position}: {text}");
                }

                var matchLength = match.MatchLength;
                string substring = textRemaining[..matchLength];

                position += matchLength;
                textRemaining = textRemaining[matchLength..];
                if (match.Definition.Token.Equals(IgnoreToken)) continue;

                nodes.Add(new LexerNode<ELang>(substring, oldPosition, match.Definition.Token, match.Definition.Pattern));
            }

            return new LexerProcessExpressionResult<ELang>(text, true, nodes: nodes);
        }
    }
}