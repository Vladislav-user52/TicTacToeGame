using System.Collections.Generic;

namespace TicTacToeGame.Models
{
    public class WeightedCell
    {
        public int X { get; }
        public int Y { get; }
        public double Weight { get; set; }
        public Dictionary<string, object> Properties { get; }
        
        public WeightedCell(int x, int y, double weight = 1.0)
        {
            X = x;
            Y = y;
            Weight = weight;
            Properties = new Dictionary<string, object>();
        }
        
        public override string ToString() => $"({X}, {Y}): {Weight:F2}";
    }
}