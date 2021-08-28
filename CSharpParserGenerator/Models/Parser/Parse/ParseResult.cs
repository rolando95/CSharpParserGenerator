

using System;
using System.Collections.Generic;

namespace CSharpParserGenerator
{
    public class ParseResult<T>
    {
        public string Text { get; }
        public bool Success { get; }
        public T Value { get; }

        public List<ErrorInfo> Errors { get; }

        public ParseResult(string text, bool success = false, object value = null, List<ErrorInfo> errors = null)
        {
            Text = text;
            Success = success;
            Errors = errors ?? new List<ErrorInfo>();
            Value = GetValue(value);
        }

        private Type GetNonNullableType(Type type)
        {
            return Nullable.GetUnderlyingType(type) ?? type;
        }

        private T GetValue(object value)
        {
            var vType = value?.GetType().Name ?? "null";
            var tType = GetNonNullableType(typeof(T));

            try
            {
                if (value != null) return (T)Convert.ChangeType(value, tType);
            }
            catch
            {

                throw new InvalidOperationException($"Invalid ParseResult value type: expected {typeof(T).Name}, received {vType}. Value: {value}");
            }

            return default;
        }
    }

    public class ErrorInfo
    {
        public string Type { get; set; }
        public string Description { get; set; }
    }
}