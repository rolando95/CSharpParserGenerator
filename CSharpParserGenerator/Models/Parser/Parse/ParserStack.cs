using System;
using System.Collections.Generic;
using System.Linq;
using Id = System.Int64;

namespace CSharpParserGenerator
{
    public class ParserStack
    {
        private int PointerIdx { get; set; }
        private Id InitialStateId { get; }
        private List<dynamic> Values { get; set; }
        public dynamic CurrentValue => Values.Last();

        private List<Id> States { get; set; }
        public Id CurrentState => States.Last();

        public ParserStack(Id initialStateId)
        {
            InitialStateId = initialStateId;
            Values = new List<dynamic>() { null };
            States = new List<Id>() { initialStateId };
        }

        public dynamic this[int idx]
        {
            get => Values[PointerIdx + idx];
            set => Values[PointerIdx + idx] = value;
        }

        public void Shift<ELang>(LexerNode<ELang> node, ActionState<ELang> action) where ELang : Enum
        {
            Values.Add(node.Substring);
            States.Add(action.To);
        }

        public void Reduce<ELang>(ActionState<ELang> action, List<ProductionRule<ELang>> productionRules) where ELang : Enum
        {
            var productionRule = productionRules[Convert.ToInt32(action.To)];
            var size = productionRule.Nodes.Count - 2;
            var pivot = Values.Count - size;

            PointerIdx = pivot + productionRule.ShiftPointerIdxOnReduce;

            if (productionRule.IsEmpty)
            {
                Values.Add(null);
                States.Add(States.Last());
            }

            if (productionRule.Operation != null)
            {
                productionRule.Operation.Callback(this);
            }

            if (size > 1)
            {
                Values.RemoveRange(pivot + 1, size - 1);
                States.RemoveRange(pivot + 1, size - 1);
            }

            States[pivot] = States[pivot - 1];

        }

        public void Goto<ELang>(ActionState<ELang> action) where ELang : Enum
        {
            States[States.Count - 1] = action.To;
        }
    }
}