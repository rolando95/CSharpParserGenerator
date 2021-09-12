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
            var rootState = new State<ELang>(Closure(ProductionRules.First()).ToList());
            var states = new List<State<ELang>>() { rootState };

            rootState = GetStatesTree(rootState, states);
            states = MergeLALR1States(ref states);
            var parserTable = GetParserTable(rootState, rootState);

            return new Parser<ELang>(Lexer, ProductionRules, states, rootState, parserTable);
        }

        public List<Token<ELang>> GetFirsts([NotNull] Token<ELang> token, List<Token<ELang>> tokensAlreadyVisited = null)
        {
            if (!token.IsNonTerminal) return new List<Token<ELang>> { token };

            tokensAlreadyVisited ??= new List<Token<ELang>>();

            var productionRules = ProductionRules.Where(p => p.Head.Equals(token));
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


        private List<ProductionRule<ELang>> Closure(ProductionRule<ELang> productionRule)
        {
            return Closure(new List<ProductionRule<ELang>> { productionRule });
        }


        private List<ProductionRule<ELang>> Closure(IEnumerable<ProductionRule<ELang>> productionRules)
        {

            bool changes;
            do
            {
                changes = false;
                var closureProductionRules = productionRules;

                foreach (var closureProductionRule in closureProductionRules)
                {
                    var node = closureProductionRule.CurrentNode;
                    var nextNode = closureProductionRule.NextNode();
                    if (!node.IsNonTerminal) continue;

                    // Next node is a non terminal
                    var firsts = !nextNode.IsEnd ? GetFirsts(closureProductionRule.GetProductionRuleWithShiftedPivot(), new List<Token<ELang>>()) : Enumerable.Empty<Token<ELang>>();
                    if (!firsts.Any() || firsts.Any(f => f.IsEnd)) firsts = firsts.Append(closureProductionRule.LookAhead);
                    foreach (var productionRule in ProductionRules.Where(p => p.Head.Equals(node)))
                    {
                        foreach (var lookAhead in firsts)
                        {
                            var generated = productionRule.GetCopyWithAnotherLookAhead(lookAhead);
                            if (!productionRules.Contains(generated))
                            {
                                productionRules = productionRules.Append(generated);
                                changes = true;
                            }
                            continue;
                        }
                    }
                }
            } while (changes);
            return productionRules.ToList();
        }

        private State<ELang> GetStatesTree([NotNull] State<ELang> currentState, [NotNull] List<State<ELang>> states)
        {
            var productionRuleGroups = currentState.ProductionRules.GroupBy(p => p.CurrentNode).ToList();
            foreach (var productionRuleGroup in productionRuleGroups)
            {
                if (productionRuleGroup.All(r => r.IsEnd)) continue;

                var nextProductions = productionRuleGroup.Select(g => g.GetProductionRuleWithShiftedPivot()).ToList();

                //Check if there is any state that contains all productions
                var matchExistingState = states.FirstOrDefault(s => s.Contains(nextProductions));
                if (matchExistingState != null)
                {
                    foreach (var productionRule in productionRuleGroup)
                    {
                        if (!productionRule.IsEnd) productionRule.NextState = matchExistingState;
                    }
                    continue;
                }

                var closure = Closure(nextProductions);
                var nextState = new State<ELang>(closure);
                states.Add(nextState);
                foreach (var productionRule in productionRuleGroup)
                {
                    if (!productionRule.IsEnd) productionRule.NextState = nextState;
                }
                GetStatesTree(nextState, states);
            }
            return currentState;
        }

        private List<State<ELang>> MergeLALR1States(ref List<State<ELang>> states)
        {
            foreach (var state in states)
            {
                var matchState = states.FirstOrDefault(s => s.Id != state.Id && state.ProductionRules.All(c => s.ProductionRules.Any(p => p.Similar(c, true, false))));

                if (matchState != null)
                {
                    state.MergeState(ref matchState);
                }
            }
            return states.Distinct().ToList();
        }

        private ParserTable<ELang> GetParserTable(
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
                    var productionRuleIdx = ProductionRules.FindIndex(p => p.Similar(productionRule, false, false));
                    var actionState = new ActionState<ELang>(productionRuleIdx == 0 ? ActionType.Accept : ActionType.Reduce, productionRuleIdx, productionRule);

                    var token = productionRule.LookAhead;
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
                    GetParserTable(rootState, productionRule.NextState, parserTable);
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