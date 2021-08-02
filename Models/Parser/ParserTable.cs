using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CSharpParserGenerator
{
    public enum ActionType { Shift, Reduce, Goto, Accept, ExecuteOp }

    [DebuggerDisplay("{Action} - State {To}")]


    public class ActionState<ELang> where ELang : Enum
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

    public class ParserTable<ELang> where ELang : Enum
    {

        private Dictionary<int, Dictionary<Token<ELang>, ActionState<ELang>>> Actions { get; set; }

        public ParserTable() { Actions = new Dictionary<int, Dictionary<Token<ELang>, ActionState<ELang>>>(); }

        public ActionState<ELang> this[int fromState, Token<ELang> tokenId]
        {
            get => GetAction(fromState, tokenId);
            set => SetAction(fromState, tokenId, value);
        }

        public void SetAction(int fromState, Token<ELang> tokenId, ActionState<ELang> action)
        {
            if (!Actions.ContainsKey(fromState)) Actions.Add(fromState, new Dictionary<Token<ELang>, ActionState<ELang>>());
            if (!Actions[fromState].ContainsKey(tokenId)) { Actions[fromState].Add(tokenId, action); return; }
            Actions[fromState][tokenId] = action;
        }

        public ActionState<ELang> GetAction(int fromState, Token<ELang> tokenId)
        {
            if (!Actions.ContainsKey(fromState) || !Actions[fromState].ContainsKey(tokenId)) return null;
            return Actions[fromState][tokenId];
        }
    }
}