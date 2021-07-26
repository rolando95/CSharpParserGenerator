using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Utils.Serialization;


using System.Linq;

namespace Lexical
{
    public class TokenDefinition<T> where T : Enum
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public T Token { get; }

        [JsonConverter(typeof(RegexJsonSerializer))]
        public Regex Pattern { get; }

        public TokenDefinition(T token, string pattern)
        {
            Token = token;
            Pattern = new Regex($"^{pattern}");
        }

        public TokenDefinition(T token, Regex pattern)
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

    public class TokenDefinitions<T> : Dictionary<T, string> where T : Enum
    {
        public IEnumerable<TokenDefinition<T>> MapTokenDefinitionEnumerable()
        {
            Dictionary<T, string> dictionary  = this;
            return dictionary.AsEnumerable().Select(pair => new TokenDefinition<T>(pair.Key, pair.Value));
        }
    }
}