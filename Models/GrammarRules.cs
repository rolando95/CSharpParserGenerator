using System;
using System.Collections.Generic;
using System.Linq;

namespace Syntax
{
    public class ProductionRules : List<object[]> { }

    public enum ESymbolNodeTypes
    {
        Pivot, Terminal, NonTerminal, Action
    }

    public class SymbolNode
    {
        public ESymbolNodeTypes Type { get; set; }
        public object Data { get; set; }
    }

    public class GrammarRule
    {
        public SymbolNode Rule { get; set; }
        public List<SymbolNode> ProductionNodes { get; set; }
    }

    public class GrammarRules<ERules> : Dictionary<ERules, List<object[]>>
        where ERules : Enum
    {
        public IEnumerable<GrammarRule> MapGrammarRuleEnumerable()
        {
            Dictionary<ERules, List<object[]>> dictionary = this;

            var rulesList = dictionary.Keys.Distinct().ToList();

            var rules = dictionary
                .AsEnumerable()
                .SelectMany(
                    pair => pair.Value,
                    (pair, pairValue) => new GrammarRule
                    {
                        Rule = new SymbolNode { Type = ESymbolNodeTypes.NonTerminal, Data = pair.Key },
                        ProductionNodes = 
                            new List<SymbolNode>() { new SymbolNode { Type = ESymbolNodeTypes.Pivot }}
                            .Concat(pairValue.Select(node => new SymbolNode
                            {
                                Data = node,
                                Type = !(node is Enum) ? ESymbolNodeTypes.Action
                                        : rulesList.Contains((ERules)node) ? ESymbolNodeTypes.NonTerminal
                                        : ESymbolNodeTypes.Terminal


                            })).ToList()
                    }
                ).ToList();
            return rules;
        }
    }

}