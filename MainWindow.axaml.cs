using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PixelEditorApp
{
    public partial class MainWindow : Window
    {
        private PixelEditor _pixelEditor;
        private Point _lastPosition;
        private bool _isDrawing;
        private Point _selectionStart;
        private bool _isSelecting;
        private readonly int _defaultWidth = 32;
        private readonly int _defaultHeight = 32;
        private readonly int _pixelSize = 16;

        public MainWindow()
        {
            InitializeComponent();
            
            _pixelEditor = new PixelEditor(_defaultWidth, _defaultHeight);
            UpdateCanvasSize();
            
            EditorCanvas.PointerMoved += OnCanvasPointerMoved;
            EditorCanvas.PointerPressed += OnCanvasPointerPressed;
            EditorCanvas.PointerReleased += OnCanvasPointerReleased;
            
            RefreshCanvas();
        }

        private void UpdateCanvasSize()
        {
            EditorCanvas.Width = _pixelEditor.Width * _pixelSize;
            EditorCanvas.Height = _pixelEditor.Height * _pixelSize;
            CanvasSizeText.Text = $"Size: {_pixelEditor.Width}x{_pixelEditor.Height}";
        }

        private void RefreshCanvas()
        {
            EditorCanvas.Source = _pixelEditor.GetBitmap();
        }

        private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
        {
            var position = e.GetPosition(EditorCanvas);
            int x = (int)(position.X / _pixelSize);
            int y = (int)(position.Y / _pixelSize);
            
            if (x < 0 || y < 0 || x >= _pixelEditor.Width || y >= _pixelEditor.Height)
                return;

            PositionText.Text = $"Position: {x}, {y}";
            
            if (_isDrawing && PencilTool.IsChecked == true)
            {
                _pixelEditor.DrawPixel(x, y, LastDrawingColor);
                RefreshCanvas();
            }
            else if (_isSelecting && SelectionTool.IsChecked == true)
            {
                // Update selection preview
                _pixelEditor.UpdateSelectionPreview((int)_selectionStart.X, (int)_selectionStart.Y, x, y);
                RefreshCanvas();
            }
        }

        private bool _lastButtonIsLeft;
        private Color LastDrawingColor => _lastButtonIsLeft ? _selectedPrimaryColor : _selectedSecondaryColor;
        
        private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var position = e.GetPosition(EditorCanvas);
            int x = (int)(position.X / _pixelSize);
            int y = (int)(position.Y / _pixelSize);
            
            if (x < 0 || y < 0 || x >= _pixelEditor.Width || y >= _pixelEditor.Height)
                return;

            _lastPosition = new Point(x, y);
            _lastButtonIsLeft = e.GetCurrentPoint(EditorCanvas).Properties.IsLeftButtonPressed;

            if (PencilTool.IsChecked == true)
            {
                _isDrawing = true;
                _pixelEditor.DrawPixel(x, y, LastDrawingColor);
                RefreshCanvas();
            }
            else if (SelectionTool.IsChecked == true)
            {
                _isSelecting = true;
                _selectionStart = new Point(x, y);
                _pixelEditor.StartSelection();
            }
            else if (FillTool.IsChecked == true)
            {
                _pixelEditor.FloodFill(x, y, LastDrawingColor);
                RefreshCanvas();
            }
        }

        private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isDrawing)
            {
                _isDrawing = false;
            }
            else if (_isSelecting)
            {
                _isSelecting = false;
                var position = e.GetPosition(EditorCanvas);
                int x = (int)(position.X / _pixelSize);
                int y = (int)(position.Y / _pixelSize);
                
                if (x < 0) x = 0;
                if (y < 0) y = 0;
                if (x >= _pixelEditor.Width) x = _pixelEditor.Width - 1;
                if (y >= _pixelEditor.Height) y = _pixelEditor.Height - 1;

                _pixelEditor.FinishSelection((int)_selectionStart.X, (int)_selectionStart.Y, x, y);
                RefreshCanvas();
            }
        }

        private async void OnNewClick(object sender, RoutedEventArgs e)
        {
            var dialog = new NewCanvasDialog();
            if (await dialog.ShowDialog<bool>(this))
            {
                _pixelEditor = new PixelEditor(dialog.CanvasWidth, dialog.CanvasHeight);
                UpdateCanvasSize();
                RefreshCanvas();
            }
        }

        private async void OnOpenClick(object sender, RoutedEventArgs e)
        {
            var options = new FilePickerOpenOptions
            {
                Title = "Open PNG File",
                AllowMultiple = false,
                FileTypeFilter = new[] { FilePickerFileTypes.ImagePng }
            };

            var result = await StorageProvider.OpenFilePickerAsync(options);
            if (result.Count > 0)
            {
                try
                {
                    using var stream = await result[0].OpenReadAsync();
                    _pixelEditor.LoadFromStream(stream);
                    UpdateCanvasSize();
                    RefreshCanvas();
                    StatusText.Text = "Image loaded";
                }
                catch (Exception ex)
                {
                    StatusText.Text = $"Error: {ex.Message}";
                }
            }
        }

        private async void OnSaveClick(object sender, RoutedEventArgs e)
        {
            var options = new FilePickerSaveOptions
            {
                Title = "Save PNG File",
                DefaultExtension = "png",
                FileTypeChoices = new[] { FilePickerFileTypes.ImagePng }
            };

            var result = await StorageProvider.SaveFilePickerAsync(options);
            if (result != null)
            {
                try
                {
                    using var stream = await result.OpenWriteAsync();
                    _pixelEditor.SaveToStream(stream);
                    StatusText.Text = "Image saved";
                }
                catch (Exception ex)
                {
                    StatusText.Text = $"Error: {ex.Message}";
                }
            }
        }

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
        
        private Color _selectedPrimaryColor = Colors.Black;
        private Color _selectedSecondaryColor = Colors.White;

        private void ColorPalette_OnColorSelected(Color selectedColor, bool isLeftClicked)
        {
            if (isLeftClicked)
            {
                _selectedPrimaryColor = selectedColor;
                PrimaryColorBorder.Background = new SolidColorBrush(selectedColor);
            }
            else
            {
                _selectedSecondaryColor = selectedColor;
                SecondaryColorBorder.Background = new SolidColorBrush(selectedColor);
            }
        }
    }
}
