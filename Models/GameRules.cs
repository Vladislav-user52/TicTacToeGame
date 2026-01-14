namespace TicTacToeGame.Models
{
    public class GameRules
    {
        private int _fieldSize;
        
        
        private readonly int _requiredLineLength = 5;
        
        public int FieldSize 
        { 
            get => _fieldSize; 
            set
            {
                _fieldSize = Math.Max(5, Math.Min(value, 15)); // Ограничиваем от 5 до 15
              
            }
        }
        
        
        public int RequiredLineLength 
        {
            get => 5; // Всегда возвращаем 5
            set {  }
        }
        
        // Победные очки
        public double WinningScoreThreshold { get; set; } = 200.0;
        
        // Множитель очков за линию
        public double LineScoreMultiplier { get; set; } = 2.0;
        
        // Штраф за пересечение с линией противника
        public double LineIntersectionPenalty { get; set; } = 0.5;
        
        // Бонус за полную линию
        public double FullLineBonus { get; set; } = 20.0;
        
        public GameRules()
        {
            _fieldSize = 7; 
            
        }
        
        public GameRules(int fieldSize)
        {
            _fieldSize = Math.Max(5, Math.Min(fieldSize, 15));
            // RequiredLineLength всегда 5
        }
    }
}