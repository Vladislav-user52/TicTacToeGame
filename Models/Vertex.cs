using System.Collections.Generic;
using TicTacToeGame.Interfaces;
namespace TicTacToeGame.Models
{
    public class Vertex<T> : IVertex<T>
    {
        public T Id { get; }
        public string Label { get; set; }
        public Dictionary<string, object> Properties { get; }
        
        public Vertex(T id, string label = null)
        {
            Id = id;
            Label = label ?? id.ToString();
            Properties = new Dictionary<string, object>();
        }
        
        public override bool Equals(object obj)
        {
            return obj is Vertex<T> vertex && Id.Equals(vertex.Id);
        }
        
        public override int GetHashCode() => Id.GetHashCode();
    }
}