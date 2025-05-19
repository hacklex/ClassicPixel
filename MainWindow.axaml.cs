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
        private Canvas _imageCanvas;
        private Image _editorImage;
        private Grid _canvasContainer;
        private bool _isDragging = false;
        private Point _lastPosition;
        private bool _isSelecting = false;
        private Point _selectionStart;
        private MainViewModel ViewModel => DataContext as MainViewModel;

        public MainWindow()
        {
            InitializeComponent();
            
            _imageCanvas = this.FindControl<Canvas>("ImageCanvas");
            _editorImage = this.FindControl<Image>("EditorImage");
            _canvasContainer = this.FindControl<Grid>("CanvasContainer");
            
            // Initialize with identity transform
            _imageCanvas.RenderTransform = new TransformGroup
            {
                Children = new Transforms
                {
                    new TranslateTransform(0, 0),
                    new ScaleTransform(1.0, 1.0)
                }
            };
            
            // Center image when window is loaded
            this.Loaded += MainWindow_Loaded;
            this.SizeChanged += MainWindow_SizeChanged;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CenterCanvas();
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CenterCanvas();
        }

        private void CenterCanvas_Click(object sender, RoutedEventArgs e)
        {
            CenterCanvas();
        }

        private void CenterCanvas()
        {
            if (_editorImage.Source != null)
            {
                // Get transform group
                var transformGroup = _imageCanvas.RenderTransform as TransformGroup;
                var translateTransform = transformGroup?.Children.OfType<TranslateTransform>().FirstOrDefault();
                var scaleTransform = transformGroup?.Children.OfType<ScaleTransform>().FirstOrDefault();
                
                if (translateTransform != null && scaleTransform != null)
                {
                    // Calculate center position based on container size and image size with current scale
                    double scale = scaleTransform.ScaleX;
                    double scaledWidth = _editorImage.Bounds.Width * scale;
                    double scaledHeight = _editorImage.Bounds.Height * scale;
                    
                    double centerX = (_canvasContainer.Bounds.Width - scaledWidth) / 2;
                    double centerY = (_canvasContainer.Bounds.Height - scaledHeight) / 2;
                    
                    // Update translation
                    translateTransform.X = centerX;
                    translateTransform.Y = centerY;
                }
            }
        }

        private void Canvas_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            var point = e.GetCurrentPoint(_imageCanvas);
            
            // If the middle mouse button is pressed or Alt+Left button, start dragging
            if (point.Properties.IsMiddleButtonPressed || 
                (point.Properties.IsLeftButtonPressed && e.KeyModifiers.HasFlag(KeyModifiers.Alt)))
            {
                _isDragging = true;
                _lastPosition = e.GetPosition(_canvasContainer);
                _imageCanvas.Cursor = new Cursor(StandardCursorType.DragMove);
                e.Handled = true;
            }
            // Otherwise, handle normal drawing/selection
            else
            {
                int x = (int)point.Position.X;
                int y = (int)point.Position.Y;
                bool isLeftButton = point.Properties.IsLeftButtonPressed;
                
                if (ViewModel.IsSelectionToolSelected)
                {
                    ViewModel.SelectionStartCommand.Execute(new PixelEventArgs(x, y, isLeftButton));
                    _selectionStart = new Point(x, y);
                    _isSelecting = true;
                }
                else
                {
                    ViewModel.DrawPixelCommand.Execute(new PixelEventArgs(x, y, isLeftButton));
                }
            }
        }

        private void Canvas_PointerMoved(object sender, PointerEventArgs e)
        {
            // If dragging the canvas
            if (_isDragging)
            {
                var currentPos = e.GetPosition(_canvasContainer);
                
                // Get transform group
                var transformGroup = _imageCanvas.RenderTransform as TransformGroup;
                var translateTransform = transformGroup?.Children.OfType<TranslateTransform>().FirstOrDefault();
                
                if (translateTransform != null)
                {
                    // Calculate movement delta
                    double deltaX = currentPos.X - _lastPosition.X;
                    double deltaY = currentPos.Y - _lastPosition.Y;
                    
                    // Move canvas by adjusting the translate transform
                    translateTransform.X += deltaX;
                    translateTransform.Y += deltaY;
                    
                    _lastPosition = currentPos;
                    e.Handled = true;
                }
            }
            // Regular drawing/selection
            else
            {
                var point = e.GetCurrentPoint(_imageCanvas);
                int x = (int)point.Position.X;
                int y = (int)point.Position.Y;
                
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
            // End dragging if middle button was released or Alt+Left button
            if (_isDragging && (e.InitialPressMouseButton == MouseButton.Middle || 
                (e.InitialPressMouseButton == MouseButton.Left && e.KeyModifiers.HasFlag(KeyModifiers.Alt))))
            {
                _isDragging = false;
                _imageCanvas.Cursor = Cursor.Default;
                e.Handled = true;
            }
            // Handle selection end
            else if (_isSelecting && ViewModel.IsSelectionToolSelected)
            {
                var point = e.GetCurrentPoint(_imageCanvas);
                int x = (int)point.Position.X;
                int y = (int)point.Position.Y;
                
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
                var mousePosition = e.GetPosition(_imageCanvas);
                
                // Get transform group
                var transformGroup = _imageCanvas.RenderTransform as TransformGroup;
                var translateTransform = transformGroup?.Children.OfType<TranslateTransform>().FirstOrDefault();
                var scaleTransform = transformGroup?.Children.OfType<ScaleTransform>().FirstOrDefault();
                
                if (translateTransform != null && scaleTransform != null)
                {
                    // Get current scale
                    double currentScale = scaleTransform.ScaleX;
                    
                    // Calculate new scale (zoom in or out)
                    double zoomFactor = e.Delta.Y > 0 ? 1.2 : 1 / 1.2;
                    double newScale = currentScale * zoomFactor;
                    
                    // Constrain scale to reasonable limits
                    newScale = Math.Max(0.1, Math.Min(10.0, newScale));
                    
                    // Calculate the point position before zoom
                    double mouseX = mousePosition.X;
                    double mouseY = mousePosition.Y;
                    
                    // Calculate the position after zoom
                    double newMouseX = mouseX * newScale / currentScale;
                    double newMouseY = mouseY * newScale / currentScale;
                    
                    // Adjust translation to keep the position under cursor stable
                    translateTransform.X -= (newMouseX - mouseX);
                    translateTransform.Y -= (newMouseY - mouseY);
                    
                    // Apply the new scale
                    scaleTransform.ScaleX = newScale;
                    scaleTransform.ScaleY = newScale;
                }
            }
        }

        // Keep your existing Canvas_PointerWheelChanged method but redirect to the container handler
        private void Canvas_PointerWheelChanged(object sender, PointerWheelEventArgs e)
        {
            if (!e.Handled)
            {
                CanvasContainer_PointerWheelChanged(sender, e);
            }
        }
    }
}