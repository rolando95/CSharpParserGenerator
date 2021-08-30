using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CSharpParserGenerator
{

    public class DefinitionRules : List<List<Token>> { }

    public class SyntaxDefinition<ELang> where ELang : Enum
    {
        public List<ProductionRule<ELang>> ProductionRules { get; }

        public SyntaxDefinition(Dictionary<ELang, DefinitionRules> productionRules)
        {
            ProductionRules = MapProductionRuleEnumerable(productionRules);
        }

        private List<ProductionRule<ELang>> MapProductionRuleEnumerable(Dictionary<ELang, DefinitionRules> dictionary)
        {
            var nonTerminalEnums = dictionary.Keys.Distinct().ToList();
            var firstNonTerminalToken = new Token<ELang>(ETokenTypes.NonTerminal, nonTerminalEnums.FirstOrDefault());

            var rules = new List<ProductionRule<ELang>>() { new ProductionRule<ELang>(Token<ELang>.RootToken(), new List<Token<ELang>>() { firstNonTerminalToken }) };

            var definitionRules = dictionary
                .AsEnumerable()
                .SelectMany(pair => pair.Value, (pair, pairValue) => new { Head = pair.Key, Nodes = pairValue })
                .ToList();

            foreach (var definitionRule in definitionRules)
            {
                var nodes = Enumerable.Empty<Token<ELang>>();
                var definitionNodes = definitionRule.Nodes;

                Op operation = null;

                var idx = -1;
                foreach (var definitionNode in definitionNodes)
                {
                    ++idx;
                    if (definitionNode.Op != null)
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
                        rules.Add(new ProductionRule<ELang>(anonymous, new List<Token<ELang>>(), definitionNode.Op, -idx));
                        continue;
                    }

                    var symbol = (ELang)Enum.ToObject(typeof(ELang), definitionNode.Symbol);
                    var type = nonTerminalEnums.Contains(symbol) ? ETokenTypes.NonTerminal : ETokenTypes.Terminal;
                    nodes = nodes.Append(new Token<ELang>(type, symbol));
                }

                var head = new Token<ELang>(ETokenTypes.NonTerminal, definitionRule.Head);
                rules.Add(new ProductionRule<ELang>(head, nodes, operation));
            }
            return rules;
        }
    }
}

#nullable disable