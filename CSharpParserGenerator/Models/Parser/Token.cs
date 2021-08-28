using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CSharpParserGenerator
{
    public enum ETokenTypes
    {
        UndefinedTokenType, Terminal, NonTerminal,
        Root = int.MaxValue,
        Pivot = int.MaxValue - 1,
        End = int.MaxValue - 2,
        Operation = int.MaxValue - 3
    }

    [DebuggerDisplay("{StringToken}")]
    public class Token<ELang> : IEquatable<Token<ELang>> where ELang : Enum
    {
        public static Token<ELang> RootToken => new Token<ELang>(type: ETokenTypes.Root);
        public static Token<ELang> PivotToken => new Token<ELang>(type: ETokenTypes.Pivot);
        public static Token<ELang> EndToken => new Token<ELang>(type: ETokenTypes.End);
        public static Token<ELang> Operation => new Token<ELang>(type: ETokenTypes.Operation);

        public ETokenTypes Type { get; }
        public ELang Symbol { get; }
        public int Id { get; }
        public List<Op> Operations { get; set; }

        public bool HasOperations => Operations?.Any() ?? false;
        public bool IsNonTerminal => Type == ETokenTypes.NonTerminal || Type == ETokenTypes.Root;
        public bool IsTerminal => Type == ETokenTypes.Terminal;
        public bool IsPivot => Type == ETokenTypes.Pivot;
        public bool IsEnd => Type == ETokenTypes.End;
        public bool IsRoot => Type == ETokenTypes.Root;

        public bool Equals(Token<ELang> other) => Type.Equals(other.Type) && Convert.ToInt32(Symbol) == Convert.ToInt32(other.Symbol);
        public bool Equals(Enum other) => Convert.ToInt32(Symbol) == Convert.ToInt32(other);
        public override bool Equals(object other) => other?.GetType() == typeof(Token<ELang>) ? Equals(other as Token<ELang>) : Equals(other as Enum);
        public override int GetHashCode() => new { Type, Symbol }.GetHashCode();

        public Token(ELang token = default, List<Op> operations = null, ETokenTypes type = ETokenTypes.UndefinedTokenType)
        {
            Symbol = token;
            Operations = operations;
            Type = type;

            Id = Convert.ToInt32(token);
            if (type == ETokenTypes.Operation) Id = (int)ETokenTypes.Operation;
            if (IsPivot) Id = (int)ETokenTypes.Pivot;
            if (IsEnd) Id = (int)ETokenTypes.End;
        }

        // Only to display in the debbuger
        [DebuggerBrowsable(DebuggerBrowsableState.Never), ExcludeFromCodeCoverage]
        public string StringToken
        {
            get
            {
                var result = Type switch
                {
                    ETokenTypes.Root => "Root",
                    ETokenTypes.Operation => "(Operation) op",
                    ETokenTypes.Pivot => "(Pivot) .",
                    ETokenTypes.End => "(End) $",
                    _ => $"({Type}) {Symbol}"
                };

                if (Type != ETokenTypes.Operation && HasOperations) result += "*";
                return result;
            }
        }
    }

    public class Token : Token<Enum>
    {
        public Token(Enum token = null, List<Op> op = null, ETokenTypes type = ETokenTypes.UndefinedTokenType) : base(token, op, type) { }
        public static implicit operator Token(Enum e) => new Token(token: e);
        public static implicit operator Token(Op op) => new Token(op: new List<Op> { op }, type: ETokenTypes.Operation);
    }
}

#nullable disable