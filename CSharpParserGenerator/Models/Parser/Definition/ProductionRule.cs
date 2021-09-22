using System;
using System.Collections.Generic;
using System.Linq;
using Utils.Sequence;
using Id = System.Int64;

namespace CSharpParserGenerator
{
    public class ProductionRule<ELang> : IEquatable<ProductionRule<ELang>> where ELang : Enum
    {
        private static Sequence Ids { get; } = new Sequence();
        public Id Id { get; protected set; } = Ids.Next();
        private int PivotIdx { get; }
        public Token<ELang> Head { get; }
        public List<Token<ELang>> Nodes { get; }
        public Token<ELang> LookAhead { get; }
        public Token<ELang> CurrentNode { get; }
        public bool IsEnd { get; }
        public bool IsFirst => PivotIdx == 0;
        public bool IsEmpty => IsFirst && IsEnd;
        public State<ELang> NextState { get; set; }

        public Op Operation { get; }
        public int ShiftPointerIdxOnReduce { get; }

        public override bool Equals(object other) => Equals(other as ProductionRule<ELang>);
        public override int GetHashCode() => new { Head, Nodes, LookAhead }.GetHashCode();
        public bool Equals(ProductionRule<ELang> other)
        {
            var result = Id.Equals(other.Id) ||
            (
                PivotIdx == other.PivotIdx &&
                Head.Equals(other.Head) &&
                Nodes.SequenceEqual(other.Nodes) &&
                (LookAhead?.Equals(other.LookAhead) ?? other.LookAhead == null)
            );
            return result;
        }

        /// <summary>
        /// Compares 2 production rules only by terminal and non-terminal symbols
        /// </summary>
        /// <returns></returns>
        public bool Similar(ProductionRule<ELang> other, bool comparePivot, bool compareLookAhead) =>
                Head.Equals(other.Head) &&
                Nodes.SequenceEqual(other.Nodes) &&
                (!comparePivot || PivotIdx == other.PivotIdx) &&
                (!compareLookAhead || (LookAhead?.Equals(other.LookAhead) ?? LookAhead == null));

        public Token<ELang> NextNode()
        {
            if (IsEnd) return null;
            if (Nodes.Count - 1 == PivotIdx) return Token<ELang>.EndToken();
            return Nodes[PivotIdx + 1];
        }

        public ProductionRule<ELang> GetProductionRuleWithShiftedPivot()
        {
            if (IsEnd) throw new InvalidOperationException("It is not possible to take a shift when you are already at the end of production");

            return new ProductionRule<ELang>(
                head: Head,
                nodes: Nodes,
                pivotIdx: PivotIdx + 1,
                lookAhead: LookAhead,
                operation: Operation,
                shiftPointerIdxOnReduce: ShiftPointerIdxOnReduce
            );
        }

        public ProductionRule<ELang> GetCopyWithAnotherLookAhead(Token<ELang> lookAhead)
        {
            return new ProductionRule<ELang>(
                head: Head,
                nodes: Nodes,
                lookAhead: lookAhead,
                operation: Operation,
                pivotIdx: PivotIdx,
                shiftPointerIdxOnReduce: ShiftPointerIdxOnReduce
            );
        }

        public ProductionRule(Token<ELang> head, List<Token<ELang>> nodes, int pivotIdx = 0, Token<ELang> lookAhead = null, Op operation = null, int shiftPointerIdxOnReduce = 0)
        {
            Head = head;
            Nodes = nodes;
            PivotIdx = pivotIdx;

            IsEnd = Nodes.Count == PivotIdx;
            CurrentNode = IsEnd ? Token<ELang>.EndToken() : Nodes[PivotIdx];

            Operation = operation;
            ShiftPointerIdxOnReduce = shiftPointerIdxOnReduce;
            LookAhead = lookAhead;
        }

        public override string ToString()
        {
            var operation = Operation != null ? " (op)" : "";
            var strNodes = Nodes.Select((n, idx) => idx == PivotIdx ? $".{n.ToString()}" : n.ToString());
            if (IsEnd) strNodes = strNodes.Append(".");
            return $"[{Head.ToString()} -> {string.Join(" ", strNodes)} /{LookAhead?.ToString()}]{operation}";
        }
    }
}