using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Utils.Extensions;
using Utils.Sequence;

namespace Syntax
{
    [DebuggerDisplay("State {Id}")]
    public class State<ERule> : IEquatable<State<ERule>> where ERule : Enum
    {
        private static Sequence Sequence { get; } = new Sequence();
        public int Id { get; } = Sequence.Next();
        public List<ProductionRule<ERule>> ProductionRules { get; }

        public State(List<ProductionRule<ERule>> productionRules, bool isRoot = false)
        {
            ProductionRules = productionRules;
            if (isRoot) Id = Sequence.Reset();
        }

        public bool Equals(State<ERule> other) => Id.Equals(other.Id) || ProductionRules.SequenceEqual(other.ProductionRules);
        public bool Equals(IEnumerable<ProductionRule<ERule>> other) => ProductionRules.SequenceEqual(other);

        public override int GetHashCode() => new { ProductionRules }.GetHashCode();
        public override bool Equals(object o) => Equals(o as State<ERule>);
        public bool Contains(ProductionRule<ERule> productionRule) => ProductionRules.Contains(productionRule);

    }
}