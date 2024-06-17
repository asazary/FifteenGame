using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FifteenGame
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly GameField _gameField;
        private bool _autoSolveIsRunning = false;
        private DiodAnimation _diodAnimation;


        public MainWindow()
        {
            InitializeComponent();

            KeyDown += MainWindow_KeyDown;
            LogView.KeyDown += (s, e) => Field.Focus();

            FieldSize3.IsChecked = true;
            RandomStepsSlider.Value = 10;

            _gameField = GameField.CreateGameField(this);
            var (fieldSize, randomStepsCount) = GetParameterValues();
            _gameField.StartGame(fieldSize, randomStepsCount);

            _gameField.MovesCount.MovedEvent += UpdateMoveCounter;
            _gameField.StatusChanged += UpdateStatus;
            _gameField.InterfaceLocker += InterfaceControl;

            _diodAnimation = new DiodAnimation(DiodUp, DiodLeft, DiodRight, DiodDown);
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            Field.Focus();
            
            if (_gameField.IsGameStarted && 
                new[] { Key.Left, Key.Right, Key.Up, Key.Down,
                        Key.A, Key.D, Key.W, Key.S }
                .Contains(e.Key))
            {
                var direction = e.Key switch
                {
                    Key.Left => Direction.Left,
                    Key.A => Direction.Left,
                    Key.Right => Direction.Right,
                    Key.D => Direction.Right,
                    Key.Up => Direction.Up,
                    Key.W => Direction.Up,
                    Key.Down => Direction.Down,
                    Key.S => Direction.Down,
                    _ => throw new ArgumentOutOfRangeException(nameof(e.Key), $"Unexpected key: {e.Key}")
                };
                _diodAnimation.DiodActive(direction);
                _gameField.Move(Steps.InverseDirection(direction));
            }
        }

        private (int fieldSize, int stepsCount) GetParameterValues()
        {
            var fieldSize = 3;
            if (FieldSize2.IsChecked == true) fieldSize = 2;
            else if (FieldSize3.IsChecked == true) fieldSize = 3;
            else if (FieldSize4.IsChecked == true) fieldSize = 4;

            var randomStepsCount = (int)RandomStepsSlider.Value;
            return (fieldSize, randomStepsCount);
        }

        private void RandomStepsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ((Slider)sender).SelectionEnd = e.NewValue;
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            _gameField.ClearField();
        }

        private void StartButton_OnClick(object sender, RoutedEventArgs e)
        {
            var (fieldSize, randomStepsCount) = GetParameterValues();
            _gameField.StartGame(fieldSize, randomStepsCount);            
        }

        private void AutoSolveButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_autoSolveIsRunning)
            {
                AutoSolveButton.Content = "Cancel";
                _gameField.AutoGame();
            }
            else
            {
                AutoSolveButton.Content = "Auto solve";
                _gameField.CancelSolveProcessing();
            }
            _autoSolveIsRunning = !_autoSolveIsRunning;
        }
                

        private void UpdateMoveCounter(int count)
        {
            MovesCount.Text = $"moves: {count,4}";
        }

        private void UpdateStatus(string status)
        {
            GameStatusText.Text = status;
        }

        private void InterfaceControl(GameStatus status)
        {
            AutoSolveButton.IsEnabled = !(status == GameStatus.NotStarted
                    || status == GameStatus.Finished || status == GameStatus.AiFail);
            StartButton.IsEnabled = !(status == GameStatus.AiProcessing
                || status == GameStatus.AiPlaying);
            ClearButton.IsEnabled = !(status == GameStatus.AiProcessing
                || status == GameStatus.AiPlaying);
            
            if (status != GameStatus.AiProcessing && status != GameStatus.AiPlaying)
            {
                AutoSolveButton.Content = "Auto solve";
                _autoSolveIsRunning = false;
            }               
        }               
    }        
}