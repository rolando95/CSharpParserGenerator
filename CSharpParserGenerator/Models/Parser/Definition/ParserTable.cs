using System;
using System.Collections.Generic;
using System.Linq;
using Id = System.Int64;

namespace CSharpParserGenerator
{
    public class ParserTable<ELang> where ELang : Enum
    {

        private Dictionary<Id, Dictionary<Token<ELang>, ActionState<ELang>>> Actions { get; set; }

        public ParserTable() { Actions = new Dictionary<Id, Dictionary<Token<ELang>, ActionState<ELang>>>(); }

        public ActionState<ELang> this[Id fromState, Token<ELang> tokenId]
        {
            get => GetAction(fromState, tokenId);
            set => SetAction(fromState, tokenId, value);
        }

        public void SetAction(Id fromState, Token<ELang> tokenId, ActionState<ELang> action)
        {
            if (!Actions.ContainsKey(fromState)) Actions.Add(fromState, new Dictionary<Token<ELang>, ActionState<ELang>>());
            if (!Actions[fromState].ContainsKey(tokenId)) { Actions[fromState].Add(tokenId, action); return; }
            Actions[fromState][tokenId] = action;
        }

        public ActionState<ELang> GetAction(Id fromState, Token<ELang> tokenId)
        {
            if (!Actions.ContainsKey(fromState) || !Actions[fromState].ContainsKey(tokenId)) return null;
            return Actions[fromState][tokenId];
        }

        public IEnumerable<Token<ELang>> GetAvailableTerminalsFromStateId(Id fromState)
        {
            if (!Actions.ContainsKey(fromState)) return new List<Token<ELang>>();

            return Actions[fromState].Keys.Where(k => k.IsTerminal || k.IsEnd).ToList();
        }
    }
}