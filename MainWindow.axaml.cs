using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Classic.Avalonia.Theme;
using PixelEditor.ViewModels;
using SelectionMode = PixelEditor.ViewModels.SelectionMode;

namespace PixelEditor
{
    public partial class MainWindow : Window
    {
        private bool _isDragging = false;
        private Point _lastPosition;
        private bool _isSelecting = false;
        private Point _selectionStart;
        private SelectionMode _currentSelectionMode = SelectionMode.Replace;
        private MainViewModel? ViewModel => DataContext as MainViewModel;
        private int ImageWidth => ViewModel?.CanvasBitmap?.PixelSize.Width ?? 1;
        private int ImageHeight => ViewModel?.CanvasBitmap?.PixelSize.Height ?? 1;
        
        // Image position and zoom properties
        private Point _lastImagePosition;
        
        // In MainWindow.axaml.cs, update the fields
        private int _selectionAnimationOffset = 0;
        private bool[,]? _selectionMap;

        // Selection animation fields
        private System.Timers.Timer _selectionAnimationTimer;
        
        public MainWindow()
        {
            InitializeComponent();
            
            // Center image when window is loaded
            this.Loaded += MainWindow_Loaded;
            this.SizeChanged += MainWindow_SizeChanged;
            
            // Initialize selection animation timer
            _selectionAnimationTimer = new System.Timers.Timer(100); // Animation frame every 100ms
            _selectionAnimationTimer.Elapsed += OnSelectionAnimationTick;
            _selectionAnimationTimer.AutoReset = true;
            
            // Initialize the selection path visual brush
            // In the constructor, update the initialization
             
            // Update selection overlay when selection properties change
            this.PropertyChanged += (s, e) => {
                if (e.Property?.Name == "DataContext" && DataContext is MainViewModel)
                {
                    var vm = (MainViewModel)DataContext;
                    vm.PropertyChanged += (sender, args) => {
                        if (args.PropertyName == nameof(MainViewModel.HasSelection) ||
                            args.PropertyName == nameof(MainViewModel.SelectionStartX) ||
                            args.PropertyName == nameof(MainViewModel.SelectionStartY) ||
                            args.PropertyName == nameof(MainViewModel.SelectionEndX) ||
                            args.PropertyName == nameof(MainViewModel.SelectionEndY))
                        {
                            UpdateSelectionOverlay(EditorImage.Bounds.Width, EditorImage.Bounds.Height);
                        }
                    };
                }
            };
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e) => CenterCanvas();
        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e) => CenterCanvas();
        private void CenterCanvas_Click(object sender, RoutedEventArgs e) => CenterCanvas();

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
                (point.Properties.IsLeftButtonPressed && e.KeyModifiers.HasFlag(KeyModifiers.Alt) && !ViewModel.IsSelectionToolSelected))
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
        
                if (ViewModel.IsSelectionToolSelected)
                {
                    ViewModel.SelectionStartCommand.Execute(new PixelEventArgs(x, y, isLeftButton, isCtrlPressed, isAltPressed));
                    _selectionStart = adjustedPoint;
                    _isSelecting = true;
                    
                    // Initialize the selection overlay when starting selection
                    UpdateSelectionOverlay(EditorImage.Bounds.Width, EditorImage.Bounds.Height);
                }
                else
                {
                    ViewModel.DrawPixelCommand.Execute(new PixelEventArgs(x, y, isLeftButton));
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
                
                if (_isSelecting && ViewModel.IsSelectionToolSelected)
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
                    ViewModel.UpdatePositionCommand.Execute(new PixelEventArgs(x, y, point.Properties.IsLeftButtonPressed));
                    
                    // Draw if buttons are pressed
                    if (point.Properties.IsLeftButtonPressed || point.Properties.IsRightButtonPressed)
                    {
                        ViewModel.DrawPixelCommand.Execute(new PixelEventArgs(x, y, point.Properties.IsLeftButtonPressed));
                    }
                }
            }
        }

        private void Canvas_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (ViewModel is null) return;
            // End dragging if middle button was released or Alt+Left button
            if (_isDragging && (e.InitialPressMouseButton == MouseButton.Middle || 
                (e.InitialPressMouseButton == MouseButton.Left && e.KeyModifiers.HasFlag(KeyModifiers.Alt))))
            {
                _isDragging = false;
                ImageCanvas.Cursor = Cursor.Default;
                e.Handled = true;
            }
            // Handle the end of selection
            else if (_isSelecting && ViewModel.IsSelectionToolSelected)
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
                // Calculate new scale (zoom in or out)
                double zoomFactor = e.Delta.Y > 0 ? 1.2 : 1 / 1.2;
                double newPositionX = mousePosition.X * zoomFactor;
                double newPositionY = mousePosition.Y * zoomFactor;

                var oldLeft = Canvas.GetLeft(EditorImage);
                var oldTop = Canvas.GetTop(EditorImage);
                
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
            
            // Create selection map if needed
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
                                // Depending on mode, add or subtract from selection
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
                        // Top edge (if above pixel is not selected or is edge)
                        if (y == 0 || !_selectionMap[x, y-1])
                        {
                            var figure = new Avalonia.Media.PathFigure
                            {
                                StartPoint = new Point(x * pixelWidth, y * pixelHeight),
                                IsClosed = false,
                                Segments = new PathSegments()
                            };
                            figure.Segments.Add(new Avalonia.Media.LineSegment
                            {
                                Point = new Point((x + 1) * pixelWidth, y * pixelHeight)
                            });
                            pathGeo.Figures.Add(figure);
                        }
                        
                        // Right edge (if right pixel is not selected or is edge)
                        if (x == selWidth - 1 || !_selectionMap[x+1, y])
                        {
                            var figure = new Avalonia.Media.PathFigure
                            {
                                StartPoint = new Point((x + 1) * pixelWidth, y * pixelHeight),
                                IsClosed = false,
                                Segments = new PathSegments()
                            };
                            figure.Segments.Add(new Avalonia.Media.LineSegment
                            {
                                Point = new Point((x + 1) * pixelWidth, (y + 1) * pixelHeight)
                            });
                            pathGeo.Figures.Add(figure);
                        }
                        
                        // Bottom edge (if below pixel is not selected or is edge)
                        if (y == selHeight - 1 || !_selectionMap[x, y+1])
                        {
                            var figure = new Avalonia.Media.PathFigure
                            {
                                StartPoint = new Point((x + 1) * pixelWidth, (y + 1) * pixelHeight),
                                IsClosed = false,
                                Segments = new PathSegments()
                            };
                            figure.Segments.Add(new Avalonia.Media.LineSegment
                            {
                                Point = new Point(x * pixelWidth, (y + 1) * pixelHeight)
                            });
                            pathGeo.Figures.Add(figure);
                        }
                        
                        // Left edge (if left pixel is not selected or is edge)
                        if (x == 0 || !_selectionMap[x-1, y])
                        {
                            var figure = new Avalonia.Media.PathFigure
                            {
                                StartPoint = new Point(x * pixelWidth, (y + 1) * pixelHeight),
                                IsClosed = false,
                                Segments = new PathSegments()
                            };
                            figure.Segments.Add(new Avalonia.Media.LineSegment
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
            
            // Make sure animation timer is running
            if (!_selectionAnimationTimer.Enabled)
            {
                _selectionAnimationTimer.Start();
            }
        }
        
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            
            // Update selection overlay when image bounds change
            if (change.Property == Image.SourceProperty && change.Sender == EditorImage)
            {
                UpdateSelectionOverlay(EditorImage.Bounds.Width, EditorImage.Bounds.Height);
            }
        }
        
        // Update the animation timer callback
        private void OnSelectionAnimationTick(object? sender, System.Timers.ElapsedEventArgs e)
        {
            // Dispatch to UI thread since we're modifying UI elements
            Dispatcher.UIThread.Post(() =>
            {
                // Update the animation offset for the marching ants effect
                _selectionAnimationOffset = (_selectionAnimationOffset + 1) % 16; 
                // Update top-right path - animate in one direction
                SelectionPathBlack.StrokeDashOffset = -_selectionAnimationOffset;
                SelectionPathWhite.StrokeDashOffset = 4-_selectionAnimationOffset;


                // if (SelectionPathTopRight?.Stroke is ImageBrush topRightBrush)
                // { 
                //     topRightBrush.DestinationRect = new Avalonia.RelativeRect(
                //         _selectionAnimationOffset, _selectionAnimationOffset, 16, 16, Avalonia.RelativeUnit.Absolute);
                // }
                //
                // // Update bottom-left path - animate in the opposite direction
                // if (SelectionPathBottomLeft?.Stroke is ImageBrush bottomLeftBrush)
                // {
                //     bottomLeftBrush.DestinationRect = new Avalonia.RelativeRect(
                //         16-_selectionAnimationOffset, 16-_selectionAnimationOffset, 16, 16, Avalonia.RelativeUnit.Absolute);
                // }
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
        
        // Helper method to access the selection regions from the ViewModel using reflection
        // (since we don't want to expose this as a public property)
        private List<(int startX, int startY, int endX, int endY)> GetSelectionRegions()
        {
            if (ViewModel == null) return new List<(int, int, int, int)>();
            
            var regionsField = ViewModel.GetType().GetField("_selectionRegions", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (regionsField != null)
            {
                var regions = regionsField.GetValue(ViewModel) as List<(int, int, int, int)>;
                if (regions != null)
                {
                    return regions;
                }
            }
            
            return new List<(int, int, int, int)>();
        }
    }
}