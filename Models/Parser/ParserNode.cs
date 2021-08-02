using System;
using System.Diagnostics;

namespace CSharpParserGenerator
{
    [DebuggerDisplay("{StringToken}")]
    public class ParserNode<ELang> where ELang : Enum
    {
        public dynamic Value { get; set; }
        public Token<ELang> Token { get; set; }
        public int Position { get; set; }
        public int StateId { get; set; }

        public ParserNode(dynamic value, Token<ELang> token, int stateId = -1, int position = 0)
        {
            Value = value;
            Token = token;
            Position = position;
            StateId = stateId;
        }

        // Only to display in the debbuger
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public string StringToken => $"{Token.StringToken}{ (Value != null ? $" - {Value.ToString()}" : null) }";
    }
}