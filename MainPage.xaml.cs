using Microsoft.Maui.Controls;
using TicTacToeGame.Algorithms;
using TicTacToeGame.Models;
using System.Collections.Generic;
using System.Linq;

namespace TicTacToeGame
{
    public partial class MainPage : ContentPage
    {
        private List<TicTacToeBoard> _boardHistory;
        private int _currentBoardIndex;
        private Dictionary<(int, int), Frame> _cellFrames;
        private bool _showEmptyCells = true;
        private bool _showWeights = false; 
        private int _displayRadius = 3;
        private TicTacToeSolver _aiSolver;
        private bool _autoExpand = true; 
        
        
        private TicTacToeBoard CurrentBoard => _boardHistory[_currentBoardIndex];
        
        // Цвета
        private readonly Color XColor = Color.FromArgb("#1E88E5");
        private readonly Color OColor = Color.FromArgb("#D32F2F");
        private readonly Color GridColor = Color.FromArgb("#BDBDBD");
        private readonly Color EmptyColor = Color.FromArgb("#F5F5F5");
        private readonly Color HighlightColor = Color.FromArgb("#FFF9C4");
        private readonly Color BorderColor = Colors.Red;
        
        public MainPage()
        {
            InitializeComponent();
            InitializeGame();
        }
        
        private void InitializeGame()
        {
            _boardHistory = new List<TicTacToeBoard>();
            _cellFrames = new Dictionary<(int, int), Frame>();
            _aiSolver = new TicTacToeSolver(4);
            _displayRadius = 3;
            _autoExpand = true;
            _showWeights = false;
            
            // Создаем начальное состояние
            AddNewBoardState(new TicTacToeBoard());
            
            UpdateGameBoard();
            UpdateStatus();
        }
        
        private void AddNewBoardState(TicTacToeBoard board)
        {
            
            if (_currentBoardIndex < _boardHistory.Count - 1)
            {
                _boardHistory.RemoveRange(
                    _currentBoardIndex + 1, 
                    _boardHistory.Count - _currentBoardIndex - 1
                );
            }
            
            _boardHistory.Add(board.Clone());
            _currentBoardIndex = _boardHistory.Count - 1;
        }
        
        private void UpdateGameBoard()
        {
            GameBoardContainer.Children.Clear();
            _cellFrames.Clear();
            
            var positions = CurrentBoard.GetGeneratedField(_displayRadius);
            
            
            int gridSize = _displayRadius * 2 + 1;
            int cellSize = CalculateCellSize(gridSize);
            int totalSize = gridSize * cellSize;
            
            GameBoardContainer.HeightRequest = totalSize + 40;
            GameBoardContainer.WidthRequest = totalSize + 40;
            
            double centerX = totalSize / 2.0;
            double centerY = totalSize / 2.0;
            
            
            foreach (var (x, y, player) in positions)
            {
                if (!_showEmptyCells && player == Player.None && 
                    !CurrentBoard.FieldGenerator.IsPositionOccupied(x, y))
                    continue;
                    
                var cellFrame = CreateCellFrame(x, y, player, cellSize);
                
                
                double posX = centerX + (x * cellSize);
                double posY = centerY - (y * cellSize); 
                
                AbsoluteLayout.SetLayoutBounds(cellFrame, 
                    new Rect(posX - cellSize/2, posY - cellSize/2, cellSize, cellSize));
                
                
                if (Math.Abs(x) == _displayRadius || Math.Abs(y) == _displayRadius)
                {
                    cellFrame.BorderColor = BorderColor;
                }
                
                GameBoardContainer.Children.Add(cellFrame);
                _cellFrames[(x, y)] = cellFrame;
            }
            
            
            if (!_cellFrames.ContainsKey((0, 0)))
            {
                var centerCell = CreateCellFrame(0, 0, Player.None, cellSize);
                AbsoluteLayout.SetLayoutBounds(centerCell, 
                    new Rect(centerX - cellSize/2, centerY - cellSize/2, cellSize, cellSize));
                GameBoardContainer.Children.Add(centerCell);
                _cellFrames[(0, 0)] = centerCell;
            }
            
            
            RadiusLabel.Text = _displayRadius.ToString();
            BorderInfoLabel.Text = $"Граница: ±{_displayRadius}";
        }
        
        private int CalculateCellSize(int gridSize)
        {
            
            if (gridSize <= 7) return 50;
            if (gridSize <= 15) return 40;
            if (gridSize <= 25) return 30;
            if (gridSize <= 35) return 25;
            return 20;
        }
        
        private Frame CreateCellFrame(int x, int y, Player player, int cellSize)
        {
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => OnCellTapped(x, y);
            
            
            double weight = CurrentBoard.FieldGenerator.GetCellWeight(x, y);
            
            
            string displayText;
            if (player != Player.None)
            {
                displayText = GetPlayerSymbol(player);
            }
            else
            {
                
                displayText = _showWeights ? weight.ToString("F1") : "";
            }
            
            var content = new Label
            {
                Text = displayText,
                FontSize = cellSize * (_showWeights ? 0.3 : 0.4), 
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
                TextColor = GetPlayerColor(player)
            };
            
            Color backgroundColor = CalculateWeightColor(weight);
            if (player != Player.None)
            {
                backgroundColor = EmptyColor;
            }
            else if (x == 0 && y == 0)
            {
                backgroundColor = HighlightColor;
            }
            
            var frame = new Frame
            {
                Content = content,
                BackgroundColor = backgroundColor,
                BorderColor = GridColor,
                CornerRadius = 5,
                Padding = 2,
                HasShadow = false
            };
            
            frame.GestureRecognizers.Add(tapGesture);
            
            // Добавляем координаты и вес в подсказку
            string tooltip = $"({x}, {y})";
            if (player == Player.None)
            {
                tooltip += $"\nВес: {weight:F2}";
            }
            ToolTipProperties.SetText(frame, tooltip);
            
            return frame;
        }
        
        private Color CalculateWeightColor(double weight)
        {
           
            
            double normalizedWeight = Math.Min(weight / 5.0, 1.0);
            
            
            byte red = (byte)(255 * (0.9 + normalizedWeight * 0.1));
            byte green = (byte)(255 * (0.9 - normalizedWeight * 0.3));
            byte blue = (byte)(200 * (0.8 - normalizedWeight * 0.3));
            
            
            return Color.FromRgba((int)red, (int)green, (int)blue, 255);
        }
        
        private string GetPlayerSymbol(Player player)
        {
            return player switch
            {
                Player.X => "✕",
                Player.O => "○",
                _ => ""
            };
        }
        
        private Color GetPlayerColor(Player player)
        {
            return player switch
            {
                Player.X => XColor,
                Player.O => OColor,
                _ => Colors.Transparent
            };
        }
        
        private Color GetCellBackgroundColor(int x, int y)
        {
            if (x == 0 && y == 0) return HighlightColor;
            
            var player = CurrentBoard.GetCell(x, y);
            if (player != Player.None) return EmptyColor;
            
            return CurrentBoard.FieldGenerator.IsPositionOccupied(x, y) 
                ? Color.FromArgb("#E0E0E0") 
                : EmptyColor;
        }
        
        private async void OnCellTapped(int x, int y)
        {
            if (CurrentBoard.CheckWinner() != GameResult.None)
                return;
                
            var newBoard = CurrentBoard.Clone();
            if (newBoard.MakeMove(x, y))
            {
                
                if (_autoExpand && IsMoveOnBorder(x, y))
                {
                    
                    _displayRadius++;
                    AutoExpandLabel.Text = $"Расширено до {_displayRadius}";
                }
                
                AddNewBoardState(newBoard);
                UpdateGameBoard();
                UpdateStatus();
                
                var result = CurrentBoard.CheckWinner();
                if (result != GameResult.None)
                {
                    await ShowGameResult(result);
                }
            }
        }
        
        // Проверяет, находится ли ход на границе текущего отображаемого поля
        private bool IsMoveOnBorder(int x, int y)
        {
            
            bool isOnXBorder = Math.Abs(x) == _displayRadius;
            bool isOnYBorder = Math.Abs(y) == _displayRadius;
            
            
            return isOnXBorder || isOnYBorder;
        }
        
        private void UpdateStatus()
        {
            StatusLabel.Text = $"Ход: {GetPlayerSymbol(CurrentBoard.CurrentPlayer)}";
            CurrentPlayerLabel.Text = GetPlayerSymbol(CurrentBoard.CurrentPlayer);
            
            var stats = CurrentBoard.GetGenerationStats();
            OccupiedLabel.Text = stats.occupied.ToString();
            FieldRadiusLabel.Text = stats.radius.ToString();
            MovesLabel.Text = CurrentBoard.MoveCount.ToString();
            
            // Отображаем информацию о линиях и очках
            double xScore = CurrentBoard.CalculatePlayerScore(Player.X);
            double oScore = CurrentBoard.CalculatePlayerScore(Player.O);
            int requiredLength = CurrentBoard.GetRequiredLineLength();
            
            // Проверяем, есть ли полные линии
            bool xHasCompleteLine = CurrentBoard.GetPlayerLines(Player.X).Any(l => l.IsComplete);
            bool oHasCompleteLine = CurrentBoard.GetPlayerLines(Player.O).Any(l => l.IsComplete);
            
            string statusInfo = $"Линия: {requiredLength} клеток\n";
            
            if (xHasCompleteLine)
                statusInfo += "✕ имеет полную линию\n";
            if (oHasCompleteLine)
                statusInfo += "○ имеет полную линию\n";
            
            statusInfo += $"Очки: ✕ {xScore:F1} | ○ {oScore:F1}";
            
            if (WeightInfoLabel != null)
            {
                WeightInfoLabel.Text = statusInfo;
            }
            
            // Проверяем победителя
            var result = CurrentBoard.CheckWinner();
            if (result != GameResult.None)
            {
                string message = result switch
                {
                    GameResult.XWins => $"✕ Победили! Очки: {xScore:F1}",
                    GameResult.OWins => $"○ Победили! Очки: {oScore:F1}",
                    GameResult.Draw => $"Ничья! ✕ {xScore:F1} | ○ {oScore:F1}",
                    _ => ""
                };
                
                WinnerLabel.Text = message;
                WinnerLabel.IsVisible = true;
            }
            else
            {
                WinnerLabel.IsVisible = false;
            }
        }
        
        private async Task ShowGameResult(GameResult result)
        {
            string message = result switch
            {
                GameResult.XWins => "✕ Победили Крестики!",
                GameResult.OWins => "○ Победили Нолики!",
                GameResult.Draw => "Ничья!",
                _ => ""
            };
            
            WinnerLabel.Text = message;
            WinnerLabel.IsVisible = true;
            
            await DisplayAlert("Игра окончена!", message, "OK");
        }
        
        // Обработчики кнопок
        private void OnNewGameClicked(object sender, EventArgs e)
        {
            InitializeGame();
        }
        
        private void OnUndoClicked(object sender, EventArgs e)
        {
            if (_currentBoardIndex > 0)
            {
                _currentBoardIndex--;
                UpdateGameBoard();
                UpdateStatus();
            }
        }
        
        private void OnSwitchPlayerClicked(object sender, EventArgs e)
        {
            
            var newBoard = CurrentBoard.Clone();
            
            
            var possibleMoves = newBoard.GetPossibleMoves();
            if (possibleMoves.Any())
            {
                var move = possibleMoves.First();
                newBoard.MakeMove(move.X, move.Y);
                
                
                AddNewBoardState(newBoard);
                
                UpdateGameBoard();
                UpdateStatus();
            }
            else
            {
                DisplayAlert("Информация", "Нет доступных ходов для смены игрока", "OK");
            }
        }
        
        private async void OnAIMoveClicked(object sender, EventArgs e)
        {
            if (CurrentBoard.CheckWinner() != GameResult.None)
                return;
                
            
            var bestMove = _aiSolver.FindBestMove(CurrentBoard, 1000);
            
            if (bestMove != null)
            {
                await Task.Delay(300);
                
                var newBoard = CurrentBoard.Clone();
                if (newBoard.MakeMove(bestMove.X, bestMove.Y))
                {
                    
                    if (_autoExpand && IsMoveOnBorder(bestMove.X, bestMove.Y))
                    {
                        
                        _displayRadius++;
                        AutoExpandLabel.Text = $"Расширено до {_displayRadius}";
                    }
                    
                    AddNewBoardState(newBoard);
                    UpdateGameBoard();
                    UpdateStatus();
                    
                    var result = CurrentBoard.CheckWinner();
                    if (result != GameResult.None)
                    {
                        await ShowGameResult(result);
                    }
                }
            }
        }
        
        private void OnShowMoreClicked(object sender, EventArgs e)
        {
            _displayRadius++;
            UpdateGameBoard();
        }
        
        private void OnToggleEmptyClicked(object sender, EventArgs e)
        {
            _showEmptyCells = !_showEmptyCells;
            var button = sender as Button;
            button.Text = _showEmptyCells ? "Скрыть пустые" : "Показать все";
            UpdateGameBoard();
        }
        
        
        private void OnToggleAutoExpandClicked(object sender, EventArgs e)
        {
            _autoExpand = !_autoExpand;
            var button = sender as Button;
            button.Text = _autoExpand ? "Авторасширение: ВКЛ" : "Авторасширение: ВЫКЛ";
            AutoExpandLabel.Text = _autoExpand ? "Авторасширение включено" : "Авторасширение выключено";
        }
        
        // Ручное расширение
        private void OnExpandClicked(object sender, EventArgs e)
        {
            _displayRadius++;
            UpdateGameBoard();
        }
        
        // Ручное сужение
        private void OnShrinkClicked(object sender, EventArgs e)
        {
            if (_displayRadius > 3)
            {
                _displayRadius--;
                UpdateGameBoard();
            }
        }
        
        // Сброс к минимальному размеру
        private void OnResetSizeClicked(object sender, EventArgs e)
        {
            _displayRadius = 3;
            UpdateGameBoard();
        }

        // Переключение отображения весов
        private void OnToggleWeightsClicked(object sender, EventArgs e)
        {
            _showWeights = !_showWeights;
            var button = sender as Button;
            button.Text = _showWeights ? "Скрыть веса" : "Показать веса";
            UpdateGameBoard();
            UpdateStatus();
        }
        
        // Показать детальную статистику весов
        private void OnShowWeightStatsClicked(object sender, EventArgs e)
        {
            var stats = CurrentBoard.FieldGenerator.GetWeightStats(_displayRadius);
            DisplayAlert("Статистика весов",
                $"Мин: {stats.min:F2}\n" +
                $"Макс: {stats.max:F2}\n" +
                $"Среднее: {stats.average:F2}", "OK");
        }
        
        
        private void OnShowWeightBoardClicked(object sender, EventArgs e)
        {
            var visualization = CurrentBoard.GetBoardVisualizationWithWeights(_displayRadius);
            DisplayAlert("Доска с весами", visualization, "OK");
        }
        
        // Сбросить веса
        private void OnResetWeightsClicked(object sender, EventArgs e)
        {
            CurrentBoard.FieldGenerator.ResetWeights();
            UpdateGameBoard();
            DisplayAlert("Веса сброшены", "Все веса клеток сброшены к значениям по умолчанию", "OK");
        }
        
        // Метод для отображения статистики поиска AI
        private void OnShowStatsClicked(object sender, EventArgs e)
        {
            var stats = _aiSolver.GetStats();
            DisplayAlert("Статистика AI", 
                $"Оценено узлов: {stats.nodesEvaluated}\n" +
                $"Сгенерировано позиций: {stats.positionsGenerated}\n" +
                $"Время выполнения: {stats.executionTime.TotalMilliseconds:F2} мс", 
                "OK");
        }
        
        
        private void OnShowTopWeightedCellsClicked(object sender, EventArgs e)
        {
            var topCells = CurrentBoard.FieldGenerator.GetTopWeightedCells(10, _displayRadius).ToList();
            
            if (topCells.Count == 0)
            {
                DisplayAlert("Топ клеток", "Нет свободных клеток для отображения", "OK");
                return;
            }
            
            var message = "Топ 10 клеток по весу:\n";
            for (int i = 0; i < topCells.Count; i++)
            {
                var cell = topCells[i];
                message += $"{i + 1}. ({cell.X}, {cell.Y}) - {cell.Weight:F2}\n";
            }
            
            DisplayAlert("Топ клеток по весу", message, "OK");
        }
        
        // Добавляем метод для отображения информации о линиях
        private async void OnShowLinesInfoClicked(object sender, EventArgs e)
        {
            var xLines = CurrentBoard.GetPlayerLines(Player.X).ToList();
            var oLines = CurrentBoard.GetPlayerLines(Player.O).ToList();
            
            string message = "Линии игроков:\n\n";
            
            message += "Крестики (✕):\n";
            if (xLines.Any())
            {
                foreach (var line in xLines.OrderByDescending(l => l.CalculateScore()).Take(3))
                {
                    message += $"- {line.Cells.Count} клеток, очки: {line.CalculateScore():F1}\n";
                }
            }
            else
            {
                message += "Нет линий\n";
            }
            
            message += "\nНолики (○):\n";
            if (oLines.Any())
            {
                foreach (var line in oLines.OrderByDescending(l => l.CalculateScore()).Take(3))
                {
                    message += $"- {line.Cells.Count} клеток, очки: {line.CalculateScore():F1}\n";
                }
            }
            else
            {
                message += "Нет линий\n";
            }
            
            // Очки
            message += $"\nОбщие очки:\n";
            message += $"✕: {CurrentBoard.CalculatePlayerScore(Player.X):F1}\n";
            message += $"○: {CurrentBoard.CalculatePlayerScore(Player.O):F1}";
            
            await DisplayAlert("Информация о линиях и очках", message, "OK");
        }
    }
}