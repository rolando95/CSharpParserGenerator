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
            var rootProductionRules = GetProductionRulesFromNonTerminal(ProductionRules.First())
                                        .Prepend(ProductionRules.First())
                                        .ToList();

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
            if (!productionRule.CurrentNode.IsNonTerminal)
            {
                return new List<Token<ELang>>() { productionRule.CurrentNode };
            }

            if (tokensAlreadyVisited.Contains(productionRule.CurrentNode)) return Enumerable.Empty<Token<ELang>>();
            tokensAlreadyVisited.Add(productionRule.CurrentNode);

            var firsts = GetFirsts(productionRule.CurrentNode, tokensAlreadyVisited);

            // Firsts doesn't contains $ symbol
            if (!firsts.Any(f => f.IsEnd))
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
            if (nonTerminalToken.Equals(Token<ELang>.RootToken()))
            {
                follows = follows.Append(Token<ELang>.EndToken());
            }

            foreach (var productionRule in ProductionRules)
            {
                var nonTerminalIndexes = productionRule.Nodes
                    .Select((node, idx) => new { Node = node, idx = idx })
                    .Where(r =>
                        !tokensAlreadyVisited.Contains(r.Node) &&
                        r.Node.Equals(nonTerminalToken)
                    )
                    .Select(n => n.idx);

                if (!nonTerminalIndexes.Any()) continue;

                foreach (var nonTerminalIndex in nonTerminalIndexes)
                {
                    foreach (var nextNode in productionRule.Nodes.Skip(nonTerminalIndex + 1))
                    {
                        //  A → αB
                        // Follow(B) = Follow(A)
                        if (nextNode.IsEnd)
                        {
                            follows = follows.Concat(GetFollows(productionRule.Head, tokensAlreadyVisited.Append(nonTerminalToken)));
                            break;
                        }

                        // A → αBβ
                        // If β is terminal, Follow(B) = { β }
                        if (nextNode.IsTerminal)
                        {
                            follows = follows.Append(nextNode);
                            break;
                        }

                        var firsts = GetFirsts(nextNode);

                        // A → αBβ
                        // If $ ∉ First(β), Follow(B) = First(β)
                        if (!firsts.Any(f => f.IsEnd))
                        {
                            follows = follows.Concat(firsts);
                            break;
                        }

                        // A → αBβ
                        // If $ ∈ First(β), Follow(B) = First(β) - $
                        follows = follows.Concat(firsts.Where(f => !f.IsEnd));
                    }
                }
            }

            return follows.Distinct().ToList();
        }

        private List<ProductionRule<ELang>> GetProductionRulesFromNonTerminal(ProductionRule<ELang> productionRule, List<Token<ELang>> tokensAlreadyVisited = null)
        {

            var nonTerminalToken = productionRule.CurrentNode;
            tokensAlreadyVisited ??= new List<Token<ELang>>();

            var generatedRules = ProductionRules.Where(r => r.Head.Equals(nonTerminalToken)).ToList();
            tokensAlreadyVisited.Add(nonTerminalToken);


            var nonVisitedRules = generatedRules
                .Where(g => g.Nodes.Any())
                .Where(n => n.CurrentNode.IsNonTerminal && !tokensAlreadyVisited.Contains(n.CurrentNode))
                .Distinct();

            var resultRules = generatedRules.Select(r => r);
            if (!nonVisitedRules.Any()) return resultRules.ToList();


            foreach (var nonVisitedRule in nonVisitedRules)
            {
                if (tokensAlreadyVisited.Contains(nonVisitedRule.CurrentNode)) continue;
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

                var matchExistingState = states.FirstOrDefault(s => s.Contains(nextProductions));
                if (matchExistingState != null)
                {
                    foreach (var producctionRule in producctionRuleGroup)
                    {
                        producctionRule.NextState = matchExistingState;
                    }
                    continue;
                }

                IEnumerable<ProductionRule<ELang>> restProductionRules = new List<ProductionRule<ELang>>();
                foreach (var nextProduction in nextProductions)
                {
                    if (!nextProduction.CurrentNode.IsNonTerminal) continue;
                    restProductionRules = restProductionRules
                                            .Union(
                                                GetProductionRulesFromNonTerminal(nextProduction)
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

            foreach (var productionRule in currentState.ProductionRules)
            {
                var currentStateId = currentState.Id;
                var productionNode = productionRule.CurrentNode;



                // Reduce
                if (productionRule.IsEnd)
                {
                    var rule = productionRule.Head;
                    var productionRuleIdx = ProductionRules.FindIndex(p => p.Similar(productionRule));
                    var actionState = new ActionState<ELang>(productionRuleIdx == 0 ? ActionType.Accept : ActionType.Reduce, productionRuleIdx, productionRule);

                    foreach (var token in follows[rule])
                    {
                        // Check conflicts
                        var currentActionState = parserTable[currentStateId, token];
                        var result = currentActionState?.Equals(actionState);
                        if (currentActionState != null && !currentActionState.Equals(actionState))
                        {
                            throw GetConflictException(currentActionState, actionState);
                        }

                        parserTable[currentStateId, token] = actionState;
                    }

                    continue;
                }

                // Goto / Shift
                {
                    var actionType = productionNode.IsNonTerminal ? ActionType.Goto : ActionType.Shift;
                    var actionState = new ActionState<ELang>(actionType, productionRule.NextState.Id, productionRule);

                    // Check conflicts
                    var currentActionState = parserTable[currentStateId, productionNode];
                    if (currentActionState != null && !actionState.Action.Equals(currentActionState.Action))
                    {
                        throw GetConflictException(currentActionState, actionState);
                    }

                    if (currentActionState != null) continue;

                    parserTable[currentStateId, productionNode] = actionState;
                    GetParserTable(follows, rootState, productionRule.NextState, parserTable);
                }

            }

            return parserTable;
        }

        private InvalidOperationException GetConflictException(ActionState<ELang> a, ActionState<ELang> b)
        {
            return new InvalidOperationException(
    @$"Conflict detected. Possible {a.Action.ToString()}/{b.Action.ToString()} operations
    To solve this conflict, refactor production rules: 
    {a.ProductionRule.StringProductionRule}
    {b.ProductionRule.StringProductionRule}"
            );
        }
    }
}