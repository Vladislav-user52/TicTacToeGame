using TicTacToeGame.Interfaces;
using TicTacToeGame.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TicTacToeGame.Algorithms
{
    public class TicTacToeSolver : IAlgorithm
    {
        public TimeSpan LastExecutionTime { get; private set; }
        
        private readonly int _maxDepth;
        private int _nodesEvaluated;
        private int _positionsGenerated;
        private readonly Random _random = new Random();
        
        // Веса для оценки позиций
        private const double CENTER_WEIGHT = 2.0;
        private const double CORNER_WEIGHT = 1.5;
        private const double EDGE_WEIGHT = 1.0;
        private const double WEIGHT_MULTIPLIER = 5.0;
        private const double LINE_SCORE_MULTIPLIER = 10.0;
        private const double COMPLETE_LINE_BONUS = 50.0;
        private const double WIN_SCORE = 10000;
        private const double LOSE_SCORE = -10000;
        
        public TicTacToeSolver(int maxDepth = 8)
        {
            _maxDepth = maxDepth;
        }
        
        // Основной метод поиска лучшего хода
        public TicTacToeMove? FindBestMove(TicTacToeBoard board, int timeLimitMs = 2000)
        {
            var stopwatch = Stopwatch.StartNew();
            _nodesEvaluated = 0;
            _positionsGenerated = 0;
            
            // Проверяем, не закончена ли уже игра
            if (board.CheckWinner() != GameResult.None)
            {
                LastExecutionTime = stopwatch.Elapsed;
                return null;
            }
            
            var currentPlayer = board.CurrentPlayer;
            var opponent = currentPlayer == Player.X ? Player.O : Player.X;
            
            // Стратегия 1: Если близки к завершению линии - завершаем её
            if (board.IsPlayerCloseToWin(currentPlayer, out var closestLine))
            {
                // Ищем клетки для завершения линии
                var completionMoves = FindLineCompletionMoves(board, closestLine);
                if (completionMoves.Any())
                {
                    // Выбираем клетку с максимальным весом
                    var bestCompletionMove = completionMoves
                        .OrderByDescending(m => board.FieldGenerator.GetCellWeight(m.X, m.Y))
                        .FirstOrDefault();
                    
                    if (bestCompletionMove != null)
                    {
                        LastExecutionTime = stopwatch.Elapsed;
                        return bestCompletionMove;
                    }
                }
            }
            
            // Стратегия 2: Блокируем противника, если он близок к победе
            if (board.IsPlayerCloseToWin(opponent, out var opponentClosestLine))
            {
                var blockingMoves = FindLineBlockingMoves(board, opponentClosestLine);
                if (blockingMoves.Any())
                {
                    
                    var bestBlockingMove = blockingMoves
                        .OrderByDescending(m => board.FieldGenerator.GetCellWeight(m.X, m.Y))
                        .FirstOrDefault();
                    
                    if (bestBlockingMove != null)
                    {
                        LastExecutionTime = stopwatch.Elapsed;
                        return bestBlockingMove;
                    }
                }
            }
            
            
            var possibleMoves = board.GetWeightedPossibleMoves().ToList();
            
            
            var evaluatedMoves = new List<(TicTacToeMove move, double score)>();
            
            foreach (var (move, weight) in possibleMoves.Take(15))
            {
                if (stopwatch.ElapsedMilliseconds > timeLimitMs) break;
                
                double score = weight * 10; 
                
                // Оцениваем улучшение линий
                score += EvaluateLineImprovement(board, move);
                
                // Оцениваем стратегическую ценность
                score += EvaluateStrategicValue(board, move);
                
                // Оцениваем потенциал создания новой линии
                score += EvaluateLineCreationPotential(board, move);
                
                evaluatedMoves.Add((move, score));
            }
            
            // Выбираем лучший ход
            var bestMove = evaluatedMoves
                .OrderByDescending(m => m.score)
                .FirstOrDefault();
            
            LastExecutionTime = stopwatch.Elapsed;
            
            return bestMove.move ?? possibleMoves.FirstOrDefault().move;
        }
        
        // Метод для поиска ходов, завершающих линию
        private List<TicTacToeMove> FindLineCompletionMoves(TicTacToeBoard board, TicTacToeBoard.Line line)
        {
            var completionMoves = new List<TicTacToeMove>();
            int requiredLength = board.GetRequiredLineLength();
            int cellsNeeded = requiredLength - line.Cells.Count;
            
            if (cellsNeeded <= 0) return completionMoves;
            
            
            var directions = new[] { (1, 0), (0, 1), (1, 1), (1, -1) };
            
            foreach (var (dx, dy) in directions)
            {
                
                foreach (var (x, y) in line.Cells)
                {
                    // Проверяем клетки в обе стороны
                    for (int i = -requiredLength; i <= requiredLength; i++)
                    {
                        int nx = x + i * dx;
                        int ny = y + i * dy;
                        
                        
                        if (line.Contains(nx, ny)) continue;
                        
                        
                        if (board.GetCell(nx, ny) == Player.None)
                        {
                            // Проверяем, находится ли клетка на продолжении линии
                            bool isOnLineExtension = true;
                            foreach (var (cx, cy) in line.Cells)
                            {
                                int diffX = Math.Abs(cx - nx);
                                int diffY = Math.Abs(cy - ny);
                                
                                // Клетка должна быть на одной линии с существующими
                                if (!((dx != 0 && diffY == 0) || (dy != 0 && diffX == 0) ||
                                      (dx != 0 && dy != 0 && diffX == diffY)))
                                {
                                    isOnLineExtension = false;
                                    break;
                                }
                            }
                            
                            if (isOnLineExtension)
                            {
                                var move = new TicTacToeMove(nx, ny, board.CurrentPlayer);
                                completionMoves.Add(move);
                            }
                        }
                    }
                }
            }
            
            return completionMoves.Distinct().ToList();
        }
        
        
        private List<TicTacToeMove> FindLineBlockingMoves(TicTacToeBoard board, TicTacToeBoard.Line line)
        {
            var blockingMoves = new List<TicTacToeMove>();
            int requiredLength = board.GetRequiredLineLength();
            int cellsNeeded = requiredLength - line.Cells.Count;
            
            if (cellsNeeded <= 0) return blockingMoves;
            
            // Находим клетки, которые могут помешать противнику завершить линию
            var directions = new[] { (1, 0), (0, 1), (1, 1), (1, -1) };
            
            foreach (var (dx, dy) in directions)
            {
                // Проверяем клетки в конце линии
                var orderedCells = line.Cells.OrderBy(c => c.x * dx + c.y * dy).ToList();
                if (orderedCells.Count >= 2)
                {
                    var firstCell = orderedCells.First();
                    var lastCell = orderedCells.Last();
                    
                    // Проверяем клетки перед началом и после конца линии
                    var potentialBlockingPositions = new List<(int, int)>();
                    
                    // Перед началом линии
                    int bx = firstCell.x - dx;
                    int by = firstCell.y - dy;
                    potentialBlockingPositions.Add((bx, by));
                    
                    // После конца линии
                    int ax = lastCell.x + dx;
                    int ay = lastCell.y + dy;
                    potentialBlockingPositions.Add((ax, ay));
                    
                    // Также проверяем промежутки в линии, если они есть
                    for (int i = 0; i < orderedCells.Count - 1; i++)
                    {
                        var cell1 = orderedCells[i];
                        var cell2 = orderedCells[i + 1];
                        
                        // Проверяем, есть ли пропуск между клетками
                        int diffX = cell2.x - cell1.x;
                        int diffY = cell2.y - cell1.y;
                        
                        if (Math.Abs(diffX) > 1 || Math.Abs(diffY) > 1)
                        {
                            // Находим промежуточные клетки
                            int steps = Math.Max(Math.Abs(diffX), Math.Abs(diffY));
                            for (int s = 1; s < steps; s++)
                            {
                                int mx = cell1.x + s * Math.Sign(diffX);
                                int my = cell1.y + s * Math.Sign(diffY);
                                potentialBlockingPositions.Add((mx, my));
                            }
                        }
                    }
                    
                    // Добавляем свободные клетки как потенциальные блокировки
                    foreach (var (bx2, by2) in potentialBlockingPositions)
                    {
                        if (board.GetCell(bx2, by2) == Player.None)
                        {
                            var move = new TicTacToeMove(bx2, by2, board.CurrentPlayer);
                            blockingMoves.Add(move);
                        }
                    }
                }
            }
            
            return blockingMoves.Distinct().ToList();
        }
        
        // Оценка улучшения существующих линий
        private double EvaluateLineImprovement(TicTacToeBoard board, TicTacToeMove move)
        {
            double improvement = 0;
            var player = board.CurrentPlayer;
            var playerLines = board.GetPlayerLines(player).ToList();
            
            foreach (var line in playerLines)
            {
                // Проверяем, улучшает ли ход эту линию
                if (WouldImproveLine(board, line, move))
                {
                    int cellsBefore = line.Cells.Count;
                    int requiredLength = board.GetRequiredLineLength();
                    
                    // Оценка улучшения линии
                    double lineImprovement = 0;
                    
                    // Бонус за приближение к завершению линии
                    int cellsNeeded = requiredLength - cellsBefore;
                    if (cellsNeeded > 0)
                    {
                        lineImprovement += (10.0 / cellsNeeded) * 2.0;
                    }
                    
                    // Учитываем вес клетки
                    double cellWeight = board.FieldGenerator.GetCellWeight(move.X, move.Y);
                    lineImprovement += cellWeight * 3.0;
                    
                    improvement += lineImprovement;
                }
            }
            
            return improvement;
        }
        
        private bool WouldImproveLine(TicTacToeBoard board, TicTacToeBoard.Line line, TicTacToeMove move)
        {
            // Проверяем, находится ли клетка рядом с линией
            foreach (var (x, y) in line.Cells)
            {
                int dx = Math.Abs(x - move.X);
                int dy = Math.Abs(y - move.Y);
                
               
                if ((dx == 0 && dy == 1) || (dx == 1 && dy == 0) || 
                    (dx == 1 && dy == 1))
                {
                    // Проверяем, что клетка лежит на продолжении линии
                    if (IsOnLineExtension(board, line, move.X, move.Y))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        
        private bool IsOnLineExtension(TicTacToeBoard board, TicTacToeBoard.Line line, int x, int y)
        {
            if (line.Cells.Count < 2) return false;
            
            // Проверяем направление линии
            var firstCell = line.Cells.First();
            var lastCell = line.Cells.Last();
            
            int dx = Math.Sign(lastCell.x - firstCell.x);
            int dy = Math.Sign(lastCell.y - firstCell.y);
            
            // Если линия еще не имеет четкого направления (одна клетка), проверяем все направления
            if (dx == 0 && dy == 0)
            {
                return true; 
            }
            
            
            foreach (var (cx, cy) in line.Cells)
            {
                
                int diffX = x - cx;
                int diffY = y - cy;
                
                
                if ((dx != 0 && diffY == 0 && Math.Sign(diffX) == dx) ||
                    (dy != 0 && diffX == 0 && Math.Sign(diffY) == dy) ||
                    (dx != 0 && dy != 0 && Math.Sign(diffX) == dx && Math.Sign(diffY) == dy))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        // Оценка стратегической ценности хода
        private double EvaluateStrategicValue(TicTacToeBoard board, TicTacToeMove move)
        {
            double value = 0;
            
            // Контроль центра
            if (Math.Abs(move.X) <= 1 && Math.Abs(move.Y) <= 1)
            {
                value += 5.0;
            }
            
            // Блокирование противника
            Player opponent = board.CurrentPlayer == Player.X ? Player.O : Player.X;
            var opponentLines = board.GetPlayerLines(opponent);
            foreach (var line in opponentLines)
            {
                int requiredLength = board.GetRequiredLineLength();
                int cellsNeeded = requiredLength - line.Cells.Count;
                
                if (cellsNeeded <= 2 && WouldBlockLine(board, line, move))
                {
                    // Важный блокирующий ход
                    value += 20.0 / (cellsNeeded + 1);
                }
            }
            
            return value;
        }
        
        private bool WouldBlockLine(TicTacToeBoard board, TicTacToeBoard.Line line, TicTacToeMove move)
        {
            
            return WouldImproveLine(board, line, new TicTacToeMove(move.X, move.Y, line.Player));
        }
        
        // Оценка потенциала создания новой линии
        private double EvaluateLineCreationPotential(TicTacToeBoard board, TicTacToeMove move)
        {
            double potential = 0;
            var player = board.CurrentPlayer;
            
            
            var directions = new[] { (1, 0), (0, 1), (1, 1), (1, -1) };
            int requiredLength = board.GetRequiredLineLength();
            
            foreach (var (dx, dy) in directions)
            {
                int playerCount = 1; 
                int emptyCount = 0;
                
                
                for (int i = 1; i < requiredLength; i++)
                {
                    // Вперед
                    var cell1 = board.GetCell(move.X + i * dx, move.Y + i * dy);
                    if (cell1 == player) playerCount++;
                    else if (cell1 == Player.None) emptyCount++;
                    
                    // Назад
                    var cell2 = board.GetCell(move.X - i * dx, move.Y - i * dy);
                    if (cell2 == player) playerCount++;
                    else if (cell2 == Player.None) emptyCount++;
                }
                
                // Оценка потенциала создания линии
                if (playerCount >= 2)
                {
                    double linePotential = playerCount * 2.0;
                    
                    // Бонус за хорошее начало линии
                    if (playerCount >= 3)
                        linePotential *= 1.5;
                    
                    
                    linePotential += emptyCount * 0.5;
                    
                    potential += linePotential;
                }
            }
            
            return potential;
        }
        
        
        private IEnumerable<(TicTacToeMove move, double weight)> FocusOnBestLine(
            TicTacToeBoard board, List<(TicTacToeMove move, double weight)> allMoves)
        {
            var currentPlayer = board.CurrentPlayer;
            var bestLine = board.GetBestLine(currentPlayer);
            
            if (bestLine != null && bestLine.Cells.Count >= 2)
            {
                // Выбираем ходы, которые продлевают лучшую линию
                var lineExtensionMoves = new List<(TicTacToeMove, double)>();
                
                foreach (var (move, weight) in allMoves)
                {
                    // Проверяем, продлевает ли ход лучшую линию
                    if (WouldExtendLine(bestLine, move.X, move.Y))
                    {
                        double extensionBonus = CalculateLineExtensionBonus(board, bestLine, move);
                        lineExtensionMoves.Add((move, weight + extensionBonus));
                    }
                }
                
                if (lineExtensionMoves.Any())
                {
                    return lineExtensionMoves.OrderByDescending(m => m.Item2);
                }
            }
            
            
            return allMoves.OrderByDescending(m => m.weight);
        }
        
        private bool WouldExtendLine(TicTacToeBoard.Line line, int x, int y)
        {
            // Проверяем, находится ли клетка рядом с концом линии
            foreach (var (cx, cy) in line.Cells)
            {
                int dx = Math.Abs(cx - x);
                int dy = Math.Abs(cy - y);
                
                if ((dx == 1 && dy == 0) || (dx == 0 && dy == 1) || 
                    (dx == 1 && dy == 1))
                {
                    return true;
                }
            }
            return false;
        }
        
        private double CalculateLineExtensionBonus(TicTacToeBoard board, TicTacToeBoard.Line line, TicTacToeMove move)
        {
            double bonus = 0;
            
            // Бонус за приближение к завершению линии
            int cellsNeeded = line.RequiredLength - line.Cells.Count;
            bonus += (10.0 / (cellsNeeded + 1)) * 2.0;
            
            // Бонус за вес клетки
            double cellWeight = board.FieldGenerator.GetCellWeight(move.X, move.Y);
            bonus += cellWeight * 3.0;
            
            // Бонус если ход завершает линию
            if (cellsNeeded == 1)
                bonus += 30.0;
            
            // Бонус если линия становится полной
            if (cellsNeeded == 0)
                bonus += 50.0;
            
            return bonus;
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
            _nodesEvaluated++;
            
            // Проверка времени
            if (timer.ElapsedMilliseconds > timeLimitMs) 
                return maximizingPlayer ? double.NegativeInfinity : double.PositiveInfinity;
            
            
            var result = board.CheckWinner();
            if (result != GameResult.None)
            {
                return EvaluateWeightedBoard(board, result);
            }
            
            // Проверка глубины
            if (depth == 0)
            {
                return QuickWeightedEvaluate(board);
            }
            
            // Получаем возможные ходы
            var moves = board.GetPossibleMoves();
            _positionsGenerated += moves.Count();
            
            if (!moves.Any())
            {
                return 0; // Ничья
            }
            
            // Сортируем ходы для лучшей эффективности альфа-бета отсечения
            var sortedMoves = SortWeightedMoves(board, moves, maximizingPlayer);
            
            if (maximizingPlayer)
            {
                double maxEval = double.NegativeInfinity;
                
                foreach (var move in sortedMoves)
                {
                    var newBoard = board.Clone();
                    newBoard.MakeMove(move.X, move.Y);
                    
                    double eval = WeightedMinimax(
                        newBoard,
                        depth - 1,
                        alpha,
                        beta,
                        false,
                        timer,
                        timeLimitMs
                    );
                    
                    maxEval = Math.Max(maxEval, eval);
                    alpha = Math.Max(alpha, eval);
                    
                    // Альфа-бета отсечение
                    if (beta <= alpha) break;
                    
                    if (timer.ElapsedMilliseconds > timeLimitMs) break;
                }
                return maxEval;
            }
            else
            {
                double minEval = double.PositiveInfinity;
                
                foreach (var move in sortedMoves)
                {
                    var newBoard = board.Clone();
                    newBoard.MakeMove(move.X, move.Y);
                    
                    double eval = WeightedMinimax(
                        newBoard,
                        depth - 1,
                        alpha,
                        beta,
                        true,
                        timer,
                        timeLimitMs
                    );
                    
                    minEval = Math.Min(minEval, eval);
                    beta = Math.Min(beta, eval);
                    
                    // Альфа-бета отсечение
                    if (beta <= alpha) break;
                    
                    if (timer.ElapsedMilliseconds > timeLimitMs) break;
                }
                return minEval;
            }
        }
        
        // Сортировка ходов с учетом весов
        private IEnumerable<TicTacToeMove> SortWeightedMoves(TicTacToeBoard board, IEnumerable<TicTacToeMove> moves, bool maximizingPlayer)
        {
            var moveScores = new List<(TicTacToeMove move, double score)>();
            
            foreach (var move in moves)
            {
                var newBoard = board.Clone();
                newBoard.MakeMove(move.X, move.Y);
                
                // Быстрая оценка позиции после хода
                double score = QuickWeightedEvaluate(newBoard);
                
                // Учитываем вес клетки
                double cellWeight = board.FieldGenerator.GetCellWeight(move.X, move.Y);
                score += cellWeight * 3.0;
                
                // Учитываем потенциал линии
                score += EvaluateLineCompletionPotential(newBoard, move.Player) * 2.0;
                
                moveScores.Add((move, score));
            }
            
            return moveScores
                .OrderByDescending(ms => maximizingPlayer ? ms.score : -ms.score)
                .Select(ms => ms.move)
                .Take(8) 
                .ToList();
        }
        
        // Оценка потенциала завершения линии
        private double EvaluateLineCompletionPotential(TicTacToeBoard board, Player player)
        {
            double potential = 0;
            var lines = board.GetPlayerLines(player);
            
            foreach (var line in lines)
            {
                int cellsNeeded = line.RequiredLength - line.Cells.Count;
                if (cellsNeeded <= 3) 
                {
                    double urgency = 1.0 / (cellsNeeded + 1);
                    potential += line.TotalWeight * urgency * 5.0;
                }
            }
            
            return potential;
        }
        

        private TicTacToeMove? GetOptimalFirstMove(TicTacToeBoard board, List<(TicTacToeMove move, double weight)> possibleMoves, Stopwatch stopwatch)
        {
            // Предпочитаем клетки с максимальным весом около центра
            var centerMoves = possibleMoves
                .Where(m => Math.Abs(m.move.X) <= 1 && Math.Abs(m.move.Y) <= 1)
                .OrderByDescending(m => m.weight)
                .ToList();
            
            if (centerMoves.Any())
            {
                LastExecutionTime = stopwatch.Elapsed;
                return centerMoves.First().move;
            }
            
            //клетки с максимальным весом
            var bestWeightMove = possibleMoves
                .OrderByDescending(m => m.weight)
                .FirstOrDefault();
            
            if (bestWeightMove.move != null)
            {
                LastExecutionTime = stopwatch.Elapsed;
                return bestWeightMove.move;
            }
            
            
            var move = possibleMoves.FirstOrDefault().move;
            LastExecutionTime = stopwatch.Elapsed;
            return move;
        }
        
        
        private double QuickWeightedEvaluate(TicTacToeBoard board)
        {
            
            var result = board.CheckWinner();
            if (result != GameResult.None)
            {
                return result switch
                {
                    GameResult.XWins => WIN_SCORE,
                    GameResult.OWins => LOSE_SCORE,
                    GameResult.Draw => 0,
                    _ => 0
                };
            }
            
            // Оценка основана на очках игроков
            double xScore = board.CalculatePlayerScore(Player.X);
            double oScore = board.CalculatePlayerScore(Player.O);
            
            double score = xScore - oScore;
            
            // Добавляем оценку стратегического преимущества
            score += EvaluateStrategicAdvantage(board, Player.X) * 2.0;
            score -= EvaluateStrategicAdvantage(board, Player.O) * 2.0;
            
            return score;
        }
        
        private double EvaluateStrategicAdvantage(TicTacToeBoard board, Player player)
        {
            double advantage = 0;
            
            // Контроль центра
            advantage += EvaluateCenterControl(board, player) * 3.0;
            
            // Потенциал для создания новых линий
            advantage += EvaluateLineCreationPotential(board, player) * 2.0;
            
            // Блокирование линий противника
            advantage += EvaluateBlockingPotential(board, player) * 1.5;
            
            return advantage;
        }
        
        private double EvaluateCenterControl(TicTacToeBoard board, Player player)
        {
            int control = 0;
            
            // Проверяем центральную область 3x3
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (board.GetCell(x, y) == player)
                        control++;
                }
            }
            
            return control * 0.5;
        }
        
        private double EvaluateLineCreationPotential(TicTacToeBoard board, Player player)
        {
            double potential = 0;
            
            // Анализируем свободные клетки на предмет создания новых линий
            var freePositions = board.FieldGenerator.GenerateFreePositions(2);
            
            foreach (var (x, y) in freePositions.Take(15)) // Ограничиваем для производительности
            {
                double cellPotential = EvaluateCellPotentialForLines(board, x, y, player);
                potential += cellPotential;
            }
            
            return potential / 10.0; 
        }
        
        private double EvaluateCellPotentialForLines(TicTacToeBoard board, int x, int y, Player player)
        {
            double potential = 0;
            double cellWeight = board.FieldGenerator.GetCellWeight(x, y);
            int requiredLength = board.GetRequiredLineLength();
            
            int[][] directions = new int[][]
            {
                new[] { 1, 0 },
                new[] { 0, 1 },
                new[] { 1, 1 },
                new[] { 1, -1 }
            };
            
            foreach (var dir in directions)
            {
                int playerCount = 0;
                int emptyCount = 0;
                
                
                for (int i = 1; i < requiredLength; i++)
                {
                    // Вперед
                    var cell1 = board.GetCell(x + i * dir[0], y + i * dir[1]);
                    if (cell1 == player) playerCount++;
                    else if (cell1 == Player.None) emptyCount++;
                    
                    // Назад
                    var cell2 = board.GetCell(x - i * dir[0], y - i * dir[1]);
                    if (cell2 == player) playerCount++;
                    else if (cell2 == Player.None) emptyCount++;
                }
                
                // Потенциал создания линии
                if (playerCount >= 2)
                {
                    double linePotential = cellWeight * (playerCount * 2);
                    
                    // Учитываем близость к завершению линии
                    if (playerCount >= requiredLength - 2 && emptyCount >= 2)
                        linePotential *= 2.0;
                    
                    potential += linePotential;
                }
            }
            
            return potential;
        }
        
        private double EvaluateBlockingPotential(TicTacToeBoard board, Player player)
        {
            Player opponent = player == Player.X ? Player.O : Player.X;
            double blockingValue = 0;
            
            // Находим лучшую линию противника
            var opponentBestLine = board.GetBestLine(opponent);
            if (opponentBestLine != null)
            {
                // Если противник близок к завершению линии, блокировка важна
                int cellsNeeded = opponentBestLine.RequiredLength - opponentBestLine.Cells.Count;
                if (cellsNeeded <= 2)
                {
                    double urgency = 1.0 / (cellsNeeded + 0.5);
                    blockingValue += opponentBestLine.TotalWeight * urgency * 3.0;
                }
            }
            
            // Также оцениваем общий потенциал противника
            var opponentLines = board.GetPlayerLines(opponent);
            foreach (var line in opponentLines)
            {
                int cellsNeeded = line.RequiredLength - line.Cells.Count;
                if (cellsNeeded <= 3)
                {
                    blockingValue += line.TotalWeight * 0.5;
                }
            }
            
            return blockingValue;
        }
        
        
        private double EvaluateWeightedBoard(TicTacToeBoard board, GameResult result)
        {
            if (result != GameResult.None)
            {
                return result switch
                {
                    GameResult.XWins => WIN_SCORE,
                    GameResult.OWins => LOSE_SCORE,
                    GameResult.Draw => 0,
                    _ => 0
                };
            }
            
            return QuickWeightedEvaluate(board);
        }
        
        
        private IEnumerable<(TicTacToeMove move, double weight)> GetWeightedMoves(TicTacToeBoard board)
        {
            var possibleMoves = board.GetPossibleMoves();
            
            foreach (var move in possibleMoves)
            {
                double weight = 1.0;
                
                
                if (board.FieldGenerator != null)
                {
                    weight *= board.FieldGenerator.GetCellWeight(move.X, move.Y);
                }
                
                // Стратегическая ценность позиции
                weight *= GetPositionWeight(move.X, move.Y, board);
                
                // Потенциал создания выигрышных линий
                weight += EvaluateMovePotential(board, move);
                
                yield return (move, weight);
            }
        }
        
        
        private double GetPositionWeight(int x, int y, TicTacToeBoard board)
        {
            double weight = 1.0;
            
            // Определяем тип клетки относительно центра
            double distanceFromCenter = Math.Sqrt(x * x + y * y);
            
            // Близкие к центру клетки более ценны
            if (distanceFromCenter <= 1.0) weight *= 2.0;
            else if (distanceFromCenter <= 2.0) weight *= 1.5;
            else if (distanceFromCenter <= 3.0) weight *= 1.2;
            
            return weight;
        }
        
        // Оценка потенциала хода 
        private double EvaluateMovePotential(TicTacToeBoard board, TicTacToeMove move)
        {
            double potential = 0;
            
            // Потенциал создания линий
            potential += EvaluateLinePotentialAtPosition(board, move.X, move.Y, move.Player) * 3;
            
            // Блокирование противника
            Player opponent = move.Player == Player.X ? Player.O : Player.X;
            potential += EvaluateLinePotentialAtPosition(board, move.X, move.Y, opponent) * 2;
            
            return potential;
        }
        
        private double EvaluateLinePotentialAtPosition(TicTacToeBoard board, int x, int y, Player player)
        {
            double potential = 0;
            
            int[][] directions = new int[][]
            {
                new[] { 1, 0 },   // горизонталь
                new[] { 0, 1 },   // вертикаль
                new[] { 1, 1 },   // диагональ \
                new[] { 1, -1 }   // диагональ /
            };
            
            foreach (var dir in directions)
            {
                int count = 1; 
                int emptyCount = 0;
                int opponentCount = 0;
                
                
                for (int i = 1; i < 3; i++)
                {
                    var cell1 = board.GetCell(x + i * dir[0], y + i * dir[1]);
                    var cell2 = board.GetCell(x - i * dir[0], y - i * dir[1]);
                    
                    if (cell1 == player) count++;
                    else if (cell1 == Player.None) emptyCount++;
                    else opponentCount++;
                    
                    if (cell2 == player) count++;
                    else if (cell2 == Player.None) emptyCount++;
                    else opponentCount++;
                }
                
                // Оценка потенциала линии
                if (count >= 3) potential += 100;
                else if (count == 2 && emptyCount > 0) potential += 50;
                else if (count == 1 && emptyCount >= 2) potential += 10;
            }
            
            return potential;
        }
        
        
        public (int nodesEvaluated, int positionsGenerated, TimeSpan executionTime) GetStats()
        {
            return (_nodesEvaluated, _positionsGenerated, LastExecutionTime);
        }
        
        // Дополнительный метод для отладки
        public string GetEvaluationInfo(TicTacToeBoard board)
        {
            double xScore = board.CalculatePlayerScore(Player.X);
            double oScore = board.CalculatePlayerScore(Player.O);
            
            var xBestLine = board.GetBestLine(Player.X);
            var oBestLine = board.GetBestLine(Player.O);
            
            string info = $"Очки: ✕ {xScore:F1} | ○ {oScore:F1}\n";
            
            if (xBestLine != null)
                info += $"Лучшая линия ✕: {xBestLine.Cells.Count}/{xBestLine.RequiredLength} клеток, вес: {xBestLine.TotalWeight:F1}\n";
            
            if (oBestLine != null)
                info += $"Лучшая линия ○: {oBestLine.Cells.Count}/{oBestLine.RequiredLength} клеток, вес: {oBestLine.TotalWeight:F1}";
            
            return info;
        }
    }
}