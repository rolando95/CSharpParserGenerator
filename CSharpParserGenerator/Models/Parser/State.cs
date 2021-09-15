using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Utils.Sequence;

namespace CSharpParserGenerator
{
    [DebuggerDisplay("State {Alias}")]
    public class State<ELang> : BaseSequence, IEquatable<State<ELang>> where ELang : Enum
    {

        public string Alias { get; private set; }
        public IEnumerable<ProductionRule<ELang>> ProductionRules { get; private set; }

        public State(List<ProductionRule<ELang>> productionRules)
        {
            ProductionRules = productionRules;
            Alias = Id.ToString();
        }

        public bool Equals(State<ELang> other) => Id.Equals(other.Id);

        public override int GetHashCode() => new { Id }.GetHashCode();
        public override bool Equals(object o) => Equals(o as State<ELang>);
        public bool Contains(IEnumerable<ProductionRule<ELang>> other) => other.All(p => ProductionRules.Contains(p));

        public void MergeState(ref State<ELang> other)
        {
            AddProductionRules(other.ProductionRules);
            other.AddProductionRules(ProductionRules);
            other.Id = Id;
            var alias = $"{Alias}-{other.Alias}";
            other.Alias = alias;
            Alias = alias;
        }

        public void AddProductionRules(IEnumerable<ProductionRule<ELang>> productionRules)
        {
            ProductionRules = ProductionRules.Concat(productionRules).Distinct().ToList();
        }
    }
}