using System;
using System.Collections.Generic;
using System.Linq;
using Utils.Sequence;

namespace CSharpParserGenerator
{
    public class GrammarRules<ELang> where ELang : Enum
    {
        public List<ProductionRule<ELang>> ProductionRules { get; }
        public List<LexerToken<ELang>> AnonymousTerminalTokens { get; private set; }

        public GrammarRules(Dictionary<ELang, Token[][]> productionRules)
        {
            ProductionRules = MapProductionRuleEnumerable(productionRules);
        }

        private List<ProductionRule<ELang>> MapProductionRuleEnumerable(Dictionary<ELang, Token[][]> dictionary)
        {
            AnonymousTerminalTokens = new List<LexerToken<ELang>>();

            var anonymousTokenSequence = new Sequence();
            var nonTerminalEnums = dictionary.Keys.Distinct().ToList();
            var firstNonTerminalToken = new Token<ELang>(type: ETokenTypes.NonTerminal, symbol: nonTerminalEnums.FirstOrDefault());

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
                        var anonymous = Token<ELang>.AnonymousNonTerminalToken(anonymousTokenSequence.Next().ToString());
                        nodes = nodes.Append(anonymous);
                        productionRules.Add(new ProductionRule<ELang>(
                            head: anonymous,
                            nodes: new List<Token<ELang>>(),
                            operation: definitionNode.Op,
                            shiftPointerIdxOnReduce: -idx
                        ));
                        continue;
                    }

                    if (definitionNode.IsAnonymous)
                    {
                        var token = new Token<ELang>(type: ETokenTypes.AnonymousTerminal, name: definitionNode.Name);
                        nodes = nodes.Append(token);
                        AnonymousTerminalTokens.Add(new LexerToken<ELang>(token, definitionNode.Name));
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