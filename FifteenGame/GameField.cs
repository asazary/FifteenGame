using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace FifteenGame
{
    internal class GameField
    {
        public static GameField CurrentGameField { get; private set; }
        const int FieldWidth = 460;
        const int FieldHeight = 450;
        private int _blockWidth;
        private int _blockHeight;
        const int BlockPadding = 5;
        const int CanvasPadding = 3;
        
        private static Color _blockColor = Color.FromArgb(255, 125, 205, 255);
        private static Color _blockActiveColor = Color.FromArgb(255, 0, 0, 50);
        private static Color _blockWinColor = Color.FromArgb(255, 20, 150, 120);
        private static Color _blockFailColor = Color.FromArgb(255, 100, 0, 0);
        private readonly SolidColorBrush _blockBrush = new(_blockColor);
        private readonly SolidColorBrush _blockActiveBrush = new(_blockActiveColor);
        private readonly SolidColorBrush _blockWinBrush = new(_blockWinColor);
        private readonly SolidColorBrush _blockFailBrush = new(_blockFailColor);
        private readonly TimeSpan _movingDuration = TimeSpan.FromMilliseconds(400);
        private readonly TimeSpan _movingDelay = TimeSpan.FromMilliseconds(100);

        private readonly Canvas _canvas;
        private readonly MainWindow _mainWindow;
        private Game _game;

        private Dictionary<int, PointBlock> _blocks = [];

        private IEnumerable<Direction> _shuffled = new List<Direction>();
        private readonly Queue<Point> _stepQueue = new();
        private CancellationTokenSource _aiProcessingCancellationToken;
        public List<string> LogList { get; set; } = new List<string>();

        public bool IsGameStarted { get; private set; } = false;
        public MovesCounter MovesCount;
        private GameStatus _gameStatus = GameStatus.NotStarted;
        public event Action<string> StatusChanged;
        public event Action<GameStatus> InterfaceLocker;
        
        
        private GameField(MainWindow window)
        {
            _canvas = window.Field;
            _mainWindow = window;
            MovesCount = MovesCounter.CreateMovesCounter();
        }

        public static GameField CreateGameField(MainWindow window)
        {
            CurrentGameField = new GameField(window);
            return CurrentGameField;
        }

        public void Init()
        {
            _canvas.Height = FieldHeight;
            _canvas.Width = FieldWidth - BlockPadding;

            _blockHeight = (FieldHeight - CanvasPadding) / _game.FieldSize;
            _blockWidth = _blockHeight;

            _blocks = [];
        }


        public void DrawField()
        {
            for (var i = 0; i < _game.FieldSize; i++)
                for (var j = 0; j < _game.FieldSize; j++)
                    if (!_game.Data[i, j].IsEmpty())
                    {
                        var (block, rect) = DrawBlock(_game.Data[i, j]);
                        _blocks[_game.Data[i, j].Val] = new PointBlock(_game.Data[i, j], block, rect);
                    }                        
        }

        public void ClearField()
        {
            _blocks.Clear();
            _canvas.Children.Clear();
            SetGameStatus(GameStatus.NotStarted);
            MovesCount.Reset();
            IsGameStarted = false;
            Log("Field cleared");
        }

        public void StartGame(int fieldSize, int randomStepsCount)
        {
            ClearField();
            _game = Game.NewGame(fieldSize);
            _shuffled = _game.ShuffleField(randomStepsCount);

            Init();
            DrawField();
            MovesCount.Reset();
            IsGameStarted = true;
            SetGameStatus(GameStatus.InProcess);

            _game.WinEvent += Win;
            _game.VariantWasChecked += GotSolveProgressInfo;
            _game.LogEvent += Log;
            Log($"Game started. Field size {fieldSize}, random moves made: {randomStepsCount}");
        }

        public async void AutoGame()
        {
            if (!IsGameStarted) return;

            _aiProcessingCancellationToken = new CancellationTokenSource();
            var token = _aiProcessingCancellationToken.Token;

            SetGameStatus(GameStatus.AiProcessing);
            var directions = await Task.Run(() => _game.Solve(token), token);
            //var directions = _game.Solve(new CancellationToken());

            if (directions is null)
            {
                Fail();
                SetGameStatus(GameStatus.AiFail, " Ai gave up ( :(");
                return;
            }

            SetGameStatus(GameStatus.AiPlaying);
            MoveAuto(directions);
        }


        public void CancelSolveProcessing()
        {
            _aiProcessingCancellationToken.Cancel();
            ClearField();
        }

        public void Move(Direction direction)
        {
            var blockPoint = _game.Move(direction);
            if (blockPoint != null)
                MoveBlock(new List<Point> { (Point)blockPoint });
        }


        public void MoveAuto(IEnumerable<Direction> data)
        {
            var pointList = new List<Point>();
            foreach (var direction in data)
            {
                var p = _game.Move(direction);
                if (p != null)
                    pointList.Add((Point)p);
            }

            SetGameStatus(GameStatus.AiPlaying, $"  {pointList.Count} steps");
            MoveBlock(pointList);
            
        }                

        public (int top, int left) CalculateBlockPosition(Point point)
        {
            var top = point.Row * _blockHeight + CanvasPadding;
            var left = point.Col * _blockWidth + CanvasPadding;
            return (top, left);
        }

        public (UIElement block, Rectangle rect) DrawBlock(Point point)
        {
            var rect = new Rectangle
            {
                Fill = _blockBrush,
                Stroke = Brushes.Black
            };
            var width = _blockWidth - BlockPadding * 2;
            var height = _blockHeight - BlockPadding * 2;
            rect.Width = width;
            rect.Height = height;
            rect.Margin = new Thickness(BlockPadding, BlockPadding, 0, 0);
            rect.HorizontalAlignment = HorizontalAlignment.Stretch;
            rect.VerticalAlignment = VerticalAlignment.Stretch;

            var textNum = new TextBlock
            {
                Text = point.Val.ToString(),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 30,
                FontWeight = FontWeights.Bold
            };

            var block = new Grid
            {
                Width = _blockWidth,
                Height = _blockHeight
            };

            var blockIndex = _canvas.Children.Add(block);
            block.Children.Add(rect);
            block.Children.Add(textNum);
            
            Canvas.SetZIndex(rect, 3);
            Canvas.SetZIndex(textNum, 4);

            var (top, left) = CalculateBlockPosition(point);

            Canvas.SetTop(block, top);
            Canvas.SetLeft(block, left);

            return (_canvas.Children[blockIndex], rect);
        }
        

        public void MoveBlock(IEnumerable<Point> blockPoints)
        {
            var story = new Storyboard();

            int num = 0;
            foreach (var point in blockPoints)
            {
                AddBlockAnimationToStory(num++, point, story);
                MovesCount++;
            }

            story.Completed += EndOfBlockAnimation;
            story.Begin();            
        }

        public void AddBlockAnimationToStory(int num, Point blockPoint, Storyboard story)
        {
            var block = _blocks[blockPoint.Val];
            var (top, left) = CalculateBlockPosition(blockPoint);

            var leftAnimation = new DoubleAnimation
            {
                To = left,
                Duration = _movingDuration
            };            
            var sb = new Storyboard();
            sb.Children.Add(leftAnimation);
            Storyboard.SetTarget(sb, block.Block);
            Storyboard.SetTargetProperty(sb, new PropertyPath("(Canvas.Left)"));
            sb.BeginTime = (_movingDuration + _movingDelay) * num;
            story.Children.Add(sb);
            
            var topAnimation = new DoubleAnimation
            {
                To = top,
                Duration = _movingDuration
            };
            sb = new Storyboard();
            sb.Children.Add(topAnimation);
            Storyboard.SetTarget(sb, block.Block);
            Storyboard.SetTargetProperty(sb, new PropertyPath("(Canvas.Top)"));
            sb.BeginTime = (_movingDuration + _movingDelay) * num;
            story.Children.Add(sb);
        }
                

        public void SetGameStatus(GameStatus status, string extraText = "")
        {
            _gameStatus = status;
            var statusText = "Status: " +
                status switch
                {
                    GameStatus.NotStarted => "Not Started",
                    GameStatus.InProcess => "Game in process",
                    GameStatus.Finished => "Finished",
                    GameStatus.AiProcessing => "AI solution in progress... ",
                    GameStatus.AiPlaying => "AI playing",
                    GameStatus.AiFail => "Fail!",
                    _ => throw new ArgumentException("Unknown game status")
                } + extraText;

            _mainWindow.Dispatcher.BeginInvoke(() =>
            {
                StatusChanged?.Invoke(statusText);
                InterfaceLocker?.Invoke(status);
            });            
        }

        public void Log(string message)
        {
            _mainWindow.Dispatcher.Invoke(() =>
            {
                _mainWindow.LogView.Items.Add(message);
            });            
        }

        public void Win()
        {            
            IsGameStarted = false;            
            foreach (var block in _blocks)
                block.Value.Rect.Fill = _blockWinBrush;
            Log($"WIN. moves made: {MovesCount}");
        }

        public void Fail()
        {
            IsGameStarted = false;
            foreach (var block in _blocks)
                block.Value.Rect.Fill = _blockFailBrush;
            Log("Ai didn't find path for win :(");
        }

        public void GotSolveProgressInfo(int num, int queueCount) =>
            SetGameStatus(GameStatus.AiProcessing, 
                $"variants: {num},  queue count: {queueCount}");


        private void EndOfBlockAnimation(object? sender, EventArgs e)
        {            
            if (_game.IsWin())
            {
                Win();
                SetGameStatus(GameStatus.Finished);
            }
            return;
        }
    }
}
