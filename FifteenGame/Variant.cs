namespace FifteenGame
{
    internal class Variant
    {
        private readonly Point[,] _data;
        private readonly int _size;
        public Point EmptyPoint { get; private set; }  // coordinates of empty point
        // the direction that led here
        public Direction Direction { get; private set; }


        public Variant(int[,] points)
        {
            _data = new Point[points.GetLength(0), points.GetLength(1)];
            _size = points.GetLength(0);

            for (var i = 0; i < _size; i++)
                for (var j = 0; j < _size; j++)
                {
                    _data[i, j] = new Point(i, j, points[i, j]);
                    if (points[i, j] == 0)
                        EmptyPoint = new Point(i, j, 0);
                }
            Direction = Direction.None;
        }

        public Variant(Point[,] data, int size, Direction direction = Direction.None)
        {
            _data = (Point[,])data.Clone();
            _size = size;
            Direction = direction;
            for (var i = 0; i < _size; i++)
                for (var j = 0; j < _size; j++)
                    if (data[i, j].Val == 0)
                        EmptyPoint = new Point(i, j, 0);
                
        }

        public Variant(Variant variant, Direction direction, Point newEmptyPoint)
        {
            _data = (Point[,])variant._data.Clone();
            Direction = direction;
            _size = variant._size;

            (_data[variant.EmptyPoint.Row, variant.EmptyPoint.Col].Val,
                _data[newEmptyPoint.Row, newEmptyPoint.Col].Val) =
            (_data[newEmptyPoint.Row, newEmptyPoint.Col].Val,
                _data[variant.EmptyPoint.Row, variant.EmptyPoint.Col].Val);

            EmptyPoint = newEmptyPoint;
        }


        public IEnumerable<Point> AsLine()
        {
            for (var i = 0; i < _size; i++)
                for (var j = 0; j < _size; j++)
                    yield return _data[i, j];
        }

        public override bool Equals(object obj) =>
            obj is Variant source && AsLine().SequenceEqual(source.AsLine());
        

        public override int GetHashCode() =>
            AsLine().Select(p => p.GetHashCode() * 3 % 7).Sum();
    

        public bool Equals(Point[,] data)
        {
            for (var i = 0; i < _size; i++)
                for (var j = 0; j < _size; j++)
                    if (_data[i, j] != data[i, j])
                        return false;
            return true;
        }

        public static Variant GetWinVariant(int fieldSize)
        {
            var nums = new int[fieldSize, fieldSize];
            var num = 1;
            for (var i = 0; i < fieldSize; i++)
                for (var j = 0; j < fieldSize; j++)
                    nums[i, j] = num++;
            nums[fieldSize - 1, fieldSize - 1] = 0;
            return new Variant(nums);
        }


        public Point[,] CloneData()
        {
            return (Point[,])_data.Clone();
        }
        
        public Point this[int i, int j] => _data[i, j];            
    }
}
