namespace TicTacToeGame.Models
{
    public class GameRules
    {
        private int _fieldSize;
        private int _requiredLineLength = 5;
        
        public int FieldSize 
        { 
            get => _fieldSize; 
            set
            {
                _fieldSize = Math.Max(5, Math.Min(value, 15));
                _requiredLineLength = Math.Min(5, _fieldSize / 2 + 1);
            }
        }
        
        public int RequiredLineLength 
        {
            get => _requiredLineLength;
            set
            {
                _requiredLineLength = Math.Max(3, Math.Min(value, 7));
            }
        }
        
        
        public bool UseSingleLineScoring { get; set; } = true;
        public bool ResetScoreOnFullBlock { get; set; } = true;
        public bool DisablePositionBonuses { get; set; } = true;
        
        
        public double CellWeightMultiplier { get; set; } = 1.0;
        public double LineLengthBonus { get; set; } = 0.0; 
        
        // Порог для победы
        public double WinningScoreThreshold 
        { 
            get => _fieldSize * 20.0; 
        }
        
        
        public double BlockDetectionRange { get; set; } = 2.0; // На сколько клеток вперед проверять блокировку
        public bool RequireBothEndsBlocked { get; set; } = true; // Требовать блокировку с обоих концов
        
        // Настройки для ИИ
        public double BlockingImportance { get; set; } = 2.0;
        public double LineExtensionPriority { get; set; } = 1.5;
        
        public GameRules()
        {
            _fieldSize = 7;
            _requiredLineLength = 5;
            
            
            ApplySimplifiedRules();
        }
        
        public GameRules(int fieldSize)
        {
            FieldSize = fieldSize;
            ApplySimplifiedRules();
        }
        
        
        private void ApplySimplifiedRules()
        {
            UseSingleLineScoring = true;
            ResetScoreOnFullBlock = true;
            DisablePositionBonuses = true;
            LineLengthBonus = 0.0; 
        }
        
        
        public double CalculateLineScore(double totalWeight, int lineLength, bool isComplete, bool isBlocked)
        {
            if (DisablePositionBonuses)
            {
                
                if (isBlocked && ResetScoreOnFullBlock)
                {
                    return 0; 
                }
                
                
                double score = totalWeight * CellWeightMultiplier;
                
                return score;
            }
            else
            {
                
                double score = totalWeight;
                
                if (isComplete)
                {
                    score *= 2.0;
                    score += LineLengthBonus * lineLength;
                }
                
                return score;
            }
        }
        
        
        public bool IsLineFullyBlocked(int currentLength, int maxLength, bool frontBlocked, bool backBlocked)
        {
            if (!RequireBothEndsBlocked)
            {
                
                return frontBlocked || backBlocked;
            }
            
            
            return frontBlocked && backBlocked && currentLength >= 2;
        }
        
        
        public double GetMaxPossibleScore(int fieldSize)
        {
            
            int totalCells = (fieldSize * 2 + 1) * (fieldSize * 2 + 1);
            return totalCells * 10.0 * CellWeightMultiplier; 
        }
        
       
        public void ToggleRuleMode(bool simplifiedMode)
        {
            if (simplifiedMode)
            {
                ApplySimplifiedRules();
            }
            else
            {
                
                UseSingleLineScoring = false;
                ResetScoreOnFullBlock = false;
                DisablePositionBonuses = false;
                LineLengthBonus = 2.0;
            }
        }
        
        
        public GameRules Clone()
        {
            return new GameRules
            {
                _fieldSize = this._fieldSize,
                _requiredLineLength = this._requiredLineLength,
                UseSingleLineScoring = this.UseSingleLineScoring,
                ResetScoreOnFullBlock = this.ResetScoreOnFullBlock,
                DisablePositionBonuses = this.DisablePositionBonuses,
                CellWeightMultiplier = this.CellWeightMultiplier,
                LineLengthBonus = this.LineLengthBonus,
                BlockDetectionRange = this.BlockDetectionRange,
                RequireBothEndsBlocked = this.RequireBothEndsBlocked,
                BlockingImportance = this.BlockingImportance,
                LineExtensionPriority = this.LineExtensionPriority
            };
        }
        
        
        public override string ToString()
        {
            return $"Правила: Размер={FieldSize}, Длина линии={RequiredLineLength}, " +
                   $"Одна линия={UseSingleLineScoring}, Обнуление при блокировке={ResetScoreOnFullBlock}, " +
                   $"Без бонусов={DisablePositionBonuses}, Порог победы={WinningScoreThreshold:F1}";
        }
    }
}