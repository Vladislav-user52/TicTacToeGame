using System.Collections.Generic;
namespace TicTacToeGame.Models
{
    public class Edge<T>
    {
        public Vertex<T> From { get; }
        public Vertex<T> To { get; }
        public double Weight { get; set; }
        public Dictionary<string, object> Properties { get; }
        
        public Edge(Vertex<T> from, Vertex<T> to, double weight = 1.0)
        {
            From = from;
            To = to;
            Weight = weight;
            Properties = new Dictionary<string, object>();
        }
        
        public override string ToString() => $"{From.Id} -> {To.Id} ({Weight})";
    }
}