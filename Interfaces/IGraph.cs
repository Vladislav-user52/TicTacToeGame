using System.Collections.Generic;

namespace TicTacToeGame.Interfaces
{
    public interface IGraph<TVertex, TEdge>
    {
        bool IsDirected { get; }
        int VertexCount { get; }
        int EdgeCount { get; }

        IVertex<TVertex> AddVertex(TVertex id, string label = null);
        bool RemoveVertex(TVertex id);
        IVertex<TVertex> GetVertex(TVertex id);
        IEnumerable<IVertex<TVertex>> Vertices { get; }

        TEdge AddEdge(TVertex from, TVertex to, double weight = 1.0);
        bool RemoveEdge(TVertex from, TVertex to);
        IEnumerable<TEdge> GetEdges(TVertex vertexId);
        IEnumerable<TEdge> AllEdges { get; }

        bool HasEdge(TVertex from, TVertex to);
        IEnumerable<IVertex<TVertex>> GetNeighbors(TVertex vertexId);
        double GetEdgeWeight(TVertex from, TVertex to);
    }
}