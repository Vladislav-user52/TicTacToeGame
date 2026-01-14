using System;
using System.Collections.Generic;
using System.Linq;

namespace TicTacToeGame.Models
{
    public class InfiniteFieldGenerator
    {
        private readonly HashSet<(int x, int y)> _occupiedPositions;
        private readonly Dictionary<(int, int), double> _cellWeights;
        private int _currentRadius;
        private (int minX, int maxX, int minY, int maxY) _bounds;

        public InfiniteFieldGenerator()
        {
            _occupiedPositions = new HashSet<(int x, int y)>();
            _cellWeights = new Dictionary<(int, int), double>();
            _currentRadius = 0;
            _bounds = (0, 0, 0, 0);
            InitializeDefaultWeights();
        }

        public InfiniteFieldGenerator(IEnumerable<(int x, int y)> initialOccupied)
        {
            _occupiedPositions = new HashSet<(int x, int y)>(initialOccupied);
            _cellWeights = new Dictionary<(int, int), double>();
            RecalculateBounds();
            _currentRadius = CalculateCurrentRadius();
            InitializeDefaultWeights();
        }

        private void InitializeDefaultWeights()
        {
            // Инициализируем веса для центральной области
            for (int x = -10; x <= 10; x++)
            {
                for (int y = -10; y <= 10; y++)
                {
                    double weight = CalculateDefaultWeight(x, y);
                    SetCellWeight(x, y, weight);
                }
            }
        }

        private double CalculateDefaultWeight(int x, int y)
        {
            // Базовый вес на основе расстояния от центра
            double distanceFromCenter = Math.Sqrt(x * x + y * y);
            
            // Центральные клетки имеют больший вес
            double baseWeight = Math.Max(10.0 / (1.0 + distanceFromCenter), 0.5);
            
            // Дополнительный вес для центральных линий
            if (x == 0 || y == 0 || Math.Abs(x) == Math.Abs(y))
                baseWeight *= 1.2;
            
            // Дополнительный вес для стратегических позиций (углы и центр)
            if (x == 0 && y == 0) baseWeight *= 2.0; // Самый центр
            else if (Math.Abs(x) == 1 && y == 0) baseWeight *= 1.5; // Горизонталь от центра
            else if (x == 0 && Math.Abs(y) == 1) baseWeight *= 1.5; // Вертикаль от центра
            
            return Math.Round(baseWeight, 2);
        }

        public IEnumerable<(int x, int y)> GenerateFieldAroundOccupied(int padding = 2)
        {
            if (_occupiedPositions.Count == 0)
            {
                // Если нет занятых клеток, возвращаем область вокруг центра
                foreach (var pos in GenerateFieldAround((0, 0), 3))
                {
                    yield return pos;
                }
                yield break;
            }

            // Рассчитываем область, охватывающую все занятые клетки
            int minX = _bounds.minX - padding;
            int maxX = _bounds.maxX + padding;
            int minY = _bounds.minY - padding;
            int maxY = _bounds.maxY + padding;

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    yield return (x, y);
                }
            }
        }

        // Генерация области фиксированного радиуса
        public IEnumerable<(int x, int y)> GenerateFieldAround((int x, int y) center, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    yield return (center.x + dx, center.y + dy);
                }
            }
        }

        // Генерация свободных позиций с автоматическим определением области поиска
        public IEnumerable<(int x, int y)> GenerateFreePositions(int padding = 2)
        {
            var area = GenerateFieldAroundOccupied(padding);
            
            foreach (var pos in area)
            {
                if (!_occupiedPositions.Contains(pos))
                {
                    yield return pos;
                }
            }
        }

        // Генерация позиций, смежных с занятыми
        public IEnumerable<(int x, int y)> GenerateAdjacentPositions()
        {
            var adjacent = new HashSet<(int x, int y)>();

            if (_occupiedPositions.Count == 0)
            {
                adjacent.Add((0, 0));
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx != 0 || dy != 0)
                            adjacent.Add((dx, dy));
                    }
                }
                return adjacent;
            }

            foreach (var (x, y) in _occupiedPositions)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        var pos = (x + dx, y + dy);
                        if (!_occupiedPositions.Contains(pos))
                        {
                            adjacent.Add(pos);
                        }
                    }
                }
            }

            return adjacent;
        }

        // Управление состоянием
        public void MarkPositionOccupied(int x, int y)
        {
            _occupiedPositions.Add((x, y));
            UpdateBounds(x, y);
            _currentRadius = Math.Max(_currentRadius, Math.Max(Math.Abs(x), Math.Abs(y)));
            
            // Уменьшаем вес занятой клетки
            SetCellWeight(x, y, GetCellWeight(x, y) * 0.3);
            
            // Увеличиваем вес соседних клеток
            UpdateWeightsAroundPosition(x, y, 0.5);
        }

        public void MarkPositionFree(int x, int y)
        {
            _occupiedPositions.Remove((x, y));
            if (_occupiedPositions.Count > 0)
            {
                RecalculateBounds();
                _currentRadius = CalculateCurrentRadius();
            }
            else
            {
                _bounds = (0, 0, 0, 0);
                _currentRadius = 0;
            }
            
            // Восстанавливаем вес освобожденной клетки
            double defaultWeight = CalculateDefaultWeight(x, y);
            SetCellWeight(x, y, defaultWeight);
        }

        public bool IsPositionOccupied(int x, int y) => _occupiedPositions.Contains((x, y));

        // Методы для работы с весами
        public void SetCellWeight(int x, int y, double weight)
        {
            _cellWeights[(x, y)] = Math.Max(weight, 0.1); // Минимальный вес 0.1
        }

        public double GetCellWeight(int x, int y)
        {
            if (_cellWeights.TryGetValue((x, y), out double weight))
                return weight;
            
            // Если вес не задан, вычисляем его динамически
            weight = CalculateDefaultWeight(x, y);
            _cellWeights[(x, y)] = weight;
            return weight;
        }

        public void IncreaseCellWeight(int x, int y, double increment = 1.0)
        {
            double currentWeight = GetCellWeight(x, y);
            SetCellWeight(x, y, currentWeight + increment);
        }

        public void DecreaseCellWeight(int x, int y, double decrement = 1.0)
        {
            double currentWeight = GetCellWeight(x, y);
            SetCellWeight(x, y, Math.Max(currentWeight - decrement, 0.1));
        }

        private void UpdateWeightsAroundPosition(int x, int y, double increment)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    int nx = x + dx;
                    int ny = y + dy;
                    
                    // Увеличиваем вес соседних клеток
                    IncreaseCellWeight(nx, ny, increment);
                }
            }
        }

        // Получить все клетки с их весами
        public IEnumerable<WeightedCell> GetAllWeightedCells(int radius = 5)
        {
            var positions = GenerateFieldAround((0, 0), radius);
            
            foreach (var (x, y) in positions)
            {
                yield return new WeightedCell(x, y, GetCellWeight(x, y));
            }
        }

        // Получить топ N клеток по весу
        public IEnumerable<WeightedCell> GetTopWeightedCells(int count, int radius = 5)
        {
            return GetAllWeightedCells(radius)
                .Where(cell => !IsPositionOccupied(cell.X, cell.Y))
                .OrderByDescending(cell => cell.Weight)
                .Take(count);
        }

        // Обновить веса на основе сделанных ходов
        public void UpdateWeightsBasedOnMoves(IEnumerable<(int x, int y)> moves, Player player)
        {
            foreach (var (x, y) in moves)
            {
                // Увеличиваем вес клеток вокруг сделанного хода
                for (int dx = -2; dx <= 2; dx++)
                {
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        
                        int nx = x + dx;
                        int ny = y + dy;
                        
                        // Увеличиваем вес, но меньше для дальних клеток
                        double distanceFactor = 1.0 / (1.0 + Math.Sqrt(dx * dx + dy * dy));
                        IncreaseCellWeight(nx, ny, 0.3 * distanceFactor);
                    }
                }
            }
        }

        // Сбросить все веса к значениям по умолчанию
        public void ResetWeights()
        {
            _cellWeights.Clear();
            InitializeDefaultWeights();
        }

        // Получить статистику по весам
        public (double min, double max, double average) GetWeightStats(int radius = 5)
        {
            var weights = GetAllWeightedCells(radius)
                .Select(c => c.Weight)
                .ToList();
            
            if (weights.Count == 0)
                return (0, 0, 0);
                
            return (
                min: weights.Min(),
                max: weights.Max(),
                average: weights.Average()
            );
        }

        // Получить веса в виде матрицы для отображения
        public double[,] GetWeightMatrix(int radius = 3)
        {
            int size = radius * 2 + 1;
            var matrix = new double[size, size];
            
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    int x = i - radius;
                    int y = radius - j; // Инвертируем для отображения сверху вниз
                    matrix[i, j] = GetCellWeight(x, y);
                }
            }
            
            return matrix;
        }

        // Получить текущий радиус поля (макс расстояние от центра)
        public int GetCurrentRadius() => _currentRadius;

        // Получить границы занятой области
        public (int minX, int maxX, int minY, int maxY) GetBounds() => _bounds;

        // Получить центр масс занятых клеток (для центрирования)
        public (int centerX, int centerY) GetCenterOfMass()
        {
            if (_occupiedPositions.Count == 0)
                return (0, 0);

            int sumX = 0, sumY = 0;
            foreach (var (x, y) in _occupiedPositions)
            {
                sumX += x;
                sumY += y;
            }

            return (sumX / _occupiedPositions.Count, sumY / _occupiedPositions.Count);
        }

        // Получить рекомендуемый радиус для отображения
        public int GetRecommendedDisplayRadius(int padding = 3)
        {
            if (_occupiedPositions.Count == 0)
                return 3;

            int width = _bounds.maxX - _bounds.minX;
            int height = _bounds.maxY - _bounds.minY;
            int maxDimension = Math.Max(width, height);
            
            return maxDimension / 2 + padding;
        }

        public int GetOccupiedCount() => _occupiedPositions.Count;

        private void UpdateBounds(int x, int y)
        {
            _bounds.minX = Math.Min(_bounds.minX, x);
            _bounds.maxX = Math.Max(_bounds.maxX, x);
            _bounds.minY = Math.Min(_bounds.minY, y);
            _bounds.maxY = Math.Max(_bounds.maxY, y);
        }

        private void RecalculateBounds()
        {
            if (_occupiedPositions.Count == 0)
            {
                _bounds = (0, 0, 0, 0);
                return;
            }

            _bounds.minX = int.MaxValue;
            _bounds.maxX = int.MinValue;
            _bounds.minY = int.MaxValue;
            _bounds.maxY = int.MinValue;

            foreach (var (x, y) in _occupiedPositions)
            {
                _bounds.minX = Math.Min(_bounds.minX, x);
                _bounds.maxX = Math.Max(_bounds.maxX, x);
                _bounds.minY = Math.Min(_bounds.minY, y);
                _bounds.maxY = Math.Max(_bounds.maxY, y);
            }
        }

        private int CalculateCurrentRadius()
        {
            if (_occupiedPositions.Count == 0)
                return 0;

            int maxDistance = 0;
            foreach (var (x, y) in _occupiedPositions)
            {
                maxDistance = Math.Max(maxDistance, Math.Max(Math.Abs(x), Math.Abs(y)));
            }

            return maxDistance;
        }

        // Проверяет, находится ли позиция на границе заданного радиуса
        public bool IsOnBorder(int x, int y, int radius)
        {
            return Math.Abs(x) == radius || Math.Abs(y) == radius;
        }

        // Получает все граничные клетки для заданного радиуса
        public IEnumerable<(int x, int y)> GetBorderCells(int radius)
        {
            // Верхняя и нижняя границы
            for (int x = -radius; x <= radius; x++)
            {
                yield return (x, radius);  // Верх
                yield return (x, -radius); // Низ
            }
            
            // Левая и правая границы (исключаем углы, т.к. они уже учтены)
            for (int y = -radius + 1; y < radius; y++)
            {
                yield return (radius, y);   // Право
                yield return (-radius, y);  // Лево
            }
        }

        // Проверяет, выходит ли ход за пределы заданного радиуса
        public bool IsMoveOutsideRadius(int x, int y, int radius)
        {
            return Math.Abs(x) > radius || Math.Abs(y) > radius;
        }

        // Рекомендуемый радиус для отображения на основе занятых клеток
        public int GetRecommendedRadiusForOccupied(int padding = 1)
        {
            if (_occupiedPositions.Count == 0)
                return 3;

            // Находим максимальную координату по модулю
            int maxCoord = 0;
            foreach (var (x, y) in _occupiedPositions)
            {
                maxCoord = Math.Max(maxCoord, Math.Max(Math.Abs(x), Math.Abs(y)));
            }

            return maxCoord + padding;
        }

        // Метод для дебаггинга - получить строковое представление весов
        public string GetWeightsString(int radius = 3)
        {
            var builder = new System.Text.StringBuilder();
            
            for (int y = radius; y >= -radius; y--)
            {
                builder.Append($"{y,3}: ");
                for (int x = -radius; x <= radius; x++)
                {
                    double weight = GetCellWeight(x, y);
                    builder.Append($"{weight,6:F2}");
                }
                builder.AppendLine();
            }
            
            builder.Append("    ");
            for (int x = -radius; x <= radius; x++)
            {
                builder.Append($"{x,6}");
            }
            
            return builder.ToString();
        }
    }
}