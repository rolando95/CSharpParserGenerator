using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using System.Linq;

namespace CSharpParserGenerator.Controllers
{
    public enum ELang
    {
        // NonTerminal Symbols
        Expression, Relational, LogicalAnd, LogicalOr, Term,


        // Terminal Symbols
        Ignore,

        LParenthesis,
        RParenthesis,

        Eq, Neq, Gt, Lt, Gte, Lte,

        And, Or,

        Boolean, String, Number,

        Property,
    }

    /// <summary>
    /// This sample seeks to create your own query language to get a list of results by using linq
    /// <example>
    /// <para>Try: /DynamicQuery?filter= HasLicense eq true and ( LastName eq "Rosales" or Age gte 30 )</para>
    /// </example>
    /// </summary>
    [ApiController]
    [Route("[Controller]")]
    public class DynamicQueryController : ControllerBase
    {

        /// <summary>
        /// Seed data
        /// </summary>
        public static IQueryable<Person> Persons { get; } = new List<Person>()
        {
            new Person() { Id = 1, FirstName = "Rolando", LastName = "Rosales", Age = 25, HasLicense = true },
            new Person() { Id = 3, FirstName = "Jose", LastName = "Gonzalez", Age = 40, HasLicense = false },
            new Person() { Id = 4, FirstName = "Abdiel", LastName = "Jaen", Age = 30, HasLicense = true },
            new Person() { Id = 5, FirstName = "Timmy", LastName = "Turner", Age = 15, HasLicense = false },
            new Person() { Id = 6, FirstName = "Phineas", LastName = "Garcia", Age = 22, HasLicense = false },
            new Person() { Id = 7, FirstName = "Samantha", LastName = "Phantom", Age = 25, HasLicense = true },
            new Person() { Id = 8, FirstName = "Tom", LastName = "null", Age = 55, HasLicense = true }
        }.AsQueryable();


        [HttpGet]
        public IActionResult Query([FromQuery(Name = "filter")] string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) return Ok(Persons);

            /// You only need to run this code once to generate the parser. 
            /// We have left the configuration and instance of the parser on the controller to make the example easier to read.
            var parser = CompileParser();


            /// You can run this part as many times as you want. Only requires one instance of the parser.

            var expressionTree = parser.Parse<ExpressionNode>(expression);
            if (!expressionTree.Success) return BadRequest(expressionTree);

            var expressionResult = GetLinqExpression<Person>(expressionTree.Value);

            var Data = Persons.Where(expressionResult);
            return Ok(new { Data });
        }

        private Parser<ELang> CompileParser()
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
            return new ParserGenerator<ELang>(lexer, rules).CompileParser();
        }

        private Expression<Func<T, bool>> GetLinqExpression<T>(ExpressionNode expression)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var body = GetLinqLogicalExpression<T>(parameter, expression);
            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        private BinaryExpression GetLinqLogicalExpression<T>(ParameterExpression parameter, ExpressionNode expression)
        {
            if (expression.isRelational)
            {
                return GetLinqRelationalExpression(parameter, expression.Property, expression.Operation, expression.Value);
            }

            var left = GetLinqLogicalExpression<T>(parameter, expression.Left);
            var right = GetLinqLogicalExpression<T>(parameter, expression.Right);
            return expression.Operation switch
            {
                ELang.And => Expression.AndAlso(left, right),
                ELang.Or => Expression.Or(left, right),
                _ => null
            };
        }

        private BinaryExpression GetLinqRelationalExpression(ParameterExpression parameter, string property, ELang operation, object value)
        {
            Expression member = Expression.Property(parameter, property); //x.property
            Expression constant = Expression.Constant(Convert.ChangeType(value, member.Type));

            if (member.Type.Equals(typeof(string)))
            {
                member = ToLower(member);
                constant = ToLower(constant);
            }

            return operation switch
            {
                ELang.Gte => Expression.GreaterThanOrEqual(member, constant),
                ELang.Gt => Expression.GreaterThan(member, constant),

                ELang.Lt => Expression.LessThan(member, constant),
                ELang.Lte => Expression.LessThanOrEqual(member, constant),

                ELang.Eq => Expression.Equal(member, constant),
                ELang.Neq => Expression.NotEqual(member, constant),

                _ => throw new InvalidOperationException("Internar error: Invalid operation")
            };
        }

        private Expression ToLower(Expression value)
        {
            Expression.IfThen(
                Expression.NotEqual(value, Expression.Constant(null)),
                value = Expression.Call(value, value.Type.GetMethod("ToLower", new Type[] { }))
            );
            return value;
        }
    }

    #region Models

    public class ExpressionNode
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ELang Operation { get; set; }

        public bool isRelational { get => !isLogical; }
        public bool isLogical { get => Operation == ELang.And || Operation == ELang.Or; }

        // Relacional Operation Props
        public string Property { get; set; }
        public object Value { get; set; }

        // Logical Operation Props
        public ExpressionNode Left { get; set; }
        public ExpressionNode Right { get; set; }

    }

    public class Person
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
        public bool HasLicense { get; set; }
    }

    #endregion
}
