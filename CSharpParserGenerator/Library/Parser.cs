using System;
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
            var lexerResult = Lexer.ProcessExpression(text);
            try
            {
                if (!lexerResult.Success) throw new InvalidOperationException(lexerResult.ErrorMessage);

                var textQueue = new Queue<ParserNode<ELang>>(
                    lexerResult.Nodes
                        .Select(n => new ParserNode<ELang>(n.Substring, n.Token, position: n.Position))
                        .Append(new ParserNode<ELang>(null, Token<ELang>.EndToken))
                );

                var intialParserNode = new ParserNode<ELang>(null, Token<ELang>.EndToken, RootState.Id);
                var parserStack = new List<ParserNode<ELang>>();
                parserStack.Add(intialParserNode);

                var nextNode = textQueue.Dequeue();
                var accept = false;

                do
                {
                    var action = ParserTable.GetAction(parserStack.Last().StateId, nextNode.Token);

                    if (action == null)
                    {
                        var availableTokens = ParserTable.GetAvailableTerminalsFromStateId(parserStack.Last().StateId).Select(t => t.Symbol.ToString());
                        throw new InvalidOperationException($"Syntax error: Invalid value \"{nextNode.Value}\" at position {nextNode.Position}. Token types expected: {string.Join(", ", availableTokens)}");
                    }

                    switch (action.Action)
                    {
                        case ActionType.Accept:
                            {
                                parserStack = Reduce(action, parserStack, ProductionRules[0]);
                                accept = true;
                                break;
                            }
                        case ActionType.Shift:
                            {
                                nextNode.StateId = action.To;
                                parserStack.Add(nextNode);
                                nextNode = textQueue.Dequeue();
                                break;
                            }
                        case ActionType.Goto:
                            {
                                nextNode.StateId = action.To;
                                break;
                            }
                        case ActionType.Reduce:
                            {
                                parserStack = Reduce(action, parserStack, ProductionRules[action.To]);
                                break;
                            }
                    }

                } while (!accept);
                return new ParseResult<TResult>(lexerResult.Text, success: true, value: intialParserNode.Value);
            }
            catch (Exception e)
            {
                var errors = new List<ErrorInfo>() { new ErrorInfo() { Type = e.GetType().Name, Description = e.Message } };
                return new ParseResult<TResult>(lexerResult.Text, errors: errors);
            }
        }

        private List<ParserNode<ELang>> Reduce(ActionState<ELang> currentAction, List<ParserNode<ELang>> parserStack, ProductionRule<ELang> productionRule)
        {
            var parserNodes = parserStack.PopRange(productionRule.Count);
            var ruleResult = RunProductionRule(productionRule, parserNodes);
            var lastNodeInStack = parserStack.Last();

            if (currentAction.Action.Equals(ActionType.Accept))
            {
                lastNodeInStack.Value = ruleResult;
                return parserStack;
            }

            var gotoAction = ParserTable.GetAction(lastNodeInStack.StateId, productionRule.Head);
            if (!gotoAction.Action.Equals(ActionType.Goto)) throw new InvalidOperationException("Internal error: next action Goto expected");

            var reduceNodeResult = new ParserNode<ELang>
            (
                value: ruleResult,
                token: productionRule.Head,
                stateId: gotoAction.To,
                position: parserNodes.Last().Position
            );

            parserStack.Add(reduceNodeResult);

            return parserStack;
        }


        private object RunProductionRule(ProductionRule<ELang> productionRule, List<ParserNode<ELang>> parserNodes)
        {
            var values = new List<object>() { parserNodes.FirstOrDefault()?.Value };

            for (var x = 0; x < productionRule.Count; ++x)
            {
                var productionNode = productionRule[x];

                values.Add(parserNodes[x].Value);
                if (productionNode.Op == null) continue;


                productionNode.Op.ForEach(op => { values.Add(op); op.Callback(values); });
            }
            return values[0];
        }
    }


}