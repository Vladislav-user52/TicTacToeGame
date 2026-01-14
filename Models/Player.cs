namespace TicTacToeGame.Models
{
    public enum Player { None, X, O }
    public enum GameResult { None, XWins, OWins, Draw }
    
    public class TicTacToeMove
    {
        public int X { get; }
        public int Y { get; }
        public Player Player { get; }
        
        public TicTacToeMove(int x, int y, Player player)
        {
            X = x;
            Y = y;
            Player = player;
        }
        
        public override string ToString() => $"({X}, {Y}) - {Player}";
    }
}