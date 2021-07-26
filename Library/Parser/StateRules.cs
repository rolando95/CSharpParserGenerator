using System;
using System.Collections.Generic;
using System.Linq;

namespace Syntax
{
    public class State
    {
        public List<StateRule> StateRules { get; set; }
    }

    public class StateRule {
        public List<SymbolNode> State { get; set; }

        public State NextState { get ; set;}
    }

    static class Mapper
    {
        public static IEnumerable<GrammarRule> MapRulesGeneratedFromNonTerminal<T>(T nonTerminalSymbol, IEnumerable<GrammarRule> rules, List<T> symbolsAlreadyVisited = null) where T : Enum
        {
            
            symbolsAlreadyVisited ??= new List<T>();
            
            var generatedRules = rules.Where(r => r.Rule.Data.Equals(nonTerminalSymbol)).ToList();
            symbolsAlreadyVisited.Add(nonTerminalSymbol);

            var nonVisitedRules = generatedRules
                .SelectMany(g => g.ProductionNodes)
                .Where(g =>  g.Type == ESymbolNodeTypes.NonTerminal && !symbolsAlreadyVisited.Contains((T)g.Data))
                .Distinct().ToList();
            
            if(!nonVisitedRules.Any()) return generatedRules;

            IEnumerable<GrammarRule> resultRules = generatedRules;
            foreach(var nonVisitedRule in nonVisitedRules)
            {
                var symbol = (T)nonVisitedRule.Data;
                if(symbolsAlreadyVisited.Contains(symbol)) continue;
                resultRules = resultRules.Concat( MapRulesGeneratedFromNonTerminal<T>(symbol, rules, symbolsAlreadyVisited));
            }
            return resultRules;
        }
    }
}