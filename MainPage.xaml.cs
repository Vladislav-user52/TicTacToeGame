using Microsoft.Maui.Controls;
using TicTacToeGame.Models;
using TicTacToeGame.Algorithms;
using System.Collections.Generic;
using System.Linq;

namespace TicTacToeGame
{
    public partial class UnifiedMainPage : ContentPage
    {
        private GameMode _currentMode = GameMode.Classic;
        private List<TicTacToeBoard> _boardHistory;
        private List<InfiniteSimpleBoard> _infiniteBoardHistory;
        private int _currentBoardIndex;
        
        private Dictionary<(int, int), Frame> _cellFrames;
        private bool _showEmptyCells = true;
        private bool _autoExpand = true;
        private int _displayRadius = 3;
        
        private TicTacToeSolver _aiSolver;
        private SimpleSolver _simpleAiSolver;
        
        private bool _gameEnded;
        
        // Масштабирование
        private double _zoomLevel = 1.0;
        private double _minZoom = 0.25;
        private double _maxZoom = 4.0;
        private const double ZOOM_STEP = 0.25;
        
        // Адаптивные размеры
        private double _containerWidth = 600;
        private double _containerHeight = 400;
        private double _fieldBaseSize = 1000;
        
        // Флаги для платформ Apple
        private readonly bool _isIOS = DeviceInfo.Platform == DevicePlatform.iOS;
        private readonly bool _isMacOS = DeviceInfo.Platform == DevicePlatform.MacCatalyst;
        private readonly bool _isApplePlatform;
        
        // Цвета для фигур 
        private readonly Color XColor = Color.FromArgb("#FF3B30");
        private readonly Color OColor = Color.FromArgb("#007AFF");
        private readonly Color GridColor = Color.FromArgb("#C6C6C8");
        private readonly Color EmptyColor = Colors.White;
        private readonly Color HighlightColor = Color.FromArgb("#FFF9C4");
        
        // Цвета для весов 
        private readonly Color[] WeightColors = new[]
        {
            Color.FromArgb("#E8F4FD"), // Very light blue
            Color.FromArgb("#C7E6FF"), // Light blue
            Color.FromArgb("#90CAF9"), // Medium blue
            Color.FromArgb("#64B5F6"), // Blue
            Color.FromArgb("#2196F3"), // Apple blue
        };
        
        private TicTacToeBoard CurrentClassicBoard => 
            _boardHistory != null && _boardHistory.Count > 0 ? _boardHistory[_currentBoardIndex] : null;
            
        private InfiniteSimpleBoard CurrentInfiniteBoard => 
            _infiniteBoardHistory != null && _infiniteBoardHistory.Count > 0 ? 
            _infiniteBoardHistory[_currentBoardIndex] : null;
            
        private bool IsClassicMode => _currentMode == GameMode.Classic;
        private bool IsInfiniteMode => _currentMode == GameMode.Infinite;
        
        public UnifiedMainPage()
        {
            try
            {
                InitializeComponent();
                
                _isApplePlatform = _isIOS || _isMacOS;
                
                
                SetupAdaptiveParameters();
                
                
                SetupApplePlatformSpecifics();
                
                
                AddZoomGestures();
                
                
                SetupZoomLabels();
                
                
                this.SizeChanged += OnWindowSizeChanged;
                
                InitializeGame(GameMode.Classic);
            }
            catch (Exception ex)
            {
                DisplayAlert("Ошибка инициализации", $"Произошла ошибка при запуске: {ex.Message}", "OK");
            }
        }
        
        private void SetupApplePlatformSpecifics()
        {
            try
            {
                if (!_isApplePlatform) return;
                
                Dispatcher.Dispatch(() =>
                {
                    try
                    {
                        
                        if (_isIOS)
                        {
                            
                            if (DeviceInfo.Idiom == DeviceIdiom.Phone)
                            {
                                SetupForIPhone();
                            }
                            
                            else if (DeviceInfo.Idiom == DeviceIdiom.Tablet)
                            {
                                SetupForIPad();
                            }
                        }
                        
                        
                        if (_isMacOS)
                        {
                            SetupForMacOS();
                        }
                        
                        
                        if (ClassicModeButton != null && InfiniteModeButton != null)
                        {
                            ClassicModeButton.BackgroundColor = Color.FromArgb("#007AFF");
                            InfiniteModeButton.BackgroundColor = Color.FromArgb("#48484A");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка в SetupApplePlatformSpecifics: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в SetupApplePlatformSpecifics (внешний): {ex.Message}");
            }
        }
        
        private void SetupForIPhone()
        {
            try
            {
                
                if (MainStackLayout != null)
                {
                    MainStackLayout.Spacing = 8;
                }
                
                
                SetMinimumButtonSizes(44);
                
                
                if (ModeInfoLabel != null)
                {
                    ModeInfoLabel.FontSize = 11;
                    ModeInfoLabel.LineBreakMode = LineBreakMode.WordWrap;
                }
                
                
                if (WeightLegend != null)
                {
                    WeightLegend.IsVisible = false;
                }
                
                
                if (iOSZoomControlFrame != null)
                {
                    iOSZoomControlFrame.IsVisible = true;
                    if (ZoomControlFrame != null)
                        ZoomControlFrame.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в SetupForIPhone: {ex.Message}");
            }
        }
        
        private void SetupForIPad()
        {
            try
            {
                
                SetMinimumButtonSizes(50);
                
                
                if (WeightLegend != null)
                {
                    WeightLegend.IsVisible = true;
                }
                
                
                if (iOSZoomControlFrame != null && ZoomControlFrame != null)
                {
                    iOSZoomControlFrame.IsVisible = false;
                    ZoomControlFrame.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в SetupForIPad: {ex.Message}");
            }
        }
        
        private void SetupForMacOS()
        {
            try
            {
                
                SetMinimumButtonSizes(40);
                
                
                _maxZoom = 5.0;
                
                
                if (ClassicControls != null) ClassicControls.IsVisible = true;
                if (WeightLegend != null) WeightLegend.IsVisible = true;
                if (LinesInfoFrame != null) LinesInfoFrame.IsVisible = true;
                
                
                if (iOSZoomControlFrame != null && ZoomControlFrame != null)
                {
                    iOSZoomControlFrame.IsVisible = false;
                    ZoomControlFrame.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в SetupForMacOS: {ex.Message}");
            }
        }
        
        private void SetMinimumButtonSizes(double minHeight)
        {
            try
            {
                
                var buttons = new[] { 
                    NewGameButton, UndoButton, AIMoveButton, 
                    ClassicModeButton, InfiniteModeButton,
                    ToggleEmptyButton, ToggleAutoExpandButton,
                    ExpandButton, ShrinkButton
                };
                
                foreach (var button in buttons)
                {
                    if (button != null)
                    {
                        button.MinimumHeightRequest = minHeight;
                        
                        
                        if (_isIOS)
                        {
                            button.CornerRadius = 10;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в SetMinimumButtonSizes: {ex.Message}");
            }
        }
        
        private void SetupAdaptiveParameters()
        {
            try
            {
                
                if (_isMacOS)
                {
                    
                    _containerWidth = 650;
                    _containerHeight = 450;
                    _fieldBaseSize = 1400;
                    _minZoom = 0.3;
                    _maxZoom = 5.0;
                }
                else if (DeviceInfo.Idiom == DeviceIdiom.Desktop && !_isMacOS)
                {
                    
                    _containerWidth = 700;
                    _containerHeight = 450;
                    _fieldBaseSize = 1200;
                    _minZoom = 0.3;
                    _maxZoom = 4.5;
                }
                else if (DeviceInfo.Idiom == DeviceIdiom.Tablet)
                {
                    
                    if (_isIOS)
                    {
                        
                        _containerWidth = 550;
                        _containerHeight = 380;
                        _fieldBaseSize = 1100;
                    }
                    else
                    {
                        _containerWidth = 450;
                        _containerHeight = 320;
                        _fieldBaseSize = 900;
                    }
                    _minZoom = 0.25;
                    _maxZoom = 4.0;
                }
                else if (DeviceInfo.Idiom == DeviceIdiom.Phone)
                {
                    
                    if (_isIOS)
                    {
                        
                        _containerWidth = 350;
                        _containerHeight = 280;
                        _fieldBaseSize = 800;
                        _minZoom = 0.15;
                        _maxZoom = 4.0;
                    }
                    else 
                    {
                        _containerWidth = 340;
                        _containerHeight = 260;
                        _fieldBaseSize = 700;
                        _minZoom = 0.2;
                        _maxZoom = 3.5;
                    }
                }
                
                
                ApplyContainerSizes();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в SetupAdaptiveParameters: {ex.Message}");
            }
        }
        
        private void ApplyContainerSizes()
        {
            try
            {
                if (HorizontalScrollView != null)
                {
                    Dispatcher.Dispatch(() =>
                    {
                        try
                        {
                            
                            HorizontalScrollView.WidthRequest = _containerWidth;
                            if (VerticalScrollView != null)
                            {
                                VerticalScrollView.HeightRequest = _containerHeight;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка в ApplyContainerSizes (внутри Dispatch): {ex.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в ApplyContainerSizes: {ex.Message}");
            }
        }
        
        private void SetupZoomLabels()
        {
            try
            {
                
                if (ZoomLevelLabel != null && iOSZoomLevelLabel != null)
                {
                    
                    ZoomLevelLabel.PropertyChanged += (s, e) =>
                    {
                        try
                        {
                            if (e.PropertyName == nameof(Label.Text))
                            {
                                iOSZoomLevelLabel.Text = ZoomLevelLabel.Text;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка в обработчике PropertyChanged: {ex.Message}");
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в SetupZoomLabels: {ex.Message}");
            }
        }
        
        private void OnWindowSizeChanged(object sender, EventArgs e)
        {
            try
            {
                
                var width = this.Width;
                var height = this.Height;
                
                if (width > 0 && height > 0)
                {
                    // Динамическая адаптация для macOS окон
                    if (_isMacOS)
                    {
                        AdjustForMacOSWindow(width, height);
                    }
                    // iOS адаптация 
                    else if (_isIOS)
                    {
                        AdjustForIOSScreen(width, height);
                    }
                    
                    else
                    {
                        AdjustForOtherPlatforms(width, height);
                    }
                    
                    
                    ApplyContainerSizes();
                    
                    
                    CenterGameBoard();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в OnWindowSizeChanged: {ex.Message}");
            }
        }
        
        private void AdjustForMacOSWindow(double width, double height) {  }
        private void AdjustForIOSScreen(double width, double height) {  }
        private double GetIOSSafeAreaInset(double height) {  return 0; }
        private void AdjustForOtherPlatforms(double width, double height) {  }
        
        private void AddZoomGestures()
        {
            try
            {
                if (GameBoardContainer == null) return;
                
                
                var pinchGesture = new PinchGestureRecognizer();
                pinchGesture.PinchUpdated += OnPinchUpdated;
                GameBoardContainer.GestureRecognizers.Add(pinchGesture);
                
                
                var doubleTapGesture = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
                doubleTapGesture.Tapped += OnDoubleTapped;
                GameBoardContainer.GestureRecognizers.Add(doubleTapGesture);
                
                
                if (_isIOS && iOSZoomControlFrame != null)
                {
                    var iOSPinchGesture = new PinchGestureRecognizer();
                    iOSPinchGesture.PinchUpdated += OnPinchUpdated;
                    iOSZoomControlFrame.GestureRecognizers.Add(iOSPinchGesture);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в AddZoomGestures: {ex.Message}");
            }
        }
        
        private void OnPinchUpdated(object sender, PinchGestureUpdatedEventArgs e)
        {
            try
            {
                if (e.Status == GestureStatus.Started)
                {
                    // Фиксируем начальный зум
                }
                else if (e.Status == GestureStatus.Running)
                {
                    // Вычисляем новый уровень зума на основе масштаба жеста
                    double newZoom = _zoomLevel * e.Scale;
                    
                    // Ограничиваем минимальный и максимальный зум
                    newZoom = Math.Clamp(newZoom, _minZoom, _maxZoom);
                    
                    // Обновляем зум
                    _zoomLevel = newZoom;
                    ApplyZoom();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в OnPinchUpdated: {ex.Message}");
            }
        }
        
        private void OnDoubleTapped(object sender, EventArgs e)
        {
            try
            {
                // Двойной тап сбрасывает зум
                ResetZoom();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в OnDoubleTapped: {ex.Message}");
            }
        }
        
        private void InitializeGame(GameMode mode)
        {
            try
            {
                _currentMode = mode;
                _currentBoardIndex = 0;
                _gameEnded = false;
                _displayRadius = 3;
                _autoExpand = true;
                _showEmptyCells = true;
                _zoomLevel = 1.0;
                
                switch (mode)
                {
                    case GameMode.Classic:
                        InitializeClassicGame();
                        break;
                        
                    case GameMode.Infinite:
                        InitializeInfiniteGame();
                        break;
                }
                
                UpdateGameBoard();
                UpdateStatus();
                UpdateModeUI();
                ApplyZoom();
            }
            catch (Exception ex)
            {
                DisplayAlert("Ошибка инициализации игры", $"Не удалось инициализировать игру: {ex.Message}", "OK");
            }
        }
        
        private void InitializeClassicGame()
        {
            try
            {
                _boardHistory = new List<TicTacToeBoard>();
                _infiniteBoardHistory = null;
                _cellFrames = new Dictionary<(int, int), Frame>();
                _aiSolver = new TicTacToeSolver(4);
                _simpleAiSolver = null;
                
                AddNewBoardState(new TicTacToeBoard());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в InitializeClassicGame: {ex.Message}");
                throw;
            }
        }
        
        private void InitializeInfiniteGame()
        {
            try
            {
                _infiniteBoardHistory = new List<InfiniteSimpleBoard>();
                _boardHistory = null;
                _cellFrames = new Dictionary<(int, int), Frame>();
                _aiSolver = null;
                _simpleAiSolver = new SimpleSolver();
                
                AddNewBoardState(new InfiniteSimpleBoard());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в InitializeInfiniteGame: {ex.Message}");
                throw;
            }
        }
        
        private void AddNewBoardState(TicTacToeBoard board)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в AddNewBoardState (Classic): {ex.Message}");
                throw;
            }
        }
        
        private void AddNewBoardState(InfiniteSimpleBoard board)
        {
            try
            {
                if (_currentBoardIndex < _infiniteBoardHistory.Count - 1)
                {
                    _infiniteBoardHistory.RemoveRange(
                        _currentBoardIndex + 1, 
                        _infiniteBoardHistory.Count - _currentBoardIndex - 1
                    );
                }
                
                _infiniteBoardHistory.Add(board.Clone());
                _currentBoardIndex = _infiniteBoardHistory.Count - 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в AddNewBoardState (Infinite): {ex.Message}");
                throw;
            }
        }
        
        private void UpdateGameBoard()
        {
            try
            {
                if (GameBoardContainer == null) return;
                
                GameBoardContainer.Children.Clear();
                _cellFrames?.Clear();
                
                if (IsClassicMode)
                {
                    UpdateClassicGameBoard();
                }
                else if (IsInfiniteMode)
                {
                    UpdateInfiniteGameBoard();
                }
                
                ApplyZoom();
                CenterGameBoard();
            }
            catch (Exception ex)
            {
                DisplayAlert("Ошибка обновления поля", $"Не удалось обновить игровое поле: {ex.Message}", "OK");
            }
        }
        
        private void UpdateClassicGameBoard()
        {
            try
            {
                if (CurrentClassicBoard == null) return;
                
                var positions = CurrentClassicBoard.GetGeneratedField(_displayRadius);
                
                int gridSize = _displayRadius * 2 + 1;
                int cellSize = CalculateCellSize(gridSize);
                
                // Размер поля адаптивный
                int fieldSize = (int)Math.Max(gridSize * cellSize + 200, _fieldBaseSize);
                
                // Устанавливаем размер контейнера
                GameBoardContainer.WidthRequest = fieldSize;
                GameBoardContainer.HeightRequest = fieldSize;
                
                // Центрируем клетки в контейнере
                double centerX = fieldSize / 2.0;
                double centerY = fieldSize / 2.0;
                
                foreach (var (x, y, player) in positions)
                {
                    if (!_showEmptyCells && player == Player.None && 
                        !CurrentClassicBoard.FieldGenerator.IsPositionOccupied(x, y))
                        continue;
                        
                    var cellFrame = CreateCellFrame(x, y, player, cellSize);
                    
                    // Позиционируем клетки относительно центра
                    double posX = centerX + (x * cellSize);
                    double posY = centerY - (y * cellSize);
                    
                    AbsoluteLayout.SetLayoutBounds(cellFrame, 
                        new Rect(posX - cellSize/2, posY - cellSize/2, cellSize, cellSize));
                    
                    if (Math.Abs(x) == _displayRadius || Math.Abs(y) == _displayRadius)
                    {
                        cellFrame.BorderColor = Color.FromArgb("#FF9500");
                    }
                    
                    GameBoardContainer.Children.Add(cellFrame);
                    _cellFrames[(x, y)] = cellFrame;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в UpdateClassicGameBoard: {ex.Message}");
                throw;
            }
        }
        
        private void UpdateInfiniteGameBoard()
        {
            try
            {
                if (CurrentInfiniteBoard == null) return;
                
                var positions = CurrentInfiniteBoard.GetGeneratedField().ToList();
                
                if (!positions.Any())
                {
                    positions.Add((0, 0, Player.None));
                }
                
                int minX = positions.Min(p => p.x);
                int maxX = positions.Max(p => p.x);
                int minY = positions.Min(p => p.y);
                int maxY = positions.Max(p => p.y);
                
                int gridWidth = maxX - minX + 1;
                int gridHeight = maxY - minY + 1;
                
                int cellSize = CalculateCellSize(Math.Max(gridWidth, gridHeight));
                
                // Размер поля с запасом
                int fieldWidth = (int)Math.Max(gridWidth * cellSize + 400, _fieldBaseSize);
                int fieldHeight = (int)Math.Max(gridHeight * cellSize + 400, _fieldBaseSize);
                
                GameBoardContainer.WidthRequest = fieldWidth;
                GameBoardContainer.HeightRequest = fieldHeight;
                
                // Центрируем клетки в контейнере
                double centerX = fieldWidth / 2.0;
                double centerY = fieldHeight / 2.0;
                
                foreach (var (x, y, player) in positions)
                {
                    var cellFrame = CreateCellFrame(x, y, player, cellSize);
                    
                    // Позиционируем клетки относительно центра
                    double posX = centerX + (x * cellSize);
                    double posY = centerY - (y * cellSize);
                    
                    AbsoluteLayout.SetLayoutBounds(cellFrame, 
                        new Rect(posX - cellSize/2, posY - cellSize/2, cellSize, cellSize));
                    
                    GameBoardContainer.Children.Add(cellFrame);
                    _cellFrames[(x, y)] = cellFrame;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в UpdateInfiniteGameBoard: {ex.Message}");
                throw;
            }
        }
        
        private void CenterGameBoard()
        {
            try
            {
                // Прокручиваем к центру после обновления
                Dispatcher.Dispatch(async () =>
                {
                    try
                    {
                        await Task.Delay(50); // Даем время на обновление layout
                        
                        if (GameBoardContainer != null && GameBoardContainer.Width > 0 && GameBoardContainer.Height > 0)
                        {
                            // Вычисляем центр поля
                            double centerX = Math.Max(0, (GameBoardContainer.Width * _zoomLevel - _containerWidth) / 2);
                            double centerY = Math.Max(0, (GameBoardContainer.Height * _zoomLevel - _containerHeight) / 2);
                            
                            // Прокручиваем к центру
                            if (HorizontalScrollView != null)
                                await HorizontalScrollView.ScrollToAsync(centerX, 0, animated: false);
                            if (VerticalScrollView != null)
                                await VerticalScrollView.ScrollToAsync(0, centerY, animated: false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка в CenterGameBoard (внутри Dispatch): {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в CenterGameBoard: {ex.Message}");
            }
        }
        
        private int CalculateCellSize(int gridSize)
        {
            try
            {
                
                int baseSize;
                
                if (_isMacOS)
                {
                    baseSize = 50; 
                }
                else if (DeviceInfo.Idiom == DeviceIdiom.Desktop && !_isMacOS)
                {
                    baseSize = 48; 
                }
                else if (DeviceInfo.Idiom == DeviceIdiom.Tablet)
                {
                    if (_isIOS)
                    {
                        baseSize = 45; 
                    }
                    else
                    {
                        baseSize = 42; 
                    }
                }
                else 
                {
                    if (_isIOS)
                    {
                        
                        if (this.Width < 400)
                        {
                            baseSize = 35; // 
                        }
                        else
                        {
                            baseSize = 38; 
                        }
                    }
                    else
                    {
                        baseSize = 36; 
                    }
                }
                
                
                if (gridSize <= 7) return baseSize;
                if (gridSize <= 11) return baseSize - 8;
                if (gridSize <= 15) return baseSize - 12;
                if (gridSize <= 19) return baseSize - 15;
                if (gridSize <= 23) return baseSize - 18;
                if (gridSize <= 27) return baseSize - 20;
                return Math.Max(baseSize - 22, 12); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в CalculateCellSize: {ex.Message}");
                return 40; 
            }
        }
        
        private Frame CreateCellFrame(int x, int y, Player player, int cellSize)
        {
            try
            {
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += (s, e) => OnCellTapped(x, y);
                
                string displayText = GetPlayerSymbol(player);
                
                // Адаптивный размер шрифта
                double fontSize = cellSize * 0.4;
                if (_isIOS && DeviceInfo.Idiom == DeviceIdiom.Phone)
                {
                    fontSize = cellSize * 0.35; // Чуть меньше на iPhone для лучшей читаемости
                }
                
                var content = new Label
                {
                    Text = displayText,
                    FontSize = fontSize,
                    HorizontalTextAlignment = TextAlignment.Center,
                    VerticalTextAlignment = TextAlignment.Center,
                    TextColor = GetPlayerColor(player),
                    FontAttributes = FontAttributes.Bold
                };
                
                Color backgroundColor = GetCellBackgroundColor(x, y, player);
                
                var frame = new Frame
                {
                    Content = content,
                    BackgroundColor = backgroundColor,
                    BorderColor = GridColor,
                    CornerRadius = 5,
                    Padding = 2,
                    HasShadow = true
                };
                
                frame.GestureRecognizers.Add(tapGesture);
                
                string tooltip = $"({x}, {y})";
                if (IsClassicMode && player == Player.None)
                {
                    double weight = CurrentClassicBoard?.FieldGenerator?.GetCellWeight(x, y) ?? 1.0;
                    tooltip += $"\nВес: {weight:F1}";
                }
                ToolTipProperties.SetText(frame, tooltip);
                
                return frame;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в CreateCellFrame: {ex.Message}");
                
                // Возвращаем простой фрейм при ошибке
                return new Frame
                {
                    BackgroundColor = Colors.White,
                    BorderColor = GridColor,
                    CornerRadius = 5,
                    Padding = 2,
                    HasShadow = true,
                    Content = new Label { Text = "?" }
                };
            }
        }
        
        private Color GetCellBackgroundColor(int x, int y, Player player)
        {
            try
            {
                if (player != Player.None)
                    return EmptyColor;
                    
                if (x == 0 && y == 0)
                    return HighlightColor;
                
                if (IsClassicMode && CurrentClassicBoard != null)
                {
                    double weight = CurrentClassicBoard.FieldGenerator?.GetCellWeight(x, y) ?? 1.0;
                    return GetWeightColor(weight);
                }
                
                return EmptyColor;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в GetCellBackgroundColor: {ex.Message}");
                return EmptyColor;
            }
        }
        
        private Color GetWeightColor(double weight)
        {
            try
            {
                int index = (int)Math.Clamp(weight - 1, 0, WeightColors.Length - 1);
                return WeightColors[index];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в GetWeightColor: {ex.Message}");
                return WeightColors[0];
            }
        }
        
        private string GetPlayerSymbol(Player player)
        {
            try
            {
                return player switch
                {
                    Player.X => "✕",
                    Player.O => "○",
                    _ => ""
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в GetPlayerSymbol: {ex.Message}");
                return "?";
            }
        }
        
        private Color GetPlayerColor(Player player)
        {
            try
            {
                return player switch
                {
                    Player.X => XColor,
                    Player.O => OColor,
                    _ => Colors.Transparent
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в GetPlayerColor: {ex.Message}");
                return Colors.Gray;
            }
        }
        
        private async void OnCellTapped(int x, int y)
        {
            try
            {
                if (_gameEnded) return;
                
                bool moveMade = false;
                GameResult result = GameResult.None;
                
                if (IsClassicMode && CurrentClassicBoard != null)
                {
                    if (CurrentClassicBoard.CheckWinner() != GameResult.None)
                        return;
                        
                    var newBoard = CurrentClassicBoard.Clone();
                    if (newBoard.MakeMove(x, y))
                    {
                        if (_autoExpand && IsMoveOnBorder(x, y))
                        {
                            _displayRadius++;
                        }
                        
                        AddNewBoardState(newBoard);
                        moveMade = true;
                        result = newBoard.CheckWinner();
                    }
                }
                else if (IsInfiniteMode && CurrentInfiniteBoard != null)
                {
                    if (CurrentInfiniteBoard.CheckWinner() != GameResult.None)
                        return;
                        
                    var newBoard = CurrentInfiniteBoard.Clone();
                    if (newBoard.MakeMove(x, y))
                    {
                        AddNewBoardState(newBoard);
                        moveMade = true;
                        result = newBoard.CheckWinner();
                    }
                }
                
                if (moveMade)
                {
                    UpdateGameBoard();
                    UpdateStatus();
                    
                    if (result != GameResult.None)
                    {
                        _gameEnded = true;
                        await ShowGameResult(result);
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка хода", $"Не удалось сделать ход: {ex.Message}", "OK");
            }
        }
        
        private bool IsMoveOnBorder(int x, int y)
        {
            return Math.Abs(x) == _displayRadius || Math.Abs(y) == _displayRadius;
        }
        
        private void UpdateStatus()
        {
            try
            {
                if (IsClassicMode && CurrentClassicBoard != null)
                {
                    UpdateClassicStatus();
                }
                else if (IsInfiniteMode && CurrentInfiniteBoard != null)
                {
                    UpdateInfiniteStatus();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в UpdateStatus: {ex.Message}");
            }
        }
        
        private void UpdateClassicStatus()
        {
            try
            {
                if (CurrentClassicBoard == null) return;
                
                if (StatusLabel != null)
                    StatusLabel.Text = $"ХОД: {GetPlayerSymbol(CurrentClassicBoard.CurrentPlayer)}";
                if (CurrentPlayerLabel != null)
                    CurrentPlayerLabel.Text = GetPlayerSymbol(CurrentClassicBoard.CurrentPlayer);
                
                var stats = CurrentClassicBoard.GetGenerationStats();
                if (OccupiedLabel != null)
                    OccupiedLabel.Text = stats.occupied.ToString();
                if (MovesLabel != null)
                    MovesLabel.Text = $"ХОДОВ: {CurrentClassicBoard.MoveCount}";
                
                double xScore = CurrentClassicBoard.CalculatePlayerScore(Player.X);
                double oScore = CurrentClassicBoard.CalculatePlayerScore(Player.O);
                
                if (XScoreLabel != null)
                    XScoreLabel.Text = xScore.ToString("F1");
                if (OScoreLabel != null)
                    OScoreLabel.Text = oScore.ToString("F1");
                
                UpdateLineInfo();
                
                int requiredLength = CurrentClassicBoard.GetRequiredLineLength();
                if (WeightInfoLabel != null)
                    WeightInfoLabel.Text = $"Линия для победы: {requiredLength} клеток";
                
                int totalCells = (_displayRadius * 2 + 1) * (_displayRadius * 2 + 1);
                if (InfoLabel != null)
                    InfoLabel.Text = totalCells.ToString();
                if (RadiusLabel != null)
                    RadiusLabel.Text = _displayRadius.ToString();
                if (BorderInfoLabel != null)
                    BorderInfoLabel.Text = $"Граница: ±{_displayRadius}";
                
                CheckAndDisplayWinner(xScore, oScore);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в UpdateClassicStatus: {ex.Message}");
            }
        }
        
        private void UpdateLineInfo()
        {
            try
            {
                if (CurrentClassicBoard == null || XLineInfoLabel == null || OLineInfoLabel == null) return;
                
                var xLine = CurrentClassicBoard.GetBestLine(Player.X);
                var oLine = CurrentClassicBoard.GetBestLine(Player.O);
                
                int requiredLength = CurrentClassicBoard.GetRequiredLineLength();
                
                string xLineInfo = "Нет линии";
                if (xLine != null && xLine.IsActive && !xLine.IsFullyBlocked)
                {
                    xLineInfo = $"{xLine.Cells.Count}/{requiredLength} клеток";
                    if (xLine.IsComplete)
                        xLineInfo += " ✓";
                }
                else if (xLine != null && xLine.IsFullyBlocked)
                {
                    xLineInfo = "Заблокирована";
                }
                XLineInfoLabel.Text = xLineInfo;
                
                string oLineInfo = "Нет линии";
                if (oLine != null && oLine.IsActive && !oLine.IsFullyBlocked)
                {
                    oLineInfo = $"{oLine.Cells.Count}/{requiredLength} клеток";
                    if (oLine.IsComplete)
                        oLineInfo += " ✓";
                }
                else if (oLine != null && oLine.IsFullyBlocked)
                {
                    oLineInfo = "Заблокирована";
                }
                OLineInfoLabel.Text = oLineInfo;
                
                if (LinesInfoFrame != null)
                    LinesInfoFrame.IsVisible = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в UpdateLineInfo: {ex.Message}");
            }
        }
        
        private void UpdateInfiniteStatus()
        {
            try
            {
                if (CurrentInfiniteBoard == null) return;
                
                if (StatusLabel != null)
                    StatusLabel.Text = $"ХОД: {GetPlayerSymbol(CurrentInfiniteBoard.CurrentPlayer)}";
                if (CurrentPlayerLabel != null)
                    CurrentPlayerLabel.Text = GetPlayerSymbol(CurrentInfiniteBoard.CurrentPlayer);
                if (MovesLabel != null)
                    MovesLabel.Text = $"ХОДОВ: {CurrentInfiniteBoard.MoveCount}";
                
                if (OccupiedLabel != null)
                    OccupiedLabel.Text = CurrentInfiniteBoard.MoveCount.ToString();
                if (InfoLabel != null)
                    InfoLabel.Text = CurrentInfiniteBoard.GetGeneratedCellsCount().ToString();
                if (RadiusLabel != null)
                    RadiusLabel.Text = "∞";
                if (BorderInfoLabel != null)
                    BorderInfoLabel.Text = "Бесконечное поле";
                if (WeightInfoLabel != null)
                    WeightInfoLabel.Text = $"Нужно собрать: {CurrentInfiniteBoard.GetRequiredLineLength()} в ряд";
                
                if (XScoreLabel != null)
                    XScoreLabel.Text = "—";
                if (OScoreLabel != null)
                    OScoreLabel.Text = "—";
                if (LinesInfoFrame != null)
                    LinesInfoFrame.IsVisible = false;
                
                var result = CurrentInfiniteBoard.CheckWinner();
                if (result != GameResult.None && WinnerLabel != null)
                {
                    string message = result switch
                    {
                        GameResult.XWins => "✕ ПОБЕДИЛИ!",
                        GameResult.OWins => "○ ПОБЕДИЛИ!",
                        GameResult.Draw => "НИЧЬЯ!",
                        _ => ""
                    };
                    
                    WinnerLabel.Text = message;
                    WinnerLabel.IsVisible = true;
                }
                else if (WinnerLabel != null)
                {
                    WinnerLabel.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в UpdateInfiniteStatus: {ex.Message}");
            }
        }
        
        private void CheckAndDisplayWinner(double xScore, double oScore)
        {
            try
            {
                if (CurrentClassicBoard == null || WinnerLabel == null) return;
                
                var result = CurrentClassicBoard.CheckWinner();
                if (result != GameResult.None)
                {
                    string message = result switch
                    {
                        GameResult.XWins => $"✕ ПОБЕДИЛИ! Очки: {xScore:F1}",
                        GameResult.OWins => $"○ ПОБЕДИЛИ! Очки: {oScore:F1}",
                        GameResult.Draw => $"НИЧЬЯ! ✕ {xScore:F1} | ○ {oScore:F1}",
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
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в CheckAndDisplayWinner: {ex.Message}");
            }
        }
        
        private async Task ShowGameResult(GameResult result)
        {
            try
            {
                string message = result switch
                {
                    GameResult.XWins => "✕ ПОБЕДИЛИ КРЕСТИКИ!",
                    GameResult.OWins => "○ ПОБЕДИЛИ НОЛИКИ!",
                    GameResult.Draw => "НИЧЬЯ!",
                    _ => ""
                };
                
                if (WinnerLabel != null)
                {
                    WinnerLabel.Text = message;
                    WinnerLabel.IsVisible = true;
                }
                
                await DisplayAlert("ИГРА ОКОНЧЕНА!", message, "ОК");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в ShowGameResult: {ex.Message}");
            }
        }
        
        private void UpdateModeUI()
        {
            try
            {
                if (ClassicModeButton != null)
                    ClassicModeButton.BackgroundColor = IsClassicMode ? 
                        Color.FromArgb("#007AFF") : Color.FromArgb("#48484A");
                if (InfiniteModeButton != null)
                    InfiniteModeButton.BackgroundColor = IsInfiniteMode ? 
                        Color.FromArgb("#007AFF") : Color.FromArgb("#48484A");
                
                if (ClassicControls != null)
                    ClassicControls.IsVisible = IsClassicMode;
                if (WeightLegend != null)
                    WeightLegend.IsVisible = IsClassicMode;
                
                if (ModeInfoLabel != null)
                    ModeInfoLabel.Text = IsClassicMode ? 
                        "Классический режим: веса клеток отображаются цветом, победа по очкам" :
                        "Бесконечный режим: все клетки равны, победа при 5 в ряд";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в UpdateModeUI: {ex.Message}");
            }
        }
        
        // МАСШТАБИРОВАНИЕ
        private void OnZoomInClicked(object sender, EventArgs e)
        {
            try
            {
                ZoomIn();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в OnZoomInClicked: {ex.Message}");
            }
        }
        
        private void OnZoomOutClicked(object sender, EventArgs e)
        {
            try
            {
                ZoomOut();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в OnZoomOutClicked: {ex.Message}");
            }
        }
        
        private void OnZoomResetClicked(object sender, EventArgs e)
        {
            try
            {
                ResetZoom();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в OnZoomResetClicked: {ex.Message}");
            }
        }
        
        private void ZoomIn()
        {
            try
            {
                if (_zoomLevel < _maxZoom)
                {
                    _zoomLevel = Math.Min(_zoomLevel + ZOOM_STEP, _maxZoom);
                    ApplyZoom();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в ZoomIn: {ex.Message}");
            }
        }
        
        private void ZoomOut()
        {
            try
            {
                if (_zoomLevel > _minZoom)
                {
                    _zoomLevel = Math.Max(_zoomLevel - ZOOM_STEP, _minZoom);
                    ApplyZoom();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в ZoomOut: {ex.Message}");
            }
        }
        
        private void ResetZoom()
        {
            try
            {
                _zoomLevel = 1.0;
                ApplyZoom();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в ResetZoom: {ex.Message}");
            }
        }
        
        private void ApplyZoom()
        {
            try
            {
                if (GameBoardContainer == null || ZoomLevelLabel == null) return;
                
                // Применяем зум к контейнеру игрового поля
                GameBoardContainer.Scale = _zoomLevel;
                
                // Обновляем метку масштаба (обе метки)
                ZoomLevelLabel.Text = $"{_zoomLevel * 100:F0}%";
                if (iOSZoomLevelLabel != null)
                {
                    iOSZoomLevelLabel.Text = $"{_zoomLevel * 100:F0}%";
                }
                
                // Центрируем поле после зума
                CenterGameBoard();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в ApplyZoom: {ex.Message}");
            }
        }
        
        private void OnNewGameClicked(object sender, EventArgs e)
        {
            try
            {
                InitializeGame(_currentMode);
            }
            catch (Exception ex)
            {
                DisplayAlert("Ошибка", $"Не удалось начать новую игру: {ex.Message}", "OK");
            }
        }
        
        private void OnUndoClicked(object sender, EventArgs e)
        {
            try
            {
                if (_gameEnded) return;
                
                if (IsClassicMode)
                {
                    if (_currentBoardIndex > 0)
                    {
                        _currentBoardIndex--;
                        UpdateGameBoard();
                        UpdateStatus();
                    }
                }
                else if (IsInfiniteMode)
                {
                    if (_currentBoardIndex > 0)
                    {
                        _currentBoardIndex--;
                        UpdateGameBoard();
                        UpdateStatus();
                    }
                }
            }
            catch (Exception ex)
            {
                DisplayAlert("Ошибка", $"Не удалось отменить ход: {ex.Message}", "OK");
            }
        }
        
        private async void OnAIMoveClicked(object sender, EventArgs e)
        {
            try
            {
                if (_gameEnded) return;
                
                if (IsClassicMode && CurrentClassicBoard != null)
                {
                    if (CurrentClassicBoard.CheckWinner() != GameResult.None)
                        return;
                        
                    var bestMove = _aiSolver?.FindBestMove(CurrentClassicBoard, 1000);
                    
                    if (bestMove != null)
                    {
                        await Task.Delay(300);
                        
                        var newBoard = CurrentClassicBoard.Clone();
                        if (newBoard.MakeMove(bestMove.X, bestMove.Y))
                        {
                            if (_autoExpand && IsMoveOnBorder(bestMove.X, bestMove.Y))
                            {
                                _displayRadius++;
                            }
                            
                            AddNewBoardState(newBoard);
                            UpdateGameBoard();
                            UpdateStatus();
                            
                            var result = newBoard.CheckWinner();
                            if (result != GameResult.None)
                            {
                                _gameEnded = true;
                                await ShowGameResult(result);
                            }
                        }
                    }
                }
                else if (IsInfiniteMode && CurrentInfiniteBoard != null)
                {
                    if (CurrentInfiniteBoard.CheckWinner() != GameResult.None)
                        return;
                        
                    var bestMove = _simpleAiSolver?.FindBestMove(CurrentInfiniteBoard, 1000);
                    
                    if (bestMove != null)
                    {
                        await Task.Delay(300);
                        
                        var newBoard = CurrentInfiniteBoard.Clone();
                        if (newBoard.MakeMove(bestMove.X, bestMove.Y))
                        {
                            AddNewBoardState(newBoard);
                            UpdateGameBoard();
                            UpdateStatus();
                            
                            var result = newBoard.CheckWinner();
                            if (result != GameResult.None)
                            {
                                _gameEnded = true;
                                await ShowGameResult(result);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка ИИ", $"ИИ не смог сделать ход: {ex.Message}", "OK");
            }
        }
        
        private void OnToggleEmptyClicked(object sender, EventArgs e)
        {
            try
            {
                _showEmptyCells = !_showEmptyCells;
                var button = sender as Button;
                
                if (button != null)
                {
                    // Адаптивные тексты кнопок для разных платформ
                    if (_isIOS && DeviceInfo.Idiom == DeviceIdiom.Phone)
                    {
                        button.Text = _showEmptyCells ? "СКРЫТЬ" : "ПОКАЗАТЬ";
                    }
                    else
                    {
                        button.Text = _showEmptyCells ? "СКРЫТЬ ПУСТЫЕ" : "ПОКАЗАТЬ ВСЕ";
                    }
                }
                
                UpdateGameBoard();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в OnToggleEmptyClicked: {ex.Message}");
            }
        }
        
        private void OnToggleAutoExpandClicked(object sender, EventArgs e)
        {
            try
            {
                _autoExpand = !_autoExpand;
                var button = sender as Button;
                
                if (button != null)
                {
                    // Адаптивные тексты кнопок для разных платформ
                    if (_isIOS && DeviceInfo.Idiom == DeviceIdiom.Phone)
                    {
                        button.Text = _autoExpand ? "АВТО ВКЛ" : "АВТО ВЫКЛ";
                    }
                    else
                    {
                        button.Text = _autoExpand ? "АВТОРАСШИРЕНИЕ: ВКЛ" : "АВТОРАСШИРЕНИЕ: ВЫКЛ";
                    }
                    
                    button.BackgroundColor = _autoExpand ? 
                        Color.FromArgb("#34C759") : Color.FromArgb("#AEAEB2");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в OnToggleAutoExpandClicked: {ex.Message}");
            }
        }
        
        private void OnExpandClicked(object sender, EventArgs e)
        {
            try
            {
                _displayRadius++;
                UpdateGameBoard();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                DisplayAlert("Ошибка", $"Не удалось расширить поле: {ex.Message}", "OK");
            }
        }
        
        private void OnShrinkClicked(object sender, EventArgs e)
        {
            try
            {
                if (_displayRadius > 3)
                {
                    _displayRadius--;
                    UpdateGameBoard();
                    UpdateStatus();
                }
            }
            catch (Exception ex)
            {
                DisplayAlert("Ошибка", $"Не удалось уменьшить поле: {ex.Message}", "OK");
            }
        }
        
        private void OnSwitchToClassicClicked(object sender, EventArgs e)
        {
            try
            {
                if (_currentMode != GameMode.Classic)
                {
                    InitializeGame(GameMode.Classic);
                }
            }
            catch (Exception ex)
            {
                DisplayAlert("Ошибка", $"Не удалось переключиться в классический режим: {ex.Message}", "OK");
            }
        }
        
        private void OnSwitchToInfiniteClicked(object sender, EventArgs e)
        {
            try
            {
                if (_currentMode != GameMode.Infinite)
                {
                    InitializeGame(GameMode.Infinite);
                }
            }
            catch (Exception ex)
            {
                DisplayAlert("Ошибка", $"Не удалось переключиться в бесконечный режим: {ex.Message}", "OK");
            }
        }
    }
}