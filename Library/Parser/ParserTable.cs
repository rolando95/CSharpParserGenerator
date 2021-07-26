using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Syntax
{
    public enum ActionType { Shift, Reduce, Goto, Accept, ExecuteOp }

    [DebuggerDisplay("{Action} - State {To}")]


    public class ActionState<ERule> where ERule : Enum
    {
        public ActionType Action { get; }
        public int To { get; }

        /// <summary>
        /// Creates a transition action. Depending on the type of action, the value of To can be inferred in a state break or production rule. 
        /// <para>If the action is Shift and To is 4, it reads as "Shift to state 4". </para>
        /// <para>If the action is Goto and To is 3, it reads as "Goto state 3". </para>
        /// <para>If the action is Reduce and To is 2, it reads as "Reduce to production rule 2" and so on.</para>
        /// </summary>
        public ActionState(ActionType action, int to) { Action = action; To = to; }
    }

    public class ParserTable<ERule> where ERule : Enum
    {

        private Dictionary<int, Dictionary<Token<ERule>, ActionState<ERule>>> Actions { get; set; }

        public ParserTable() { Actions = new Dictionary<int, Dictionary<Token<ERule>, ActionState<ERule>>>(); }

        public ActionState<ERule> this[int fromState, Token<ERule> tokenId]
        {
            get => GetAction(fromState, tokenId);
            set => SetAction(fromState, tokenId, value);
        }

        public void SetAction(int fromState, Token<ERule> tokenId, ActionState<ERule> action)
        {
            if (!Actions.ContainsKey(fromState)) Actions.Add(fromState, new Dictionary<Token<ERule>, ActionState<ERule>>());
            if (!Actions[fromState].ContainsKey(tokenId)) { Actions[fromState].Add(tokenId, action); return; }
            Actions[fromState][tokenId] = action;
        }

        public ActionState<ERule> GetAction(int fromState, Token<ERule> tokenId)
        {
            if (!Actions.ContainsKey(fromState) || !Actions[fromState].ContainsKey(tokenId)) return null;
            return Actions[fromState][tokenId];
        }
    }
}