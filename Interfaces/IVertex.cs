using System.Collections.Generic;

namespace TicTacToeGame.Interfaces
{
    public interface IVertex<T>
    {
        T Id { get; }
        string Label { get; }
        Dictionary<string, object> Properties { get; }
    }
}