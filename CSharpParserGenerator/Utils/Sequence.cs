namespace Utils.Sequence
{
    public class Sequence
    {
        private int _id { get; set; } = 0;
        public int Id { get => _id; }
        public int Next() => _id++;
        public int Reset()
        {
            _id = 0; return Next();
        }
    }
}