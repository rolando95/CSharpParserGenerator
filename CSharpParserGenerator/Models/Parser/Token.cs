using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Utils.Sequence;
using Id = System.Int64;

namespace CSharpParserGenerator
{
    public enum ETokenTypes
    {
        Terminal, NonTerminal, AnonymousNonTerminal,
        Root = int.MaxValue,
        Pivot = int.MaxValue - 1,
        End = int.MaxValue - 2
    }

    [DebuggerDisplay("{StringToken}")]
    public class Token<ELang> : IEquatable<Token<ELang>> where ELang : Enum
    {
        public static Token<ELang> RootToken() => new Token<ELang>(type: ETokenTypes.Root);
        public static Token<ELang> PivotToken() => new Token<ELang>(type: ETokenTypes.Pivot);
        public static Token<ELang> AnonymousNonTerminalToken() => new Token<ELang>(type: ETokenTypes.AnonymousNonTerminal);
        public static Token<ELang> EndToken() => new Token<ELang>(type: ETokenTypes.End);
        public ETokenTypes Type { get; }
        public Id Symbol { get; }

        private static Sequence AnonymousTokenSequence { get; } = new Sequence();

        public bool IsNonTerminal => Type == ETokenTypes.NonTerminal || Type == ETokenTypes.Root || Type == ETokenTypes.AnonymousNonTerminal;
        public bool IsTerminal => Type == ETokenTypes.Terminal;
        public bool IsPivot => Type == ETokenTypes.Pivot;
        public bool IsEnd => Type == ETokenTypes.End;
        public bool IsRoot => Type == ETokenTypes.Root;

        public bool Equals(Token<ELang> other) => Type.Equals(other.Type) && Convert.ToInt32(Symbol) == Convert.ToInt32(other.Symbol);
        public bool Equals(Enum other) => Convert.ToInt32(Symbol) == Convert.ToInt32(other);
        public override int GetHashCode() => new { Type, Symbol }.GetHashCode();

        public Token(ETokenTypes type, ELang symbol = default)
        {
            if (type == ETokenTypes.AnonymousNonTerminal)
            {
                Symbol = AnonymousTokenSequence.Next();
            }
            else
            {
                Symbol = Convert.ToInt32(symbol);
            }

            Type = type;

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
                    ETokenTypes.Pivot => ".",
                    ETokenTypes.End => "$",
                    ETokenTypes.AnonymousNonTerminal => $"@{Symbol}",
                    _ => $"{(ELang)Enum.ToObject(typeof(ELang), Symbol)}"
                };
                return result;
            }
        }
    }

    public class Token
    {
        public Enum Symbol { get; }
        public Op Op { get; }
        public bool IsOperation => Op != null;
        public Token(Enum symbol) { Symbol = symbol; }
        public Token(Op op) { Op = op; }
        public static implicit operator Token(Enum e) => new Token(symbol: e);
        public static implicit operator Token(Op op) => new Token(op: op);
    }
}

#nullable disable