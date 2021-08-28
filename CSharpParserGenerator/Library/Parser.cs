using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Utils.Extensions;

namespace CSharpParserGenerator
{
    public class Parser<ELang> where ELang : Enum
    {
        public Lexer<ELang> Lexer { get; }
        public List<ProductionRule<ELang>> ProductionRules { get; }
        public List<State<ELang>> States { get; }
        public State<ELang> RootState { get; }
        public ParserTable<ELang> ParserTable { get; }

        public Parser(
            [NotNull] Lexer<ELang> lexer,
            [NotNull] List<ProductionRule<ELang>> productionRules,
            [NotNull] List<State<ELang>> states,
            [NotNull] State<ELang> rootState,
            [NotNull] ParserTable<ELang> parserTable
        )
        {
            Lexer = lexer;
            ProductionRules = productionRules;
            States = states;
            RootState = rootState;
            ParserTable = parserTable;
        }

        public ParseResult<object> Parse(string text)
        {
            return Parse<object>(text);
        }

        public ParseResult<TResult> Parse<TResult>(string text)
        {
            dynamic result;
            try
            {
                var lexerNodes = Lexer.ParseLexerNodes(text);
                var syntaxTree = ProcessSyntax(lexerNodes);
                result = ProcessSemantic(syntaxTree);
            }
            catch (Exception e)
            {
                var errors = new List<ErrorInfo>() { new ErrorInfo() { Type = e.GetType().Name, Description = e.Message } };
                return new ParseResult<TResult>(text, success: false, errors: errors);
            }
            return new ParseResult<TResult>(text, success: true, value: result);
        }

        private SyntaxNode<ELang> ProcessSyntax(IEnumerable<LexerNode<ELang>> lexerNodes)
        {
            var lexerNodesEnumerator = lexerNodes.GetEnumerator();
            var nextNode = NextLexerNode(lexerNodesEnumerator);
            var currentState = RootState.Id;
            var intialParserNode = new ParserStackNode<ELang>(LexerNode<ELang>.EndLexerNode(), currentState);
            var parserStack = new List<ParserStackNode<ELang>>() { intialParserNode };

            var accept = false;

            do
            {

                var action = ParserTable.GetAction(currentState, nextNode.Token);

                if (action == null)
                {
                    var availableTokens = ParserTable.GetAvailableTerminalsFromStateId(parserStack.Last().StateId).Select(t => t.IsEnd ? "EOF" : t.Symbol.ToString());
                    throw new InvalidOperationException($"Syntax error: Invalid value \"{nextNode.Substring}\" at position {nextNode.Position}. Any of these tokens were expected: {string.Join(", ", availableTokens)}");
                }

                switch (action.Action)
                {
                    case ActionType.Accept:
                        {
                            if (parserStack.Count() != 2) throw new InvalidOperationException("Internal Error: Unknown error");
                            intialParserNode.SyntaxNode = parserStack.Last().SyntaxNode;
                            accept = true;
                            break;
                        }
                    case ActionType.Shift:
                        {
                            currentState = action.To;
                            parserStack.Add(new ParserStackNode<ELang>(nextNode, currentState));
                            nextNode = NextLexerNode(lexerNodesEnumerator);
                            break;
                        }
                    case ActionType.Goto:
                        {
                            currentState = action.To;
                            break;
                        }
                    case ActionType.Reduce:
                        {
                            var productionRule = ProductionRules[action.To];
                            var parserNodes = parserStack.PopRange(productionRule.Count);
                            currentState = ParserTable.GetAction(parserStack.Last().StateId, productionRule.Head).To;

                            var syntaxNode = GetSyntaxNode(productionRule, parserNodes);

                            parserStack.Add(new ParserStackNode<ELang>
                            (
                                productionRule.Head,
                                syntaxNode,
                                currentState,
                                parserNodes.LastOrDefault()?.Position ?? 0
                            ));
                            break;
                        }
                }

            } while (!accept);
            return intialParserNode.SyntaxNode;

        }

        private object ProcessSemantic(SyntaxNode<ELang> node)
        {
            if (node.Token.IsTerminal) return node.ChildrenValues[0];

            bool childrenOperations = false;

            for (var i = 1; i < node.Children.Count(); ++i)
            {
                var data = node.ChildrenValues[i];
                var token = node.Children[i];

                switch (token.Type)
                {
                    case ETokenTypes.Operation:
                        {
                            (data as Op).Callback(node.ChildrenValues);
                            childrenOperations = true;
                        }
                        break;
                    case ETokenTypes.NonTerminal:
                        {
                            var child = (data as SyntaxNode<ELang>);
                            node.ChildrenValues[i] = ProcessSemantic(child);
                        }
                        break;
                }
            }

            if (!childrenOperations && node.Children.Count() > 1)
            {
                node.ChildrenValues[0] = node.ChildrenValues[1];
            }

            return node.ChildrenValues[0];
        }

        private LexerNode<ELang> NextLexerNode(IEnumerator<LexerNode<ELang>> lexerNodesEnumerator)
        {
            lexerNodesEnumerator.MoveNext();
            return lexerNodesEnumerator.Current;
        }

        //
        // Given a production rule such as
        // 
        //     E -> . (op) a + b (op) $ 
        //
        // and a parserNodes as
        // 
        //     "10" "+" "20"
        //     
        // The following will return:
        //      ________________________________________________  
        //    / SyntaxNode                                      \
        //   |                                                  | 
        //   | Token          : E                               |
        //   | children       : [E, op, a, +, b, op]            |
        //   | childrenValues : [nul, op, "10", "+", "20", op]  |
        //   |  	                                            |
        //    \_________________________________________________/ 
        //
        private SyntaxNode<ELang> GetSyntaxNode(ProductionRule<ELang> productionRule, List<ParserStackNode<ELang>> parserNodes)
        {

            var childrenValues = Enumerable.Empty<object>();
            var children = Enumerable.Empty<Token<ELang>>();

            var pivotNode = productionRule.Nodes.First();
            if (pivotNode.HasOperations)
            {
                foreach (var operation in pivotNode.Operations)
                {
                    childrenValues = childrenValues.Append(operation);
                    children = children.Append(Token<ELang>.Operation);
                }
            }

            var productionNodes = productionRule.Nodes.Where(n => !n.IsEnd && !n.IsPivot).ToList();

            foreach (var node in parserNodes.Zip(productionNodes, (parser, prod) => new { Token = prod, parser.SyntaxNode, parser.Substring }))
            {

                switch (node.Token.Type)
                {
                    case ETokenTypes.NonTerminal: { childrenValues = childrenValues.Append(node.SyntaxNode); } break;
                    case ETokenTypes.Terminal: { childrenValues = childrenValues.Append(node.Substring); } break;
                    default: throw new InvalidOperationException("Internal Error: Unknown error");
                }

                children = children.Append(node.Token);

                if (node.Token.HasOperations)
                {
                    foreach (var operation in node.Token.Operations)
                    {
                        childrenValues = childrenValues.Append(operation);
                        children = children.Append(Token<ELang>.Operation);
                    }
                }
            }

            return new SyntaxNode<ELang>(productionRule.Head, childrenValues.Prepend(null).ToArray(), children.Prepend(productionRule.Head).ToArray());
        }
    }
}