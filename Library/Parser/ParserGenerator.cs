using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Syntax
{
    public class ParserGenerator<ERule>
        where ERule : Enum
    {
        private IEnumerable<GrammarRule> Rules { get; set; }

        public ParserGenerator([NotNull] GrammarRules<ERule> rules)
        {
            Rules = rules.MapGrammarRuleEnumerable();
        }

        public void CompileParser()
        {
            if(!Rules.Any()) return;
        }
    }
}