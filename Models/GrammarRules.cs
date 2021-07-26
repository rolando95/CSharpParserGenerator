using System;
using System.Collections.Generic;
using System.Linq;

namespace Syntax
{
    public class ProductionRules : List<SymbolNode[]> { }

    public enum ESymbolNodeTypes
    {
        UndefinedTokenType, Pivot, Terminal, NonTerminal, Op
    }

    public class SymbolNode
    {
        public ESymbolNodeTypes Type { get; set; }
        public Enum Data { get; set; } = null;
        public Op Op { get; set; } = null;

        public bool IsNotTerminal => Type == ESymbolNodeTypes.NonTerminal;
        public bool IsOp => Type == ESymbolNodeTypes.Op;

        public static implicit operator SymbolNode(Enum e) => new SymbolNode() { Type = ESymbolNodeTypes.UndefinedTokenType, Data = e };
        public static implicit operator SymbolNode(Op op) => new SymbolNode() { Type = ESymbolNodeTypes.Op, Op = op };
    }

    public class GrammarRule
    {
        public SymbolNode Rule { get; set; }
        public IEnumerable<SymbolNode> ProductionNodes { get; set; }
    }

    public class GrammarRules<ERules> : Dictionary<ERules, List<SymbolNode[]>>
        where ERules : Enum
    {
        public IEnumerable<GrammarRule> MapGrammarRuleEnumerable()
        {
            Dictionary<ERules, List<SymbolNode[]>> dictionary = this;

            var rulesList = dictionary.Keys.Distinct().ToList();
            var rules = dictionary
                .AsEnumerable()
                .SelectMany(pair => pair.Value, (pair, pairValue) => new GrammarRule
                {
                    Rule = new SymbolNode { Type = ESymbolNodeTypes.NonTerminal, Data = pair.Key },
                    ProductionNodes =
                        new List<SymbolNode>() { new SymbolNode { Type = ESymbolNodeTypes.Pivot } }
                        .Concat(pairValue.Select(node =>
                        {
                            node.Type =
                                (!node.IsOp && rulesList.Contains((ERules)node.Data)) ? ESymbolNodeTypes.NonTerminal
                                : ESymbolNodeTypes.Terminal;
                            return node;
                        })).ToList()
                });
            return rules;
        }
    }

}