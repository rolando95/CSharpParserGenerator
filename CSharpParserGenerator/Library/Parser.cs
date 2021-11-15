using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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
            object result;
            try
            {
                var lexerNodes = Lexer.ParseLexerNodes(text);
                result = ProcessSyntax(text, lexerNodes);
            }
            catch (Exception e)
            {
                var errors = new List<ErrorInfo>() { new ErrorInfo() { Type = e.GetType().Name, Description = e.Message } };
                return new ParseResult<TResult>(text, success: false, errors: errors);
            }
            return new ParseResult<TResult>(text, success: true, value: result);
        }

        private object ProcessSyntax(string text, IEnumerable<LexerNode<ELang>> lexerNodes)
        {
            object result = null;
            var lexerNodesEnumerator = lexerNodes.GetEnumerator();

            var currentNode = NextLexerNode(lexerNodesEnumerator);
            var currentToken = currentNode.Token;
            var currentState = RootState.Id;
            var parseStack = new ParseStack(RootState.Id);

            var accept = false;

            do
            {
                var action = ParserTable.GetAction(currentState, currentToken);

                if (action == null)
                {
                    var availableTokens = ParserTable.GetAvailableTerminalsFromStateId(currentState).Select(t => t.IsEnd ? "EOF" : t.ToString());
                    
                    if (currentNode.Token.Equals(Token<ELang>.EndToken()))
                    {
                        throw new InvalidOperationException($"Syntax error: invalid EOF. Any of these tokens were expected: {string.Join(", ", availableTokens)}");
                    }

                    var errorTextFragment = Utils.GetErrorTextFragment(text, currentNode.Position, currentNode.Substring.Length);
                    throw new InvalidOperationException(message: $"Syntax error: Invalid token {currentNode.Token} ({currentNode.Substring}) at \"{errorTextFragment}\" (position {currentNode.Position}). Any of these tokens were expected: {string.Join(", ", availableTokens)}");
                }

                switch (action.Action)
                {
                    case ActionType.Accept:
                        {
                            result = parseStack.CurrentValue;
                            accept = true;
                            break;
                        }
                    case ActionType.Shift:
                        {
                            parseStack.Shift(currentNode, action);
                            currentNode = NextLexerNode(lexerNodesEnumerator);
                            currentToken = currentNode.Token;
                            currentState = parseStack.CurrentState;
                            break;
                        }
                    case ActionType.Goto:
                        {
                            parseStack.Goto(action);
                            currentState = parseStack.CurrentState;
                            currentToken = currentNode.Token;
                            break;
                        }
                    case ActionType.Reduce:
                        {
                            parseStack.Reduce(action, ProductionRules);
                            currentToken = action.ProductionRule.Head;
                            currentState = parseStack.CurrentState;
                            break;
                        }
                }

            } while (!accept);
            return result;

        }

        private LexerNode<ELang> NextLexerNode(IEnumerator<LexerNode<ELang>> lexerNodesEnumerator)
        {
            lexerNodesEnumerator.MoveNext();
            return lexerNodesEnumerator.Current;
        }
    }
}