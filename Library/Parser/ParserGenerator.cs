using System;
using System.Collections.Generic;


namespace Syntax
{
    public class ParserGenerator<ERule>
        where ERule : Enum
    {
        private IEnumerable<GrammarRule> Rules { get; set; }

        public ParserGenerator(GrammarRules<ERule> rules)
        {
            Rules = rules.MapGrammarRuleEnumerable();
        }

        public void CompileParser()
        {

        }
    }
}