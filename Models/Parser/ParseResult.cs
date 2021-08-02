

using System;
using System.Collections.Generic;

namespace CSharpParserGenerator
{
    public class ParseResult<T>
    {
        public bool Success { get; }
        public T Value { get; }

        public List<ErrorInfo> Errors { get; }

        public ParseResult(bool success = false, dynamic value = null, List<ErrorInfo> errors = null)
        {
            Success = success;
            Errors = errors ?? new List<ErrorInfo>();

            var vType = value?.GetType();
            var tType = typeof(T);

            if (tType.Equals(typeof(object)) || tType.Equals(vType))
            {
                Value = (T)value;
                return;
            }

            try
            {
                Value = Convert.ChangeType(value, tType);
            }
            catch
            {
                throw new InvalidOperationException($"Invalid ParseResult value type: expected {typeof(T).Name}, received {value.GetType().Name}. Value: {value}");
            }
        }
    }

    public class ErrorInfo
    {
        public string Type { get; set; }
        public string Description { get; set; }
    }
}