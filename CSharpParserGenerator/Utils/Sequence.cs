namespace Utils.Sequence
{
    using Id = System.Int64;
    public class Sequence
    {
        private Id _id { get; set; } = 0;
        public Id Id { get => _id; }
        public Id Next() => _id++;
        public Id Reset()
        {
            _id = 0; return Next();
        }
    }
}