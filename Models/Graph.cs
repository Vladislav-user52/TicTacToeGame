using TicTacToeGame.Interfaces;
using TicTacToeGame.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TicTacToeGame.Models
{
    public class Graph<T> : IGraph<T, Edge<T>> where T : IComparable<T>
    {
        private readonly Dictionary<T, Vertex<T>> _vertices;
        private readonly Dictionary<T, List<Edge<T>>> _adjacencyList;
        private readonly Dictionary<(T, T), Edge<T>> _edges;
        
        public bool IsDirected => false;
        public int VertexCount => _vertices.Count;
        public int EdgeCount => _edges.Count / 2; // Каждое ребро учитывается дважды
        
        public IEnumerable<IVertex<T>> Vertices => _vertices.Values.Cast<IVertex<T>>();
        public IEnumerable<Edge<T>> AllEdges => _edges.Values.Distinct(); // Возвращаем уникальные ребра
        
        public Graph()
        {
            _vertices = new Dictionary<T, Vertex<T>>();
            _adjacencyList = new Dictionary<T, List<Edge<T>>>();
            _edges = new Dictionary<(T, T), Edge<T>>();
        }
        
        public IVertex<T> AddVertex(T id, string label = null)
        {
            if (!_vertices.ContainsKey(id))
            {
                var vertex = new Vertex<T>(id, label);
                _vertices[id] = vertex;
                _adjacencyList[id] = new List<Edge<T>>();
            }
            return _vertices[id];
        }
        
        public bool RemoveVertex(T id)
        {
            if (!_vertices.ContainsKey(id))
                return false;
                
            // Находим все инцидентные ребра
            var incidentEdges = new List<(T, T)>();
            
            foreach (var edgeKey in _edges.Keys)
            {
                if (edgeKey.Item1.CompareTo(id) == 0 || edgeKey.Item2.CompareTo(id) == 0)
                {
                    incidentEdges.Add(edgeKey);
                }
            }
            
            // Удаляем ребра
            foreach (var edgeKey in incidentEdges)
            {
                _edges.Remove(edgeKey);
                
                // Удаляем из списков смежности соседей
                var otherVertex = edgeKey.Item1.CompareTo(id) == 0 ? edgeKey.Item2 : edgeKey.Item1;
                if (_adjacencyList.ContainsKey(otherVertex))
                {
                    _adjacencyList[otherVertex].RemoveAll(e => 
                        e.From.Id.CompareTo(id) == 0 || e.To.Id.CompareTo(id) == 0);
                }
            }
            
            // Удаляем вершину
            _adjacencyList.Remove(id);
            _vertices.Remove(id);
            
            return true;
        }
        
        public IVertex<T> GetVertex(T id)
        {
            return _vertices.ContainsKey(id) ? _vertices[id] : null;
        }
        
        public Edge<T> AddEdge(T from, T to, double weight = 1.0)
        {
            if (!_vertices.ContainsKey(from)) AddVertex(from);
            if (!_vertices.ContainsKey(to)) AddVertex(to);
            
            // Создаем оба ключа для неориентированного графа
            var key1 = (from, to);
            var key2 = (to, from);
            
            // Проверяем, существует ли уже ребро
            if (_edges.ContainsKey(key1) || _edges.ContainsKey(key2))
            {
                // Обновляем вес существующего ребра
                var existingKey = _edges.ContainsKey(key1) ? key1 : key2;
                _edges[existingKey].Weight = weight;
                return _edges[existingKey];
            }
            
            // Создаем ребро
            var edge = new Edge<T>(_vertices[from], _vertices[to], weight);
            
            // Добавляем в оба направления
            _edges[key1] = edge;
            _edges[key2] = edge;
            
            // Добавляем в списки смежности обеих вершин
            _adjacencyList[from].Add(edge);
            _adjacencyList[to].Add(edge);
            
            return edge;
        }
        
        public bool RemoveEdge(T from, T to)
        {
            var key1 = (from, to);
            var key2 = (to, from);
            
            // Проверяем наличие ребра
            if (!_edges.ContainsKey(key1) && !_edges.ContainsKey(key2))
                return false;
                
            // Удаляем ребро из словаря
            var edgeToRemove = _edges.ContainsKey(key1) ? _edges[key1] : _edges[key2];
            _edges.Remove(key1);
            _edges.Remove(key2);
            
            // Удаляем из списков смежности
            _adjacencyList[from].Remove(edgeToRemove);
            _adjacencyList[to].Remove(edgeToRemove);
            
            return true;
        }
        
        public IEnumerable<Edge<T>> GetEdges(T vertexId)
        {
            return _adjacencyList.ContainsKey(vertexId) ? 
                _adjacencyList[vertexId] : Enumerable.Empty<Edge<T>>();
        }
        
        public bool HasEdge(T from, T to)
        {
            var key1 = (from, to);
            var key2 = (to, from);
            
            return _edges.ContainsKey(key1) || _edges.ContainsKey(key2);
        }
        
        public IEnumerable<IVertex<T>> GetNeighbors(T vertexId)
        {
            if (!_adjacencyList.ContainsKey(vertexId))
                return Enumerable.Empty<IVertex<T>>();
                
            // Для неориентированного графа возвращаем всех соседей
            return _adjacencyList[vertexId]
                .Select(e => e.From.Id.CompareTo(vertexId) == 0 ? e.To : e.From)
                .Cast<IVertex<T>>();
        }
        
        public double GetEdgeWeight(T from, T to)
        {
            var key1 = (from, to);
            var key2 = (to, from);
            
            if (_edges.ContainsKey(key1))
                return _edges[key1].Weight;
            if (_edges.ContainsKey(key2))
                return _edges[key2].Weight;
                
            return double.PositiveInfinity;
        }
        
        // Для неориентированного графа это то же самое, что GetNeighbors
        public IEnumerable<IVertex<T>> GetIncomingNeighbors(T vertexId)
        {
            return GetNeighbors(vertexId);
        }
        
        // Дополнительные методы для неориентированного графа
        
        public int GetDegree(T vertexId)
        {
            return _adjacencyList.ContainsKey(vertexId) ? _adjacencyList[vertexId].Count : 0;
        }
        
        public bool IsComplete()
        {
            int n = VertexCount;
            if (n == 0) return true;
            
            // В полном графе каждая вершина соединена со всеми остальными
            return _vertices.All(v => GetDegree(v.Key) == n - 1);
        }
        
        public bool IsRegular(int degree)
        {
            if (VertexCount == 0) return true;
            return _vertices.All(v => GetDegree(v.Key) == degree);
        }
        
        public bool IsBipartite()
        {
            if (VertexCount == 0) return true;
            
            var colors = new Dictionary<T, int>();
            var queue = new Queue<T>();
            
            // Начинаем с первой вершины
            var firstVertex = _vertices.Keys.First();
            colors[firstVertex] = 0;
            queue.Enqueue(firstVertex);
            
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                int currentColor = colors[current];
                
                foreach (var neighbor in GetNeighbors(current))
                {
                    if (!colors.ContainsKey(neighbor.Id))
                    {
                        // Красим соседа в противоположный цвет
                        colors[neighbor.Id] = 1 - currentColor;
                        queue.Enqueue(neighbor.Id);
                    }
                    else if (colors[neighbor.Id] == currentColor)
                    {
                        // Две смежные вершины одного цвета - граф не двудольный
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        public override string ToString()
        {
            return $"Неориентированный граф: {VertexCount} вершин, {EdgeCount} ребер";
        }
    }
}