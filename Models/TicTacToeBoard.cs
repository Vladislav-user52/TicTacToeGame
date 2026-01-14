using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TicTacToeGame.Models
{
    public class TicTacToeBoard : IEquatable<TicTacToeBoard>
    {
        private readonly Dictionary<(int, int), Player> _board;
        private readonly InfiniteFieldGenerator _fieldGenerator;
        private readonly GameRules _gameRules;
        private readonly Dictionary<Player, List<Line>> _playerLines;
        
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
            
            public Line(Player player, int requiredLength)
            {
                Player = player;
                Cells = new List<(int x, int y)>();
                RequiredLength = requiredLength;
            }
            
            public void AddCell(int x, int y, double weight)
            {
                Cells.Add((x, y));
                TotalWeight += weight;
            }
            
            public bool Contains(int x, int y) => Cells.Any(c => c.x == x && c.y == y);
            
            public double CalculateScore()
            {
                double score = TotalWeight;
                
                // Бонус за полную линию
                if (IsComplete)
                    score *= 2.0;
                
                // Бонус за центральные клетки
                int centerCount = Cells.Count(c => Math.Abs(c.x) <= 1 && Math.Abs(c.y) <= 1);
                score += centerCount * 5.0;
                
                return score;
            }
            
            public override string ToString()
            {
                return $"{Player} Line: {Cells.Count}/{RequiredLength} cells, Score: {CalculateScore():F1}";
            }
        }
        
        public TicTacToeBoard(GameRules gameRules = null)
        {
            _board = new Dictionary<(int, int), Player>();
            _fieldGenerator = new InfiniteFieldGenerator();
            _gameRules = gameRules ?? new GameRules();
            
            
            
            
            _playerLines = new Dictionary<Player, List<Line>>
            {
                [Player.X] = new List<Line>(),
                [Player.O] = new List<Line>()
            };
            CurrentPlayer = Player.X; 
        }
        
        private TicTacToeBoard(TicTacToeBoard other)
        {
            _board = new Dictionary<(int, int), Player>(other._board);
            _fieldGenerator = new InfiniteFieldGenerator(other._board.Keys);
            _gameRules = other._gameRules;
            _playerLines = new Dictionary<Player, List<Line>>
            {
                [Player.X] = other._playerLines[Player.X].Select(l => new Line(l.Player, l.RequiredLength)
                {
                    Cells = new List<(int, int)>(l.Cells),
                    TotalWeight = l.TotalWeight
                }).ToList(),
                [Player.O] = other._playerLines[Player.O].Select(l => new Line(l.Player, l.RequiredLength)
                {
                    Cells = new List<(int, int)>(l.Cells),
                    TotalWeight = l.TotalWeight
                }).ToList()
            };
            CurrentPlayer = other.CurrentPlayer;
        }
        
        public bool MakeMove(int x, int y)
        {
            if (_board.ContainsKey((x, y)))
                return false;
                
            _board[(x, y)] = CurrentPlayer;
            _fieldGenerator.MarkPositionOccupied(x, y);
            
            // Обновляем веса после хода
            UpdateWeightsAfterMove(x, y, CurrentPlayer);
            
            // Обновляем линии игрока
            UpdatePlayerLines(x, y, CurrentPlayer);
            
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
                _board.Remove((x, y));
                _fieldGenerator.MarkPositionFree(x, y);
                
                // Уменьшаем вес клетки при отмене хода
                _fieldGenerator.DecreaseCellWeight(x, y, 1.0);
                
                // Удаляем клетку из линий
                RemoveCellFromLines(x, y);
                
                CurrentPlayer = CurrentPlayer == Player.X ? Player.O : Player.X;
            }
        }
        
        private void RemoveCellFromLines(int x, int y)
        {
            foreach (var player in new[] { Player.X, Player.O })
            {
                foreach (var line in _playerLines[player].ToList())
                {
                    if (line.Contains(x, y))
                    {
                        // Удаляем клетку из линии
                        var cellToRemove = line.Cells.FirstOrDefault(c => c.x == x && c.y == y);
                        if (cellToRemove != default)
                        {
                            line.Cells.Remove(cellToRemove);
                            line.TotalWeight -= _fieldGenerator.GetCellWeight(x, y);
                            
                            // Если линия стала пустой или слишком короткой, удаляем её
                            if (line.Cells.Count < 2)
                            {
                                _playerLines[player].Remove(line);
                            }
                        }
                    }
                }
            }
        }
        
        public Player GetCell(int x, int y)
        {
            return _board.ContainsKey((x, y)) ? _board[(x, y)] : Player.None;
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
            
            // 1. Вес клетки (самый важный фактор)
            double cellWeight = _fieldGenerator.GetCellWeight(move.X, move.Y);
            priority += cellWeight * 20;
            
            // 2. Расстояние от центра
            double distanceFromCenter = Math.Sqrt(move.X * move.X + move.Y * move.Y);
            priority += 15.0 / (1.0 + distanceFromCenter);
            
            // 3. Соседние фигуры
            (int friendly, int opponent) adjacentCounts = CountAdjacentPieces(move.X, move.Y);
            priority += adjacentCounts.friendly * 8;  // Свои фигуры рядом - хорошо
            priority -= adjacentCounts.opponent * 4; // Фигуры противника рядом - осторожно
            
            // 4. Потенциал линий (новая логика)
            priority += EvaluateLinePotentialForNewRules(move.X, move.Y, CurrentPlayer) * 3;
            
            // 5. Стратегические позиции
            priority += EvaluateStrategicPosition(move.X, move.Y);
            
            // 6. Блокирующие ходы
            priority += EvaluateBlockingPotential(move.X, move.Y, CurrentPlayer);
            
            return priority;
        }
        
        private double EvaluateLinePotentialForNewRules(int x, int y, Player player)
        {
            double potential = 0;
            int requiredLength = _gameRules.RequiredLineLength;
            
            int[][] directions = new int[][]
            {
                new[] { 1, 0 },
                new[] { 0, 1 },
                new[] { 1, 1 },
                new[] { 1, -1 }
            };
            
            foreach (var dir in directions)
            {
                int playerCount = 1; // Сама клетка
                int emptyCount = 0;
                
                // Проверяем в обе стороны
                for (int i = 1; i < requiredLength; i++)
                {
                    // Вперед
                    var cellForward = GetCell(x + i * dir[0], y + i * dir[1]);
                    if (cellForward == player) 
                        playerCount++;
                    else if (cellForward == Player.None) 
                        emptyCount++;
                    
                    // Назад
                    var cellBackward = GetCell(x - i * dir[0], y - i * dir[1]);
                    if (cellBackward == player) 
                        playerCount++;
                    else if (cellBackward == Player.None) 
                        emptyCount++;
                }
                
                // Оценка потенциала линии
                if (playerCount >= requiredLength) 
                    potential += 100;
                else if (playerCount == requiredLength - 1 && emptyCount > 0) 
                    potential += 60;
                else if (playerCount == requiredLength - 2 && emptyCount >= 2) 
                    potential += 35;
                else if (playerCount == requiredLength - 3 && emptyCount >= 3) 
                    potential += 20;
                else if (playerCount >= 2) 
                    potential += 10;
            }
            
            return potential;
        }
        
        private (int friendly, int opponent) CountAdjacentPieces(int x, int y)
        {
            int friendlyCount = 0;
            int opponentCount = 0;
            
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    var neighbor = GetCell(x + dx, y + dy);
                    if (neighbor == CurrentPlayer)
                        friendlyCount++;
                    else if (neighbor != Player.None)
                        opponentCount++;
                }
            }
            return (friendlyCount, opponentCount);
        }
        
        private double EvaluateLinePotential(int x, int y, Player player)
        {
            double potential = 0;
            
            int[][] directions = new int[][]
            {
                new[] { 1, 0 },
                new[] { 0, 1 },
                new[] { 1, 1 },
                new[] { 1, -1 }
            };
            
            foreach (var dir in directions)
            {
                potential += EvaluateDirectionPotential(x, y, dir[0], dir[1], player);
            }
            
            return potential;
        }
        
        private double EvaluateDirectionPotential(int x, int y, int dx, int dy, Player player)
        {
            int playerCount = 1; // Сама клетка
            int emptyCount = 0;
            Player opponent = player == Player.X ? Player.O : Player.X;
            
            // Проверяем в обе стороны
            for (int i = 1; i < 5; i++)
            {
                // Вперед
                var cellForward = GetCell(x + i * dx, y + i * dy);
                if (cellForward == player) 
                    playerCount++;
                else if (cellForward == Player.None) 
                    emptyCount++;
                
                // Назад
                var cellBackward = GetCell(x - i * dx, y - i * dy);
                if (cellBackward == player) 
                    playerCount++;
                else if (cellBackward == Player.None) 
                    emptyCount++;
            }
            
            // Оценка потенциала линии
            if (playerCount >= 5) return 100;
            if (playerCount == 4 && emptyCount > 0) return 50;
            if (playerCount == 3 && emptyCount >= 2) return 30;
            if (playerCount == 2 && emptyCount >= 3) return 15;
            
            return 0;
        }
        
        private double EvaluateStrategicPosition(int x, int y)
        {
            double strategicValue = 0;
            
            // Центр доски имеет высшую стратегическую ценность
            if (x == 0 && y == 0) 
                strategicValue += 25;
            
            // Углы также важны
            bool isCorner = (x == -3 || x == 3) && (y == -3 || y == 3);
            if (isCorner) 
                strategicValue += 15;
            
            // Клетки рядом с центром
            bool nearCenter = Math.Abs(x) <= 1 && Math.Abs(y) <= 1;
            if (nearCenter) 
                strategicValue += 10;
            
            return strategicValue;
        }
        
        private double EvaluateBlockingPotential(int x, int y, Player player)
        {
            Player opponent = player == Player.X ? Player.O : Player.X;
            double blockingValue = 0;
            int requiredLength = _gameRules.RequiredLineLength;
            
            int[][] directions = new int[][]
            {
                new[] { 1, 0 },
                new[] { 0, 1 },
                new[] { 1, 1 },
                new[] { 1, -1 }
            };
            
            foreach (var dir in directions)
            {
                // Проверяем, блокирует ли этот ход потенциальную линию противника
                int opponentCount = 0;
                int emptyCount = 0;
                
                for (int i = -(requiredLength - 1); i <= (requiredLength - 1); i++)
                {
                    if (i == 0) continue; // Пропускаем саму клетку
                    
                    int checkX = x + i * dir[0];
                    int checkY = y + i * dir[1];
                    var cell = GetCell(checkX, checkY);
                    
                    if (cell == opponent) 
                        opponentCount++;
                    else if (cell == Player.None) 
                        emptyCount++;
                }
                
                // Если противник имеет почти полную линию, блокирующий ход ценен
                if (opponentCount >= requiredLength - 1 && emptyCount > 0)
                    blockingValue += 40;
                else if (opponentCount >= requiredLength - 2 && emptyCount >= 2)
                    blockingValue += 20;
            }
            
            return blockingValue;
        }
        
        // Новый метод для получения взвешенных ходов
        public IEnumerable<(TicTacToeMove move, double weight)> GetWeightedPossibleMoves()
        {
            var possibleMoves = GetPossibleMoves();
            
            foreach (var move in possibleMoves)
            {
                double baseWeight = _fieldGenerator.GetCellWeight(move.X, move.Y);
                
                // Учитываем дополнительные факторы
                double strategicWeight = EvaluateStrategicPosition(move.X, move.Y) / 10.0;
                double lineWeight = EvaluateLinePotentialForNewRules(move.X, move.Y, CurrentPlayer) / 100.0;
                double blockWeight = EvaluateBlockingPotential(move.X, move.Y, CurrentPlayer) / 40.0;
                
                double totalWeight = baseWeight * (1.0 + strategicWeight + lineWeight + blockWeight);
                
                yield return (move, totalWeight);
            }
        }
        
        // Метод для обновления весов после хода
        public void UpdateWeightsAfterMove(int x, int y, Player player)
        {
            // Увеличиваем вес сделанного хода
            _fieldGenerator.IncreaseCellWeight(x, y, 3.0);
            
            // Обновляем веса вокруг сделанного хода
            var adjacentPositions = new List<(int, int)>();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    adjacentPositions.Add((x + dx, y + dy));
                }
            }
            
            // Увеличиваем вес соседних клеток
            foreach (var (adjX, adjY) in adjacentPositions)
            {
                if (!_fieldGenerator.IsPositionOccupied(adjX, adjY))
                {
                    _fieldGenerator.IncreaseCellWeight(adjX, adjY, 1.5);
                }
            }
            
            // Уменьшаем вес клеток, которые стали менее привлекательными
            UpdateStrategicWeights(player);
        }
        
        private void UpdateStrategicWeights(Player lastPlayer)
        {
            // Находим все потенциально опасные линии
            var dangerousLines = FindDangerousLines(lastPlayer);
            
            foreach (var line in dangerousLines)
            {
                // Увеличиваем вес клеток, которые могут заблокировать опасную линию
                foreach (var (x, y) in line.blockingCells)
                {
                    if (!_fieldGenerator.IsPositionOccupied(x, y))
                    {
                        _fieldGenerator.IncreaseCellWeight(x, y, 2.0);
                    }
                }
            }
        }
        
        private IEnumerable<(List<(int, int)> dangerousCells, List<(int, int)> blockingCells)> FindDangerousLines(Player player)
        {
            var result = new List<(List<(int, int)>, List<(int, int)>)>();
            int requiredLength = _gameRules.RequiredLineLength;
            
            // Проверяем все направления от каждой занятой клетки
            foreach (var ((x, y), cellPlayer) in _board)
            {
                if (cellPlayer != player) continue;
                
                int[][] directions = new int[][]
                {
                    new[] { 1, 0 },
                    new[] { 0, 1 },
                    new[] { 1, 1 },
                    new[] { 1, -1 }
                };
                
                foreach (var dir in directions)
                {
                    var dangerousCells = new List<(int, int)>();
                    var blockingCells = new List<(int, int)>();
                    
                    // Проверяем линию
                    for (int i = -(requiredLength - 1); i <= (requiredLength - 1); i++)
                    {
                        int checkX = x + i * dir[0];
                        int checkY = y + i * dir[1];
                        var cell = GetCell(checkX, checkY);
                        
                        if (cell == player)
                        {
                            dangerousCells.Add((checkX, checkY));
                        }
                        else if (cell == Player.None)
                        {
                            blockingCells.Add((checkX, checkY));
                        }
                    }
                    
                    if (dangerousCells.Count >= requiredLength - 1 && blockingCells.Count > 0)
                    {
                        result.Add((dangerousCells, blockingCells));
                    }
                }
            }
            
            return result;
        }
        
        // Новый метод для обновления линий игрока
        private void UpdatePlayerLines(int x, int y, Player player)
        {
            double cellWeight = _fieldGenerator.GetCellWeight(x, y);
            int requiredLength = _gameRules.RequiredLineLength;
            
            int[][] directions = new int[][]
            {
                new[] { 1, 0 },   // горизонталь
                new[] { 0, 1 },   // вертикаль
                new[] { 1, 1 },   // диагональ \
                new[] { 1, -1 }   // диагональ /
            };
            
            foreach (var dir in directions)
            {
                // Проверяем, можно ли добавить клетку к существующей линии
                var lineToExtend = FindLineToExtend(x, y, dir[0], dir[1], player);
                
                if (lineToExtend != null)
                {
                    lineToExtend.AddCell(x, y, cellWeight);
                }
                else
                {
                    // Создаем новую потенциальную линию
                    var newLine = new Line(player, requiredLength);
                    newLine.AddCell(x, y, cellWeight);
                    
                    // Проверяем соседние клетки в обоих направлениях
                    int count = 1;
                    
                    // Вперед
                    for (int i = 1; i < requiredLength; i++)
                    {
                        int nx = x + i * dir[0];
                        int ny = y + i * dir[1];
                        if (GetCell(nx, ny) == player)
                        {
                            newLine.AddCell(nx, ny, _fieldGenerator.GetCellWeight(nx, ny));
                            count++;
                        }
                        else break;
                    }
                    
                    // Назад
                    for (int i = 1; i < requiredLength; i++)
                    {
                        int nx = x - i * dir[0];
                        int ny = y - i * dir[1];
                        if (GetCell(nx, ny) == player)
                        {
                            newLine.AddCell(nx, ny, _fieldGenerator.GetCellWeight(nx, ny));
                            count++;
                        }
                        else break;
                    }
                    
                    // Сохраняем линию если в ней есть хотя бы 2 клетки
                    if (count >= 2)
                    {
                        _playerLines[player].Add(newLine);
                    }
                }
            }
            
            // Объединяем пересекающиеся линии
            MergeIntersectingLines(player);
        }
        
        private Line FindLineToExtend(int x, int y, int dx, int dy, Player player)
        {
            int requiredLength = _gameRules.RequiredLineLength;
            
            foreach (var line in _playerLines[player])
            {
                // Проверяем, находится ли клетка рядом с концом линии
                foreach (var (cx, cy) in line.Cells)
                {
                    int distanceX = Math.Abs(cx - x);
                    int distanceY = Math.Abs(cy - y);
                    
                    // Проверяем, лежит ли клетка на той же линии (в том же направлении)
                    if ((dx != 0 && distanceY == 0 && Math.Abs(cx - x) <= requiredLength) ||
                        (dy != 0 && distanceX == 0 && Math.Abs(cy - y) <= requiredLength) ||
                        (dx != 0 && dy != 0 && Math.Abs(cx - x) == Math.Abs(cy - y) && 
                         Math.Abs(cx - x) <= requiredLength))
                    {
                        return line;
                    }
                }
            }
            
            return null;
        }
        
        private void MergeIntersectingLines(Player player)
        {
            var lines = _playerLines[player];
            bool merged;
            
            do
            {
                merged = false;
                for (int i = 0; i < lines.Count; i++)
                {
                    for (int j = i + 1; j < lines.Count; j++)
                    {
                        if (lines[i].Cells.Any(cell => lines[j].Contains(cell.x, cell.y)))
                        {
                            // Объединяем линии
                            foreach (var cell in lines[j].Cells)
                            {
                                if (!lines[i].Contains(cell.x, cell.y))
                                {
                                    lines[i].AddCell(cell.x, cell.y, 
                                        _fieldGenerator.GetCellWeight(cell.x, cell.y));
                                }
                            }
                            lines.RemoveAt(j);
                            merged = true;
                            break;
                        }
                    }
                    if (merged) break;
                }
            } while (merged);
        }
        // есть ли у игрока 5 клеток ПОДРЯД
        private bool HasConsecutiveLine(Player player, int requiredLength)
        {
            
            int searchRadius = Math.Max(10, requiredLength + 5);
            
            for (int x = -searchRadius; x <= searchRadius; x++)
            {
                for (int y = -searchRadius; y <= searchRadius; y++)
                {
                    if (GetCell(x, y) == player)
                    {
                        // Проверяем 4 направления от этой клетки
                        if (CheckConsecutiveInDirection(x, y, 1, 0, player, requiredLength) ||   // →
                            CheckConsecutiveInDirection(x, y, 0, 1, player, requiredLength) ||   // ↑
                            CheckConsecutiveInDirection(x, y, 1, 1, player, requiredLength) ||   // ↗
                            CheckConsecutiveInDirection(x, y, 1, -1, player, requiredLength))    // ↘
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }

        // Проверяет, есть ли requiredLength клеток подряд в заданном направлении
        private bool CheckConsecutiveInDirection(int startX, int startY, int dx, int dy, Player player, int requiredLength)
        {
            // Проверяем, есть ли requiredLength клеток подряд
            for (int i = 0; i < requiredLength; i++)
            {
                if (GetCell(startX + i * dx, startY + i * dy) != player)
                    return false;
            }
            return true;
        }
        // Исправленный метод проверки победителя
        public GameResult CheckWinner()
        {
            int requiredLength = _gameRules.RequiredLineLength;
            
            // 1. Проверяем, есть ли у кого-то 5 клеток ПОДРЯД
            bool xHasLine5 = HasConsecutiveLine(Player.X, requiredLength);
            bool oHasLine5 = HasConsecutiveLine(Player.O, requiredLength);
            
            // Если никто не собрал 5 клеток подряд - игра продолжается
            if (!xHasLine5 && !oHasLine5)
            {
                // Проверка на ничью (нет свободных клеток после 50 ходов)
                if (MoveCount >= 50 && !_fieldGenerator.GenerateFreePositions(1).Any())
                {
                    return GameResult.Draw;
                }
                return GameResult.None;
            }
            
            // 2. Если кто-то собрал 5 клеток - игра заканчивается
            // 3. Подсчитываем очки для определения победителя
            double xTotalScore = CalculatePlayerScore(Player.X);
            double oTotalScore = CalculatePlayerScore(Player.O);
            
            // Определяем победителя по очкам
            if (xTotalScore > oTotalScore)
                return GameResult.XWins;
            else if (oTotalScore > xTotalScore)
                return GameResult.OWins;
            else
                return GameResult.Draw;
        }
        
        // Подсчет очков игрока (сумма весов всех его линий)
        public double CalculatePlayerScore(Player player)
        {
            double totalScore = 0;
            
            // Суммируем вес ВСЕХ линий игрока (не только полных)
            foreach (var line in _playerLines[player])
            {
                // Базовый вес клеток в линии
                double lineScore = line.TotalWeight;
                
                // Бонус за длину линии (чем длиннее - тем лучше)
                double lengthBonus = line.Cells.Count * 0.5;
                lineScore += lengthBonus;
                
                // Бонус за полную линию
                if (line.IsComplete)
                    lineScore *= 2.0; // Удваиваем ценность полной линии
                
                // Бонус за центральные клетки
                int centerCount = line.Cells.Count(c => Math.Abs(c.x) <= 1 && Math.Abs(c.y) <= 1);
                lineScore += centerCount * 1.0;
                
                totalScore += lineScore;
            }
            
            // Дополнительный бонус за стратегические позиции
            totalScore += GetStrategicBonus(player);
            
            return totalScore;
        }
        
        // Стратегический бонус
        private double GetStrategicBonus(Player player)
        {
            double bonus = 0;
            
            // Бонус за контроль центра
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (GetCell(x, y) == player)
                        bonus += 2.0;
                }
            }
            
            // Бонус за пересечение линий
            int intersectionCount = CountLineIntersections(player);
            bonus += intersectionCount * 3.0;
            
            return bonus;
        }
        
        // Получение лучшей линии игрока (для ИИ)
        public Line GetBestLine(Player player)
        {
            return _playerLines[player]
                .OrderByDescending(l => {
                    double score = l.TotalWeight;
                    if (l.IsComplete) score *= 1.5;
                    score += l.Cells.Count * 0.3;
                    return score;
                })
                .FirstOrDefault();
        }
        
        // Проверка, близок ли игрок к завершению линии
        public bool IsPlayerCloseToWin(Player player, out Line closestLine)
        {
            closestLine = null;
            int requiredLength = _gameRules.RequiredLineLength;
            
            foreach (var line in _playerLines[player])
            {
                int cellsNeeded = requiredLength - line.Cells.Count;
                if (cellsNeeded <= 2 && cellsNeeded > 0)
                {
                    if (closestLine == null || cellsNeeded < (requiredLength - closestLine.Cells.Count))
                    {
                        closestLine = line;
                    }
                }
            }
            
            return closestLine != null;
        }
        
        private int CountLineIntersections(Player player)
        {
            int intersections = 0;
            var lines = _playerLines[player];
            
            for (int i = 0; i < lines.Count; i++)
            {
                for (int j = i + 1; j < lines.Count; j++)
                {
                    if (lines[i].Cells.Any(cell => lines[j].Contains(cell.x, cell.y)))
                    {
                        intersections++;
                    }
                }
            }
            
            return intersections;
        }
        
        // Получить все линии игрока
        public IEnumerable<Line> GetPlayerLines(Player player)
        {
            return _playerLines[player];
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
        
        // Старая проверка линии (оставлена для совместимости)
        private bool CheckLine(int startX, int startY, int dx, int dy, Player player)
        {
            int count = 1;
            
            for (int i = 1; i < 5; i++)
            {
                int x = startX + i * dx;
                int y = startY + i * dy;
                if (GetCell(x, y) == player)
                    count++;
                else
                    break;
            }
            
            for (int i = 1; i < 5; i++)
            {
                int x = startX - i * dx;
                int y = startY - i * dy;
                if (GetCell(x, y) == player)
                    count++;
                else
                    break;
            }
            
            return count >= 5;
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
            return GetBoardStateString() == other.GetBoardStateString();
        }
        
        public override bool Equals(object obj) => Equals(obj as TicTacToeBoard);
        public override int GetHashCode() => GetBoardStateString().GetHashCode();
        
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
        
        // Метод для получения визуализации с весами
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
            
            builder.AppendLine();
            builder.AppendLine();
            builder.AppendLine("Легенда:");
            builder.AppendLine("  X - крестик");
            builder.AppendLine("  O - нолик");
            builder.AppendLine("  числа - стратегический вес клетки");
            
            return builder.ToString();
        }
        
        // Метод для получения визуализации с линиями
        public string GetBoardVisualizationWithLines(int radius = 3)
        {
            var builder = new StringBuilder();
            
            builder.AppendLine("Доска с линиями игроков:");
            builder.AppendLine();
            
            // Очки игроков
            double xScore = CalculatePlayerScore(Player.X);
            double oScore = CalculatePlayerScore(Player.O);
            builder.AppendLine($"Очки: ✕ {xScore:F1} | ○ {oScore:F1}");
            builder.AppendLine();
            
            // Линии игроков
            builder.AppendLine("Линии крестиков (✕):");
            var xLines = GetPlayerLines(Player.X).ToList();
            if (xLines.Any())
            {
                foreach (var line in xLines.OrderByDescending(l => l.CalculateScore()).Take(3))
                {
                    builder.AppendLine($"  - {line.Cells.Count}/{line.RequiredLength} клеток, вес: {line.TotalWeight:F1}, очки: {line.CalculateScore():F1}");
                }
            }
            else
            {
                builder.AppendLine("  Нет линий");
            }
            
            builder.AppendLine();
            builder.AppendLine("Линии ноликов (○):");
            var oLines = GetPlayerLines(Player.O).ToList();
            if (oLines.Any())
            {
                foreach (var line in oLines.OrderByDescending(l => l.CalculateScore()).Take(3))
                {
                    builder.AppendLine($"  - {line.Cells.Count}/{line.RequiredLength} клеток, вес: {line.TotalWeight:F1}, очки: {line.CalculateScore():F1}");
                }
            }
            else
            {
                builder.AppendLine("  Нет линий");
            }
            
            return builder.ToString();
        }
        
        // Проверяет, будет ли ход на границе заданного радиуса
        public bool WouldMoveBeOnBorder(int x, int y, int radius)
        {
            return _fieldGenerator.IsOnBorder(x, y, radius);
        }

        // Получает все возможные ходы на границе заданного радиуса
        public IEnumerable<TicTacToeMove> GetBorderMoves(int radius)
        {
            var borderCells = _fieldGenerator.GetBorderCells(radius);
            
            return borderCells
                .Where(pos => !_fieldGenerator.IsPositionOccupied(pos.x, pos.y))
                .Select(pos => new TicTacToeMove(pos.x, pos.y, CurrentPlayer))
                .OrderByDescending(move => _fieldGenerator.GetCellWeight(move.X, move.Y));
        }

        // Визуализация с выделением границ
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
                    
                    // Выделяем граничные клетки
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
        
        // Получение статистики по весам клеток
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
        
        // Сброс весов к значениям по умолчанию
        public void ResetWeights()
        {
            _fieldGenerator.ResetWeights();
        }
        
        // Получение топ-N самых перспективных клеток
        public IEnumerable<(int x, int y, double weight)> GetTopStrategicCells(int count = 10, int radius = 3)
        {
            var positions = _fieldGenerator.GenerateFieldAround((0, 0), radius)
                .Where(pos => !_fieldGenerator.IsPositionOccupied(pos.x, pos.y))
                .Select(pos => (pos.x, pos.y, weight: _fieldGenerator.GetCellWeight(pos.x, pos.y)))
                .OrderByDescending(cell => cell.weight)
                .Take(count);
            
            return positions;
        }
        
        // Получить текущий размер поля
        public int GetFieldSize() => _gameRules.FieldSize;
        
        // Получить требуемую длину линии
        public int GetRequiredLineLength() => _gameRules.RequiredLineLength;
        
        // Изменить размер поля
        // Изменить размер поля
        public void SetFieldSize(int size)
        {
            // Устанавливаем новый размер поля
            _gameRules.FieldSize = size;
            
            // Получаем новую требуемую длину линии
            int newRequiredLength = _gameRules.RequiredLineLength;
            
            // Обновляем требуемую длину ВСЕХ существующих линий
            foreach (var line in _playerLines[Player.X])
            {
                line.RequiredLength = newRequiredLength;
            }
            
            foreach (var line in _playerLines[Player.O])
            {
                line.RequiredLength = newRequiredLength;
            }
            

        }
    }
}