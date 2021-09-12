using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Utils.Sequence;
using Id = System.Int64;

namespace CSharpParserGenerator
{
    [DebuggerDisplay("State {Alias}")]
    public class State<ELang> : IEquatable<State<ELang>> where ELang : Enum
    {
        private static Sequence Sequence { get; } = new Sequence();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Id _Id { get; set; } = Sequence.Next();
        public Id Id => _Id;

        public string Alias { get; set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IEnumerable<ProductionRule<ELang>> _ProductionRules { get; set; }
        public IEnumerable<ProductionRule<ELang>> ProductionRules => _ProductionRules;

        public State(List<ProductionRule<ELang>> productionRules)
        {
            _ProductionRules = productionRules;
            Alias = _Id.ToString();
        }

        public bool Equals(State<ELang> other) => Id.Equals(other.Id);

        public override int GetHashCode() => new { Id }.GetHashCode();
        public override bool Equals(object o) => Equals(o as State<ELang>);
        public bool Contains(IEnumerable<ProductionRule<ELang>> other) => other.All(p => ProductionRules.Contains(p));

        public void MergeState(ref State<ELang> other)
        {
            AddProductionRules(other.ProductionRules);
            other.AddProductionRules(ProductionRules);
            other._Id = Id;
            var alias = $"{Alias}-{other.Alias}";
            other.Alias = alias;
            Alias = alias;
        }

        public void AddProductionRules(IEnumerable<ProductionRule<ELang>> productionRules)
        {
            _ProductionRules = _ProductionRules.Concat(productionRules).Distinct().ToList();
        }
    }
}