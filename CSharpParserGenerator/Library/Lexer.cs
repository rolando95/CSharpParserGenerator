

using System;
using System.Collections.Generic;
using System.Linq;

namespace CSharpParserGenerator
{
    public class Lexer<ELang> where ELang : Enum
    {
        public IEnumerable<LexerToken<ELang>> Tokens { get; private set; }
        private ELang IgnoreToken { get; }

        public Lexer(LexerDefinition<ELang> tokens, ELang ignoreToken = default)
        {
            Tokens = tokens.Tokens;
            IgnoreToken = ignoreToken;
        }

        public void AddTokens(IEnumerable<LexerToken<ELang>> tokens)
        {
            var difference = tokens.Except(Tokens);
            Tokens = difference.Concat(Tokens);
        }

        public LexerProcessExpressionResult<ELang> ProcessExpression(string text)
        {
            try
            {
                return new LexerProcessExpressionResult<ELang>(text, true, nodes: ParseLexerNodes(text).ToList());
            }
            catch (Exception e)
            {
                return new LexerProcessExpressionResult<ELang>(text, false, errorMessage: e.Message);
            }
        }

        public IEnumerable<LexerNode<ELang>> ParseLexerNodes(string text)
        {
            int position = 0;
            string textRemaining = text ?? "";

            while (textRemaining.Length > 0)
            {
                var oldPosition = position;

                var match = Tokens
                    .Select((t, idx) => new { Definition = t, MatchLength = t.MatchLength(textRemaining) })
                    .FirstOrDefault(t => t.MatchLength > 0);

                if (match == null)
                {
                    throw new InvalidOperationException($"Invalid character \"{textRemaining[0]}\" at position {position}: {text}");
                }

                var matchLength = match.MatchLength;
                string substring = textRemaining[..matchLength];

                position += matchLength;
                textRemaining = textRemaining[matchLength..];
                if (match.Definition.Token.Equals(IgnoreToken)) continue;

                yield return new LexerNode<ELang>(substring, oldPosition, match.Definition.Token, match.Definition.Pattern);
            }

            yield return LexerNode<ELang>.EndLexerNode(position);
        }
    }
}