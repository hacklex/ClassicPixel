using System;
using System.IO;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace PixelEditor.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private PixelEditor _pixelEditor;
        private WriteableBitmap? _canvasBitmap;
        private Color _primaryColor = Colors.Black;
        private Color _secondaryColor = Colors.White;
        private bool _isPencilToolSelected = true;
        private bool _isSelectionToolSelected;
        private bool _isFillToolSelected;
        private string _statusText = "Ready";
        private string _positionText = "Position: 0, 0";
        private string _canvasSizeText = "Size: 32x32";
        private int _pixelScale = 1;
        
        // Add pixel scale property with validation
        public int PixelScale
        {
            get => _pixelScale;
            set
            {
                // Ensure value is between 1 and 10
                int newValue = Math.Clamp(value, 1, 10);
                if (SetProperty(ref _pixelScale, newValue))
                {
                    // Update bitmap when scale changes
                    UpdateCanvasBitmap();
                    StatusText = $"Pixel Scale: {_pixelScale}x";
                }
            }
        }
        
        // Add commands to increase/decrease scale
        public ICommand IncreaseScaleCommand { get; }
        public ICommand DecreaseScaleCommand { get; }
        
        public Color PrimaryColor
        {
            get => _primaryColor;
            set => SetProperty(ref _primaryColor, value);
        }
        
        public Color SecondaryColor
        {
            get => _secondaryColor;
            set => SetProperty(ref _secondaryColor, value);
        }
        
        public WriteableBitmap? CanvasBitmap
        {
            get => _canvasBitmap;
            private set => SetProperty(ref _canvasBitmap, value);
        }
        
        public bool IsPencilToolSelected 
        { 
            get => _isPencilToolSelected; 
            set => SetProperty(ref _isPencilToolSelected, value); 
        }
        
        public bool IsSelectionToolSelected 
        { 
            get => _isSelectionToolSelected; 
            set => SetProperty(ref _isSelectionToolSelected, value); 
        }
        
        public bool IsFillToolSelected 
        { 
            get => _isFillToolSelected; 
            set => SetProperty(ref _isFillToolSelected, value); 
        }

        public string StatusText 
        { 
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }
        
        public string PositionText 
        { 
            get => _positionText;
            set => SetProperty(ref _positionText, value);
        }
        
        public string CanvasSizeText 
        { 
            get => _canvasSizeText;
            set => SetProperty(ref _canvasSizeText, value);
        }

        public ColorPaletteViewModel ColorPaletteViewModel { get; } = new();

        // Commands
        public ICommand NewCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand DrawPixelCommand { get; }
        public ICommand SelectionStartCommand { get; }
        public ICommand SelectionUpdateCommand { get; }
        public ICommand SelectionEndCommand { get; }
        public ICommand UpdatePositionCommand { get; }

        public MainViewModel()
        {
            _pixelEditor = new PixelEditor(32, 32);
            
            NewCommand = new RelayCommand(_ => OnNew());
            OpenCommand = new RelayCommand(_ => OnOpen());
            SaveCommand = new RelayCommand(_ => OnSave());
            ExitCommand = new RelayCommand(_ => OnExit());
            DrawPixelCommand = new RelayCommand(p => OnDrawPixel(p));
            SelectionStartCommand = new RelayCommand(p => OnSelectionStart(p));
            SelectionUpdateCommand = new RelayCommand(p => OnSelectionUpdate(p));
            SelectionEndCommand = new RelayCommand(p => OnSelectionEnd(p));
            UpdatePositionCommand = new RelayCommand(p => OnUpdatePosition(p));
            AddCurrentColorCommand = new RelayCommand(_ => ColorPaletteViewModel.AddColor(PrimaryColor));
            
            // Initialize scale commands
            IncreaseScaleCommand = new RelayCommand(_ => PixelScale++);
            DecreaseScaleCommand = new RelayCommand(_ => PixelScale--);

            ColorPaletteViewModel.ColorSelected += OnColorSelected;
            UpdateCanvasBitmap();
        }
 
        public ICommand AddCurrentColorCommand { get; }

        private async void OnExit()
        {
            if (Program.MainWindow != null)
            {
                Program.MainWindow.Close();
            }
        }

        private async void OnNew()
        {
            // Create and show the NewCanvasDialog using Program.MainWindow as owner
            var dialog = new NewCanvasDialog
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            
            if (Program.MainWindow != null)
            {
                var result = await dialog.ShowDialog<bool?>(Program.MainWindow);
                
                if (result == true)
                {
                    int width = dialog.CanvasWidth;
                    int height = dialog.CanvasHeight;
                    CreateNewCanvas(width, height);
                }
            }
        }

        private async void OnOpen()
        {
            if (Program.MainWindow != null)
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Open Image",
                    Filters = new System.Collections.Generic.List<FileDialogFilter>
                    {
                        new FileDialogFilter { Name = "PNG Files", Extensions = { "png" } },
                        new FileDialogFilter { Name = "All Files", Extensions = { "*" } }
                    }
                };
                
                var result = await dialog.ShowAsync(Program.MainWindow);
                
                if (result != null && result.Length > 0)
                {
                    try
                    {
                        using (var stream = File.OpenRead(result[0]))
                        {
                            LoadFromStream(stream);
                        }
                        StatusText = "File opened successfully.";
                    }
                    catch (Exception ex)
                    {
                        StatusText = $"Error opening file: {ex.Message}";
                    }
                }
            }
        }

        private async void OnSave()
        {
            if (Program.MainWindow != null)
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Save Image",
                    Filters = new System.Collections.Generic.List<FileDialogFilter>
                    {
                        new FileDialogFilter { Name = "PNG Files", Extensions = { "png" } },
                        new FileDialogFilter { Name = "All Files", Extensions = { "*" } }
                    },
                    DefaultExtension = "png"
                };
                
                var result = await dialog.ShowAsync(Program.MainWindow);
                
                if (!string.IsNullOrEmpty(result))
                {
                    try
                    {
                        using (var stream = File.Create(result))
                        {
                            SaveToStream(stream);
                        }
                        StatusText = "File saved successfully.";
                    }
                    catch (Exception ex)
                    {
                        StatusText = $"Error saving file: {ex.Message}";
                    }
                }
            }
        }

        public void CreateNewCanvas(int width, int height)
        {
            _pixelEditor = new PixelEditor(width, height);
            UpdateCanvasBitmap();
            UpdateCanvasSizeText();
        }

        public void LoadFromStream(Stream stream)
        {
            _pixelEditor.LoadFromStream(stream);
            UpdateCanvasBitmap();
            UpdateCanvasSizeText();
        }

        public void SaveToStream(Stream stream)
        {
            _pixelEditor.SaveToStream(stream);
        }

        private void UpdateCanvasBitmap()
        {
            // Get the bitmap scaled to the current pixel scale
            var originalBitmap = _pixelEditor.GetBitmap();
            
            if (PixelScale == 1)
            {
                CanvasBitmap = originalBitmap;
                return;
            }
            
            // Create a new bitmap with the scaled dimensions
            var scaledBitmap = new WriteableBitmap(
                new Avalonia.PixelSize(originalBitmap.PixelSize.Width * PixelScale, 
                                      originalBitmap.PixelSize.Height * PixelScale),
                new Avalonia.Vector(96, 96),
                Avalonia.Platform.PixelFormat.Bgra8888);
            
            using (var srcCtx = originalBitmap.Lock())
            using (var destCtx = scaledBitmap.Lock())
            {
                unsafe
                {
                    var src = (uint*)srcCtx.Address;
                    var dest = (uint*)destCtx.Address;
                    
                    int srcWidth = originalBitmap.PixelSize.Width;
                    int srcHeight = originalBitmap.PixelSize.Height;
                    int srcStride = srcCtx.RowBytes / 4;
                    int destStride = destCtx.RowBytes / 4;
                    
                    for (int y = 0; y < srcHeight; y++)
                    {
                        for (int x = 0; x < srcWidth; x++)
                        {
                            uint pixel = src[y * srcStride + x];
                            
                            // Fill a square of size PixelScale with the same color
                            for (int dy = 0; dy < PixelScale; dy++)
                            {
                                for (int dx = 0; dx < PixelScale; dx++)
                                {
                                    int destX = x * PixelScale + dx;
                                    int destY = y * PixelScale + dy;
                                    dest[destY * destStride + destX] = pixel;
                                }
                            }
                        }
                    }
                }
            }
            
            CanvasBitmap = scaledBitmap;
        }

        private void UpdateCanvasSizeText()
        {
            CanvasSizeText = $"Size: {_pixelEditor.Width}x{_pixelEditor.Height}";
        }

        private void OnColorSelected(Color selectedColor, bool isLeftButton)
        {
            if (isLeftButton)
            {
                PrimaryColor = selectedColor;
            }
            else
            {
                SecondaryColor = selectedColor;
            }
        }

        private void OnDrawPixel(object? parameter)
        {
            if (parameter is PixelEventArgs args)
            {
                if (IsPencilToolSelected)
                {
                    // Convert screen coordinates to canvas coordinates based on pixel scale
                    int canvasX = args.X / PixelScale;
                    int canvasY = args.Y / PixelScale;
                    
                    // Check bounds before drawing
                    if (canvasX >= 0 && canvasX < _pixelEditor.Width && canvasY >= 0 && canvasY < _pixelEditor.Height)
                    {
                        _pixelEditor.DrawPixel(canvasX, canvasY, args.IsLeftButton ? PrimaryColor : SecondaryColor);
                        UpdateCanvasBitmap();
                    }
                }
                else if (IsFillToolSelected)
                {
                    // Convert screen coordinates to canvas coordinates based on pixel scale
                    int canvasX = args.X / PixelScale;
                    int canvasY = args.Y / PixelScale;
                    
                    // Check bounds before filling
                    if (canvasX >= 0 && canvasX < _pixelEditor.Width && canvasY >= 0 && canvasY < _pixelEditor.Height)
                    {
                        _pixelEditor.FloodFill(canvasX, canvasY, args.IsLeftButton ? PrimaryColor : SecondaryColor);
                        UpdateCanvasBitmap();
                    }
                }
            }
        }

        private void OnSelectionStart(object? parameter)
        {
            if (IsSelectionToolSelected && parameter is PixelEventArgs)
            {
                _pixelEditor.StartSelection();
                UpdateCanvasBitmap();
            }
        }

        private void OnSelectionUpdate(object? parameter)
        {
            if (IsSelectionToolSelected && parameter is SelectionEventArgs args)
            {
                // Convert screen coordinates to canvas coordinates based on pixel scale
                int startX = args.StartX / PixelScale;
                int startY = args.StartY / PixelScale;
                int endX = args.EndX / PixelScale;
                int endY = args.EndY / PixelScale;
                
                // Ensure coordinates are within canvas bounds
                startX = Math.Clamp(startX, 0, _pixelEditor.Width - 1);
                startY = Math.Clamp(startY, 0, _pixelEditor.Height - 1);
                endX = Math.Clamp(endX, 0, _pixelEditor.Width - 1);
                endY = Math.Clamp(endY, 0, _pixelEditor.Height - 1);
                
                _pixelEditor.UpdateSelectionPreview(startX, startY, endX, endY);
                UpdateCanvasBitmap();
            }
        }

        private void OnSelectionEnd(object? parameter)
        {
            if (IsSelectionToolSelected && parameter is SelectionEventArgs args)
            {
                // Convert screen coordinates to canvas coordinates based on pixel scale
                int startX = args.StartX / PixelScale;
                int startY = args.StartY / PixelScale;
                int endX = args.EndX / PixelScale;
                int endY = args.EndY / PixelScale;
                
                // Ensure coordinates are within canvas bounds
                startX = Math.Clamp(startX, 0, _pixelEditor.Width - 1);
                startY = Math.Clamp(startY, 0, _pixelEditor.Height - 1);
                endX = Math.Clamp(endX, 0, _pixelEditor.Width - 1);
                endY = Math.Clamp(endY, 0, _pixelEditor.Height - 1);
                
                _pixelEditor.FinishSelection(startX, startY, endX, endY);
                UpdateCanvasBitmap();
            }
        }

        private void OnUpdatePosition(object? parameter)
        {
            if (parameter is PixelEventArgs args)
            {
                // Convert screen coordinates to canvas coordinates
                int canvasX = args.X / PixelScale;
                int canvasY = args.Y / PixelScale;
                
                // Update position text
                if (canvasX >= 0 && canvasX < _pixelEditor.Width && canvasY >= 0 && canvasY < _pixelEditor.Height)
                {
                    PositionText = $"Position: {canvasX}, {canvasY}";
                }
                else
                {
                    PositionText = "Position: Outside canvas";
                }
                
                // Update status text based on active tool
                if (IsPencilToolSelected)
                    StatusText = "Pencil Tool";
                else if (IsSelectionToolSelected)
                    StatusText = "Selection Tool";
                else if (IsFillToolSelected)
                    StatusText = "Fill Tool";
            }
        }
    }

    public class PixelEventArgs
    {
        public int X { get; }
        public int Y { get; }
        public bool IsLeftButton { get; }

        public PixelEventArgs(int x, int y, bool isLeftButton)
        {
            X = x;
            Y = y;
            IsLeftButton = isLeftButton;
        }
    }

    public class SelectionEventArgs
    {
        public int StartX { get; }
        public int StartY { get; }
        public int EndX { get; }
        public int EndY { get; }

        public SelectionEventArgs(int startX, int startY, int endX, int endY)
        {
            StartX = startX;
            StartY = startY;
            EndX = endX;
            EndY = endY;
        }
    }
}