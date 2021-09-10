using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Utils.Extensions;

namespace CSharpParserGenerator
{
    [DebuggerDisplay("{StringProductionRule}")]
    public class ProductionRule<ELang> : IEquatable<ProductionRule<ELang>> where ELang : Enum
    {
        private int PivotIdx { get; }
        public Guid Id { get; }
        public Token<ELang> Head { get; }
        public List<Token<ELang>> Nodes { get; }
        public List<Token<ELang>> LookAheadNodes { get; }
        public Token<ELang> CurrentNode { get; }
        public bool IsEnd { get; }
        public bool IsFirst => PivotIdx == 0;
        public bool IsEmpty => IsFirst && IsEnd;
        public State<ELang> NextState { get; set; }

        public Op Operation { get; }
        public int ShiftPointerIdxOnReduce { get; }
        public Token<ELang> NextNode() => IsEnd ? null : Nodes[PivotIdx + 2];
        public override bool Equals(object other) => Equals(other as ProductionRule<ELang>);
        public override int GetHashCode() => new { Head, Nodes }.GetHashCode();
        public bool Equals(ProductionRule<ELang> other)
        {
            var result = Id.Equals(other.Id) ||
            (
                Head.Equals(other.Head) &&
                Nodes.SequenceEqual(other.Nodes) &&
                LookAheadNodes.SequenceEqual(other.LookAheadNodes)
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

        public ProductionRule<ELang> GetProductionRuleWithShiftedPivot()
        {
            if (IsEnd) throw new InvalidOperationException("It is not possible to take a shift when you are already at the end of production");

            var nodesWithShiftedPivot = Nodes.Copy();
            nodesWithShiftedPivot.Swap(PivotIdx, PivotIdx + 1);

            return new ProductionRule<ELang>(
                head: Head,
                nodes: nodesWithShiftedPivot,
                lookAheadNodes: LookAheadNodes,
                operation: Operation,
                shiftPointerIdxOnReduce: ShiftPointerIdxOnReduce
            );
        }

        public ProductionRule<ELang> GetCopyWithAnotherLookAhead(List<Token<ELang>> lookAheadNodes)
        {
            return new ProductionRule<ELang>(
                head: Head,
                nodes: Nodes,
                lookAheadNodes: lookAheadNodes,
                operation: Operation,
                shiftPointerIdxOnReduce: ShiftPointerIdxOnReduce
            );
        }

        public ProductionRule(Token<ELang> head, IEnumerable<Token<ELang>> nodes, List<Token<ELang>> lookAheadNodes = null, Op operation = null, int shiftPointerIdxOnReduce = 0)
        {
            Id = Guid.NewGuid();
            Head = head;

            Nodes = nodes.ToList();
            PivotIdx = Nodes.FindIndex(n => n.IsPivot);

            var pivotNode = Token<ELang>.PivotToken();
            var endNode = Token<ELang>.EndToken();

            if (PivotIdx < 0)
            {
                Nodes.Insert(0, pivotNode);
                PivotIdx = 0;
            }

            if (!Nodes.Any(n => n.IsEnd))
            {
                Nodes.Add(endNode);
            }

            CurrentNode = Nodes[PivotIdx + 1];
            IsEnd = CurrentNode.Equals(endNode);
            Operation = operation;
            ShiftPointerIdxOnReduce = shiftPointerIdxOnReduce;
            LookAheadNodes = lookAheadNodes ?? new List<Token<ELang>>();
            LookAheadNodes.Sort();
        }

        // Only to display in the debbuger
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public string StringProductionRule
        {
            get
            {
                var operation = Operation != null ? " (op)" : "";
                var strNodes = Nodes.SkipLast(1).Select(n => n.StringToken);
                var lookAhead = LookAheadNodes == null ? "" : $", {string.Join("/", LookAheadNodes.Select(n => n.StringToken))}";
                return $"[{Head.StringToken} -> {string.Join(" ", strNodes)}{lookAhead}]{operation}".Replace(" ,", ",");
            }
        }
    }
}