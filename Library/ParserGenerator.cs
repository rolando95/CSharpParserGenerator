using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Lexical;

namespace Syntax
{
    public class ParserGenerator<ERule>
        where ERule : Enum
    {
        private List<ProductionRule<ERule>> ProductionRules { get; set; }
        private Lexer<ERule> Lexer { get; set; }
        public ParserGenerator([NotNull] Lexer<ERule> lexer, [NotNull] SyntaxDefinition<ERule> definition)
        {
            Lexer = lexer;
            ProductionRules = definition.ProductionRules;
        }

        public void CompileParser()
        {
            var states = new List<State<ERule>>();
            var rootRules = GetRecursiveRulesFromNonTerminal(ProductionRules.First().Head);

            var rules = ProductionRules.Select(r => r.Head).Distinct();

            var first = new Dictionary<Token<ERule>, List<Token<ERule>>>();
            var follows = new Dictionary<Token<ERule>, List<Token<ERule>>>();
            foreach (var rule in rules)
            {
                first.Add(rule, GetFirsts(rule));
                follows.Add(rule, GetFollows(rule));
            }
            var rootState = GetStatesTree(new State<ERule>(rootRules, true), states);
            var parserTable = GetParserTable(follows, rootState, rootState);
        }

        public List<Token<ERule>> GetFirsts(Token<ERule> nonTerminalToken, List<Token<ERule>> tokensAlreadyVisited = null)
        {
            tokensAlreadyVisited ??= new List<Token<ERule>>();

            var rules = GetRecursiveRulesFromNonTerminal(nonTerminalToken);

            var firsts = rules
                .Where(r => r.CurrentNode.IsTerminal)
                .Where(r => !tokensAlreadyVisited.Contains(r.CurrentNode))
                .Select(r => r.CurrentNode)
                .ToList();

            tokensAlreadyVisited.AddRange(firsts);

            var emptyFirstRules = rules
                .Where(f => f.CurrentNode.IsEnd)
                .Where(f => !tokensAlreadyVisited.Contains(f.Head))
                .ToList();

            if (!emptyFirstRules.Any()) return firsts.Distinct().ToList();

            tokensAlreadyVisited.AddRange(emptyFirstRules.Select(f => f.Head));
            firsts.Add(emptyFirstRules.First().CurrentNode);

            foreach (var emptyFirstRule in emptyFirstRules)
            {
                firsts.AddRange(GetFirsts(emptyFirstRule.Head, tokensAlreadyVisited));
            }
            return firsts.Distinct().ToList();
        }

        public List<Token<ERule>> GetFollows(Token<ERule> nonTerminalToken, List<Token<ERule>> tokensAlreadyVisited = null)
        {
            tokensAlreadyVisited ??= new List<Token<ERule>>();

            var follows = new List<Token<ERule>>() { new Token<ERule>(type: ETokenTypes.End) };

            foreach (var productionRule in ProductionRules)
            {
                var nonTerminalIndexes = productionRule.Nodes
                    .Select((node, idx) => new { Node = node, idx = idx })
                    .ToList()
                    .Where(r =>
                        !tokensAlreadyVisited.Contains(r.Node) &&
                        r.Node.Equals(nonTerminalToken)
                    );

                if (!nonTerminalIndexes.Any()) continue;


                foreach (var nonTerminalIndex in nonTerminalIndexes)
                {
                    // Is last
                    if (nonTerminalIndex.idx == productionRule.Nodes.Count() - 2)
                    {
                        if (nonTerminalIndex.Node.Equals(productionRule.Head)) continue;
                        follows.AddRange(GetFollows(productionRule.Head, tokensAlreadyVisited.Append(nonTerminalToken).ToList()));
                        continue;
                    }

                    var nextNode = productionRule.Nodes[nonTerminalIndex.idx + 1];

                    // Next node is Terminal
                    if (nextNode.IsTerminal)
                    {
                        follows.Add(nextNode);
                        continue;
                    }

                    // Next node is not terminal
                    var first = GetFirsts(nextNode);

                    if (!first.Any(f => f.IsEnd))
                    {
                        follows.AddRange(first);
                        continue;
                    }

                    follows.AddRange(
                        first.Where(f => !f.IsEnd)
                        .Union(GetFollows(productionRule.Head, tokensAlreadyVisited.Append(nonTerminalToken).ToList()))
                    );
                    continue;
                }
            }

            return follows.Distinct().ToList();
        }

        private List<ProductionRule<ERule>> GetRecursiveRulesFromNonTerminal(Token<ERule> nonTerminalToken, List<Token<ERule>> tokensAlreadyVisited = null)
        {

            tokensAlreadyVisited ??= new List<Token<ERule>>();

            var generatedRules = ProductionRules.Where(r => r.Head.Equals(nonTerminalToken)).ToList();
            tokensAlreadyVisited.Add(nonTerminalToken);


            var nonVisitedRules = generatedRules
                .Where(g => g.Nodes.Any())
                .Select(g => g.CurrentNode)
                .Where(n => n.IsNonTerminal && !tokensAlreadyVisited.Contains(n))
                .Distinct();

            var resultRules = generatedRules.Select(r => r);
            if (!nonVisitedRules.Any()) return resultRules.ToList();


            foreach (var nonVisitedRule in nonVisitedRules)
            {
                if (tokensAlreadyVisited.Contains(nonVisitedRule)) continue;
                resultRules = resultRules.Concat(GetRecursiveRulesFromNonTerminal(nonVisitedRule, tokensAlreadyVisited));
            }
            return resultRules.ToList();
        }

        private State<ERule> GetStatesTree([NotNull] State<ERule> currentState, [NotNull] List<State<ERule>> states = null)
        {
            states.Add(currentState);

            var productionRuleGroups = currentState.ProductionRules.GroupBy(p => p.CurrentNode).ToList();
            foreach (var producctionRuleGroup in productionRuleGroups)
            {
                if (producctionRuleGroup.All(r => r.IsEnd)) continue;

                var nextProductions = producctionRuleGroup.Select(g => g.GetProductionRuleWithPivotShifted()).ToList();

                var matchExistingState = states.FirstOrDefault(s => s.Equals(nextProductions));
                if (matchExistingState != null)
                {
                    foreach (var producctionRule in producctionRuleGroup)
                    {
                        producctionRule.NextState = matchExistingState;
                    }
                    continue;
                }
                if (nextProductions.Count == 1)
                {
                    var nextProduction = nextProductions.First();
                    var nextNode = nextProduction.CurrentNode;
                    var matchState = states.FirstOrDefault(s => s.Contains(nextProduction));
                    if (matchState != null)
                    {
                        foreach (var producctionRule in producctionRuleGroup)
                        {
                            producctionRule.NextState = matchState;
                        }
                        continue;
                    }
                }

                IEnumerable<ProductionRule<ERule>> restProductionRules = new List<ProductionRule<ERule>>();
                foreach (var nextProduction in nextProductions)
                {
                    if (!nextProduction.CurrentNode.IsNonTerminal) continue;
                    restProductionRules = restProductionRules
                                            .Union(
                                                GetRecursiveRulesFromNonTerminal(nextProduction.CurrentNode)
                                            );
                }

                var nextState = new State<ERule>(nextProductions.Concat(restProductionRules).ToList());
                foreach (var productionRule in producctionRuleGroup)
                {
                    if (!productionRule.IsEnd) productionRule.NextState = nextState;
                }
                GetStatesTree(nextState, states);
            }
            return currentState;
        }

        private ParserTable<ERule> GetParserTable(
            Dictionary<Token<ERule>, List<Token<ERule>>> follows,
            State<ERule> rootState,
            State<ERule> currentState,
            ParserTable<ERule> parserTable = null
        )
        {
            parserTable ??= new ParserTable<ERule>();

            foreach (var producctionRule in currentState.ProductionRules)
            {
                var currentStateId = currentState.Id;
                var productionNode = producctionRule.CurrentNode;

                if (parserTable[currentStateId, productionNode] != null) continue;

                // Reduce
                if (producctionRule.IsEnd)
                {
                    var rule = producctionRule.Head;
                    var productionRuleIdx = ProductionRules.FindIndex(p => p.Similar(producctionRule));
                    var actionState = new ActionState<ERule>(productionRuleIdx == 0 ? ActionType.Accept : ActionType.Reduce, productionRuleIdx);

                    foreach (var token in follows[rule])
                    {
                        parserTable[currentStateId, token] = actionState;
                    }

                    continue;
                }

                {
                    var actionType = productionNode.IsNonTerminal ? ActionType.Goto : ActionType.Shift;
                    var actionState = new ActionState<ERule>(actionType, producctionRule.NextState.Id);
                    parserTable[currentStateId, productionNode] = actionState;
                    GetParserTable(follows, rootState, producctionRule.NextState, parserTable);
                }

            }

            return parserTable;
        }
    }
}