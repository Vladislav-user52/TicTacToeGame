using System;
using System.Collections.Generic;
using System.Linq;
using TicTacToeGame.Interfaces;
namespace TicTacToeGame.Models
{
    public class DirectedGraph<T> : IGraph<T, Edge<T>> where T : IComparable<T>
    {
        private readonly Dictionary<T, Vertex<T>> _vertices;
        private readonly Dictionary<T, List<Edge<T>>> _adjacencyList;
        private readonly Dictionary<(T, T), Edge<T>> _edges;
        
        public bool IsDirected => true;
        public int VertexCount => _vertices.Count;
        public int EdgeCount => _edges.Count;
        
        public IEnumerable<IVertex<T>> Vertices => _vertices.Values.Cast<IVertex<T>>();
        public IEnumerable<Edge<T>> AllEdges => _edges.Values;
        
        public DirectedGraph()
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
                
            var edgesToRemove = _edges.Where(e => 
                e.Key.Item1.CompareTo(id) == 0 || e.Key.Item2.CompareTo(id) == 0)
                .Select(e => e.Key).ToList();
                
            foreach (var key in edgesToRemove)
            {
                _edges.Remove(key);
            }
            
            foreach (var vertex in _vertices.Keys)
            {
                if (_adjacencyList.ContainsKey(vertex))
                {
                    _adjacencyList[vertex].RemoveAll(e => 
                        e.From.Id.CompareTo(id) == 0 || e.To.Id.CompareTo(id) == 0);
                }
            }
            
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
            
            var key = (from, to);
            
            if (_edges.ContainsKey(key))
            {
                _edges[key].Weight = weight;
                return _edges[key];
            }
            
            var edge = new Edge<T>(_vertices[from], _vertices[to], weight);
            _edges[key] = edge;
            _adjacencyList[from].Add(edge);
            
            return edge;
        }
        
        public bool RemoveEdge(T from, T to)
        {
            var key = (from, to);
            
            if (!_edges.ContainsKey(key))
                return false;
                
            var edge = _edges[key];
            _adjacencyList[from].Remove(edge);
            _edges.Remove(key);
            
            return true;
        }
        
        public IEnumerable<Edge<T>> GetEdges(T vertexId)
        {
            return _adjacencyList.ContainsKey(vertexId) ? 
                _adjacencyList[vertexId] : Enumerable.Empty<Edge<T>>();
        }
        
        public bool HasEdge(T from, T to)
        {
            return _edges.ContainsKey((from, to));
        }
        
        public IEnumerable<IVertex<T>> GetNeighbors(T vertexId)
        {
            if (!_adjacencyList.ContainsKey(vertexId))
                return Enumerable.Empty<IVertex<T>>();
                
            return _adjacencyList[vertexId].Select(e => e.To);
        }
        
        public double GetEdgeWeight(T from, T to)
        {
            var key = (from, to);
            return _edges.ContainsKey(key) ? _edges[key].Weight : double.PositiveInfinity;
        }
        
        public IEnumerable<IVertex<T>> GetIncomingNeighbors(T vertexId)
        {
            return _vertices.Values.Where(v => HasEdge(v.Id, vertexId)).Cast<IVertex<T>>();
        }
        
        public override string ToString()
        {
            return $"Ориентированный граф: {VertexCount} вершин, {EdgeCount} дуг";
        }
    }
}