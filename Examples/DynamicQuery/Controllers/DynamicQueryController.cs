using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using DynamicQuery.Models;
using DynamicQuery.Services;
using DynamicQuery.DBContext;
using System;

namespace DynamicQuery.Controllers
{

    /// <summary>
    /// This sample seeks to create your own query language to get a list of results by using linq<br/>
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
    ///     - For example: **haslicense eq true and DateOfBirth lte "1990-01-01"**
    /// * It is possible to group operations with parentheses
    /// </summary>
    [ApiController]
    [Route("[Controller]")]
    public class DynamicQueryController : ControllerBase
    {
        private readonly MyQueryParser _parser;
        private readonly LinqExpressionConverter _linqConverter;
        private readonly MyDbContext _context;

        public DynamicQueryController(MyQueryParser parser, MyDbContext context, LinqExpressionConverter linqConverter)
        {
            _parser = parser;
            _context = context;
            _linqConverter = linqConverter;
        }

        /// <remarks>
        /// This example uses a **Person** class list as the data model. The properties they contain are **Id, firstname, lastname, Age,** and **HasLicense**.
        ///
        /// Try:
        /// * /Persons?filter=
        /// * /Persons?filter= DateOfBirth lte "1990-01-01"
        /// * /Persons?filter= haslicense eq true and ( lastname eq "Rosales" or DateOfBirth lte "1990-01-01" )  
        /// </remarks>
        [HttpGet("/Persons")]
        public IActionResult GetPersons([FromQuery(Name = "filter")] string expression)
        {
            try
            {

                if (string.IsNullOrWhiteSpace(expression)) return Ok(new { Data = _context.People });

                var MyExpressionTree = _parser.Parse(expression);
                if (!MyExpressionTree.Success) return BadRequest(MyExpressionTree);

                var expressionResult = _linqConverter.ToExpressionLambda<Person>(MyExpressionTree.Value);
                var Data = _context.People.Where(expressionResult);

                return Ok(new { Data });

            }
            catch (Exception e)
            {
                return BadRequest($"Invalid operation: {e.Message}");
            }
        }
    }
}
