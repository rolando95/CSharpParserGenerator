using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace DynamicQuery.Controllers
{


    [ApiController]
    [Route("[Controller]")]
    public class DynamicQueryController : ControllerBase
    {
        private readonly DynamicQueryParser Parser;

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

        public DynamicQueryController(DynamicQueryParser parser)
        {
            Parser = parser;
        }

        /// <remarks>
        ///  This sample seeks to create your own query language to get a list of results by using linq<br/>
        /// About syntaxis: 
        /// * In this syntax, properties are case sensitive. numeric values group integers and floats and strings are enclosed in quotation marks
        ///     - Property: **LastName**
        ///     - String: **"Tom"**
        ///     - Number: **30**
        ///     - Boolean: **true**
        /// * You can use any relational operation in this format:  _Property RelationalOperator value_ 
        ///     - For example: **Firstname eq "Jose"** 
        ///     - Allowed relational operators: 
        ///         * Equal: **eq**
        ///         * Not equal: **neq**
        ///         * Greater than or equal: **gte**
        ///         * Less than or equal: **lte**
        ///         * Greater than: **gt**
        ///         * Less than: **lt**
        /// * You can perform logical operations using the **AND**, **OR** operators.
        ///     - Operator precedence is taken into account, so AND operations are performed before OR
        ///     - For example: **haslicense eq true and Age gte 30**
        /// * It is possible to group operations with parentheses
        /// 
        /// Try:
        /// * /DynamicQuery?filter=
        /// * /DynamicQuery?filter= age lt 30
        /// * /DynamicQuery?filter= haslicense eq true and ( LastName eq "Rosales" or Age gte 30 ) 
        ///  
        /// <br/>This example uses a **Person** class list as the data model. The properties they contain are **Id, FirstName, LastName, Age, and HasLicense**. 
        /// <br/>You are free to modify properties, records or even use some model in database.
        /// </remarks>
        [HttpGet]
        public IActionResult Query([FromQuery(Name = "filter")] string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) return Ok(new { Data = Persons });

            var expressionTree = Parser.Parse(expression);
            if (!expressionTree.Success) return BadRequest(expressionTree);

            var expressionResult = LinqExpressionConverter.ToExpressionLambda<Person>(expressionTree.Value);
            var Data = Persons.Where(expressionResult);

            return Ok(new { Data });
        }
    }
}
