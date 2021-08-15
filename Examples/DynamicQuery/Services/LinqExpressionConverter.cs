using System;
using System.Linq.Expressions;

namespace DynamicQuery
{
    public static class LinqExpressionConverter
    {
        public static Expression<Func<T, bool>> ToExpressionLambda<T>(ExpressionNode expression)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var body = GetLinqLogicalExpression<T>(parameter, expression);
            return Expression.Lambda<Func<T, bool>>(body, parameter);
        }


        private static BinaryExpression GetLinqLogicalExpression<T>(ParameterExpression parameter, ExpressionNode expression)
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

        private static BinaryExpression GetLinqRelationalExpression(ParameterExpression parameter, string property, ELang operation, object value)
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

                _ => throw new InvalidOperationException("Internar error: Invalid operation")
            };
        }

        private static Expression StringExpressionToLower(Expression value)
        {
            Expression.IfThen(
                Expression.NotEqual(value, Expression.Constant(null)),
                value = Expression.Call(value, value.Type.GetMethod("ToLower", new Type[] { }))
            );
            return value;
        }
    }
}