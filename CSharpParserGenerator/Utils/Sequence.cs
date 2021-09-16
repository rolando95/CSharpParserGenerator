namespace Utils.Sequence
{
    using Id = System.Int64;

    public class Sequence
    {
        public Id Id { get; private set; } = 0;
        public Id Next() => Id++;
    }
}