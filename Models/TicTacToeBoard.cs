using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TicTacToeGame.Models
{
    public class TicTacToeBoard : IEquatable<TicTacToeBoard>
    {
        private readonly Dictionary<(int, int), Player> _board;
        private InfiniteFieldGenerator _fieldGenerator;
        private readonly GameRules _gameRules;
        
        private readonly Dictionary<Player, Line> _bestPlayerLine;
        
        public Player CurrentPlayer { get; private set; }
        public int MoveCount => _board.Count;
        public InfiniteFieldGenerator FieldGenerator => _fieldGenerator;
        
        public class Line
        {
            public List<(int x, int y)> Cells { get; set; }
            public Player Player { get; set; }
            public double TotalWeight { get; set; }
            public bool IsComplete => Cells.Count >= RequiredLength;
            public int RequiredLength { get; set; }
            public bool IsFullyBlocked { get; set; }
            public bool IsActive { get; set; }
            public int[] Direction { get; set; } 
            public Line(Player player, int requiredLength, int[] direction = null)
            {
                Player = player;
                Cells = new List<(int x, int y)>();
                RequiredLength = requiredLength;
                IsFullyBlocked = false;
                IsActive = true;
                Direction = direction ?? new[] { 0, 0 };
            }
            
            public void AddCell(int x, int y, double weight)
            {
                Cells.Add((x, y));
                TotalWeight += weight;
            }
            
            public bool Contains(int x, int y) => Cells.Any(c => c.x == x && c.y == y);
            
            public double CalculateScore()
            {
                if (!IsActive)
                    return 0;
                    
                if (IsFullyBlocked)
                    return 0;
                    
                return TotalWeight;
            }
            
            public void Reset()
            {
                IsActive = false;
                IsFullyBlocked = true;
            }
            
            public override string ToString()
            {
                return $"{Player} Line: {Cells.Count}/{RequiredLength} cells, " +
                       $"Weight: {TotalWeight:F1}, Blocked: {IsFullyBlocked}, Active: {IsActive}";
            }
        }
        
        public TicTacToeBoard(GameRules gameRules = null)
        {
            _board = new Dictionary<(int, int), Player>();
            _fieldGenerator = new InfiniteFieldGenerator();
            _gameRules = gameRules ?? new GameRules();
            
            _bestPlayerLine = new Dictionary<Player, Line>
            {
                [Player.X] = null,
                [Player.O] = null
            };
            CurrentPlayer = Player.X;
        }
        
        protected TicTacToeBoard(TicTacToeBoard other)
        {
            _board = new Dictionary<(int, int), Player>(other._board);
            _fieldGenerator = new InfiniteFieldGenerator(other._board.Keys);
            _gameRules = other._gameRules;
            
            _bestPlayerLine = new Dictionary<Player, Line>
            {
                [Player.X] = other._bestPlayerLine[Player.X] != null ? 
                    new Line(other._bestPlayerLine[Player.X].Player, other._bestPlayerLine[Player.X].RequiredLength, other._bestPlayerLine[Player.X].Direction)
                    {
                        Cells = new List<(int, int)>(other._bestPlayerLine[Player.X].Cells),
                        TotalWeight = other._bestPlayerLine[Player.X].TotalWeight,
                        IsFullyBlocked = other._bestPlayerLine[Player.X].IsFullyBlocked,
                        IsActive = other._bestPlayerLine[Player.X].IsActive
                    } : null,
                [Player.O] = other._bestPlayerLine[Player.O] != null ?
                    new Line(other._bestPlayerLine[Player.O].Player, other._bestPlayerLine[Player.O].RequiredLength, other._bestPlayerLine[Player.O].Direction)
                    {
                        Cells = new List<(int, int)>(other._bestPlayerLine[Player.O].Cells),
                        TotalWeight = other._bestPlayerLine[Player.O].TotalWeight,
                        IsFullyBlocked = other._bestPlayerLine[Player.O].IsFullyBlocked,
                        IsActive = other._bestPlayerLine[Player.O].IsActive
                    } : null
            };
            CurrentPlayer = other.CurrentPlayer;
        }
        
        public bool MakeMove(int x, int y)
        {
            if (_board.ContainsKey((x, y)))
                return false;
                
            _board[(x, y)] = CurrentPlayer;
            _fieldGenerator.MarkPositionOccupied(x, y);
            
            UpdateWeightsAfterMove(x, y, CurrentPlayer);
            UpdateBestLineForPlayer(x, y, CurrentPlayer);
            
            Player opponent = CurrentPlayer == Player.X ? Player.O : Player.X;
            CheckAndResetBlockedLine(opponent);
            
            CurrentPlayer = CurrentPlayer == Player.X ? Player.O : Player.X;
            return true;
        }
        
        public bool MakeMove(TicTacToeMove move)
        {
            return MakeMove(move.X, move.Y);
        }
        
        public void UndoMove(int x, int y)
        {
            if (_board.ContainsKey((x, y)))
            {
                var player = _board[(x, y)];
                _board.Remove((x, y));
                _fieldGenerator.MarkPositionFree(x, y);
                
                _fieldGenerator.DecreaseCellWeight(x, y, 1.0);
                
                var bestLine = _bestPlayerLine[player];
                if (bestLine != null && bestLine.Contains(x, y))
                {
                    var cellToRemove = bestLine.Cells.FirstOrDefault(c => c.x == x && c.y == y);
                    if (cellToRemove != default)
                    {
                        bestLine.Cells.Remove(cellToRemove);
                        bestLine.TotalWeight -= _fieldGenerator.GetCellWeight(x, y);
                        
                        if (bestLine.Cells.Count < 2)
                        {
                            _bestPlayerLine[player] = null;
                        }
                        else
                        {
                        
                            UpdateLineDirection(bestLine);
                            bestLine.IsFullyBlocked = IsLineFullyBlocked(bestLine, player);
                        }
                    }
                }
                
                CurrentPlayer = CurrentPlayer == Player.X ? Player.O : Player.X;
            }
        }
        
        private void UpdateLineDirection(Line line)
        {
            if (line.Cells.Count < 2)
            {
                line.Direction = new[] { 0, 0 };
                return;
            }
            
            var firstCell = line.Cells.First();
            var lastCell = line.Cells.Last();
            
            int dx = lastCell.x - firstCell.x;
            int dy = lastCell.y - firstCell.y;
            
            
            if (dx != 0) dx = Math.Sign(dx);
            if (dy != 0) dy = Math.Sign(dy);
            
            line.Direction = new[] { dx, dy };
        }
        
        private void UpdateBestLineForPlayer(int x, int y, Player player)
        {
            double cellWeight = _fieldGenerator.GetCellWeight(x, y);
            int requiredLength = _gameRules.RequiredLineLength;
            Player opponent = player == Player.X ? Player.O : Player.X;
            
            var currentBestLine = _bestPlayerLine[player];
            
            if (currentBestLine == null)
            {
                
                var newLine = FindBestLineFromCell(x, y, player, requiredLength);
                if (newLine != null)
                {
                    _bestPlayerLine[player] = newLine;
                    newLine.IsFullyBlocked = IsLineFullyBlocked(newLine, player);
                }
                return;
            }
            
            if (!currentBestLine.IsActive)
            {
                
                var newLine = FindBestLineFromCell(x, y, player, requiredLength);
                if (newLine != null)
                {
                    _bestPlayerLine[player] = newLine;
                    newLine.IsFullyBlocked = IsLineFullyBlocked(newLine, player);
                }
                return;
            }
            
            
            bool canExtend = CanExtendLine(currentBestLine, x, y);
            
            if (canExtend)
            {
                
                bool hasEnemyBetween = HasEnemyBetweenLineAndCell(currentBestLine, x, y, opponent);
                
                if (!hasEnemyBetween)
                {
                    currentBestLine.AddCell(x, y, cellWeight);
                    
                    
                    UpdateLineDirection(currentBestLine);
                    
                    
                    if (IsLineContinuous(currentBestLine))
                    {
                        currentBestLine.IsFullyBlocked = IsLineFullyBlocked(currentBestLine, player);
                    }
                    else
                    {
                        
                        var newLine = FindBestLineFromCell(x, y, player, requiredLength);
                        if (newLine != null && newLine.TotalWeight > currentBestLine.TotalWeight)
                        {
                            _bestPlayerLine[player] = newLine;
                            newLine.IsFullyBlocked = IsLineFullyBlocked(newLine, player);
                        }
                    }
                }
                else
                {
                    
                    var newLine = FindBestLineFromCell(x, y, player, requiredLength);
                    if (newLine != null)
                    {
                        double currentScore = currentBestLine.CalculateScore();
                        double potentialScore = newLine.CalculateScore();
                        
                        if (potentialScore > currentScore)
                        {
                            _bestPlayerLine[player] = newLine;
                            newLine.IsFullyBlocked = IsLineFullyBlocked(newLine, player);
                        }
                    }
                }
            }
            else
            {
                
                var potentialLine = FindPotentialLineFromCell(x, y, player, requiredLength);
                
                if (potentialLine != null)
                {
                    double currentScore = currentBestLine.CalculateScore();
                    double potentialScore = potentialLine.CalculateScore();
                    
                    if (potentialScore > currentScore)
                    {
                        _bestPlayerLine[player] = potentialLine;
                        potentialLine.IsFullyBlocked = IsLineFullyBlocked(potentialLine, player);
                    }
                }
            }
        }
        
        private bool HasEnemyBetweenLineAndCell(Line line, int x, int y, Player opponent)
        {
            if (line.Cells.Count == 0)
                return false;
                
            
            var nearestCell = line.Cells
                .OrderBy(c => Math.Abs(c.x - x) + Math.Abs(c.y - y))
                .First();
                
            int dx = Math.Sign(x - nearestCell.x);
            int dy = Math.Sign(y - nearestCell.y);
            
        
            int checkX = nearestCell.x + dx;
            int checkY = nearestCell.y + dy;
            
            while (checkX != x || checkY != y)
            {
                if (GetCell(checkX, checkY) == opponent)
                    return true;
                    
                checkX += dx;
                checkY += dy;
            }
            
            return false;
        }
        
        private void CheckAndResetBlockedLine(Player player)
        {
            var bestLine = _bestPlayerLine[player];
            if (bestLine == null || !bestLine.IsActive)
                return;
                
            bool isBlocked = IsLineFullyBlocked(bestLine, player);
            
            if (isBlocked && _gameRules.ResetScoreOnFullBlock)
            {
                bestLine.Reset();
                TryFindNewBestLineFromExistingCells(player);
            }
        }
        
        private void TryFindNewBestLineFromExistingCells(Player player)
        {
            int requiredLength = _gameRules.RequiredLineLength;
            
            var playerCells = _board
                .Where(kvp => kvp.Value == player)
                .Select(kvp => (kvp.Key.Item1, kvp.Key.Item2))
                .ToList();
                
            if (playerCells.Count == 0)
                return;
                
            Line bestNewLine = null;
            double bestScore = 0;
            
            foreach (var (startX, startY) in playerCells)
            {
                var line = FindBestLineFromCell(startX, startY, player, requiredLength);
                if (line != null)
                {
                    double score = line.TotalWeight;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestNewLine = line;
                    }
                }
            }
            
            if (bestNewLine != null)
            {
                _bestPlayerLine[player] = bestNewLine;
                bestNewLine.IsFullyBlocked = IsLineFullyBlocked(bestNewLine, player);
                
                if (bestNewLine.IsFullyBlocked && _gameRules.ResetScoreOnFullBlock)
                {
                    bestNewLine.Reset();
                }
            }
        }
        
        private Line FindBestLineFromCell(int x, int y, Player player, int requiredLength)
        {
            double cellWeight = _fieldGenerator.GetCellWeight(x, y);
            Player opponent = player == Player.X ? Player.O : Player.X;
            
            int[][] directions = new int[][]
            {
                new[] { 1, 0 },
                new[] { 0, 1 },
                new[] { 1, 1 },
                new[] { 1, -1 }
            };
            
            Line bestLine = null;
            double bestScore = 0;
            
            foreach (var dir in directions)
            {
               
                var line = CollectContinuousLine(x, y, dir[0], dir[1], player, opponent, requiredLength);
                
                if (line != null && line.Cells.Count >= 2)
                {
                    double score = line.TotalWeight;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestLine = line;
                    }
                }
            }
            
            return bestLine;
        }
        
        private Line CollectContinuousLine(int startX, int startY, int dx, int dy, Player player, Player opponent, int requiredLength)
        {
            var line = new Line(player, requiredLength, new[] { dx, dy });
            
           
            for (int i = 0; i < requiredLength; i++)
            {
                int checkX = startX + i * dx;
                int checkY = startY + i * dy;
                var cellPlayer = GetCell(checkX, checkY);
                
                if (cellPlayer == player)
                {
                    line.AddCell(checkX, checkY, _fieldGenerator.GetCellWeight(checkX, checkY));
                }
                else if (cellPlayer == opponent || cellPlayer == Player.None)
                {
                    
                    break;
                }
            }
            
            
            for (int i = 1; i < requiredLength; i++)
            {
                int checkX = startX - i * dx;
                int checkY = startY - i * dy;
                var cellPlayer = GetCell(checkX, checkY);
                
                if (cellPlayer == player)
                {
                    line.AddCell(checkX, checkY, _fieldGenerator.GetCellWeight(checkX, checkY));
                }
                else if (cellPlayer == opponent || cellPlayer == Player.None)
                {
                    
                    break;
                }
            }
            
           
            if (line.Cells.Count >= 2)
            {
                line.Cells = OrderCellsByDirection(line.Cells, dx, dy);
                
                
                if (!IsLineContinuous(line))
                {
                    return null;
                }
            }
            
            return line.Cells.Count >= 2 ? line : null;
        }
        
        private List<(int x, int y)> OrderCellsByDirection(List<(int x, int y)> cells, int dx, int dy)
        {
            if (dx != 0 && dy == 0) 
                return cells.OrderBy(c => c.x).ToList();
            else if (dx == 0 && dy != 0) 
                return cells.OrderBy(c => c.y).ToList();
            else if (dx != 0 && dy != 0) 
            {
                if (dx == dy) 
                    return cells.OrderBy(c => c.x).ThenBy(c => c.y).ToList();
                else 
                    return cells.OrderBy(c => c.x).ThenByDescending(c => c.y).ToList();
            }
            
            return cells;
        }
        
        private bool IsLineContinuous(Line line)
        {
            if (line.Cells.Count < 2)
                return true;
                
            var sortedCells = line.Cells;
            int dx = line.Direction[0];
            int dy = line.Direction[1];
            
            
            for (int i = 1; i < sortedCells.Count; i++)
            {
                var prev = sortedCells[i - 1];
                var curr = sortedCells[i];
                
                
                if (Math.Abs(curr.x - prev.x) != Math.Abs(dx) || 
                    Math.Abs(curr.y - prev.y) != Math.Abs(dy))
                {
                    return false;
                }
                
                
                if (dx != 0 && Math.Sign(curr.x - prev.x) != dx)
                    return false;
                if (dy != 0 && Math.Sign(curr.y - prev.y) != dy)
                    return false;
            }
            
            return true;
        }
        
        private Line FindPotentialLineFromCell(int x, int y, Player player, int requiredLength)
        {
            double cellWeight = _fieldGenerator.GetCellWeight(x, y);
            Player opponent = player == Player.X ? Player.O : Player.X;
            
            int[][] directions = new int[][]
            {
                new[] { 1, 0 },
                new[] { 0, 1 },
                new[] { 1, 1 },
                new[] { 1, -1 }
            };
            
            Line bestLine = null;
            double bestScore = 0;
            
            foreach (var dir in directions)
            {
                
                var line = new Line(player, requiredLength, new[] { dir[0], dir[1] });
                line.AddCell(x, y, cellWeight);
                
                
                for (int i = 1; i < requiredLength; i++)
                {
                    int nx = x + i * dir[0];
                    int ny = y + i * dir[1];
                    var cellPlayer = GetCell(nx, ny);
                    
                    if (cellPlayer == player)
                    {
                        line.AddCell(nx, ny, _fieldGenerator.GetCellWeight(nx, ny));
                    }
                    else if (cellPlayer == opponent)
                    {
                        break; 
                    }
                    
                }
                
                
                for (int i = 1; i < requiredLength; i++)
                {
                    int nx = x - i * dir[0];
                    int ny = y - i * dir[1];
                    var cellPlayer = GetCell(nx, ny);
                    
                    if (cellPlayer == player)
                    {
                        line.AddCell(nx, ny, _fieldGenerator.GetCellWeight(nx, ny));
                    }
                    else if (cellPlayer == opponent)
                    {
                        break; 
                    }
                }
                
                if (line.Cells.Count >= 2)
                {
                    
                    if (IsPotentialLineContinuous(line))
                    {
                        double score = line.CalculateScore();
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestLine = line;
                        }
                    }
                }
            }
            
            return bestLine;
        }
        
        private bool IsPotentialLineContinuous(Line line)
        {
            if (line.Cells.Count < 2)
                return true;
                
            var sortedCells = OrderCellsByDirection(line.Cells, line.Direction[0], line.Direction[1]);
            int dx = line.Direction[0];
            int dy = line.Direction[1];
            
            
            for (int i = 1; i < sortedCells.Count; i++)
            {
                var prev = sortedCells[i - 1];
                var curr = sortedCells[i];
                
                
                int steps = Math.Max(Math.Abs(curr.x - prev.x), Math.Abs(curr.y - prev.y));
                
                for (int step = 1; step < steps; step++)
                {
                    int checkX = prev.x + step * Math.Sign(curr.x - prev.x);
                    int checkY = prev.y + step * Math.Sign(curr.y - prev.y);
                    
                    Player opponent = line.Player == Player.X ? Player.O : Player.X;
                    if (GetCell(checkX, checkY) == opponent)
                    {
                        return false; 
                    }
                }
            }
            
            return true;
        }
        
        private bool CanExtendLine(Line line, int x, int y)
        {
            if (line.Cells.Count == 0)
                return false;
                
            if (line.Contains(x, y))
                return true;
                
            
            var sortedCells = OrderCellsByDirection(line.Cells, line.Direction[0], line.Direction[1]);
            var firstCell = sortedCells.First();
            var lastCell = sortedCells.Last();
            
            int dx = line.Direction[0];
            int dy = line.Direction[1];
            
            
            int frontX = lastCell.x + dx;
            int frontY = lastCell.y + dy;
            int backX = firstCell.x - dx;
            int backY = firstCell.y - dy;
            
            return (x == frontX && y == frontY) || (x == backX && y == backY);
        }
        
        public Player GetCell(int x, int y)
        {
            return _board.ContainsKey((x, y)) ? _board[(x, y)] : Player.None;
        }
        
        private bool IsLineFullyBlocked(Line line, Player player)
        {
            if (!_gameRules.ResetScoreOnFullBlock || line.Cells.Count < 2)
                return false;
            
            if (!line.IsActive)
                return true;
            
            var sortedCells = OrderCellsByDirection(line.Cells, line.Direction[0], line.Direction[1]);
            var firstCell = sortedCells.First();
            var lastCell = sortedCells.Last();
            
            int dx = line.Direction[0];
            int dy = line.Direction[1];
            
            Player opponent = player == Player.X ? Player.O : Player.X;
            
            bool frontBlocked = IsDirectionBlockedCorrectly(lastCell.x, lastCell.y, dx, dy, player, opponent);
            bool backBlocked = IsDirectionBlockedCorrectly(firstCell.x, firstCell.y, -dx, -dy, player, opponent);
            
            return frontBlocked && backBlocked;
        }
        
        private bool IsDirectionBlockedCorrectly(int startX, int startY, int dx, int dy, Player player, Player opponent)
        {
            for (int i = 1; i <= 2; i++)
            {
                int checkX = startX + i * dx;
                int checkY = startY + i * dy;
                var cell = GetCell(checkX, checkY);
                
                if (cell == Player.None)
                {
                    return false;
                }
                else if (cell == opponent)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        private bool IsScatteredLineBlocked(Line line, Player player)
        {
            Player opponent = player == Player.X ? Player.O : Player.X;
            
            foreach (var (x, y) in line.Cells)
            {
                int[][] directions = new int[][]
                {
                    new[] { 1, 0 },
                    new[] { 0, 1 },
                    new[] { 1, 1 },
                    new[] { 1, -1 }
                };
                
                foreach (var dir in directions)
                {
                    bool canExtend = false;
                    
                    for (int i = -3; i <= 3; i++)
                    {
                        if (i == 0) continue;
                        
                        int checkX = x + i * dir[0];
                        int checkY = y + i * dir[1];
                        var cell = GetCell(checkX, checkY);
                        
                        if (cell == Player.None)
                        {
                            canExtend = true;
                            break;
                        }
                    }
                    
                    if (canExtend)
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        public IEnumerable<TicTacToeMove> GetPossibleMoves()
        {
            IEnumerable<(int x, int y)> positions;
            
            if (MoveCount < 8)
            {
                positions = _fieldGenerator.GenerateAdjacentPositions();
            }
            else if (MoveCount < 20)
            {
                positions = _fieldGenerator.GenerateFreePositions(2);
            }
            else
            {
                positions = _fieldGenerator.GenerateFreePositions(3);
            }
            
            return positions
                .Where(pos => !_fieldGenerator.IsPositionOccupied(pos.x, pos.y))
                .Select(pos => new TicTacToeMove(pos.x, pos.y, CurrentPlayer))
                .OrderByDescending(move => EvaluateMovePriority(move));
        }
        
        private double EvaluateMovePriority(TicTacToeMove move)
        {
            double priority = 0;
            
            double cellWeight = _fieldGenerator.GetCellWeight(move.X, move.Y);
            priority += cellWeight * 20;
            
            return priority;
        }
        
        public IEnumerable<(TicTacToeMove move, double weight)> GetWeightedPossibleMoves()
        {
            var possibleMoves = GetPossibleMoves();
            
            foreach (var move in possibleMoves)
            {
                double baseWeight = _fieldGenerator.GetCellWeight(move.X, move.Y);
                yield return (move, baseWeight);
            }
        }
        
        public void UpdateWeightsAfterMove(int x, int y, Player player)
        {
            _fieldGenerator.IncreaseCellWeight(x, y, 3.0);
        }
        
        private bool HasConsecutiveLine(Player player, int requiredLength)
        {
            int searchRadius = Math.Max(10, requiredLength + 5);
            
            for (int x = -searchRadius; x <= searchRadius; x++)
            {
                for (int y = -searchRadius; y <= searchRadius; y++)
                {
                    if (GetCell(x, y) == player)
                    {
                        if (CheckConsecutiveInDirection(x, y, 1, 0, player, requiredLength) ||
                            CheckConsecutiveInDirection(x, y, 0, 1, player, requiredLength) ||
                            CheckConsecutiveInDirection(x, y, 1, 1, player, requiredLength) ||
                            CheckConsecutiveInDirection(x, y, 1, -1, player, requiredLength))
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }

        private bool CheckConsecutiveInDirection(int startX, int startY, int dx, int dy, Player player, int requiredLength)
        {
            for (int i = 0; i < requiredLength; i++)
            {
                if (GetCell(startX + i * dx, startY + i * dy) != player)
                    return false;
            }
            return true;
        }
        
        public GameResult CheckWinner()
        {
            int requiredLength = _gameRules.RequiredLineLength;
            
            bool xHasLine = HasConsecutiveLine(Player.X, requiredLength);
            bool oHasLine = HasConsecutiveLine(Player.O, requiredLength);
            
            if (!xHasLine && !oHasLine)
            {
                if (MoveCount >= 50 && !_fieldGenerator.GenerateFreePositions(1).Any())
                {
                    return GameResult.Draw;
                }
                return GameResult.None;
            }
            
            double xScore = CalculatePlayerScore(Player.X);
            double oScore = CalculatePlayerScore(Player.O);
            
            if (xScore > oScore)
                return GameResult.XWins;
            else if (oScore > xScore)
                return GameResult.OWins;
            else
                return GameResult.Draw;
        }
        
        public double CalculatePlayerScore(Player player)
        {
            var bestLine = _bestPlayerLine[player];
            
            if (bestLine == null)
                return 0;
                
            if (!bestLine.IsActive || bestLine.IsFullyBlocked)
                return 0;
                
            return bestLine.TotalWeight;
        }
        
        public Line GetBestLine(Player player)
        {
            return _bestPlayerLine[player];
        }
        
        public bool IsPlayerCloseToWin(Player player, out Line closestLine)
        {
            closestLine = _bestPlayerLine[player];
            
            if (closestLine == null || !closestLine.IsActive || closestLine.IsFullyBlocked)
                return false;
                
            int cellsNeeded = _gameRules.RequiredLineLength - closestLine.Cells.Count;
            return cellsNeeded <= 2 && cellsNeeded > 0;
        }
        
        public IEnumerable<Line> GetPlayerLines(Player player)
        {
            var line = _bestPlayerLine[player];
            if (line != null)
            {
                yield return line;
            }
        }
        
        public List<Line> GetAllPlayerLines(Player player, int minLength = 3)
        {
            var lines = new List<Line>();
            
            var playerCells = _board
                .Where(kvp => kvp.Value == player)
                .Select(kvp => kvp.Key)
                .ToList();
            
            if (playerCells.Count < minLength)
                return lines;
            
            int[][] directions = new int[][]
            {
                new[] { 1, 0 },
                new[] { 0, 1 },
                new[] { 1, 1 },
                new[] { 1, -1 }
            };
            
            var lineHashes = new HashSet<string>();
            
            foreach (var (x, y) in playerCells)
            {
                foreach (var dir in directions)
                {
                    var line = CheckLineInDirection(x, y, dir[0], dir[1], player);
                    
                    if (line != null && line.Cells.Count >= minLength)
                    {
                        var sortedCells = line.Cells.OrderBy(c => c.x).ThenBy(c => c.y).ToList();
                        var lineHash = string.Join(";", sortedCells.Select(c => $"{c.x},{c.y}"));
                        
                        if (!lineHashes.Contains(lineHash))
                        {
                            lines.Add(line);
                            lineHashes.Add(lineHash);
                        }
                    }
                }
            }
            
            return lines;
        }
        
        private Line CheckLineInDirection(int startX, int startY, int dx, int dy, Player player)
        {
            var line = new Line(player, _gameRules.RequiredLineLength, new[] { dx, dy });
            Player opponent = player == Player.X ? Player.O : Player.X;
            
            
            int backwardCount = 0;
            for (int i = 1; i <= 10; i++)
            {
                int checkX = startX - i * dx;
                int checkY = startY - i * dy;
                var cell = GetCell(checkX, checkY);
                
                if (cell == player)
                {
                    backwardCount++;
                }
                else if (cell == opponent || cell == Player.None)
                {
                    break; 
                }
            }
            
            int realStartX = startX - backwardCount * dx;
            int realStartY = startY - backwardCount * dy;
            
            
            for (int i = 0; i < 10; i++)
            {
                int checkX = realStartX + i * dx;
                int checkY = realStartY + i * dy;
                var cell = GetCell(checkX, checkY);
                
                if (cell == player)
                {
                    double weight = _fieldGenerator.GetCellWeight(checkX, checkY);
                    line.AddCell(checkX, checkY, weight);
                }
                else if (cell == opponent || cell == Player.None)
                {
                    break; 
                }
            }
            
            if (line.Cells.Count >= 2)
            {
                line.IsFullyBlocked = IsLineFullyBlocked(line, player);
            }
            
            return line;
        }
        
        public string GetAllPlayerLinesInfo()
        {
            var builder = new StringBuilder();
            
            builder.AppendLine("=== ВСЕ ЛИНИИ ИЗ 3+ КЛЕТОК ===");
            builder.AppendLine();
            
            foreach (var player in new[] { Player.X, Player.O })
            {
                builder.AppendLine($"Игрок {player}:");
                var allLines = GetAllPlayerLines(player, 3);
                
                if (allLines.Count == 0)
                {
                    builder.AppendLine("  Нет линий из 3+ клеток");
                    builder.AppendLine();
                    continue;
                }
                
                allLines = allLines
                    .OrderByDescending(l => l.Cells.Count)
                    .ThenByDescending(l => l.TotalWeight)
                    .ToList();
                
                for (int i = 0; i < allLines.Count; i++)
                {
                    var line = allLines[i];
                    builder.AppendLine($"  Линия #{i + 1}:");
                    builder.AppendLine($"    Клеток: {line.Cells.Count}");
                    builder.AppendLine($"    Вес линии: {line.TotalWeight:F1}");
                    builder.AppendLine($"    Заблокирована: {(line.IsFullyBlocked ? "ДА" : "НЕТ")}");
                    
                    var cellsStr = string.Join(" → ", line.Cells.Select(c => $"({c.x},{c.y})"));
                    builder.AppendLine($"    Клетки: {cellsStr}");
                    
                    if (line.Cells.Count >= 2)
                    {
                        var first = line.Cells[0];
                        var second = line.Cells[1];
                        string direction = GetLineDirection(first, second);
                        builder.AppendLine($"    Направление: {direction}");
                    }
                    
                    builder.AppendLine();
                }
                
                builder.AppendLine($"Всего линий: {allLines.Count}");
                builder.AppendLine();
            }
            
            builder.AppendLine("=== ЛУЧШИЕ ЛИНИИ (по системе подсчета очков) ===");
            foreach (var player in new[] { Player.X, Player.O })
            {
                var bestLine = _bestPlayerLine[player];
                builder.AppendLine($"Игрок {player}:");
                if (bestLine != null)
                {
                    builder.AppendLine($"  Клеток: {bestLine.Cells.Count}/{bestLine.RequiredLength}");
                    builder.AppendLine($"  Общий вес: {bestLine.TotalWeight:F1}");
                    builder.AppendLine($"  Активна: {bestLine.IsActive}");
                    builder.AppendLine($"  Заблокирована: {bestLine.IsFullyBlocked}");
                    builder.AppendLine($"  Очки: {CalculatePlayerScore(player):F1}");
                    
                    var cellsStr = string.Join(" → ", bestLine.Cells.Select(c => $"({c.x},{c.y})"));
                    builder.AppendLine($"  Клетки: {cellsStr}");
                }
                else
                {
                    builder.AppendLine("  Нет лучшей линии");
                }
                builder.AppendLine();
            }
            
            return builder.ToString();
        }
        
        private string GetLineDirection((int x, int y) first, (int x, int y) second)
        {
            int dx = second.x - first.x;
            int dy = second.y - first.y;
            
            if (dx == 0) return "Вертикаль";
            if (dy == 0) return "Горизонталь";
            if (dx == dy) return "Диагональ ↘";
            if (dx == -dy) return "Диагональ ↗";
            
            return "Разные направления";
        }
        
        public IEnumerable<(int x, int y, Player player)> GetGeneratedField(int radius = 3)
        {
            var positions = _fieldGenerator.GenerateFieldAround((0, 0), radius);
            
            foreach (var (x, y) in positions)
            {
                yield return (x, y, GetCell(x, y));
            }
        }
        
        public (int occupied, int generated, int radius) GetGenerationStats()
        {
            return (
                occupied: _fieldGenerator.GetOccupiedCount(),
                generated: _fieldGenerator.GenerateFreePositions(2).Count(),
                radius: _fieldGenerator.GetCurrentRadius()
            );
        }
        
        public TicTacToeBoard Clone()
        {
            return new TicTacToeBoard(this);
        }
        
        public string GetBoardStateString()
        {
            var builder = new StringBuilder();
            foreach (var ((x, y), player) in _board.OrderBy(c => c.Key.Item1).ThenBy(c => c.Key.Item2))
            {
                builder.Append($"{x},{y}:{player};");
            }
            builder.Append($"Current:{CurrentPlayer}");
            return builder.ToString();
        }
        
        public bool Equals(TicTacToeBoard other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            
            if (CurrentPlayer != other.CurrentPlayer) return false;
            if (MoveCount != other.MoveCount) return false;
            
            if (_board.Count != other._board.Count) return false;
            
            foreach (var kvp in _board)
            {
                if (!other._board.TryGetValue(kvp.Key, out var otherPlayer) || 
                    otherPlayer != kvp.Value)
                    return false;
            }
            
            return true;
        }
        
        public override bool Equals(object obj) => Equals(obj as TicTacToeBoard);
        
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(CurrentPlayer);
            hash.Add(MoveCount);
            
            foreach (var kvp in _board.OrderBy(x => x.Key))
            {
                hash.Add(kvp.Key);
                hash.Add(kvp.Value);
            }
            
            return hash.ToHashCode();
        }
        
        public string GetBoardVisualization(int radius = 3)
        {
            var builder = new StringBuilder();
            
            for (int y = radius; y >= -radius; y--)
            {
                builder.Append($"{y,3}: ");
                for (int x = -radius; x <= radius; x++)
                {
                    var player = GetCell(x, y);
                    char symbol = player switch
                    {
                        Player.X => 'X',
                        Player.O => 'O',
                        _ => _fieldGenerator.IsPositionOccupied(x, y) ? '*' : '.'
                    };
                    builder.Append($"{symbol} ");
                }
                builder.AppendLine();
            }
            
            builder.Append("    ");
            for (int x = -radius; x <= radius; x++)
            {
                builder.Append($"{x,2}");
            }
            
            return builder.ToString();
        }
        
        public string GetBoardVisualizationWithWeights(int radius = 3)
        {
            var builder = new StringBuilder();
            
            builder.AppendLine("Доска с весами клеток:");
            builder.AppendLine("(X/O - фигуры, числа - вес клетки)");
            builder.AppendLine();
            
            for (int y = radius; y >= -radius; y--)
            {
                builder.Append($"{y,3}: ");
                for (int x = -radius; x <= radius; x++)
                {
                    var player = GetCell(x, y);
                    if (player != Player.None)
                    {
                        char symbol = player == Player.X ? 'X' : 'O';
                        builder.Append($"{symbol,4}");
                    }
                    else
                    {
                        double weight = _fieldGenerator.GetCellWeight(x, y);
                        builder.Append($"{weight:F1} ");
                    }
                }
                builder.AppendLine();
            }
            
            builder.Append("    ");
            for (int x = -radius; x <= radius; x++)
            {
                builder.Append($"{x,4}");
            }
            
            return builder.ToString();
        }
        
        public string GetBoardVisualizationWithLines(int radius = 3)
        {
            var builder = new StringBuilder();
            
            builder.AppendLine("Доска с линиями игроков:");
            builder.AppendLine();
            
            double xScore = CalculatePlayerScore(Player.X);
            double oScore = CalculatePlayerScore(Player.O);
            builder.AppendLine($"Очки: ✕ {xScore:F1} | ○ {oScore:F1}");
            builder.AppendLine();
            
            builder.AppendLine("Лучшая линия крестиков (✕):");
            var xLine = _bestPlayerLine[Player.X];
            if (xLine != null)
            {
                builder.AppendLine($"  Клеток: {xLine.Cells.Count}/{xLine.RequiredLength}");
                builder.AppendLine($"  Сумма весов: {xLine.TotalWeight:F1}");
                builder.AppendLine($"  Заблокирована: {(xLine.IsFullyBlocked ? "ДА" : "НЕТ")}");
                builder.AppendLine($"  Активна: {(xLine.IsActive ? "ДА" : "НЕТ")}");
                builder.AppendLine($"  Очки: {xLine.CalculateScore():F1}");
            }
            else
            {
                builder.AppendLine("  Нет линии");
            }
            
            builder.AppendLine();
            builder.AppendLine("Лучшая линия ноликов (○):");
            var oLine = _bestPlayerLine[Player.O];
            if (oLine != null)
            {
                builder.AppendLine($"  Клеток: {oLine.Cells.Count}/{oLine.RequiredLength}");
                builder.AppendLine($"  Сумма весов: {oLine.TotalWeight:F1}");
                builder.AppendLine($"  Заблокирована: {(oLine.IsFullyBlocked ? "ДА" : "НЕТ")}");
                builder.AppendLine($"  Активна: {(oLine.IsActive ? "ДА" : "НЕТ")}");
                builder.AppendLine($"  Очки: {oLine.CalculateScore():F1}");
            }
            else
            {
                builder.AppendLine("  Нет линии");
            }
            
            return builder.ToString();
        }
        
        public string GetBoardVisualizationWithNewRules(int radius = 3)
        {
            var builder = new StringBuilder();
            
            builder.AppendLine("НОВЫЕ ПРАВИЛА:");
            builder.AppendLine($"• Очки ТОЛЬКО за одну лучшую линию");
            builder.AppendLine($"• Клетки вне лучшей линии не дают очков");
            builder.AppendLine($"• При блокировке линия обнуляется и больше не считается");
            builder.AppendLine($"• Нужно строить новую линию");
            builder.AppendLine();
            
            double xScore = CalculatePlayerScore(Player.X);
            double oScore = CalculatePlayerScore(Player.O);
            
            var xBestLine = _bestPlayerLine[Player.X];
            var oBestLine = _bestPlayerLine[Player.O];
            
            builder.AppendLine($"Очки по новым правилам:");
            builder.AppendLine($"  ✕: {xScore:F1} | ○: {oScore:F1}");
            builder.AppendLine();
            
            builder.AppendLine("Лучшая линия крестиков (✕):");
            if (xBestLine != null)
            {
                builder.AppendLine($"  Клеток: {xBestLine.Cells.Count}/{xBestLine.RequiredLength}");
                builder.AppendLine($"  Сумма весов: {xBestLine.TotalWeight:F1}");
                builder.AppendLine($"  Заблокирована: {(xBestLine.IsFullyBlocked ? "ДА" : "НЕТ")}");
                builder.AppendLine($"  Активна: {(xBestLine.IsActive ? "ДА" : "НЕТ")}");
                
                if (!xBestLine.IsActive || xBestLine.IsFullyBlocked)
                    builder.AppendLine($"  [СЧЕТ = 0]");
            }
            else
            {
                builder.AppendLine("  Нет линии [СЧЕТ = 0]");
            }
            
            builder.AppendLine();
            builder.AppendLine("Лучшая линия ноликов (○):");
            if (oBestLine != null)
            {
                builder.AppendLine($"  Клеток: {oBestLine.Cells.Count}/{oBestLine.RequiredLength}");
                builder.AppendLine($"  Сумма весов: {oBestLine.TotalWeight:F1}");
                builder.AppendLine($"  Заблокирована: {(oBestLine.IsFullyBlocked ? "ДА" : "НЕТ")}");
                builder.AppendLine($"  Активна: {(oBestLine.IsActive ? "ДА" : "НЕТ")}");
                
                if (!oBestLine.IsActive || oBestLine.IsFullyBlocked)
                    builder.AppendLine($"  [СЧЕТ = 0]");
            }
            else
            {
                builder.AppendLine("  Нет линии [СЧЕТ = 0]");
            }
            
            return builder.ToString();
        }
        
        public string GetDebugInfo()
        {
            var builder = new StringBuilder();
            
            foreach (var player in new[] { Player.X, Player.O })
            {
                var line = _bestPlayerLine[player];
                builder.AppendLine($"{player}:");
                
                if (line != null)
                {
                    builder.AppendLine($"  Cells: {string.Join(", ", line.Cells)}");
                    builder.AppendLine($"  TotalWeight: {line.TotalWeight:F1}");
                    builder.AppendLine($"  IsActive: {line.IsActive}");
                    builder.AppendLine($"  IsFullyBlocked: {line.IsFullyBlocked}");
                    builder.AppendLine($"  IsBlockedCheck: {IsLineFullyBlocked(line, player)}");
                    builder.AppendLine($"  Score: {CalculatePlayerScore(player):F1}");
                }
                else
                {
                    builder.AppendLine($"  No line");
                }
                builder.AppendLine();
            }
            
            return builder.ToString();
        }
        
        public bool WouldMoveBeOnBorder(int x, int y, int radius)
        {
            return _fieldGenerator.IsOnBorder(x, y, radius);
        }

        public IEnumerable<TicTacToeMove> GetBorderMoves(int radius)
        {
            var borderCells = _fieldGenerator.GetBorderCells(radius);
            
            return borderCells
                .Where(pos => !_fieldGenerator.IsPositionOccupied(pos.x, pos.y))
                .Select(pos => new TicTacToeMove(pos.x, pos.y, CurrentPlayer))
                .OrderByDescending(move => _fieldGenerator.GetCellWeight(move.X, move.Y));
        }

        public string GetBoardVisualizationWithBorders(int radius = 3)
        {
            var builder = new StringBuilder();
            
            builder.AppendLine("Доска с выделением границ:");
            
            for (int y = radius; y >= -radius; y--)
            {
                builder.Append($"{y,3}: ");
                for (int x = -radius; x <= radius; x++)
                {
                    var player = GetCell(x, y);
                    char symbol = player switch
                    {
                        Player.X => 'X',
                        Player.O => 'O',
                        _ => _fieldGenerator.IsPositionOccupied(x, y) ? '*' : '.'
                    };
                    
                    if (_fieldGenerator.IsOnBorder(x, y, radius))
                    {
                        builder.Append($"[{symbol}]");
                    }
                    else
                    {
                        builder.Append($" {symbol} ");
                    }
                }
                builder.AppendLine();
            }
            
            builder.Append("    ");
            for (int x = -radius; x <= radius; x++)
            {
                builder.Append($"{x,3}");
            }
            
            return builder.ToString();
        }
        
        public (double minWeight, double maxWeight, double avgWeight) GetWeightStatistics(int radius = 3)
        {
            double minWeight = double.MaxValue;
            double maxWeight = double.MinValue;
            double totalWeight = 0;
            int count = 0;
            
            var positions = _fieldGenerator.GenerateFieldAround((0, 0), radius);
            
            foreach (var (x, y) in positions)
            {
                double weight = _fieldGenerator.GetCellWeight(x, y);
                minWeight = Math.Min(minWeight, weight);
                maxWeight = Math.Max(maxWeight, weight);
                totalWeight += weight;
                count++;
            }
            
            double avgWeight = count > 0 ? totalWeight / count : 0;
            
            return (minWeight, maxWeight, avgWeight);
        }
        
        public void ResetWeights()
        {
            _fieldGenerator.ResetWeights();
        }
        
        public IEnumerable<(int x, int y, double weight)> GetTopStrategicCells(int count = 10, int radius = 3)
        {
            var positions = _fieldGenerator.GenerateFieldAround((0, 0), radius)
                .Where(pos => !_fieldGenerator.IsPositionOccupied(pos.x, pos.y))
                .Select(pos => (pos.x, pos.y, weight: _fieldGenerator.GetCellWeight(pos.x, pos.y)))
                .OrderByDescending(cell => cell.weight)
                .Take(count);
            
            return positions;
        }
        
        public int GetFieldSize() => _gameRules.FieldSize;
        
        public int GetRequiredLineLength() => _gameRules.RequiredLineLength;
        
        public void SetFieldSize(int size)
        {
            _gameRules.FieldSize = size;
            
            int newRequiredLength = _gameRules.RequiredLineLength;
            
            if (_bestPlayerLine[Player.X] != null)
                _bestPlayerLine[Player.X].RequiredLength = newRequiredLength;
            
            if (_bestPlayerLine[Player.O] != null)
                _bestPlayerLine[Player.O].RequiredLength = newRequiredLength;
        }
        
        public (int x, int y, Player player) GetCenterCell()
        {
            return (0, 0, GetCell(0, 0));
        }
        
        public IEnumerable<(int x, int y)> GetEmptyCellsInRadius(int radius = 3)
        {
            var positions = _fieldGenerator.GenerateFieldAround((0, 0), radius);
            
            foreach (var (x, y) in positions)
            {
                if (GetCell(x, y) == Player.None)
                {
                    yield return (x, y);
                }
            }
        }
        
        public IEnumerable<(int x, int y)> GetOccupiedCells()
        {
            return _board.Keys.Select(k => (k.Item1, k.Item2));
        }
        
        public int GetPlayerCellCount(Player player)
        {
            return _board.Values.Count(v => v == player);
        }
        
        public double GetAverageCellWeight()
        {
            var stats = GetWeightStatistics(5);
            return stats.avgWeight;
        }
        
        public string GetRulesSummary()
        {
            return $"Правила: Размер={GetFieldSize()}, " +
                   $"Длина линии={GetRequiredLineLength()}, " +
                   $"Только одна линия={_gameRules.UseSingleLineScoring}, " +
                   $"Обнуление при блокировке={_gameRules.ResetScoreOnFullBlock}";
        }
        
        public bool IsCellInBestLine(int x, int y, Player player)
        {
            var bestLine = _bestPlayerLine[player];
            if (bestLine == null)
                return false;
                
            return bestLine.Contains(x, y);
        }
        
        public double GetLineCompletionPercentage(Player player)
        {
            var bestLine = _bestPlayerLine[player];
            if (bestLine == null || bestLine.RequiredLength == 0)
                return 0;
                
            return (double)bestLine.Cells.Count / bestLine.RequiredLength;
        }
        
        public void ClearBoard()
        {
            _board.Clear();
            _fieldGenerator.ResetWeights();
            _fieldGenerator = new InfiniteFieldGenerator();
            _bestPlayerLine[Player.X] = null;
            _bestPlayerLine[Player.O] = null;
            CurrentPlayer = Player.X;
        }
        
        public bool IsLineBlocked(Player player)
        {
            var line = _bestPlayerLine[player];
            if (line == null)
                return false;
                
            return line.IsFullyBlocked;
        }
        
        public bool HasActiveLine(Player player)
        {
            var line = _bestPlayerLine[player];
            if (line == null)
                return false;
                
            return line.IsActive && !line.IsFullyBlocked;
        }
        
        public int GetBestLineLength(Player player)
        {
            var line = _bestPlayerLine[player];
            if (line == null)
                return 0;
                
            return line.Cells.Count;
        }
        
        public IEnumerable<(int x, int y)> GetBestLineCells(Player player)
        {
            var line = _bestPlayerLine[player];
            if (line == null)
                return Enumerable.Empty<(int, int)>();
                
            return line.Cells;
        }
    }
}