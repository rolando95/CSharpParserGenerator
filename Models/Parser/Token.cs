using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Syntax
{
    public enum ETokenTypes
    {
        UndefinedTokenType, Terminal, NonTerminal,
        Pivot = int.MaxValue,
        End = int.MaxValue - 2,
        Operation = int.MaxValue - 3
    }

    [DebuggerDisplay("{StringToken}")]
    public class Token<ERule> : IEquatable<Token<ERule>> where ERule : Enum
    {
        public ETokenTypes Type { get; }
        public ERule Symbol { get; }
        public int Id { get; }
        public Op Op { get; set; } = null;

        public bool IsNonTerminal => Type == ETokenTypes.NonTerminal;
        public bool IsTerminal => Type == ETokenTypes.Terminal;
        public bool IsPivot => Type == ETokenTypes.Pivot;
        public bool IsEnd => Type == ETokenTypes.End;

        public bool Equals(Token<ERule> other) => Type.Equals(other.Type) && Convert.ToInt32(Symbol) == Convert.ToInt32(other.Symbol);
        public bool Equals(Enum other) => Convert.ToInt32(Symbol) == Convert.ToInt32(other);
        public override bool Equals(object other) => other?.GetType() == typeof(Token<ERule>) ? Equals(other as Token<ERule>) : Equals(other as Enum);
        public override int GetHashCode() => new { Type, Symbol }.GetHashCode();

        public Token(ERule token = default, Op op = null, ETokenTypes type = ETokenTypes.UndefinedTokenType)
        {
            Symbol = token;
            Op = op;
            Type = type;

            Id = Convert.ToInt32(token);
            if (type == ETokenTypes.Operation) Id = (int)ETokenTypes.Operation;
            if (IsPivot) Id = (int)ETokenTypes.Pivot;
            if (IsEnd) Id = (int)ETokenTypes.End;
        }

        // Only to display in the debbuger
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public string StringToken
        {
            get
            {
                if (Type == ETokenTypes.Operation) return "Operation";
                if (IsPivot) return "(Pivot) .";
                if (IsEnd) return "(End) $";
                return $"({Type}) {Symbol}";
            }
        }
    }

    public class Token : Token<Enum>
    {
        public Token(Enum token = null, Op op = null, ETokenTypes type = ETokenTypes.UndefinedTokenType) : base(token, op, type) { }
        public static implicit operator Token(Enum e) => new Token(token: e);
        public static implicit operator Token(Op op) => new Token(op: op, type: ETokenTypes.Operation);
    }
}

#nullable disable