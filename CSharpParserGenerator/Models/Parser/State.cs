using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Utils.Sequence;

namespace CSharpParserGenerator
{
    [DebuggerDisplay("State {Id}")]
    public class State<ELang> : IEquatable<State<ELang>> where ELang : Enum
    {
        private static Sequence Sequence { get; } = new Sequence();

        public int Id { get; } = Sequence.Next();


        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IEnumerable<ProductionRule<ELang>> _productionRules { get; set; }

        public IEnumerable<ProductionRule<ELang>> ProductionRules => _productionRules;

        public State(List<ProductionRule<ELang>> productionRules, bool isRoot = false)
        {
            _productionRules = productionRules;
            if (isRoot) Id = Sequence.Reset();
        }

        public bool Equals(State<ELang> other) => Id.Equals(other.Id) || ProductionRules.SequenceEqual(other.ProductionRules);
        public bool Equals(IEnumerable<ProductionRule<ELang>> other) => ProductionRules.SequenceEqual(other);

        public override int GetHashCode() => new { ProductionRules }.GetHashCode();
        public override bool Equals(object o) => Equals(o as State<ELang>);
        public bool Contains(IEnumerable<ProductionRule<ELang>> other) => other.All(p => ProductionRules.Contains(p));

        public void AddProductionRules(IEnumerable<ProductionRule<ELang>> productionRules)
        {
            _productionRules = _productionRules.Concat(productionRules).Distinct();
        }
    }
}