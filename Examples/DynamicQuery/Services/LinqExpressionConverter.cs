using System;
using System.Linq.Expressions;
using DynamicQuery.Models;

namespace DynamicQuery.Services
{
    public class LinqExpressionConverter
    {
        //
        // Given as input to a MyExpressionTree, returns a Lambda expression that can be used as a query<br/>
        // 
        // Example: Given this expression tree:
        //  
        //                      / MyExpressionTree----------\ 
        //                     |                             |
        //                     | Operation : Elang.Or        |
        //                     |                             |
        //                      \___Left____________Right___/ 
        //                           /                 \
        //                          /                   \
        //                         /                     \
        //    / MyExpressionTree----------\         / MyExpressionTree----------\ 
        //   |                             |       |                             |
        //   | Property  : HasLicense      |       | Property  : DateOfBirth     |
        //   | Operation : ELang.Eq        |       | Operation : ELang.Lte       |
        //   | Value     : true            |       | Value     : "1990-01-01"    |
        //   |                             |       |                             |
        //    \___________________________/         \___________________________/ 
        //  
        // The following will be returned:
        //   
        // (T x) => x.HasLicense == true || x.DateOfBirth <= new DateTime("1990-01-01")
        //
        public Expression<Func<T, bool>> ToExpressionLambda<T>(MyExpressionTree expression)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var body = GetLinqLogicalExpression<T>(parameter, expression);
            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }

        private BinaryExpression GetLinqLogicalExpression<T>(ParameterExpression parameter, MyExpressionTree expression)
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
                ELang.Or => Expression.OrElse(left, right),

                _ => throw new InvalidOperationException("Internal error: Invalid operation")
            };
        }

        private BinaryExpression GetLinqRelationalExpression(ParameterExpression parameter, string property, ELang operation, object value)
        {
            Expression member = Expression.Property(parameter, property); //x.property
            Expression constant = Expression.Constant(Convert.ChangeType(value, member.Type));

            if (member.Type.Equals(typeof(string)))
            {
                member = StringExpressionToLower(member);
                constant = StringExpressionToLower(constant);
            }

            return operation switch
            {
                ELang.Gte => Expression.GreaterThanOrEqual(member, constant),
                ELang.Gt => Expression.GreaterThan(member, constant),

                ELang.Lt => Expression.LessThan(member, constant),
                ELang.Lte => Expression.LessThanOrEqual(member, constant),

                ELang.Eq => Expression.Equal(member, constant),
                ELang.Neq => Expression.NotEqual(member, constant),

                _ => throw new InvalidOperationException("Internal error: Invalid operation")
            };
        }

        private Expression StringExpressionToLower(Expression value)
        {
            Expression.IfThen(
                Expression.NotEqual(value, Expression.Constant(null)),
                value = Expression.Call(value, value.Type.GetMethod("ToLower", new Type[] { }))
            );
            return value;
        }
    }
}