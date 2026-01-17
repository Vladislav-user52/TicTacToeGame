using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TicTacToeGame.Models;

namespace TicTacToeGame.Algorithms
{
    public class SimpleSolver
    {
        public TimeSpan LastExecutionTime { get; private set; }
        private readonly Random _random = new Random();
        
        public SimpleSolver()
        {
        }
        
        public TicTacToeMove? FindBestMove(TicTacToeBoard board, int timeLimitMs = 1000)
        {
            var stopwatch = Stopwatch.StartNew();
            
            if (board.CheckWinner() != GameResult.None)
            {
                LastExecutionTime = stopwatch.Elapsed;
                return null;
            }
            
            var currentPlayer = board.CurrentPlayer;
            var opponent = currentPlayer == Player.X ? Player.O : Player.X;
            var possibleMoves = board.GetPossibleMoves().ToList();
            
            if (!possibleMoves.Any())
            {
                LastExecutionTime = stopwatch.Elapsed;
                return null;
            }
            
            
            foreach (var move in possibleMoves)
            {
                var testBoard = board.Clone();
                testBoard.MakeMove(move);
                
                if (testBoard.CheckWinner() == (currentPlayer == Player.X ? GameResult.XWins : GameResult.OWins))
                {
                    LastExecutionTime = stopwatch.Elapsed;
                    return move;
                }
            }
            
            
            var immediateThreats = new List<TicTacToeMove>();
            
            foreach (var move in possibleMoves)
            {
                var testBoard = board.Clone();
                var opponentMove = new TicTacToeMove(move.X, move.Y, opponent);
                testBoard.MakeMove(opponentMove);
                
                if (testBoard.CheckWinner() == (opponent == Player.X ? GameResult.XWins : GameResult.OWins))
                {
                    immediateThreats.Add(new TicTacToeMove(move.X, move.Y, currentPlayer));
                }
            }
            
            if (immediateThreats.Any())
            {
                var bestBlock = SelectBestBlockingMove(board, immediateThreats, currentPlayer);
                LastExecutionTime = stopwatch.Elapsed;
                return bestBlock;
            }
            
            
            var opponentThreats = FindOpponentThreats(board, currentPlayer);
            if (opponentThreats.Any())
            {
                var bestThreatBlock = SelectBestBlockingMove(board, opponentThreats, currentPlayer);
                LastExecutionTime = stopwatch.Elapsed;
                return bestThreatBlock;
            }
            
            
            var evaluatedMoves = new List<(TicTacToeMove move, double score)>();
            
            foreach (var move in possibleMoves)
            {
                double score = 0;
                
                
                score += GetPositionScore(move.X, move.Y);
                score += CountAdjacent(board, move.X, move.Y, currentPlayer) * 30;
                score += CountAdjacent(board, move.X, move.Y, Player.None) * 5;
                
                
                score += EvaluateLinePotential(board, move, currentPlayer) * 1.5;
                
                
                score -= EvaluateOpponentOpportunity(board, move, opponent) * 2.0;
                
                
                score += EvaluateBlockingValue(board, move, opponent) * 1.2;
                
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
            
            
            var fallback = possibleMoves
                .OrderBy(m => GetPositionScore(m.X, m.Y))
                .ThenBy(_ => _random.Next())
                .FirstOrDefault();
                
            LastExecutionTime = stopwatch.Elapsed;
            return fallback;
        }
        
        
        private List<TicTacToeMove> FindOpponentThreats(TicTacToeBoard board, Player currentPlayer)
        {
            var opponent = currentPlayer == Player.X ? Player.O : Player.X;
            var threats = new List<TicTacToeMove>();
            
            
            int searchRadius = 10; 
            
            
            for (int x = -searchRadius; x <= searchRadius; x++)
            {
                for (int y = -searchRadius; y <= searchRadius; y++)
                {
                    
                    if (board.GetCell(x, y) == opponent)
                    {
                        
                        int[][] directions = {
                            new[] {1, 0}, new[] {0, 1}, new[] {1, 1}, new[] {1, -1}
                        };
                        
                        foreach (var dir in directions)
                        {
                            
                            int count = 1;
                            List<(int x, int y)> emptyCells = new List<(int, int)>();
                            
                            
                            for (int i = 1; i < 5; i++)
                            {
                                int nx = x + i * dir[0];
                                int ny = y + i * dir[1];
                                
                                var cell = board.GetCell(nx, ny);
                                if (cell == opponent) count++;
                                else if (cell == Player.None) emptyCells.Add((nx, ny));
                                else break;
                            }
                            
                            
                            for (int i = 1; i < 5; i++)
                            {
                                int nx = x - i * dir[0];
                                int ny = y - i * dir[1];
                                
                                var cell = board.GetCell(nx, ny);
                                if (cell == opponent) count++;
                                else if (cell == Player.None) emptyCells.Add((nx, ny));
                                else break;
                            }
                            
                            
                            if (count >= 3 && emptyCells.Any())
                            {
                                foreach (var cell in emptyCells)
                                {
                                    
                                    if (board.GetCell(cell.x, cell.y) == Player.None)
                                    {
                                        threats.Add(new TicTacToeMove(cell.x, cell.y, currentPlayer));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            return threats.DistinctBy(m => new { m.X, m.Y }).ToList();
        }
        
        
        
        private TicTacToeMove SelectBestBlockingMove(TicTacToeBoard board, List<TicTacToeMove> blocks, Player currentPlayer)
        {
            
            var evaluated = blocks.Select(move =>
            {
                double score = 0;

                
                score += GetPositionScore(move.X, move.Y);
                score += CountAdjacent(board, move.X, move.Y, currentPlayer) * 25;
                score += EvaluateLinePotential(board, move, currentPlayer);

                
                score += CountBlockedDirections(board, move, currentPlayer == Player.X ? Player.O : Player.X) * 20;

                return (move, score);
            });

            return evaluated.OrderByDescending(x => x.score).First().move;
        }
        
        
        private double EvaluateLinePotential(TicTacToeBoard board, TicTacToeMove move, Player player)
        {
            double potential = 0;
            
            int[][] directions = {
                new[] {1, 0}, new[] {0, 1}, new[] {1, 1}, new[] {1, -1}
            };
            
            foreach (var dir in directions)
            {
                int count = 0;
                int empty = 0;
                
                
                for (int i = -4; i <= 4; i++)
                {
                    if (i == 0) continue; 
                    
                    int x = move.X + i * dir[0];
                    int y = move.Y + i * dir[1];
                    var cell = board.GetCell(x, y);
                    
                    if (cell == player) count++;
                    else if (cell == Player.None) empty++;
                }
                
                
                if (count >= 2) potential += count * 25 + empty * 10;
                if (count >= 3) potential += 50;
                if (count >= 4) potential += 150;
            }
            
            return potential;
        }
        
        
        private double EvaluateBlockingValue(TicTacToeBoard board, TicTacToeMove move, Player opponent)
        {
            double blockingValue = 0;
            
            
            int[][] directions = {
                new[] {1, 0}, new[] {0, 1}, new[] {1, 1}, new[] {1, -1}
            };
            
            foreach (var dir in directions)
            {
                
                int opponentCount = 0;
                
                for (int i = -4; i <= 4; i++)
                {
                    int x = move.X + i * dir[0];
                    int y = move.Y + i * dir[1];
                    
                    if (board.GetCell(x, y) == opponent) opponentCount++;
                }
                
                
                if (opponentCount >= 2) blockingValue += opponentCount * 15;
                if (opponentCount >= 3) blockingValue += 40;
            }
            
            return blockingValue;
        }
        
        
        private double EvaluateOpponentOpportunity(TicTacToeBoard board, TicTacToeMove move, Player opponent)
        {
            double danger = 0;
            
            
            var testBoard = board.Clone();
            testBoard.MakeMove(move);
            
            
            int[][] directions = {
                new[] {1, 0}, new[] {0, 1}, new[] {1, 1}, new[] {1, -1}
            };
            
            foreach (var dir in directions)
            {
                
                for (int i = -4; i <= 4; i++)
                {
                    int x = move.X + i * dir[0];
                    int y = move.Y + i * dir[1];
                    
                    if (testBoard.GetCell(x, y) == Player.None)
                    {
                        var opponentMove = new TicTacToeMove(x, y, opponent);
                        var nextBoard = testBoard.Clone();
                        nextBoard.MakeMove(opponentMove);
                        
                        
                        if (nextBoard.CheckWinner() == (opponent == Player.X ? GameResult.XWins : GameResult.OWins))
                        {
                            danger += 100; 
                        }
                        
                        
                        danger += EvaluateLinePotential(nextBoard, opponentMove, opponent) / 10.0;
                    }
                }
            }
            
            return danger;
        }
        
        
        private int CountBlockedDirections(TicTacToeBoard board, TicTacToeMove move, Player opponent)
        {
            int blockedDirections = 0;
            
            int[][] directions = {
                new[] {1, 0}, new[] {0, 1}, new[] {1, 1}, new[] {1, -1}
            };
            
            foreach (var dir in directions)
            {
                bool hasOpponentLine = false;
                
                
                for (int i = -3; i <= 3; i++)
                {
                    if (i == 0) continue;
                    
                    int x = move.X + i * dir[0];
                    int y = move.Y + i * dir[1];
                    
                    if (board.GetCell(x, y) == opponent)
                    {
                        hasOpponentLine = true;
                        break;
                    }
                }
                
                if (hasOpponentLine) blockedDirections++;
            }
            
            return blockedDirections;
        }
        
        private double GetPositionScore(int x, int y)
        {
            
            double distance = Math.Sqrt(x * x + y * y);
            
            if (distance == 0) return 100;
            if (distance <= 1) return 80;
            if (distance <= 2) return 60;
            if (distance <= 3) return 40;
            if (distance <= 4) return 20;
            if (distance <= 5) return 10;
            return 0;
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
        
        public (int nodesEvaluated, int positionsGenerated, TimeSpan executionTime) GetStats()
        {
            return (0, 0, LastExecutionTime);
        }
    }
}