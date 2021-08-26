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
            var rulesList = dictionary.Keys.Distinct().ToList();
            var definitionRules = dictionary
                .AsEnumerable()
                .SelectMany(pair => pair.Value, (pair, pairValue) => new { Rule = pair.Key, Nodes = pairValue });

            var rules = new List<ProductionRule<ELang>>();
            foreach (var definitionRule in definitionRules)
            {
                var nodes = new List<Token<ELang>>() { Token<ELang>.PivotToken };
                var definitionNodes = definitionRule.Nodes;

                foreach (var definitionNode in definitionNodes)
                {
                    if (definitionNode.Type == ETokenTypes.Operation)
                    {
                        var last = nodes.Last();

                        if (last.HasOperations) last.Operations.AddRange(definitionNode.Operations);
                        else last.Operations = definitionNode.Operations;

                        continue;
                    }

                    var type = rulesList.Contains((ELang)definitionNode.Symbol) ? ETokenTypes.NonTerminal : ETokenTypes.Terminal;
                    nodes.Add(new Token<ELang>((ELang)definitionNode.Symbol, definitionNode.Operations, type));
                }

                var rule = new Token<ELang>((ELang)definitionRule.Rule, type: ETokenTypes.NonTerminal);
                rules.Add(new ProductionRule<ELang>(rule, nodes));
            }

            var root = new ProductionRule<ELang>(Token<ELang>.RootToken, new List<Token<ELang>>() { rules.FirstOrDefault()?.Head });
            rules.Insert(0, root);
            return rules;
        }
    }
}

#nullable disable