using System;
using System.Collections.Generic;
using CSharpParserGenerator;

namespace DynamicQuery
{
    public class DynamicQueryParser
    {
        private Parser<ELang> Parser { get; set; }

        public DynamicQueryParser()
        {

            CompileParser();
        }

        public ParseResult<ExpressionNode> Parse(string expression)
        {
            return Parser.Parse<ExpressionNode>(expression);
        }

        private void CompileParser()
        {
            var tokens = new LexerDefinition<ELang>
            {
                [ELang.Ignore] = "[ \\n]+",
                [ELang.LParenthesis] = "\\(", // Left parenthesis
                [ELang.RParenthesis] = "\\)", // Rigt parenthesis

                [ELang.Eq] = "(?i)eq", // Equal
                [ELang.Neq] = "(?i)neq", // Not Equal

                [ELang.Gte] = "(?i)gte", // Greater than or equal
                [ELang.Lte] = "(?i)lte", // Less than or equal

                [ELang.Gt] = "(?i)gt", // Greater than
                [ELang.Lt] = "(?i)lt", // Less than

                [ELang.And] = "(?i)and", // And
                [ELang.Or] = "(?i)or", // Or

                [ELang.Boolean] = "(?i)(true|false)", // Boolean
                [ELang.String] = "(\".*\")", // Quoted String
                [ELang.Number] = "[-+]?\\d*(\\.\\d+)?", // Number

                [ELang.Property] = "[_a-zA-Z]+\\w*" // Property
            };

            var rules = new SyntaxDefinition<ELang>(new Dictionary<ELang, DefinitionRules>()
            {
                [ELang.Expression] = new DefinitionRules
                {
                    new List<Token> { ELang.LogicalOr }
                },
                [ELang.LogicalOr] = new DefinitionRules
                {
                    new List<Token> { ELang.LogicalOr, ELang.Or, ELang.LogicalAnd, new Op(o => o[0] = new ExpressionNode() { Operation = ELang.Or, Left = o[1], Right = o[3] }) },
                    new List<Token> { ELang.LogicalAnd }
                },
                [ELang.LogicalAnd] = new DefinitionRules
                {
                    new List<Token> { ELang.LogicalAnd, ELang.And, ELang.Relational, new Op(o => o[0] = new ExpressionNode() { Operation = ELang.And, Left = o[1], Right = o[3] }) },
                    new List<Token> { ELang.Relational }
                },
                [ELang.Relational] = new DefinitionRules
                {
                    new List<Token> { ELang.Property, ELang.Eq, ELang.Term, new Op(o => o[0] = new ExpressionNode() { Property = o[1], Operation = ELang.Eq, Value = o[3] }) },
                    new List<Token> { ELang.Property, ELang.Neq, ELang.Term, new Op(o => o[0] = new ExpressionNode() { Property = o[1], Operation = ELang.Neq, Value = o[3] }) },

                    new List<Token> { ELang.Property, ELang.Gt, ELang.Term, new Op(o => o[0] = new ExpressionNode() { Property = o[1], Operation = ELang.Gt, Value = o[3] }) },
                    new List<Token> { ELang.Property, ELang.Lt, ELang.Term, new Op(o => o[0] = new ExpressionNode() { Property = o[1], Operation = ELang.Lt, Value = o[3] }) },

                    new List<Token> { ELang.Property, ELang.Gte, ELang.Term, new Op(o => o[0] = new ExpressionNode() { Property = o[1], Operation = ELang.Gte, Value = o[3] }) },
                    new List<Token> { ELang.Property, ELang.Lte, ELang.Term, new Op(o => o[0] = new ExpressionNode() { Property = o[1], Operation = ELang.Lte, Value = o[3] }) },

                    new List<Token> { ELang.LParenthesis, ELang.Expression, ELang.RParenthesis, new Op(o => { o[0] = o[2]; }) },
                },
                [ELang.Term] = new DefinitionRules
                {
                    new List<Token> { ELang.Number, new Op(o => { o[0] = Convert.ToDouble(o[1]); }) },
                    new List<Token> { ELang.Boolean, new Op(o => { o[0] = Convert.ToBoolean(o[1]); }) },
                    new List<Token> { ELang.String, new Op(o => { o[0] = (o[1] as String)[1..^1]; }) },
                }
            });

            var lexer = new Lexer<ELang>(tokens, ELang.Ignore);
            Parser = new ParserGenerator<ELang>(lexer, rules).CompileParser();
        }
    }
}