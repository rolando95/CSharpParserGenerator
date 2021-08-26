using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CSharpParserGenerator
{
    public class ParserGenerator<ELang>
        where ELang : Enum
    {
        private List<ProductionRule<ELang>> ProductionRules { get; set; }
        private Lexer<ELang> Lexer { get; set; }

        public ParserGenerator([NotNull] Lexer<ELang> lexer, [NotNull] SyntaxDefinition<ELang> definition)
        {
            Lexer = lexer;
            ProductionRules = definition.ProductionRules;
        }

        public Parser<ELang> CompileParser()
        {
            var states = new List<State<ELang>>();
            var rootProductionRules = GetProductionRulesFromNonTerminal(ProductionRules.First().Head);

            var follows = new Dictionary<Token<ELang>, List<Token<ELang>>>();
            var firsts = new Dictionary<Token<ELang>, List<Token<ELang>>>();
            var rules = ProductionRules.Select(r => r.Head).Distinct();
            foreach (var rule in rules) 
            {
                firsts.Add(rule, GetFirsts(rule));
                follows.Add(rule, GetFollows(rule));
            }

            var rootState = GetStatesTree(new State<ELang>(rootProductionRules, true), states);
            var parserTable = GetParserTable(follows, rootState, rootState);

            return new Parser<ELang>(Lexer, ProductionRules, states, rootState, parserTable);
        }

        public List<Token<ELang>> GetFirsts(Token<ELang> nonTerminalToken, List<Token<ELang>> tokensAlreadyVisited = null)
        {
            tokensAlreadyVisited ??= new List<Token<ELang>>();

            var productionRules = ProductionRules.Where(p => p.Head.Equals(nonTerminalToken));
            var firsts = Enumerable.Empty<Token<ELang>>();

            foreach (var productionRule in productionRules)
            {
                firsts = firsts.Concat(GetFirsts(productionRule, tokensAlreadyVisited));
            }
            return firsts.Distinct().ToList();
        }

        private IEnumerable<Token<ELang>> GetFirsts(ProductionRule<ELang> productionRule, [NotNull] List<Token<ELang>> tokensAlreadyVisited)
        {
            if(!productionRule.CurrentNode.IsNonTerminal) 
            {
                return new List<Token<ELang>>() { productionRule.CurrentNode };
            }

            if(tokensAlreadyVisited.Contains(productionRule.CurrentNode)) return Enumerable.Empty<Token<ELang>>();
            tokensAlreadyVisited.Add(productionRule.CurrentNode);
            
            var firsts = GetFirsts(productionRule.CurrentNode, tokensAlreadyVisited);
            
            // Firsts doesn't contains $ symbol
            if(!firsts.Any(f => f.IsEnd))
            {
                return firsts;
            }

            // For production rule S -> .A B C $,
            // first = { First(A) - e} U First(S -> A .B C $)
            return firsts.Where(n => !n.IsEnd)
                .Union(
                    GetFirsts(productionRule.GetProductionRuleWithShiftedPivot(), tokensAlreadyVisited)
                );
        }

        public List<Token<ELang>> GetFollows([NotNull] Token<ELang> nonTerminalToken, IEnumerable<Token<ELang>> tokensAlreadyVisited = null)
        {
            tokensAlreadyVisited ??= Enumerable.Empty<Token<ELang>>();

            var follows = Enumerable.Empty<Token<ELang>>();

            // Root token places $ symbol
            if(nonTerminalToken.Equals(Token<ELang>.RootToken)) 
            {
                follows = follows.Append(Token<ELang>.EndToken);
            }

            foreach (var productionRule in ProductionRules)
            {
                var nonTerminalIndexes = productionRule.Nodes
                    .Select((node, idx) => new { Node = node, idx = idx })
                    .ToList()
                    .Where(r =>
                        !tokensAlreadyVisited.Contains(r.Node) &&
                        r.Node.Equals(nonTerminalToken)
                    );


                foreach (var nonTerminalIndex in nonTerminalIndexes)
                {
                    //  A → αB
                    // Follow(B) = Follow(A)
                    if (nonTerminalIndex.idx == productionRule.Count)
                    {
                        if (nonTerminalIndex.Node.Equals(productionRule.Head)) continue;
                        
                        follows = follows.Concat(
                                GetFollows(productionRule.Head, tokensAlreadyVisited.Append(nonTerminalToken))
                        );
                        continue;
                    }

                    var nextNode = productionRule.Nodes[nonTerminalIndex.idx + 1];

                    // A → αBβ
                    // If β is terminal, Follow(B) = { β }
                    if (nextNode.IsTerminal)
                    {
                        follows = follows.Append(nextNode);
                        continue;
                    }

                    var firsts = GetFirsts(nextNode);

                    // A → αBβ
                    // If $ ∉ First(β), Follow(B) = First(β)
                    if (!firsts.Any(f => f.IsEnd))
                    {
                        follows = firsts.Concat(firsts);
                        continue;
                    }

                    // A → αBβ
                    // If $ ∈ First(β), Follow(B) = { First(β) - $ } U Follow(A)
                    follows = follows.Concat(
                        firsts.Where(f => !f.IsEnd)
                        .Union(
                            GetFollows(productionRule.Head, tokensAlreadyVisited.Append(nonTerminalToken))
                        )
                    );
                    continue;
                }
            }

            return follows.Distinct().ToList();
        }

        private List<ProductionRule<ELang>> GetProductionRulesFromNonTerminal(Token<ELang> nonTerminalToken, List<Token<ELang>> tokensAlreadyVisited = null)
        {

            tokensAlreadyVisited ??= new List<Token<ELang>>();

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
                resultRules = resultRules.Concat(GetProductionRulesFromNonTerminal(nonVisitedRule, tokensAlreadyVisited));
            }
            return resultRules.ToList();
        }

        private State<ELang> GetStatesTree([NotNull] State<ELang> currentState, [NotNull] List<State<ELang>> states = null)
        {
            states.Add(currentState);

            var productionRuleGroups = currentState.ProductionRules.GroupBy(p => p.CurrentNode).ToList();
            foreach (var producctionRuleGroup in productionRuleGroups)
            {
                if (producctionRuleGroup.All(r => r.IsEnd)) continue;

                var nextProductions = producctionRuleGroup.Select(g => g.GetProductionRuleWithShiftedPivot()).ToList();

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

                IEnumerable<ProductionRule<ELang>> restProductionRules = new List<ProductionRule<ELang>>();
                foreach (var nextProduction in nextProductions)
                {
                    if (!nextProduction.CurrentNode.IsNonTerminal) continue;
                    restProductionRules = restProductionRules
                                            .Union(
                                                GetProductionRulesFromNonTerminal(nextProduction.CurrentNode)
                                            );
                }

                var nextState = new State<ELang>(nextProductions.Concat(restProductionRules).ToList());
                foreach (var productionRule in producctionRuleGroup)
                {
                    if (!productionRule.IsEnd) productionRule.NextState = nextState;
                }
                GetStatesTree(nextState, states);
            }
            return currentState;
        }

        private ParserTable<ELang> GetParserTable(
            Dictionary<Token<ELang>, List<Token<ELang>>> follows,
            State<ELang> rootState,
            State<ELang> currentState,
            ParserTable<ELang> parserTable = null
        )
        {
            parserTable ??= new ParserTable<ELang>();

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
                    var actionState = new ActionState<ELang>(productionRuleIdx == 0 ? ActionType.Accept : ActionType.Reduce, productionRuleIdx);

                    foreach (var token in follows[rule])
                    {
                        parserTable[currentStateId, token] = actionState;
                    }

                    continue;
                }

                {
                    var actionType = productionNode.IsNonTerminal ? ActionType.Goto : ActionType.Shift;
                    var actionState = new ActionState<ELang>(actionType, producctionRule.NextState.Id);
                    parserTable[currentStateId, productionNode] = actionState;
                    GetParserTable(follows, rootState, producctionRule.NextState, parserTable);
                }

            }

            return parserTable;
        }
    }
}