using System;
using System.Collections.Generic;
using System.Linq;

namespace TicTacToeGame.Models
{
    public class SimpleFieldGenerator : InfiniteFieldGenerator
    {
        private readonly HashSet<(int x, int y)> _generatedCells;
        
        public SimpleFieldGenerator()
        {
            _generatedCells = new HashSet<(int x, int y)>();
            // Генерируем только центральную клетку
            GenerateCell(0, 0);
        }
        
        public SimpleFieldGenerator(IEnumerable<(int x, int y)> initialOccupied)
        {
            _generatedCells = new HashSet<(int x, int y)>();
            
            // Генерируем все занятые клетки и их соседей
            foreach (var (x, y) in initialOccupied)
            {
                GenerateCellAndNeighbors(x, y);
            }
        }
        
        public new void MarkPositionOccupied(int x, int y)
        {
            base.MarkPositionOccupied(x, y);
            GenerateCellAndNeighbors(x, y);
        }
        
        private void GenerateCell(int x, int y)
        {
            if (!_generatedCells.Contains((x, y)))
            {
                _generatedCells.Add((x, y));
                // Устанавливаем единичный вес для всех клеток
                SetCellWeight(x, y, 1.0);
            }
        }
        
        private void GenerateCellAndNeighbors(int x, int y)
        {
            // Генерируем саму клетку
            GenerateCell(x, y);
            
            // Генерируем соседние клетки
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
        
        public new IEnumerable<(int x, int y)> GenerateFreePositions(int padding = 2)
        {
            // Возвращаем только сгенерированные свободные клетки
            foreach (var (x, y) in _generatedCells)
            {
                if (!IsPositionOccupied(x, y))
                {
                    yield return (x, y);
                }
            }
        }
        
        public new IEnumerable<(int x, int y)> GenerateAdjacentPositions()
        {
            // Возвращаем соседние к занятым сгенерированные клетки
            var adjacent = new HashSet<(int x, int y)>();
            
            if (GetOccupiedCount() == 0)
            {
                // Если нет занятых, возвращаем центральную и соседние
                foreach (var (x, y) in _generatedCells)
                {
                    if (Math.Abs(x) <= 1 && Math.Abs(y) <= 1)
                    {
                        adjacent.Add((x, y));
                    }
                }
                return adjacent;
            }
            
            foreach (var (x, y) in GetOccupiedPositions())
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        
                        int nx = x + dx;
                        int ny = y + dy;
                        
                        if (_generatedCells.Contains((nx, ny)) && !IsPositionOccupied(nx, ny))
                        {
                            adjacent.Add((nx, ny));
                        }
                    }
                }
            }
            
            return adjacent;
        }
        
        private IEnumerable<(int x, int y)> GetOccupiedPositions()
        {
            return _generatedCells.Where(pos => IsPositionOccupied(pos.x, pos.y));
        }
        
        public bool IsCellGenerated(int x, int y)
        {
            return _generatedCells.Contains((x, y));
        }
        
        public int GetGeneratedCellsCount()
        {
            return _generatedCells.Count;
        }
        
        public new void ResetWeights()
        {
            
            foreach (var (x, y) in _generatedCells)
            {
                SetCellWeight(x, y, 1.0);
            }
        }
        
        public new (double min, double max, double average) GetWeightStats(int radius = 5)
        {
            
            return (1.0, 1.0, 1.0);
        }
    }
}