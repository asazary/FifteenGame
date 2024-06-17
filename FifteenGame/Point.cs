namespace FifteenGame
{
    internal struct Point
    {
        public readonly int Row;
        public readonly int Col;
        public int Val;

        public Point(int row, int col, int val = 0)
        {
            Row = row;
            Col = col;
            Val = val;
        }

        public readonly Point Clone() => new Point(Row, Col, Val);

        public bool Equals(Point source) => 
            Val == source.Val && Row == source.Row && Col == source.Col;

        public override bool Equals(object? obj) =>
            obj is Point ? this.Equals((Point)obj) : false;
        
        public override int GetHashCode() => 
            Val.GetHashCode() + Row.GetHashCode() * 10 + Col.GetHashCode() * 100;

        public bool IsEmpty() => Val == 0;

        public static bool operator ==(Point p1, Point p2) =>
            p1.Equals(p2);

        public static bool operator !=(Point p1, Point p2) =>
            !p1.Equals(p2);

        public override string ToString()
        {
            return $"Row = {Row}, Col = {Col}, Val = {Val} ||";
        }
    }
}
