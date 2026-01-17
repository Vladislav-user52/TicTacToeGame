namespace TicTacToeGame.Models
{
    public class SimpleGameRules : GameRules
    {
        public SimpleGameRules()
        {
            
            FieldSize = int.MaxValue; 
            RequiredLineLength = 5; 
            
            
            UseSingleLineScoring = false;
            ResetScoreOnFullBlock = false;
            DisablePositionBonuses = true;
            CellWeightMultiplier = 1.0;
            LineLengthBonus = 0.0;
        }
        
        public override string ToString()
        {
            return $"Простые правила: Линия для победы = {RequiredLineLength}, Бесконечное поле";
        }
    }
}