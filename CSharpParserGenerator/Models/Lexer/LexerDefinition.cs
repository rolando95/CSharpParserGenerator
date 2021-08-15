using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Utils.Serialization;


using System.Linq;

namespace CSharpParserGenerator
{
    public class LexerToken<ELang> where ELang : Enum
    {
        public Token<ELang> Token { get; }

        [JsonConverter(typeof(RegexJsonSerializer))]
        public Regex Pattern { get; }

        public LexerToken(Token<ELang> token, TokenRegex pattern)
        {
            Token = token;
            Pattern = pattern;
        }

        public LexerToken(Token<ELang> token, Regex pattern)
        {
            Token = token;
            Pattern = pattern;
        }

        public int MatchLength(string text)
        {
            var m = Pattern.Match(text);
            return m.Success ? m.Length : 0;
        }
    }

    public class TokenRegex
    {
        public Regex Pattern { get; }
        private TokenRegex(string pattern) { Pattern = new Regex($"^{pattern}", RegexOptions.Singleline); }

        public static implicit operator TokenRegex(string e) => new TokenRegex(e);
        public static implicit operator Regex(TokenRegex e) => e.Pattern;

    }

    public class LexerDefinition<ELang> : Dictionary<ELang, TokenRegex> where ELang : Enum
    {
        public IEnumerable<LexerToken<ELang>> MapTokenDefinitionEnumerable()
        {
            Dictionary<ELang, TokenRegex> dictionary = this;
            return dictionary.AsEnumerable().Select(pair => new LexerToken<ELang>(new Token<ELang>(pair.Key, type: ETokenTypes.Terminal), pair.Value));
        }
    }
}