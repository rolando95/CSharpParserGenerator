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
        public List<ProductionRule<ELang>> ProductionRules { get; }

        public State(List<ProductionRule<ELang>> productionRules, bool isRoot = false)
        {
            ProductionRules = productionRules;
            if (isRoot) Id = Sequence.Reset();
        }

        public bool Equals(State<ELang> other) => Id.Equals(other.Id) || ProductionRules.SequenceEqual(other.ProductionRules);
        public bool Equals(IEnumerable<ProductionRule<ELang>> other) => ProductionRules.SequenceEqual(other);

        public override int GetHashCode() => new { ProductionRules }.GetHashCode();
        public override bool Equals(object o) => Equals(o as State<ELang>);
        public bool Contains(IEnumerable<ProductionRule<ELang>> other) => other.All(p => ProductionRules.Contains(p));

    }
}