namespace Utils.Sequence
{
    using Id = System.Int64;

    public class Sequence
    {
        public Id Id { get; private set; } = 0;
        public Id Next() => Id++;
    }

    public class BaseSequence
    {
        private static Sequence Ids { get; } = new Sequence();
        public Id Id { get; protected set; } = Ids.Next();
    }
}