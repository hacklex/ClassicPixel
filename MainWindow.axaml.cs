using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using PixelEditorApp.ViewModels;
using System;
using System.Linq;
using Avalonia.Media;

namespace PixelEditorApp
{
    public partial class MainWindow : Window
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

        public MainWindow()
        {
            InitializeComponent();
            
            // Center image when window is loaded
            this.Loaded += MainWindow_Loaded;
            this.SizeChanged += MainWindow_SizeChanged;
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
                // Calculate new scale (zoom in or out)
                double zoomFactor = e.Delta.Y > 0 ? 1.2 : 1 / 1.2;
                double newPositionX = mousePosition.X * zoomFactor;
                double newPositionY = mousePosition.Y * zoomFactor;

                var oldLeft = Canvas.GetLeft(EditorImage);
                var oldTop = Canvas.GetTop(EditorImage);
                
                var newLeft = oldLeft + newPositionX - mousePosition.X;
                var newTop = oldTop + newPositionY - mousePosition.Y;

                EditorImage.Width = EditorImage.Bounds.Width * zoomFactor;
                EditorImage.Height = EditorImage.Bounds.Height * zoomFactor;
                Canvas.SetLeft(EditorImage, newLeft);
                Canvas.SetTop(EditorImage, newTop);
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
    }
}