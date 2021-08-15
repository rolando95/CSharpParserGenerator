using System.Text.Json.Serialization;

namespace DynamicQuery
{
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
}