using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TicTacToeGame.Models;
using TicTacToeGame.Interfaces;

namespace TicTacToeGame.Algorithms
{
    public class TicTacToeSolver : IAlgorithm
    {
        public TimeSpan LastExecutionTime { get; private set; }
        
        private readonly Random _random = new Random();
        
        private const double FORCED_BLOCK_SCORE = 1000000;
        private const double DANGER_BLOCK_SCORE = 500000;
        private const double LINE_5_SCORE = 100000;
        
        
        private HashSet<(int x, int y)> _processedWinCells = new HashSet<(int x, int y)>();
        private HashSet<(int x, int y)> _blockedCells = new HashSet<(int x, int y)>();
        
        public TicTacToeSolver(int maxDepth = 8)
        {
        }
        
        public TicTacToeMove? FindBestMove(TicTacToeBoard board, int timeLimitMs = 2000)
        {
            var stopwatch = Stopwatch.StartNew();
            
            if (board.CheckWinner() != GameResult.None)
            {
                LastExecutionTime = stopwatch.Elapsed;
                return null;
            }
            
            var currentPlayer = board.CurrentPlayer;
            var opponent = currentPlayer == Player.X ? Player.O : Player.X;
            int requiredLength = board.GetRequiredLineLength();
            
            
            _processedWinCells.Clear();
            _blockedCells.Clear();
            
        
            foreach (var move in board.GetPossibleMoves())
            {
                var testBoard = board.Clone();
                testBoard.MakeMove(move);
                
                if (testBoard.CheckWinner() == (currentPlayer == Player.X ? GameResult.XWins : GameResult.OWins))
                {
                    LastExecutionTime = stopwatch.Elapsed;
                    return move;
                }
            }
            
            
            var opponentWinCells = new List<(int x, int y)>();
            
            foreach (var move in board.GetPossibleMoves())
            {
                var testBoard = board.Clone();
                
                var opponentMove = new TicTacToeMove(move.X, move.Y, opponent);
                testBoard.MakeMove(opponentMove);
                
                if (testBoard.CheckWinner() == (opponent == Player.X ? GameResult.XWins : GameResult.OWins))
                {
                    opponentWinCells.Add((move.X, move.Y));
                }
            }
            
            
            if (opponentWinCells.Any())
            {
                
                var blockingMoves = new List<TicTacToeMove>();
                
                foreach (var (winX, winY) in opponentWinCells)
                {
                    
                    _processedWinCells.Add((winX, winY));
                    
                    
                    if (board.GetCell(winX, winY) == Player.None)
                    {
                        var blockMove = new TicTacToeMove(winX, winY, currentPlayer);
                        blockingMoves.Add(blockMove);
                        _blockedCells.Add((winX, winY));
                    }
                    
                    
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            
                            int blockX = winX + dx;
                            int blockY = winY + dy;
                            
                            if (board.GetCell(blockX, blockY) == Player.None)
                            {
                                
                                var testBoard = board.Clone();
                                var blockMove = new TicTacToeMove(blockX, blockY, currentPlayer);
                                testBoard.MakeMove(blockMove);
                                
                                var winMove = new TicTacToeMove(winX, winY, opponent);
                                testBoard.MakeMove(winMove);
                                
                                if (!(testBoard.CheckWinner() == (opponent == Player.X ? GameResult.XWins : GameResult.OWins)))
                                {
                                    blockingMoves.Add(new TicTacToeMove(blockX, blockY, currentPlayer));
                                    _blockedCells.Add((blockX, blockY));
                                }
                            }
                        }
                    }
                }
                
                if (blockingMoves.Any())
                {
                    
                    var bestBlock = blockingMoves
                        .OrderByDescending(m => GetSimpleBlockPriority(board, m))
                        .First();
                    
                    LastExecutionTime = stopwatch.Elapsed;
                    return bestBlock;
                }
            }
            
            
            var opponentDangerCells = new List<TicTacToeMove>();
            
            
            foreach (var line in board.GetPlayerLines(opponent))
            {
                if (line.Cells.Count >= 2) 
                {
                    
                    var extensionCells = FindSimpleLineExtensions(board, line, requiredLength);
                    
                    foreach (var (x, y) in extensionCells)
                    {
                        
                        if (_processedWinCells.Contains((x, y)) || _blockedCells.Contains((x, y)))
                            continue;
                            
                        if (board.GetCell(x, y) == Player.None)
                        {
                            opponentDangerCells.Add(new TicTacToeMove(x, y, currentPlayer));
                        }
                    }
                }
            }
            
            if (opponentDangerCells.Any())
            {
               
                var dangerCellGroups = opponentDangerCells
                    .GroupBy(m => (m.X, m.Y))
                    .Select(g => new { 
                        Move = g.First(), 
                        Count = g.Count(),
                        Priority = GetSimpleBlockPriority(board, g.First())
                    })
                    .OrderByDescending(x => x.Count)
                    .ThenByDescending(x => x.Priority)
                    .ToList();
                
                var bestDangerBlock = dangerCellGroups.First().Move;
                
                LastExecutionTime = stopwatch.Elapsed;
                return bestDangerBlock;
            }
            
            
            var ourLineMoves = new List<TicTacToeMove>();
            
            foreach (var line in board.GetPlayerLines(currentPlayer))
            {
                if (line.Cells.Count >= 2)
                {
                    var extensionCells = FindSimpleLineExtensions(board, line, requiredLength);
                    
                    foreach (var (x, y) in extensionCells)
                    {
                        
                        if (_blockedCells.Contains((x, y)))
                            continue;
                            
                        if (board.GetCell(x, y) == Player.None)
                        {
                            ourLineMoves.Add(new TicTacToeMove(x, y, currentPlayer));
                        }
                    }
                }
            }
            
            if (ourLineMoves.Any())
            {
                var bestLineMove = ourLineMoves
                    .GroupBy(m => (m.X, m.Y))
                    .Select(g => new { 
                        Move = g.First(), 
                        Count = g.Count(),
                        Priority = GetSimpleBlockPriority(board, g.First())
                    })
                    .OrderByDescending(x => x.Count)
                    .ThenByDescending(x => x.Priority)
                    .First()
                    .Move;
                
                LastExecutionTime = stopwatch.Elapsed;
                return bestLineMove;
            }
            
            
            var allMoves = board.GetWeightedPossibleMoves().ToList();
            
            if (allMoves.Any())
            {
                
                var evaluatedMoves = new List<(TicTacToeMove move, double score)>();
                
                foreach (var (move, weight) in allMoves.Take(15))
                {
                    
                    if (_blockedCells.Contains((move.X, move.Y)))
                        continue;
                        
                    double score = weight * 100;
                    
                    
                    double blockMultiplier = _processedWinCells.Contains((move.X, move.Y)) ? 0.5 : 1.0;
                    score += EvaluateSimpleBlock(board, move, opponent) * 1000 * blockMultiplier;
                    
                    
                    score += EvaluateSimpleLine(board, move, currentPlayer) * 100;
                    
                    
                    if (Math.Abs(move.X) <= 1 && Math.Abs(move.Y) <= 1) score += 500;
                    
                    evaluatedMoves.Add((move, score));
                }
                
                if (evaluatedMoves.Any())
                {
                    var bestMove = evaluatedMoves
                        .OrderByDescending(m => m.score)
                        .First()
                        .move;
                    
                    LastExecutionTime = stopwatch.Elapsed;
                    return bestMove;
                }
            }
            
            
            var fallbackMoves = board.GetPossibleMoves()
                .Where(m => !_blockedCells.Contains((m.X, m.Y)))
                .OrderByDescending(m => GetSimpleBlockPriority(board, m))
                .ToList();
                
            var fallback = fallbackMoves.FirstOrDefault();
            LastExecutionTime = stopwatch.Elapsed;
            return fallback;
        }
        
        
        private List<(int x, int y)> FindSimpleLineExtensions(TicTacToeBoard board, TicTacToeBoard.Line line, int requiredLength)
        {
            var extensions = new List<(int x, int y)>();
            
            if (line.Cells.Count == 0) return extensions;
            
            
            if (line.Cells.Count == 1)
            {
                var cell = line.Cells.First();
                
                
                int[][] directions = {
                    new[] {1, 0}, new[] {0, 1}, new[] {1, 1}, new[] {1, -1}
                };
                
                foreach (var dir in directions)
                {
                    for (int i = 1; i < requiredLength; i++)
                    {
                        extensions.Add((cell.x + i * dir[0], cell.y + i * dir[1]));
                        extensions.Add((cell.x - i * dir[0], cell.y - i * dir[1]));
                    }
                }
            }
            else
            {
               
                var first = line.Cells.First();
                var last = line.Cells.Last();
                
                int dx = 0, dy = 0;
                
                if (last.x != first.x) dx = Math.Sign(last.x - first.x);
                if (last.y != first.y) dy = Math.Sign(last.y - first.y);
                
                
                if (dx != 0 || dy != 0)
                {
                    
                    extensions.Add((first.x - dx, first.y - dy));
                    extensions.Add((last.x + dx, last.y + dy));
                }
                else
                {
                    
                    foreach (var cell in line.Cells)
                    {
                        for (int dx2 = -1; dx2 <= 1; dx2++)
                        {
                            for (int dy2 = -1; dy2 <= 1; dy2++)
                            {
                                if (dx2 == 0 && dy2 == 0) continue;
                                extensions.Add((cell.x + dx2, cell.y + dy2));
                            }
                        }
                    }
                }
            }
            
            return extensions.Distinct().Where(pos => 
                Math.Abs(pos.x) <= 10 && Math.Abs(pos.y) <= 10).ToList();
        }
        
        
        private double GetSimpleBlockPriority(TicTacToeBoard board, TicTacToeMove move)
        {
            double priority = 0;
            
            
            priority += board.FieldGenerator.GetCellWeight(move.X, move.Y) * 100;
            
            
            if (move.X == 0 && move.Y == 0) priority += 1000;
            else if (Math.Abs(move.X) <= 1 && Math.Abs(move.Y) <= 1) priority += 500;
            
            
            priority += CountAdjacent(board, move.X, move.Y, board.CurrentPlayer) * 50;
            
            return priority;
        }
        
        
        private double EvaluateSimpleBlock(TicTacToeBoard board, TicTacToeMove move, Player opponent)
        {
            double blockScore = 0;
            
            
            foreach (var line in board.GetPlayerLines(opponent))
            {
                if (line.Cells.Count >= 2)
                {
                    
                    foreach (var (x, y) in line.Cells)
                    {
                        int dx = Math.Abs(x - move.X);
                        int dy = Math.Abs(y - move.Y);
                        
                        if ((dx == 0 && dy == 1) || (dx == 1 && dy == 0) || 
                            (dx == 1 && dy == 1))
                        {
                            blockScore += 100 * line.Cells.Count;
                            break;
                        }
                    }
                }
            }
            
            return blockScore;
        }
        
        
        private double EvaluateSimpleLine(TicTacToeBoard board, TicTacToeMove move, Player player)
        {
            double lineScore = 0;
            
            var testBoard = board.Clone();
            testBoard.MakeMove(move);
            
            
            foreach (var line in testBoard.GetPlayerLines(player))
            {
                lineScore += line.Cells.Count * 50;
                
               
                if (line.Cells.Count >= 4) lineScore += 200;
                if (line.Cells.Count >= 3) lineScore += 100;
            }
            
            return lineScore;
        }
        
        
        private int CountAdjacent(TicTacToeBoard board, int x, int y, Player player)
        {
            int count = 0;
            
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    if (board.GetCell(x + dx, y + dy) == player)
                        count++;
                }
            }
            
            return count;
        }
        
        
        private double WeightedMinimax(
            TicTacToeBoard board,
            int depth,
            double alpha,
            double beta,
            bool maximizingPlayer,
            Stopwatch timer,
            int timeLimitMs)
        {
            return 0;
        }
        
        private IEnumerable<TicTacToeMove> SortWeightedMoves(TicTacToeBoard board, IEnumerable<TicTacToeMove> moves, bool maximizingPlayer)
        {
            return moves.Take(5);
        }
        
        private double QuickWeightedEvaluate(TicTacToeBoard board)
        {
            return 0;
        }
        
        public (int nodesEvaluated, int positionsGenerated, TimeSpan executionTime) GetStats()
        {
            return (0, 0, LastExecutionTime);
        }
        
        public string GetEvaluationInfo(TicTacToeBoard board)
        {
            return $"Текущий игрок: {board.CurrentPlayer} | " +
                   $"Обработано выигрышных клеток: {_processedWinCells.Count} | " +
                   $"Заблокировано клеток: {_blockedCells.Count}";
        }
    }
}