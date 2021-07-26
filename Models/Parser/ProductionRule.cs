using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Utils.Extensions;
using Utils.Sequence;

namespace Syntax
{
    [DebuggerDisplay("{StringProductionRule}")]
    public class ProductionRule<ERule> : IEquatable<ProductionRule<ERule>> where ERule : Enum
    {
        public Guid Id { get; }
        public Token<ERule> Head { get; }
        public List<Token<ERule>> Nodes { get; }
        public Token<ERule> CurrentNode { get; }
        public bool IsEnd { get; }
        public State<ERule> NextState { get; set; }
        private int PivotIdx { get; }

        public override bool Equals(object other) => Equals(other as ProductionRule<ERule>);
        public override int GetHashCode() => new { Head, Nodes }.GetHashCode();
        public bool Equals(ProductionRule<ERule> other)
        {
            var result = Id.Equals(other.Id) ||
            (
                Head.Equals(other.Head) &&
                Nodes.SequenceEqual(other.Nodes)
            );
            return result;
        }

        /// <summary>
        /// Compares 2 production rules only by terminal and non-terminal symbols
        /// </summary>
        /// <returns></returns>
        public bool Similar(ProductionRule<ERule> other) =>
                Head.Equals(other.Head) &&
                Nodes.Where(n => n.IsTerminal || n.IsNonTerminal)
                .SequenceEqual(other.Nodes.Where(n => n.IsTerminal || n.IsNonTerminal));

        public ProductionRule<ERule> GetProductionRuleWithPivotShifted()
        {
            if (IsEnd) throw new InvalidOperationException("It is not possible to take a shift when you are already at the end of production");

            var nodesWithShiftedPivot = Nodes.Copy();
            nodesWithShiftedPivot.Swap(PivotIdx, PivotIdx + 1);

            return new ProductionRule<ERule>(Head, nodesWithShiftedPivot);
        }

        public ProductionRule(Token<ERule> head, IEnumerable<Token<ERule>> nodes)
        {
            Id = Guid.NewGuid();
            Head = head;

            Nodes = nodes.ToList();
            PivotIdx = Nodes.FindIndex(n => n.IsPivot);

            var pivotNode = new Token<ERule>(type: ETokenTypes.Pivot);
            var endNode = new Token<ERule>(type: ETokenTypes.End);

            if (PivotIdx < 0)
            {
                Nodes = nodes.Prepend(pivotNode).Append(endNode).ToList();
                PivotIdx = 0;
            }

            CurrentNode = Nodes[PivotIdx + 1];
            IsEnd = CurrentNode.Equals(endNode);
        }

        // Only to display in the debbuger
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public string StringProductionRule
        {
            get
            {
                var strNodes = Nodes.Select(n =>
                    (n.IsNonTerminal || n.IsTerminal) ? $"{n.Symbol.ToString()}{(n.Op != null ? "*" : "")}"
                    : n.IsPivot ? "."
                    : n.IsEnd ? "$"
                    : "UNKNOWN"
                );
                return $"{Head.Symbol} -> {string.Join(" ", strNodes)}".Replace(". ", ".");
            }
        }
    }
}