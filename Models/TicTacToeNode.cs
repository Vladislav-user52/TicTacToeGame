using System.Collections.Generic;
using System.Linq;

namespace TicTacToeGame.Models
{
    public class TicTacToeNode
    {
        public TicTacToeBoard Board { get; }
        public TicTacToeMove LastMove { get; }
        public int Depth { get; }
        public double Score { get; set; }
        public int GeneratedMovesCount { get; private set; }
        
        public TicTacToeNode(TicTacToeBoard board, TicTacToeMove lastMove = null, int depth = 0)
        {
            Board = board.Clone();
            LastMove = lastMove;
            Depth = depth;
            GeneratedMovesCount = Board.GetPossibleMoves().Count();
        }
        
        public bool IsTerminal => Board.CheckWinner() != GameResult.None;
        
        public GameResult Result => Board.CheckWinner();

        public IEnumerable<TicTacToeMove> GetPrioritizedMoves()
        {
            return Board.GetPossibleMoves();
        }

        public (int occupied, int radius) GetFieldInfo()
        {
            var stats = Board.GetGenerationStats();
            return (stats.occupied, stats.radius);
        }
    }
}