using System.Windows.Media;
using System.Windows.Shapes;

namespace FifteenGame
{
    internal class DiodAnimation
    {        
        public static Color CommonDiodColor = Color.FromArgb(255, 200, 200, 200);
        public readonly SolidColorBrush CommonDiodBrush = new(CommonDiodColor);
        public static Color ActiveDiodColor = Color.FromArgb(255, 70, 200, 140);
        public readonly SolidColorBrush ActiveDiodBrush = new(ActiveDiodColor);

        private readonly Dictionary<Direction, Rectangle> _diods = new Dictionary<Direction, Rectangle>();

        public DiodAnimation(Rectangle Up, Rectangle Left, Rectangle Right, Rectangle Down)
        {
            _diods[Direction.Up] = Up;
            _diods[Direction.Down] = Down;
            _diods[Direction.Left] = Left;
            _diods[Direction.Right] = Right;
        }

        public async void DiodActive(Direction direction)
        {
            var diod = _diods[direction];
            diod.Fill = ActiveDiodBrush;
            await Task.Delay(200);
            diod.Fill = CommonDiodBrush;
        }
    }
}
