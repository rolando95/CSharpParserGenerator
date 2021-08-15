using System.Text.Json.Serialization;

namespace DynamicQuery.Models
{
    public class MyExpressionTree
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ELang Operation { get; set; }

        public bool isRelational { get => !isLogical; }
        public bool isLogical { get => Operation == ELang.And || Operation == ELang.Or; }

        // Relacional Operation Props
        public string Property { get; set; }
        public object Value { get; set; }

        // Logical Operation Props
        public MyExpressionTree Left { get; set; }
        public MyExpressionTree Right { get; set; }

    }
}