using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Classic.Avalonia.Theme;
using PixelEditor.ViewModels;

namespace PixelEditor
{
    public partial class MainWindow : ClassicWindow
    {
        private bool _isDragging = false;
        private Point _lastPosition;
        private bool _isSelecting = false;
        private Point _selectionStart;
        private MainViewModel? ViewModel => DataContext as MainViewModel;
        private int ImageWidth => ViewModel?.CanvasBitmap?.PixelSize.Width ?? 1;
        private int ImageHeight => ViewModel?.CanvasBitmap?.PixelSize.Height ?? 1;
        
        // Image position and zoom properties
        private Point _lastImagePosition;
        
        // Selection animation fields
        private System.Timers.Timer _selectionAnimationTimer;
        private int _selectionAnimationOffset = 0;
        private bool[,]? _selectionMap;

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
                            UpdateSelectionOverlay();
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
            
            // If the middle mouse button is pressed or Alt+Left button, start dragging
            if (point.Properties.IsMiddleButtonPressed || 
                (point.Properties.IsLeftButtonPressed && e.KeyModifiers.HasFlag(KeyModifiers.Alt)))
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

                if (ViewModel.IsSelectionToolSelected)
                {
                    ViewModel.SelectionStartCommand.Execute(new PixelEventArgs(x, y, isLeftButton));
                    _selectionStart = adjustedPoint;
                    _isSelecting = true;
                    
                    // Initialize the selection overlay when starting selection
                    UpdateSelectionOverlay();
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
                    ViewModel.SelectionUpdateCommand.Execute(new SelectionEventArgs(
                        (int)_selectionStart.X, (int)_selectionStart.Y, x, y));
                    
                    // Update the selection overlay whenever selection changes
                    UpdateSelectionOverlay();
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
                
                ViewModel.SelectionEndCommand.Execute(new SelectionEventArgs(
                    (int)_selectionStart.X, (int)_selectionStart.Y, x, y));
                
                _isSelecting = false;
                
                // Update the selection overlay after ending selection
                UpdateSelectionOverlay();
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

                EditorImage.Width = EditorImage.Bounds.Width * zoomFactor;
                EditorImage.Height = EditorImage.Bounds.Height * zoomFactor;
                Canvas.SetLeft(EditorImage, newLeft);
                Canvas.SetTop(EditorImage, newTop);
                
                // Update selection outline
                UpdateSelectionOverlay();
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
        
        private void UpdateSelectionOverlay()
        {
            if (ViewModel == null || !ViewModel.HasSelection || SelectionPath == null)
            {
                if (_selectionAnimationTimer.Enabled)
                {
                    _selectionAnimationTimer.Stop();
                }
                return;
            }
                
            // Calculate the pixel size based on the image scale
            double pixelWidth = EditorImage.Bounds.Width / ImageWidth;
            double pixelHeight = EditorImage.Bounds.Height / ImageHeight;
            
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
            
            // Fill the selection map with the rectangular selection
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
            
            // Build the geometry for the selection outline
            var pathBuilder = new Avalonia.Media.PathGeometry();
            
            // Check each cell in the selection map to find border edges
            for (int x = 0; x < selWidth; x++)
            {
                for (int y = 0; y < selHeight; y++)
                {
                    if (_selectionMap[x, y])
                    {
                        // Check all four sides of the pixel
                        
                        // Top edge (if above pixel is not selected or is edge)
                        if (y == 0 || !_selectionMap[x, y-1])
                        {
                            var figure = new Avalonia.Media.PathFigure
                            {
                                StartPoint = new Point(x * pixelWidth, y * pixelHeight),
                                IsClosed = false
                            };
                            figure.Segments.Add(new Avalonia.Media.LineSegment
                            {
                                Point = new Point((x + 1) * pixelWidth, y * pixelHeight)
                            });
                            pathBuilder.Figures.Add(figure);
                        }
                        
                        // Right edge (if right pixel is not selected or is edge)
                        if (x == selWidth - 1 || !_selectionMap[x+1, y])
                        {
                            var figure = new Avalonia.Media.PathFigure
                            {
                                StartPoint = new Point((x + 1) * pixelWidth, y * pixelHeight),
                                IsClosed = false
                            };
                            figure.Segments.Add(new Avalonia.Media.LineSegment
                            {
                                Point = new Point((x + 1) * pixelWidth, (y + 1) * pixelHeight)
                            });
                            pathBuilder.Figures.Add(figure);
                        }
                        
                        // Bottom edge (if below pixel is not selected or is edge)
                        if (y == selHeight - 1 || !_selectionMap[x, y+1])
                        {
                            var figure = new Avalonia.Media.PathFigure
                            {
                                StartPoint = new Point((x + 1) * pixelWidth, (y + 1) * pixelHeight),
                                IsClosed = false
                            };
                            figure.Segments.Add(new Avalonia.Media.LineSegment
                            {
                                Point = new Point(x * pixelWidth, (y + 1) * pixelHeight)
                            });
                            pathBuilder.Figures.Add(figure);
                        }
                        
                        // Left edge (if left pixel is not selected or is edge)
                        if (x == 0 || !_selectionMap[x-1, y])
                        {
                            var figure = new Avalonia.Media.PathFigure
                            {
                                StartPoint = new Point(x * pixelWidth, (y + 1) * pixelHeight),
                                IsClosed = false
                            };
                            figure.Segments.Add(new Avalonia.Media.LineSegment
                            {
                                Point = new Point(x * pixelWidth, y * pixelHeight)
                            });
                            pathBuilder.Figures.Add(figure);
                        }
                    }
                }
            }
            
            // Apply the geometry to the path
            SelectionPath.Data = pathBuilder;
            
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
                UpdateSelectionOverlay();
            }
        }
        
        private void OnSelectionAnimationTick(object? sender, System.Timers.ElapsedEventArgs e)
        {
            // Dispatch to UI thread since we're modifying UI elements
            Dispatcher.UIThread.Post(() =>
            {
                if (SelectionPath?.Stroke is VisualBrush visualBrush)
                {
                    // Update the animation offset for the marching ants effect
                    _selectionAnimationOffset = (_selectionAnimationOffset + 1) % 8;
                    
                    // Update the brush to create the animated marching effect
                    visualBrush.DestinationRect = new Avalonia.RelativeRect(
                        _selectionAnimationOffset, 0, 8, 8, Avalonia.RelativeUnit.Absolute);
                }
            });
        }
    }
}