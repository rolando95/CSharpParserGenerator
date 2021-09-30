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

        public ParserGenerator([NotNull] Lexer<ELang> lexer, [NotNull] GrammarRules<ELang> definition)
        {
            Lexer = lexer;
            Lexer.AddTokens(definition.AnonymousTerminalTokens);
            ProductionRules = definition.ProductionRules;
        }

        public Parser<ELang> CompileParser()
        {
            var rootState = new State<ELang>(Closure(ProductionRules.First()).ToList());
            var lrStates = GenerateLr1(rootState);
            var lalrStates = ConvertLr1ToLalr1(lrStates);
            var parserTable = GenerateParserTable(rootState);

            return new Parser<ELang>(Lexer, ProductionRules, lalrStates, rootState, parserTable);
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

        public IEnumerable<Token<ELang>> GetFirsts([NotNull] ProductionRule<ELang> productionRule, [NotNull] List<Token<ELang>> tokensAlreadyVisited)
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

        public List<ProductionRule<ELang>> Closure(ProductionRule<ELang> productionRule)
        {
            return Closure(new List<ProductionRule<ELang>> { productionRule });
        }

        public List<ProductionRule<ELang>> Closure(IEnumerable<ProductionRule<ELang>> productionRules)
        {

            var changes = productionRules;
            do
            {
                var oldChanges = changes;
                changes = Enumerable.Empty<ProductionRule<ELang>>();

                foreach (var closureProductionRule in oldChanges)
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
                            if (!productionRules.Concat(changes).Contains(generated))
                            {
                                changes = changes.Append(generated);
                            }
                            continue;
                        }
                    }
                }
                productionRules = productionRules.Concat(changes);
            } while (changes.Any());
            return productionRules.ToList();
        }

        private List<State<ELang>> GenerateLr1([NotNull] State<ELang> currentState, List<State<ELang>> states = null)
        {
            states ??= new List<State<ELang>> { currentState };
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
                GenerateLr1(nextState, states);
            }
            return states;
        }

        private List<State<ELang>> ConvertLr1ToLalr1(List<State<ELang>> states)
        {
            var result = states.Select(s => s);
            foreach (var state in result)
            {
                var matchState = result.FirstOrDefault(s => s.Id != state.Id && state.ProductionRules.All(c => s.ProductionRules.Any(p => p.Similar(c, true, false))));

                if (matchState != null)
                {
                    state.MergeState(ref matchState);
                }
            }
            return result.Distinct().ToList();
        }

        private ParserTable<ELang> GenerateParserTable(State<ELang> currentState, ParserTable<ELang> parserTable = null)
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
                    var actionType = productionRule.Head.IsRoot ? ActionType.Accept : ActionType.Reduce;
                    var actionState = new ActionState<ELang>(actionType, productionRuleIdx, productionRule);

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
                    GenerateParserTable(productionRule.NextState, parserTable);
                }

            }

            return parserTable;
        }

        private InvalidOperationException GetConflictException(ActionState<ELang> a, ActionState<ELang> b)
        {
            return new InvalidOperationException(
    @$"Conflict detected. Possible {a.Action.ToString()}/{b.Action.ToString()} operations
    To solve this conflict, refactor production rules: 
    {a.ProductionRule.ToString()}
    {b.ProductionRule.ToString()}"
            );
        }
    }
}