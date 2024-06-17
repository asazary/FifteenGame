namespace FifteenGame
{
    internal class MovesCounter
    {
        private int _count = 0;
        public event Action<int>? MovedEvent;

        private MovesCounter()
        {
            _count = 0;
        }

        public static MovesCounter CreateMovesCounter() =>
            new MovesCounter();

        public void Reset()
        {
            _count = 0;
            MovedEvent?.Invoke(_count);
        }

        public void Inc()
        {
            ++_count;
            MovedEvent?.Invoke(_count);
        }

        public static MovesCounter operator++(MovesCounter counter)
        {
            counter._count++;
            counter.MovedEvent?.Invoke(counter._count);
            return counter;
        }

        public override string ToString() => _count.ToString();

    }
}
