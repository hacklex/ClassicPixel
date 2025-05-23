using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using PixelEditor.ViewModels;
using SelectionMode = PixelEditor.ViewModels.SelectionMode;

namespace PixelEditor
{
    public partial class MainWindow : Window
    {
        private MainViewModel? ViewModel => DataContext as MainViewModel;
        private int ImageWidth => ViewModel?.CanvasBitmap?.PixelSize.Width ?? 1;
        private int ImageHeight => ViewModel?.CanvasBitmap?.PixelSize.Height ?? 1;
        
        private bool _isDragging;
        private Point _lastPosition;
        private Color _lastMouseDownColor = Colors.Transparent;
        private Point _lastImagePosition;
        
        private SelectionMode _currentSelectionMode = SelectionMode.Replace;
        private bool[,]? _selectionMap;
        private bool _isSelecting;
        private Point _selectionStart;
        private int _selectionAnimationOffset;
        private readonly System.Timers.Timer _selectionAnimationTimer;
        
        public MainWindow()
        {
            InitializeComponent();
            
            // Center image when the window is loaded
            Loaded += MainWindow_Loaded;
            SizeChanged += MainWindow_SizeChanged;
            
            // Add keyboard event handling
            KeyDown += MainWindow_KeyDown;
            
            // Initialize selection animation timer
            _selectionAnimationTimer = new System.Timers.Timer(100); // Animation frame every 100 ms
            _selectionAnimationTimer.Elapsed += OnSelectionAnimationTick;
            _selectionAnimationTimer.AutoReset = true;
            
            // Initialize the selection path visual brush
            // In the constructor, update the initialization
             
            // Update selection overlay when selection properties change
            PropertyChanged += (_, e) =>
            {
                if (e.Property.Name != "DataContext" || DataContext is not MainViewModel) return;
                var vm = (MainViewModel)DataContext;
                vm.PropertyChanged += (_, args) => {
                    if (args.PropertyName == nameof(MainViewModel.HasSelection) ||
                        args.PropertyName == nameof(MainViewModel.SelectionStartX) ||
                        args.PropertyName == nameof(MainViewModel.SelectionStartY) ||
                        args.PropertyName == nameof(MainViewModel.SelectionEndX) ||
                        args.PropertyName == nameof(MainViewModel.SelectionEndY))
                    {
                        UpdateSelectionOverlay(EditorImage.Bounds.Width, EditorImage.Bounds.Height);
                    }
                };
            };
        }

        private void MainWindow_Loaded(object? sender, RoutedEventArgs e) => CenterCanvas();
        private void MainWindow_SizeChanged(object? sender, SizeChangedEventArgs e) => CenterCanvas();
        private void CenterCanvas_Click(object? sender, RoutedEventArgs e) => CenterCanvas();

        private void CenterCanvas()
        {
            if (EditorImage.Source == null) return;
            var x = (CanvasContainer.Bounds.Width - EditorImage.Bounds.Width) / 2;
            var y = (CanvasContainer.Bounds.Height - EditorImage.Bounds.Height) / 2;
                
            Canvas.SetLeft(EditorImage, x);
            Canvas.SetTop(EditorImage, y);
        } 

        private void Canvas_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (ViewModel is null) return;
            var point = e.GetCurrentPoint(ImageCanvas);
            
            // If the middle mouse button is pressed or Alt+Left button for canvas dragging
            if (point.Properties.IsMiddleButtonPressed || 
                (point.Properties.IsLeftButtonPressed && e.KeyModifiers.HasFlag(KeyModifiers.Alt) && 
                 !ViewModel.IsSelectionToolSelected && !ViewModel.IsMagicWandToolSelected))
            {
                _isDragging = true;
                _lastPosition = e.GetPosition(CanvasContainer);
                _lastImagePosition = new(Canvas.GetLeft(EditorImage), Canvas.GetTop(EditorImage));
                ImageCanvas.Cursor = new Cursor(StandardCursorType.DragMove);
                e.Handled = true;
            }
            // Otherwise, handle normal drawing/selection
            else
            {
                // Convert point coordinates to account for EditorImage position and scale
                Point adjustedPoint = GetImageCoordinates(point.Position);
                int x = (int)adjustedPoint.X;
                int y = (int)adjustedPoint.Y;
                bool isLeftButton = point.Properties.IsLeftButtonPressed;
                bool isCtrlPressed = e.KeyModifiers.HasFlag(KeyModifiers.Control);
                bool isAltPressed = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
                _lastMouseDownColor = ViewModel.GetPixelColor(x, y);
        
                switch (ViewModel.SelectedTool)
                {
                    case ToolType.Selection:
                        ViewModel.SelectionStartCommand.Execute(new PixelEventArgs(x, y, isLeftButton, _lastMouseDownColor, isCtrlPressed, isAltPressed));
                        _selectionStart = adjustedPoint;
                        _isSelecting = true;
                        
                        // Initialize the selection overlay when starting selection
                        UpdateSelectionOverlay(EditorImage.Bounds.Width, EditorImage.Bounds.Height);
                        break;
                        
                    case ToolType.MagicWand:
                        ViewModel.MagicWandSelectCommand.Execute(new PixelEventArgs(x, y, isLeftButton, _lastMouseDownColor, isCtrlPressed, isAltPressed));
                        
                        // Update the selection overlay for magic wand selection
                        UpdateSelectionOverlay(EditorImage.Bounds.Width, EditorImage.Bounds.Height);
                        break;
                        
                    case ToolType.ColorPicker:
                        Color pickedColor = ViewModel.GetPixelColor(x, y);
                        if (isLeftButton)
                        {
                            ViewModel.PrimaryColor = pickedColor;
                            ViewModel.StatusText = $"Primary color set to: {pickedColor}";
                        }
                        else
                        {
                            ViewModel.SecondaryColor = pickedColor;
                            ViewModel.StatusText = $"Secondary color set to: {pickedColor}";
                        }
                        break;
                        
                    default:
                        // Drawing tools (Pencil, Fill, Eraser)
                        ViewModel.DrawPixelCommand.Execute(new PixelEventArgs(x, y, isLeftButton, _lastMouseDownColor));
                        break;
                }
            }
        }

        private Point GetImageCoordinates(Point canvasPoint)
        {
            // Convert canvas coordinates to image coordinates, accounting for position and scale
            return new Point(
                (canvasPoint.X - Canvas.GetLeft(EditorImage)) * ImageWidth / EditorImage.Bounds.Width,
                (canvasPoint.Y - Canvas.GetTop(EditorImage)) * ImageHeight / EditorImage.Bounds.Height
            );
        }

        private void Canvas_PointerMoved(object sender, PointerEventArgs e)
        {
            if (ViewModel is null) return;
            // If dragging the image
            if (_isDragging)
            {
                var currentPos = e.GetPosition(CanvasContainer);

                // Calculate movement delta
                double deltaX = currentPos.X - _lastPosition.X;
                double deltaY = currentPos.Y - _lastPosition.Y;
 
                // Update the image transform
                var newPosition = _lastImagePosition + new Point(deltaX, deltaY);
 
                Canvas.SetLeft(EditorImage, newPosition.X);
                Canvas.SetTop(EditorImage, newPosition.Y);
                
                e.Handled = true;
            }
            // Regular drawing/selection
            else
            {
                var point = e.GetCurrentPoint(ImageCanvas);
                Point adjustedPoint = GetImageCoordinates(point.Position);
                int x = (int)adjustedPoint.X;
                int y = (int)adjustedPoint.Y;
                
                if (_isSelecting && ViewModel.SelectedTool == ToolType.Selection)
                {
                    bool isCtrlPressed = e.KeyModifiers.HasFlag(KeyModifiers.Control);
                    bool isAltPressed = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
                    
                    _currentSelectionMode = SelectionMode.Replace;
                    if (isCtrlPressed) _currentSelectionMode = SelectionMode.Add;
                    else if (isAltPressed) _currentSelectionMode = SelectionMode.Subtract;
                    
                    ViewModel.SelectionUpdateCommand.Execute(new SelectionEventArgs(
                        (int)_selectionStart.X, (int)_selectionStart.Y, x, y, _currentSelectionMode));
                    
                    // Update the selection overlay whenever selection changes
                    UpdateSelectionOverlay(EditorImage.Bounds.Width, EditorImage.Bounds.Height);
                }
                else
                {
                    ViewModel.UpdatePositionCommand.Execute(new PixelEventArgs(x, y, point.Properties.IsLeftButtonPressed, _lastMouseDownColor));
                    
                    // Draw if buttons are pressed
                    if (point.Properties.IsLeftButtonPressed || point.Properties.IsRightButtonPressed)
                    {
                        ViewModel.DrawPixelCommand.Execute(new PixelEventArgs(x, y, point.Properties.IsLeftButtonPressed, _lastMouseDownColor));
                    }
                }
            }
        }

        private void Canvas_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (ViewModel is null) return;
            // End dragging if the middle button was released or Alt+Left button
            if (_isDragging && (e.InitialPressMouseButton == MouseButton.Middle || 
                (e.InitialPressMouseButton == MouseButton.Left && e.KeyModifiers.HasFlag(KeyModifiers.Alt))))
            {
                _isDragging = false;
                ImageCanvas.Cursor = Cursor.Default;
                e.Handled = true;
            }
            // Handle the end of selection
            else if (_isSelecting && ViewModel.SelectedTool == ToolType.Selection)
            {
                var point = e.GetCurrentPoint(ImageCanvas);
                Point adjustedPoint = GetImageCoordinates(point.Position);
                int x = (int)adjustedPoint.X;
                int y = (int)adjustedPoint.Y;
                
                bool isCtrlPressed = e.KeyModifiers.HasFlag(KeyModifiers.Control);
                bool isAltPressed = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
                
                _currentSelectionMode = SelectionMode.Replace;
                if (isCtrlPressed) _currentSelectionMode = SelectionMode.Add;
                else if (isAltPressed) _currentSelectionMode = SelectionMode.Subtract;
                
                ViewModel.SelectionEndCommand.Execute(new SelectionEventArgs(
                    (int)_selectionStart.X, (int)_selectionStart.Y, x, y, _currentSelectionMode));
                
                _isSelecting = false;
                
                // Update the selection overlay after ending selection
                UpdateSelectionOverlay(EditorImage.Bounds.Width, EditorImage.Bounds.Height);
            }
            else
            {
                // Reset the initial right-click color when the mouse button is released
                if (e.InitialPressMouseButton == MouseButton.Right)
                {
                    var viewModelType = ViewModel.GetType();
                    var field = viewModelType.GetField("_initialRightClickColor", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (field != null)
                    {
                        field.SetValue(ViewModel, null);
                    }
                }
            }
        }

        private void CanvasContainer_PointerWheelChanged(object sender, PointerWheelEventArgs e)
        {
            // Handle zooming with Control+MouseWheel
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                e.Handled = true;
                
                // Get the current mouse position
                var mousePosition = e.GetPosition(EditorImage);
                var positionInCanvas = e.GetPosition(CanvasContainer);
                // Calculate the new scale (zoom in or out)
                double zoomFactor = e.Delta.Y > 0 ? 1.2 : 1 / 1.2;
                double newPositionX = mousePosition.X * zoomFactor;
                double newPositionY = mousePosition.Y * zoomFactor;
                
                var newLeft = positionInCanvas.X - newPositionX;
                var newTop = positionInCanvas.Y - newPositionY;

                Canvas.SetLeft(EditorImage, newLeft);
                Canvas.SetTop(EditorImage, newTop);
                
                var newWidth = EditorImage.Bounds.Width * zoomFactor;
                var newHeight = EditorImage.Bounds.Height * zoomFactor;

                EditorImage.Width = newWidth;
                EditorImage.Height = newHeight;
                
                // Update selection outline
                UpdateSelectionOverlay(newWidth, newHeight);
            }
        }

        // Redirect wheel events to the container handler
        private void Canvas_PointerWheelChanged(object sender, PointerWheelEventArgs e)
        {
            if (!e.Handled)
            {
                CanvasContainer_PointerWheelChanged(sender, e);
            }
        }
        
        // Update the UpdateSelectionOverlay method
        private void UpdateSelectionOverlay(double canvasWidth, double canvasHeight)
        {
            if (ViewModel == null || !ViewModel.HasSelection || 
                SelectionPathBlack == null || SelectionPathWhite == null)
            {
                if (_selectionAnimationTimer.Enabled)
                {
                    _selectionAnimationTimer.Stop();
                }
                return;
            }
                
            // Calculate the pixel size based on the image scale
            double pixelWidth = canvasWidth / ImageWidth;
            double pixelHeight = canvasHeight / ImageHeight;
            
            // Create the selection map if needed
            int selWidth = ImageWidth;
            int selHeight = ImageHeight;
            if (_selectionMap == null || _selectionMap.GetLength(0) != selWidth || _selectionMap.GetLength(1) != selHeight)
            {
                _selectionMap = new bool[selWidth, selHeight];
            }
            
            // Clear selection map
            for (int x = 0; x < selWidth; x++)
            {
                for (int y = 0; y < selHeight; y++)
                {
                    _selectionMap[x, y] = false;
                }
            }
            
            // Ask the ViewModel for all selection regions through a new method
            var selectionRegions = GetSelectionRegions();
            
            if (selectionRegions.Count == 0)
            {
                // If there are no regions yet, use the current selection rectangle being dragged
                int left = Math.Min(ViewModel.SelectionStartX, ViewModel.SelectionEndX);
                int top = Math.Min(ViewModel.SelectionStartY, ViewModel.SelectionEndY);
                int right = Math.Max(ViewModel.SelectionStartX, ViewModel.SelectionEndX);
                int bottom = Math.Max(ViewModel.SelectionStartY, ViewModel.SelectionEndY);
                
                for (int x = left; x <= right; x++)
                {
                    for (int y = top; y <= bottom; y++)
                    {
                        if (x >= 0 && x < selWidth && y >= 0 && y < selHeight)
                        {
                            _selectionMap[x, y] = true;
                        }
                    }
                }
            }
            else
            {
                // Fill the selection map with all selection regions
                foreach (var region in selectionRegions)
                {
                    for (int x = region.startX; x <= region.endX; x++)
                    {
                        for (int y = region.startY; y <= region.endY; y++)
                        {
                            if (x >= 0 && x < selWidth && y >= 0 && y < selHeight)
                            {
                                _selectionMap[x, y] = true;
                            }
                        }
                    }
                }
                
                // If we're currently selecting, also include the current selection rectangle
                if (_isSelecting)
                {
                    int left = Math.Min(ViewModel.SelectionStartX, ViewModel.SelectionEndX);
                    int top = Math.Min(ViewModel.SelectionStartY, ViewModel.SelectionEndY);
                    int right = Math.Max(ViewModel.SelectionStartX, ViewModel.SelectionEndX);
                    int bottom = Math.Max(ViewModel.SelectionStartY, ViewModel.SelectionEndY);
                    
                    for (int x = left; x <= right; x++)
                    {
                        for (int y = top; y <= bottom; y++)
                        {
                            if (x >= 0 && x < selWidth && y >= 0 && y < selHeight)
                            {
                                // Depending on the mode, add or subtract from selection
                                if (_currentSelectionMode == SelectionMode.Add)
                                {
                                    _selectionMap[x, y] = true;
                                }
                                else if (_currentSelectionMode == SelectionMode.Subtract)
                                {
                                    _selectionMap[x, y] = false;
                                }
                            }
                        }
                    }
                }
            }
            
            // Build the geometries for top-right and bottom-left selection outlines
            var pathGeo = new PathGeometry()
            {
                Figures = new PathFigures(),
            }; 
            
            // Check each cell in the selection map to find border edges
            for (int x = 0; x < selWidth; x++)
            {
                for (int y = 0; y < selHeight; y++)
                {
                    if (_selectionMap[x, y])
                    {
                        // Top edge (if the above pixel is not selected or is edge)
                        if (y == 0 || !_selectionMap[x, y-1])
                        {
                            var figure = new PathFigure
                            {
                                StartPoint = new Point(x * pixelWidth, y * pixelHeight),
                                IsClosed = false,
                                Segments = new PathSegments()
                            };
                            figure.Segments.Add(new LineSegment
                            {
                                Point = new Point((x + 1) * pixelWidth, y * pixelHeight)
                            });
                            pathGeo.Figures.Add(figure);
                        }
                        
                        // Right edge (if the right pixel is not selected or is edge)
                        if (x == selWidth - 1 || !_selectionMap[x+1, y])
                        {
                            var figure = new PathFigure
                            {
                                StartPoint = new Point((x + 1) * pixelWidth, y * pixelHeight),
                                IsClosed = false,
                                Segments = new PathSegments()
                            };
                            figure.Segments.Add(new LineSegment
                            {
                                Point = new Point((x + 1) * pixelWidth, (y + 1) * pixelHeight)
                            });
                            pathGeo.Figures.Add(figure);
                        }
                        
                        // Bottom edge (if the below pixel is not selected or is edge)
                        if (y == selHeight - 1 || !_selectionMap[x, y+1])
                        {
                            var figure = new PathFigure
                            {
                                StartPoint = new Point((x + 1) * pixelWidth, (y + 1) * pixelHeight),
                                IsClosed = false,
                                Segments = new PathSegments()
                            };
                            figure.Segments.Add(new LineSegment
                            {
                                Point = new Point(x * pixelWidth, (y + 1) * pixelHeight)
                            });
                            pathGeo.Figures.Add(figure);
                        }
                        
                        // Left edge (if the left pixel is not selected or is edge)
                        if (x == 0 || !_selectionMap[x-1, y])
                        {
                            var figure = new PathFigure
                            {
                                StartPoint = new Point(x * pixelWidth, (y + 1) * pixelHeight),
                                IsClosed = false,
                                Segments = new PathSegments()
                            };
                            figure.Segments.Add(new LineSegment
                            {
                                Point = new Point(x * pixelWidth, y * pixelHeight)
                            });
                            pathGeo.Figures.Add(figure);
                        }
                    }
                }
            }
            
            // Apply the geometries to the paths
            SelectionPathBlack.Data = pathGeo;
            SelectionPathWhite.Data = pathGeo.Clone();
            
            // Make sure the animation timer is running
            if (!_selectionAnimationTimer.Enabled)
            {
                _selectionAnimationTimer.Start();
            }
        }
        
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == Image.SourceProperty && change.Sender == EditorImage) 
                UpdateSelectionOverlay(EditorImage.Bounds.Width, EditorImage.Bounds.Height);
        }
        
        private void OnSelectionAnimationTick(object? sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _selectionAnimationOffset = (_selectionAnimationOffset + 1) % 16; 
                SelectionPathBlack.StrokeDashOffset = -_selectionAnimationOffset;
                SelectionPathWhite.StrokeDashOffset = 4-_selectionAnimationOffset;
            });
        }

        private Point _titlePointerPressLocation;
        private bool _isInWindowDrag;

        private void OnTitlePointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var posRelativeToTitle = e.GetPosition(CustomTitleBar);
            if (posRelativeToTitle.X < 0 || posRelativeToTitle.Y < 0) return;
            if (posRelativeToTitle.X >= CustomTitleBar.Bounds.Width || posRelativeToTitle.Y >= CustomTitleBar.Bounds.Height) return;
            _titlePointerPressLocation = e.GetPosition(this);
            _isInWindowDrag = true;
        }

        private void OnTitlePointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _isInWindowDrag = false;
        }

        private void OnTitlePointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isInWindowDrag) return;
            var cur = e.GetPosition(this);
            var delta = _titlePointerPressLocation - cur;
            this.Position = new PixelPoint( 
                (int)(this.Position.X - delta.X), (int)(this.Position.Y - delta.Y));
        }
        
        private List<(int startX, int startY, int endX, int endY)> GetSelectionRegions() => ViewModel?.SelectionRegions ?? [];
        
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (ViewModel == null) return;

            switch (e.Key)
            {
                case Key.S:
                    ViewModel.CycleSelectionTools();
                    e.Handled = true;
                    break;
                case Key.B:
                    ViewModel.CycleDrawingTools();
                    e.Handled = true;
                    break;
                case Key.F:
                    ViewModel.SelectFillTool();
                    e.Handled = true;
                    break;
                case Key.K:
                    ViewModel.SelectColorPickerTool();
                    e.Handled = true;
                    break;
                case Key.X:
                    ViewModel.SwapSelectedColors();
                    break;
            }
        }

        private void OnTransparentColorPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (ViewModel is null) return; 
            var isLeftButton = e.GetCurrentPoint(sender as Visual).Properties.IsLeftButtonPressed;
            if (isLeftButton)
                ViewModel.PrimaryColor = Colors.Transparent;
            else
                ViewModel.SecondaryColor = Colors.Transparent;
        }

        private void ToggleButtonIsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton toggleButton || ViewModel is null) return;
            if (!Enum.TryParse<ToolType>(toggleButton.Tag?.ToString() ?? "", out var toolType)) return;
            if (toggleButton.IsChecked != true && ViewModel.SelectedTool == toolType)
                toggleButton.IsChecked = true;
        }
    }
}