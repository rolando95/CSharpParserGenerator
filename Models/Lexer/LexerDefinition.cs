using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Utils.Serialization;


using System.Linq;

namespace Lexical
{
    public class LexerToken<T> where T : Enum
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public T Token { get; }

        [JsonConverter(typeof(RegexJsonSerializer))]
        public Regex Pattern { get; }

        public LexerToken(T token, TokenRegex pattern)
        {
            Token = token;
            Pattern = pattern;
        }

        public LexerToken(T token, Regex pattern)
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
        private TokenRegex(string pattern) { Pattern = new Regex($"^{pattern}"); }

        public static implicit operator TokenRegex(string e) => new TokenRegex(e);
        public static implicit operator Regex(TokenRegex e) => e.Pattern;

    }

    public class LexerDefinition<T> : Dictionary<T, TokenRegex> where T : Enum
    {
        public IEnumerable<LexerToken<T>> MapTokenDefinitionEnumerable()
        {
            Dictionary<T, TokenRegex> dictionary = this;
            return dictionary.AsEnumerable().Select(pair => new LexerToken<T>(pair.Key, pair.Value));
        }
    }
}