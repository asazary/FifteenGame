namespace FifteenGame
{
    internal struct Step(int dy, int dx, Direction direction)
    {
        public readonly int Dx = dx;
        public readonly int Dy = dy;
        public readonly Direction Direction = direction;
    }

    internal enum Direction { None = 0, Left = 1, Right = 2, Up = 3, Down = 4 }

    internal static class Steps
    {
        private static Dictionary<Direction, Step> _steps = new Dictionary<Direction, Step>()
            {
                { Direction.Left,  new Step(0, -1, Direction.Left)},
                { Direction.Right, new Step(0, 1, Direction.Right)},
                { Direction.Up,  new Step(-1, 0, Direction.Up)},
                { Direction.Down,  new Step(1, 0, Direction.Down)},
                { Direction.None,  new Step(1, 0, Direction.None)},
            };

        public static Step GetStep(Direction direction) =>
            _steps[direction];

        public static Direction InverseDirection(Direction direction) =>
        direction switch
        {
            Direction.Left => Direction.Right,
            Direction.Right => Direction.Left,
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.None => Direction.None,
            _ => throw new ArgumentException($"Unknown direction: {direction}")
        };
    }
}
