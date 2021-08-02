using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Utils.Extensions;
using Utils.Sequence;

namespace CSharpParserGenerator
{
    [DebuggerDisplay("{StringProductionRule}")]
    public class ProductionRule<ELang> : IEquatable<ProductionRule<ELang>> where ELang : Enum
    {
        /// <summary>
        /// Gets the number of nodes that the production contains ignoring the pivot and end
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Get nodes idx ignoring the pivot and end
        /// </summary>
        /// 
        public Token<ELang> this[int idx] => Nodes.Where(n => n.IsTerminal || n.IsNonTerminal).ToList()[idx];

        public Guid Id { get; }
        public Token<ELang> Head { get; }
        public List<Token<ELang>> Nodes { get; }
        public Token<ELang> CurrentNode { get; }
        public bool IsEnd { get; }
        public State<ELang> NextState { get; set; }
        private int PivotIdx { get; }

        public override bool Equals(object other) => Equals(other as ProductionRule<ELang>);
        public override int GetHashCode() => new { Head, Nodes }.GetHashCode();
        public bool Equals(ProductionRule<ELang> other)
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
        public bool Similar(ProductionRule<ELang> other) =>
                Head.Equals(other.Head) &&
                Nodes.Where(n => n.IsTerminal || n.IsNonTerminal)
                .SequenceEqual(other.Nodes.Where(n => n.IsTerminal || n.IsNonTerminal));

        public ProductionRule<ELang> GetProductionRuleWithPivotShifted()
        {
            if (IsEnd) throw new InvalidOperationException("It is not possible to take a shift when you are already at the end of production");

            var nodesWithShiftedPivot = Nodes.Copy();
            nodesWithShiftedPivot.Swap(PivotIdx, PivotIdx + 1);

            return new ProductionRule<ELang>(Head, nodesWithShiftedPivot);
        }

        public ProductionRule(Token<ELang> head, IEnumerable<Token<ELang>> nodes)
        {
            Id = Guid.NewGuid();
            Head = head;

            Nodes = nodes.ToList();
            PivotIdx = Nodes.FindIndex(n => n.IsPivot);

            var pivotNode = Token<ELang>.PivotToken;
            var endNode = Token<ELang>.EndToken;

            if (PivotIdx < 0)
            {
                Nodes = nodes.Prepend(pivotNode).Append(endNode).ToList();
                PivotIdx = 0;
            }

            CurrentNode = Nodes[PivotIdx + 1];
            IsEnd = CurrentNode.Equals(endNode);
            Count = Nodes.Count - 2;
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