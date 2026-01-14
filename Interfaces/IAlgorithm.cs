using System;

namespace TicTacToeGame.Interfaces
{
    public interface IAlgorithm
    {
        TimeSpan LastExecutionTime { get; }
    }
}