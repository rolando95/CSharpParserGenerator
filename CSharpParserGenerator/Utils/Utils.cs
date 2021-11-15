namespace CSharpParserGenerator
{
    public static class Utils
    {
        public static string GetErrorTextFragment(string text, int pivot, int substringLength, int length = 50)
        {
            var lenght2 = length / 2;
            var result = "";

            if (pivot - lenght2 > 0)
            {
                result += $"...{text[(pivot - lenght2)..(pivot)]}";
            }
            else
            {
                result += text[..pivot];
            }

            result += $"^^^{text[pivot..(pivot+substringLength)]}^^^";

            var pivot2 = pivot + substringLength;
            if (pivot2 + lenght2 < text.Length)
            {
                result += $"{text[(pivot2)..(pivot2 + lenght2)]}...";
            }
            else
            {
                result += text[pivot2..^0];
            }

            return result;
        }
    }
}
