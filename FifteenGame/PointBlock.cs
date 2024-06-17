using System.Windows;
using System.Windows.Shapes;

namespace FifteenGame
{
    struct PointBlock
    {
        public Point Point { get; private set; }
        public UIElement Block {  get; private set; }
        public Rectangle Rect { get; private set; }

        public PointBlock(Point point, UIElement block, Rectangle rect)
        {
            Point = point;
            Block = block;
            Rect = rect;
        }
    }
}
