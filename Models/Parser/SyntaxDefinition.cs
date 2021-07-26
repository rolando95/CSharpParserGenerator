using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Syntax
{

    public class DefinitionRules : List<List<Token>> { }

    public class SyntaxDefinition<ERule> where ERule : Enum
    {
        public List<ProductionRule<ERule>> ProductionRules { get; }

        public SyntaxDefinition(Dictionary<ERule, DefinitionRules> productionRules)
        {
            ProductionRules = MapProductionRuleEnumerable(productionRules);
        }

        private List<ProductionRule<ERule>> MapProductionRuleEnumerable(Dictionary<ERule, DefinitionRules> dictionary)
        {
            var rulesList = dictionary.Keys.Distinct().ToList();
            var definitionRules = dictionary
                .AsEnumerable()
                .SelectMany(pair => pair.Value, (pair, pairValue) => new { Rule = pair.Key, Nodes = pairValue });

            var rules = new List<ProductionRule<ERule>>();
            foreach (var definitionRule in definitionRules)
            {
                var nodes = new List<Token<ERule>>();
                var definitionNodes = definitionRule.Nodes;

                foreach (var definitionNode in definitionNodes)
                {
                    if (definitionNode.Type == ETokenTypes.Operation)
                    {
                        if (!nodes.Any()) throw new InvalidOperationException("First production node must be an Terminal / NonTerminal token instead of Operation");
                        var last = nodes.Last();
                        if (last.Op != null) throw new InvalidOperationException("More than one operation after a Terminal / NonTerminal token is not allowed");
                        last.Op = definitionNode.Op;
                        continue;
                    }

                    var type = rulesList.Contains((ERule)definitionNode.Symbol) ? ETokenTypes.NonTerminal : ETokenTypes.Terminal;
                    nodes.Add(new Token<ERule>((ERule)definitionNode.Symbol, definitionNode.Op, type));
                }

                var rule = new Token<ERule>((ERule)definitionRule.Rule, type: ETokenTypes.NonTerminal);
                rules.Add(new ProductionRule<ERule>(rule, nodes));

            }

            return rules;
        }
    }
}

#nullable disable