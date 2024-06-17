namespace FifteenGame
{
    internal class Game
    {
        public static Game CurrentGame { get; private set; }

        private const int EmptyPointValue = 0;
        internal Point[,] Data { get; private set; }
        internal int FieldSize = 3;
        private const int _maxAiVariantsForView = 8000;
        private readonly Point[] _winPosition = [];
        private readonly Variant WinVariant;
        public event Action WinEvent;
        public event Action<int, int> VariantWasChecked;
        public event Action<string> LogEvent;
        public void Log(string text) => LogEvent?.Invoke(text);

        static Game()
        {
            CurrentGame = new Game();
        }

        private Game(int fieldSize = 3)
        {
            FieldSize = fieldSize;
            WinVariant = Variant.GetWinVariant(fieldSize);
            Data = WinVariant.CloneData();

            CurrentGame = this;
        }

        public static Game NewGame(int fieldSize = 3)
        {
            CurrentGame = new Game(fieldSize);
            return CurrentGame;
        }

        public Point GetEmptyPoint()
        {            
            for (var i = 0; i < FieldSize; ++i)
                for (var j = 0; j < FieldSize; ++j)
                    if (Data[i, j].Val == EmptyPointValue)
                        return Data[i, j];

            throw new ApplicationException("Can't find empty point");
        }

        public bool CanMoveEmptyPoint(Point point, Direction direction)
        {
            var step = Steps.GetStep(direction);
            return point.Row + step.Dy >= 0 && point.Row + step.Dy < FieldSize
                && point.Col + step.Dx >= 0 && point.Col + step.Dx < FieldSize;
        }        

        // returns block position after move
        public Point? Move(Direction direction)
        {
            // moving empty point
            var emptyPoint = GetEmptyPoint();
            if (!CanMoveEmptyPoint(emptyPoint, direction))             
                return null;
            
            var step = Steps.GetStep(direction);    // step for empty point
            // block next to empty point and the place where will be empty point
            var blockPoint = Data[emptyPoint.Row + step.Dy, emptyPoint.Col + step.Dx];

            (blockPoint.Val, emptyPoint.Val) = (emptyPoint.Val, blockPoint.Val);
            Data[emptyPoint.Row, emptyPoint.Col].Val = emptyPoint.Val;
            Data[blockPoint.Row, blockPoint.Col].Val = blockPoint.Val;


            //if (IsWin()) WinEvent?.Invoke();
            
            // emptyPoint now contains the block data (end position)
            return emptyPoint;
        }

        public IEnumerable<Direction> ShuffleField(int randomStepsCount)
        {
            var random = new Random();
            var directionList = new List<Direction>();

            Log("Shuffling...");

            for (var i = 0; i < randomStepsCount; )
            {
                var direction = (Direction)random.Next(1, 5);                
                var pointInfo = Move(direction);

                if (pointInfo != null)
                {
                    i++;
                    directionList.Add(direction);
                }
            }
            return directionList;
        }

        public bool IsWin() => WinVariant.Equals(Data);

        public bool IsWin(Point[,] currentPositions)
        {
            var arr = new Point[FieldSize * FieldSize];
            var k = 0;
            for (var i = 0; i < FieldSize; i++)
                for (var j = 0; j < FieldSize; j++)
                    arr[k] = currentPositions[i, j];

            return _winPosition.SequenceEqual(arr);
        }

        
        public Variant MoveVariant(Variant variant, Direction direction)
        {
            // moving empty point
            var emptyPoint = variant.EmptyPoint;
            if (!CanMoveEmptyPoint(emptyPoint, direction))
                throw new ArgumentException("This direction can't be apply to variant");

            var step = Steps.GetStep(direction);    // step for empty point
            // block next to empty point and the place where will be empty point
            var blockPoint = variant[emptyPoint.Row + step.Dy, emptyPoint.Col + step.Dx];

            var newVariant = new Variant(variant, direction, blockPoint);
                        
            return newVariant;
        }



        public IEnumerable<Direction>? Solve(CancellationToken cancelToken) {

            Log("Start solving...");
            var startVariant = new Variant(Data, FieldSize);
            var winVariant = Variant.GetWinVariant(FieldSize);

            var pathFound = FindPath(startVariant, winVariant, cancelToken, 
                out Dictionary<Variant, Variant?> primePath);

            if (cancelToken.IsCancellationRequested) return null;
            if (pathFound)
            {
                var primeDirs = ExtractPath(primePath, winVariant, true);
                primeDirs.Reverse();
                return primeDirs;
            }
            // ---
            
            Log($"Solution not found. {primePath.Count} variants were viewed");
            Log("trying another method...");

            pathFound = FindPath(winVariant, startVariant, cancelToken,
                out Dictionary<Variant, Variant?> secondPath);

            if (cancelToken.IsCancellationRequested) return null;
            if (pathFound)
            {
                var secondDirs = ExtractPath(secondPath, startVariant, false);
                return secondDirs;
            }
            //--

            Log($"Solution still not found. {primePath.Count} extra variants were viewed");
            Log("trying again...");

            var commonElem = primePath.Keys
                .FirstOrDefault(secondPath.Keys.Contains);
            if (commonElem != null)
            {
                Log("Path found! Extracting...");
                var primeDirs = ExtractPath(primePath, commonElem, true);
                primeDirs.Reverse();
                var secondDirs = ExtractPath(secondPath, commonElem, false);
                //secondDirs.RemoveAt(0);
                primeDirs.AddRange(secondDirs);
                return primeDirs;
            }

            return null; 
        }


        public bool FindPath(Variant startVariant, Variant targetVariant,
            CancellationToken cancelToken, out Dictionary<Variant, Variant?> outPath)
        {
            var queue = new Queue<Variant>();
            var path = new Dictionary<Variant, Variant?>();
                        
            outPath = [];

            path[startVariant] = null;

            queue.Enqueue(startVariant);

            var count = 0;
            while (queue.Count > 0)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    Log("Canceled by user");
                    return false;
                }
                
                if (count > _maxAiVariantsForView)
                {
                    outPath = path;
                    return false;
                }

                var curVar = queue.Dequeue();
                VariantWasChecked?.Invoke(count++, queue.Count);

                var nextVariants = AllAdjacentVariants(curVar)
                    .Where(v => !path.ContainsKey(v))
                    .ToList();
                foreach (var nextVariant in nextVariants)
                {
                    path[nextVariant] = curVar;
                    queue.Enqueue(nextVariant);
                }

                if (path.ContainsKey(targetVariant))
                {
                    Log("Path found");
                    break;
                }
            }

            outPath = path;
            return true;
        }

        private List<Direction> ExtractPath(Dictionary<Variant, Variant?> path, 
            Variant targetVariant, bool isPrimeDirection)
        {
            Log("Extracting path...");
            var test2 = path.Keys.FirstOrDefault(k => k.Equals(targetVariant));
            var finishDirection = path.Keys.First(k => k.Equals(targetVariant)).Direction;

            var directions = new List<Direction> { isPrimeDirection ? 
                finishDirection : Steps.InverseDirection(finishDirection) };
            var prevVar = path[targetVariant];

            while (prevVar != null && prevVar.Direction != Direction.None)
            {
                directions.Add(isPrimeDirection ? 
                    prevVar!.Direction : Steps.InverseDirection(prevVar!.Direction));
                prevVar = path[prevVar];
            }
            Log($"path consists of {directions.Count} steps");
            return directions;
        }


        private IEnumerable<Variant> AllAdjacentVariants(Variant variant)
        {
            foreach (var dir in new[] { Direction.Left, Direction.Right, Direction.Up, Direction.Down })
                if (CanMoveEmptyPoint(variant.EmptyPoint, dir))
                    yield return MoveVariant(variant, dir);
        }
                
    }

   
}
