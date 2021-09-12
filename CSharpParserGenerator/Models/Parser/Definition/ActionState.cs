using System;
using System.Diagnostics;
using Id = System.Int64;

namespace CSharpParserGenerator
{
    public enum ActionType { Shift, Reduce, Goto, Accept, ExecuteOp }

    [DebuggerDisplay("{Action} - State {To}")]
    public class ActionState<ELang> : IEquatable<ActionState<ELang>> where ELang : Enum
    {
        public ActionType Action { get; }
        public Id To { get; }
        public ProductionRule<ELang> ProductionRule { get; }

        /// <summary>
        /// Creates a transition action. Depending on the type of action, the value of To can be inferred in a state break or production rule. 
        /// <para>If the action is Shift and To is 4, it reads as "Shift to state 4". </para>
        /// <para>If the action is Goto and To is 3, it reads as "Goto state 3". </para>
        /// <para>If the action is Reduce and To is 2, it reads as "Reduce to production rule 2" and so on.</para>
        /// </summary>
        public ActionState(ActionType action, Id to, ProductionRule<ELang> productionRule) { Action = action; To = to; ProductionRule = productionRule; }

        public override int GetHashCode() => new { Action, To }.GetHashCode();
        public bool Equals(ActionState<ELang> other)
        {
            return other != null && Action.Equals(other.Action) && To == other.To;
        }
    }
}