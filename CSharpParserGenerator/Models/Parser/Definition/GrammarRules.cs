using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CSharpParserGenerator
{
    public class GrammarRules<ELang> where ELang : Enum
    {
        public List<ProductionRule<ELang>> ProductionRules { get; }

        public GrammarRules(Dictionary<ELang, Token[][]> productionRules)
        {
            ProductionRules = MapProductionRuleEnumerable(productionRules);
        }

        private List<ProductionRule<ELang>> MapProductionRuleEnumerable(Dictionary<ELang, Token[][]> dictionary)
        {
            var nonTerminalEnums = dictionary.Keys.Distinct().ToList();
            var firstNonTerminalToken = new Token<ELang>(ETokenTypes.NonTerminal, nonTerminalEnums.FirstOrDefault());

            var productionRules = new List<ProductionRule<ELang>>()
            {
                new ProductionRule<ELang>(
                    head: Token<ELang>.RootToken(),
                    nodes: new List<Token<ELang>>() { firstNonTerminalToken },
                    lookAhead: Token<ELang>.EndToken()
                )
            };

            var definitionRules = dictionary
                .AsEnumerable()
                .SelectMany(pair => pair.Value, (pair, pairValue) => new { Head = pair.Key, Nodes = pairValue })
                .ToList();

            foreach (var definitionRule in definitionRules)
            {
                var head = new Token<ELang>(ETokenTypes.NonTerminal, definitionRule.Head);
                var nodes = Enumerable.Empty<Token<ELang>>();
                var definitionNodes = definitionRule.Nodes ?? new Token[0];
                Op operation = null;

                var idx = -1;
                foreach (var definitionNode in definitionNodes)
                {
                    ++idx;
                    if (definitionNode.IsOperation)
                    {
                        // Semantic action at end
                        if (definitionNode == definitionNodes.Last())
                        {
                            operation = definitionNode.Op;
                            continue;
                        }

                        // Semantic action at middle
                        var anonymous = Token<ELang>.AnonymousNonTerminalToken();
                        nodes = nodes.Append(anonymous);
                        productionRules.Add(new ProductionRule<ELang>(
                            head: anonymous,
                            nodes: new List<Token<ELang>>(),
                            operation: definitionNode.Op,
                            shiftPointerIdxOnReduce: -idx
                        ));
                        continue;
                    }

                    var symbol = (ELang)Enum.ToObject(typeof(ELang), definitionNode.Symbol);
                    var type = nonTerminalEnums.Contains(symbol) ? ETokenTypes.NonTerminal : ETokenTypes.Terminal;
                    nodes = nodes.Append(new Token<ELang>(type, symbol));
                }


                productionRules.Add(new ProductionRule<ELang>(
                    head: head,
                    nodes: nodes.ToList(),
                    operation: operation
                ));
            }
            return productionRules;
        }
    }
}

#nullable disable