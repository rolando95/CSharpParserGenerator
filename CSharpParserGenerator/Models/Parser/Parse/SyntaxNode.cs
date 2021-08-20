using System;

namespace CSharpParserGenerator
{
    public class SyntaxNode<ELang> where ELang : Enum
    {
        public Token<ELang> Token { get; }
        public object[] ChildrenValues { get; }
        public Token<ELang>[] Children { get; }
        public SyntaxNode(Token<ELang> token, object[] childrenValues, Token<ELang>[] children)
        {
            Token = token;
            ChildrenValues = childrenValues;
            Children = children;
        }
    }
}
