using TicTacToeGame.Interfaces;
using TicTacToeGame.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TicTacToeGame.Algorithms
{
    public static class GraphAlgorithms
    {
        // Поиск в ширину (BFS)
        public static List<IVertex<T>> BreadthFirstSearch<T>(IGraph<T, Edge<T>> graph, T startVertex)
            where T : IComparable<T>
        {
            var visited = new HashSet<T>();
            var result = new List<IVertex<T>>();
            var queue = new Queue<T>();

            queue.Enqueue(startVertex);
            visited.Add(startVertex);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                result.Add(graph.GetVertex(current));

                foreach (var neighbor in graph.GetNeighbors(current))
                {
                    if (!visited.Contains(neighbor.Id))
                    {
                        visited.Add(neighbor.Id);
                        queue.Enqueue(neighbor.Id);
                    }
                }
            }

            return result;
        }

        // Поиск в глубину (DFS)
        public static List<IVertex<T>> DepthFirstSearch<T>(IGraph<T, Edge<T>> graph, T startVertex)
            where T : IComparable<T>
        {
            var visited = new HashSet<T>();
            var result = new List<IVertex<T>>();

            DFSRecursive(graph, startVertex, visited, result);

            return result;
        }

        private static void DFSRecursive<T>(IGraph<T, Edge<T>> graph, T vertex,
            HashSet<T> visited, List<IVertex<T>> result) where T : IComparable<T>
        {
            visited.Add(vertex);
            result.Add(graph.GetVertex(vertex));

            foreach (var neighbor in graph.GetNeighbors(vertex))
            {
                if (!visited.Contains(neighbor.Id))
                {
                    DFSRecursive(graph, neighbor.Id, visited, result);
                }
            }
        }

        // Проверка на связность (для неориентированного графа)
        public static bool IsConnected<T>(Graph<T> graph) where T : IComparable<T>
        {
            if (graph.VertexCount == 0)
                return true;

            var visited = new HashSet<T>();
            var stack = new Stack<T>();
            var firstVertex = graph.Vertices.First().Id;

            stack.Push(firstVertex);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (!visited.Contains(current))
                {
                    visited.Add(current);

                    foreach (var neighbor in graph.GetNeighbors(current))
                    {
                        if (!visited.Contains(neighbor.Id))
                        {
                            stack.Push(neighbor.Id);
                        }
                    }
                }
            }

            return visited.Count == graph.VertexCount;
        }

        public static IGraph<(int x, int y), Edge<(int x, int y)>>
        GeneratePositionGraph(TicTacToeBoard board, int radius = 3)
        {
            var graph = new Graph<(int x, int y)>();

            // Генерируем позиции из доски
            var positions = board.GetGeneratedField(radius);

            foreach (var (x, y, _) in positions)
            {
                graph.AddVertex((x, y));

                // Добавляем ребра между соседними позициями
                (int, int)[] directions = { (1, 0), (0, 1), (1, 1), (1, -1) };

                foreach (var (dx, dy) in directions)
                {
                    var neighbor = (x + dx, y + dy);
                    if (graph.GetVertex(neighbor) != null)
                    {
                        graph.AddEdge((x, y), neighbor);
                    }
                }
            }

            return graph;
        }
    }
}