using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CSharpParserGenerator
{
    [DebuggerDisplay("{StringToken}")]
    public class ParserStackNode<ELang> : LexerNode<ELang> where ELang : Enum
    {

        public int StateId { get; set; }

        public SyntaxNode<ELang> SyntaxNode { get; set; }

        public ParserStackNode(Token<ELang> nonTerminalToken, SyntaxNode<ELang> syntaxNode, int stateId, int position) : base(null, position, nonTerminalToken, null)
        {
            StateId = stateId;
            SyntaxNode = syntaxNode;
        }

        public ParserStackNode(LexerNode<ELang> node, int stateId) : base(node.Substring, node.Position, node.Token, node.Pattern)
        {
            StateId = stateId;
            SyntaxNode = new SyntaxNode<ELang>(node.Token, new string[] { node.Substring }, new Token<ELang>[] { node.Token });
        }

        // Only to display in the debbuger
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public new string StringToken => $"{Token.StringToken}{ (Substring != null ? $" - {Substring.ToString()}" : null) }";
    }
}