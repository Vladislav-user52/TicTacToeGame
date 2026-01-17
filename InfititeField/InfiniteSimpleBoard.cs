using System;
using System.Collections.Generic;
using System.Linq;

namespace TicTacToeGame.Models
{
    public class InfiniteSimpleBoard : TicTacToeBoard
    {
        private readonly HashSet<(int, int)> _generatedCells;
        
        public InfiniteSimpleBoard(GameRules gameRules = null) 
            : base(gameRules ?? new SimpleGameRules())
        {
            _generatedCells = new HashSet<(int, int)>();
            
            
            GenerateCell(0, 0);
        }
        
        private InfiniteSimpleBoard(InfiniteSimpleBoard other) 
            : base(other)
        {
            _generatedCells = new HashSet<(int, int)>(other._generatedCells);
        }
        
        public new bool MakeMove(int x, int y)
        {
            
            GenerateCellAndNeighbors(x, y);
            
            return base.MakeMove(x, y);
        }
        
        public new bool MakeMove(TicTacToeMove move)
        {
            return MakeMove(move.X, move.Y);
        }
        
        private void GenerateCell(int x, int y)
        {
            if (!_generatedCells.Contains((x, y)))
            {
                _generatedCells.Add((x, y));
                
                
                FieldGenerator.SetCellWeight(x, y, 1.0);
            }
        }
        
        
        public new GameResult CheckWinner()
        {
            
            int requiredLength = GetRequiredLineLength(); 
            
            
            var generatedCells = GetGeneratedField().ToList();
            
            foreach (var cell in generatedCells)
            {
                Player player = cell.player;
                if (player == Player.None) continue;
                
                int x = cell.x;
                int y = cell.y;
                
                
                int[][] directions = {
                    new[] {1, 0},   // горизонталь
                    new[] {0, 1},   // вертикаль
                    new[] {1, 1},   // диагональ \
                    new[] {1, -1}   // диагональ /
                };
                
                foreach (var dir in directions)
                {
                    
                    bool hasLine = true;
                    
                    for (int i = 0; i < requiredLength; i++)
                    {
                        int checkX = x + i * dir[0];
                        int checkY = y + i * dir[1];
                        
                        if (GetCell(checkX, checkY) != player)
                        {
                            hasLine = false;
                            break;
                        }
                    }
                    
                    if (hasLine)
                    {
                        return player == Player.X ? GameResult.XWins : GameResult.OWins;
                    }
                }
            }
            
            
            if (MoveCount >= 100)
            {
                
                bool hasEmptyCells = generatedCells.Any(c => c.player == Player.None);
                if (!hasEmptyCells)
                {
                    return GameResult.Draw;
                }
            }
            
            return GameResult.None;
        }
        private void GenerateCellAndNeighbors(int x, int y)
        {
            
            GenerateCell(x, y);

            
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = x + dx;
                    int ny = y + dy;
                    GenerateCell(nx, ny);
                }
            }
        }
        
        public new IEnumerable<(int x, int y, Player player)> GetGeneratedField(int radius = 3)
        {
            
            foreach (var (x, y) in _generatedCells.OrderBy(c => Math.Abs(c.Item1) + Math.Abs(c.Item2)))
            {
                yield return (x, y, GetCell(x, y));
            }
        }
        
        public bool IsCellGenerated(int x, int y)
        {
            return _generatedCells.Contains((x, y));
        }
        
        public int GetGeneratedCellsCount()
        {
            return _generatedCells.Count;
        }
        
        public new string GetBoardVisualization(int radius = 3)
        {
            var builder = new System.Text.StringBuilder();
            
            // Находим границы сгенерированных клеток
            int minX = _generatedCells.Min(c => c.Item1);
            int maxX = _generatedCells.Max(c => c.Item1);
            int minY = _generatedCells.Min(c => c.Item2);
            int maxY = _generatedCells.Max(c => c.Item2);
            
            for (int y = maxY; y >= minY; y--)
            {
                builder.Append($"{y,3}: ");
                for (int x = minX; x <= maxX; x++)
                {
                    if (_generatedCells.Contains((x, y)))
                    {
                        var player = GetCell(x, y);
                        char symbol = player switch
                        {
                            Player.X => 'X',
                            Player.O => 'O',
                            _ => '.'
                        };
                        builder.Append($"{symbol} ");
                    }
                    else
                    {
                        builder.Append("  ");
                    }
                }
                builder.AppendLine();
            }
            
            builder.Append("    ");
            for (int x = minX; x <= maxX; x++)
            {
                builder.Append($"{Math.Abs(x),2}");
            }
            
            return builder.ToString();
        }
        
        public new InfiniteSimpleBoard Clone()
        {
            return new InfiniteSimpleBoard(this);
        }
    }
}