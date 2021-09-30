using System;
using System.Diagnostics.CodeAnalysis;

namespace CSharpParserGenerator
{
    public enum ETokenTypes
    {
        Terminal, NonTerminal, AnonymousNonTerminal, AnonymousTerminal,
        Root = int.MaxValue,
        End = int.MaxValue - 1
    }

    public class Token<ELang> : IEquatable<Token<ELang>> where ELang : Enum
    {
        public static Token<ELang> RootToken() => new Token<ELang>(type: ETokenTypes.Root, name: "Root");
        public static Token<ELang> AnonymousNonTerminalToken(string name) => new Token<ELang>(type: ETokenTypes.AnonymousNonTerminal, name: name);
        public static Token<ELang> AnonymousTerminalToken(string name) => new Token<ELang>(type: ETokenTypes.AnonymousTerminal, name: name);
        public static Token<ELang> EndToken() => new Token<ELang>(type: ETokenTypes.End, name: "$");
        private ETokenTypes Type { get; }
        private ELang Symbol { get; }
        public string Name { get; }
        public bool IsNonTerminal => Type == ETokenTypes.NonTerminal || Type == ETokenTypes.Root || Type == ETokenTypes.AnonymousNonTerminal;
        public bool IsTerminal => Type == ETokenTypes.Terminal || Type == ETokenTypes.AnonymousTerminal;
        public bool IsEnd => Type == ETokenTypes.End;
        public bool IsRoot => Type == ETokenTypes.Root;
        public bool IsAnonymousToken => Type == ETokenTypes.AnonymousNonTerminal || Type == ETokenTypes.AnonymousTerminal || Type == ETokenTypes.Root || Type == ETokenTypes.End;

        public bool Equals(Enum other) => !IsAnonymousToken && Symbol.Equals(other);
        public bool Equals(Token<ELang> other)
        {
            var result = GetHashCode() == other.GetHashCode();
            return result;
        }

        public override int GetHashCode()
        {
            var Value = (IsAnonymousToken ? Name : Symbol.ToString());
            return new { IsTerminal, IsNonTerminal, Value }.GetHashCode();
        }

        public Token(ETokenTypes type, ELang symbol = default, string name = null)
        {
            if (type == ETokenTypes.AnonymousNonTerminal)
            {
                Name = name;
            }
            else
            {
                Symbol = symbol;
                Name = name ?? symbol.ToString();
            }

            Type = type;

        }

        [ExcludeFromCodeCoverage]
        public override string ToString()
        {
            var result = Type switch
            {
                ETokenTypes.AnonymousNonTerminal => $"@{Name}",
                ETokenTypes.AnonymousTerminal => $"\"{Name}\"",
                _ => $"{Name}"
            };
            return result;
        }
    }

    public class Token
    {
        public Enum Symbol { get; }
        public string Name { get; }
        public Op Op { get; }
        public bool IsOperation => Op != null;
        public bool IsAnonymous => Name != null;
        public Token(Enum symbol) { Symbol = symbol; }
        public Token(Op op) { Op = op; }
        public Token(string name) { Name = name; }
        public static implicit operator Token(Enum e) => new Token(symbol: e);
        public static implicit operator Token(Op op) => new Token(op: op);
        public static implicit operator Token(string name) => new Token(name: name);
    }
}

#nullable disable