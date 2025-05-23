using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.Timers;

namespace PixelEditor.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private PixelEditor _pixelEditor;
        private WriteableBitmap? _canvasBitmap;
        private Color _primaryColor = Colors.Black;
        private Color _secondaryColor = Colors.White;
        private ToolType _selectedTool = ToolType.Pencil;
        private string _statusText = "Ready";
        private string _positionText = "Position: 0, 0";
        private string _canvasSizeText = "Size: 32x32";
        private int _magicWandTolerance = 32;
        private int _selectionStartX;
        private int _selectionStartY;
        private int _selectionEndX;
        private int _selectionEndY;
        private bool _hasSelection;
        
        // Magic wand tolerance property with validation
        public int MagicWandTolerance
        {
            get => _magicWandTolerance;
            set
            {
                // Ensure the value is between 0 and 255
                int newValue = Math.Clamp(value, 0, 255);
                if (SetProperty(ref _magicWandTolerance, newValue))
                {
                    StatusText = $"Magic Wand Tolerance: {_magicWandTolerance}";
                }
            }
        }
        
        // Add commands to increase/decrease scale and tolerance
        public ICommand IncreaseScaleCommand { get; }
        public ICommand DecreaseScaleCommand { get; }
        public ICommand IncreaseMagicWandToleranceCommand { get; }
        public ICommand DecreaseMagicWandToleranceCommand { get; }
        
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
        
        public ToolType SelectedTool
        {
            get => _selectedTool;
            set => SetProperty(ref _selectedTool, value);
        }
        
        // Convenience properties for backward compatibility and readability in code
        public bool IsPencilToolSelected => SelectedTool == ToolType.Pencil;
        public bool IsSelectionToolSelected => SelectedTool == ToolType.Selection;
        public bool IsMagicWandToolSelected => SelectedTool == ToolType.MagicWand;
        public bool IsFillToolSelected => SelectedTool == ToolType.Fill;
        public bool IsEraserToolSelected => SelectedTool == ToolType.Eraser;
        public bool IsColorPickerToolSelected => SelectedTool == ToolType.ColorPicker;

        public bool HasSelection
        {
            get => _hasSelection;
            private set => SetProperty(ref _hasSelection, value);
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

        public int SelectionStartX 
        { 
            get => _selectionStartX;
            private set => SetProperty(ref _selectionStartX, value);
        }
        
        public int SelectionStartY 
        { 
            get => _selectionStartY;
            private set => SetProperty(ref _selectionStartY, value);
        }
        
        public int SelectionEndX 
        { 
            get => _selectionEndX;
            private set => SetProperty(ref _selectionEndX, value);
        }
        
        public int SelectionEndY 
        { 
            get => _selectionEndY;
            private set => SetProperty(ref _selectionEndY, value);
        }
        
        public List<(int startX, int startY, int endX, int endY)> SelectionRegions { get; private set; } = [];

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
        public ICommand MagicWandSelectCommand { get; }
        public ICommand UpdatePositionCommand { get; }

        public MainViewModel()
        {
            _pixelEditor = new PixelEditor(32, 32);
            
            NewCommand = new RelayCommand(_ => OnNew());
            OpenCommand = new RelayCommand(_ => OnOpen());
            SaveCommand = new RelayCommand(_ => OnSave());
            ExitCommand = new RelayCommand(_ => OnExit());
            DrawPixelCommand = new RelayCommand(OnDrawPixel);
            SelectionStartCommand = new RelayCommand(OnSelectionStart);
            SelectionUpdateCommand = new RelayCommand(OnSelectionUpdate);
            SelectionEndCommand = new RelayCommand(OnSelectionEnd);
            MagicWandSelectCommand = new RelayCommand(OnMagicWandSelect);
            UpdatePositionCommand = new RelayCommand(OnUpdatePosition);
            AddCurrentColorCommand = new RelayCommand(_ => ColorPaletteViewModel.AddColor(PrimaryColor));
            
            IncreaseMagicWandToleranceCommand = new RelayCommand(_ => MagicWandTolerance += 8);
            DecreaseMagicWandToleranceCommand = new RelayCommand(_ => MagicWandTolerance -= 8);
            
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
    
            CanvasBitmap = originalBitmap; 
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
                // Convert screen coordinates to canvas coordinates based on the pixel scale
                int canvasX = args.X;
                int canvasY = args.Y;
                
                // Check bounds before operations
                if (canvasX >= 0 && canvasX < _pixelEditor.Width && canvasY >= 0 && canvasY < _pixelEditor.Height)
                {
                    if (IsPencilToolSelected)
                    {
                        _pixelEditor.DrawPixel(canvasX, canvasY, args.IsLeftButton ? PrimaryColor : SecondaryColor);
                        UpdateCanvasBitmap();
                    }
                    else if (IsFillToolSelected)
                    {
                        _pixelEditor.FloodFill(canvasX, canvasY, args.IsLeftButton ? PrimaryColor : SecondaryColor);
                        UpdateCanvasBitmap();
                    }
                    else if (IsColorPickerToolSelected)
                    {
                        if (args.IsLeftButton)
                        {
                            PrimaryColor = _pixelEditor.GetPixelColor(canvasX, canvasY);
                        }
                        else
                        {
                            SecondaryColor = _pixelEditor.GetPixelColor(canvasX, canvasY);
                        }
                    }
                    else if (IsEraserToolSelected)
                    {
                        // Create a transparent color for erasing
                        var transparentColor = Colors.Transparent;
                        
                        if (args.IsLeftButton)
                        {
                            // Simple eraser - just make pixels transparent
                            _pixelEditor.DrawPixel(canvasX, canvasY, Color.FromArgb(0,0,0,0));
                        }
                        else
                        {
                            // Smart eraser - erase only similar colors using tolerance
                            if (_pixelEditor.IsColorSimilar(_pixelEditor.GetPixelColor(canvasX, canvasY), args.MouseDownColor, _magicWandTolerance))
                                _pixelEditor.DrawPixel(canvasX, canvasY, Color.FromArgb(0,0,0,0));
                        }
                        UpdateCanvasBitmap();
                    }
                }
            }
        }

        private void OnSelectionStart(object? parameter)
        {
            if (IsSelectionToolSelected && parameter is PixelEventArgs args)
            {
                // Convert screen coordinates to canvas coordinates
                int canvasX = args.X;
                int canvasY = args.Y;
                
                // Store selection start coordinates
                SelectionStartX = canvasX;
                SelectionStartY = canvasY;
                
                // Also set end coordinates to same position initially
                SelectionEndX = canvasX;
                SelectionEndY = canvasY;
                
                // If we're starting a new selection and not adding or subtracting,
                // clear existing selection regions
                if (!args.IsCtrlPressed && !args.IsAltPressed && SelectionRegions.Count > 0)
                {
                    SelectionRegions.Clear();
                }
                
                // We're making a rectangular selection in the UI now
                HasSelection = false; // Don't show selection until we have an actual area
                StatusText = args.IsCtrlPressed ? "Adding to selection..." : 
                             args.IsAltPressed ? "Subtracting from selection..." : "Selection started";
            }
        }

        private void OnSelectionUpdate(object? parameter)
        {
            if (IsSelectionToolSelected && parameter is SelectionEventArgs args)
            {
                // Convert screen coordinates to canvas coordinates based on pixel scale
                int startX = args.StartX;
                int startY = args.StartY;
                int endX = args.EndX;
                int endY = args.EndY;
                
                // Ensure coordinates are within canvas bounds
                startX = Math.Clamp(startX, 0, _pixelEditor.Width - 1);
                startY = Math.Clamp(startY, 0, _pixelEditor.Height - 1);
                endX = Math.Clamp(endX, 0, _pixelEditor.Width - 1);
                endY = Math.Clamp(endY, 0, _pixelEditor.Height - 1);
                
                // Update selection coordinates in view model
                SelectionStartX = startX;
                SelectionStartY = startY;
                SelectionEndX = endX;
                SelectionEndY = endY;
                
                // Only set HasSelection to true if we have an actual area
                bool hasCurrentArea = (startX != endX) || (startY != endY);
                HasSelection = hasCurrentArea || SelectionRegions.Count > 0;
                
                if (hasCurrentArea)
                {
                    string modeText = args.Mode == SelectionMode.Add ? "Adding to" : 
                                     args.Mode == SelectionMode.Subtract ? "Subtracting from" : "";
                    StatusText = $"{modeText} Selection: ({startX},{startY}) to ({endX},{endY})";
                }
                else
                {
                    StatusText = "Selecting...";
                }
            }
        }

        public void ClearSelection()
        {
            if (HasSelection)
            {
                HasSelection = false;
                // Clear all selection regions
                SelectionRegions.Clear();
                
                // Clear the selection in the model
                _pixelEditor.ClearSelection();
                
                StatusText = "Selection cleared";
            }
        }
        
        private void OnSelectionEnd(object? parameter)
        {
            if (IsSelectionToolSelected && parameter is SelectionEventArgs args)
            {
                // Convert screen coordinates to canvas coordinates based on pixel scale
                int startX = args.StartX;
                int startY = args.StartY;
                int endX = args.EndX;
                int endY = args.EndY;
                
                // Ensure coordinates are within canvas bounds
                startX = Math.Clamp(startX, 0, _pixelEditor.Width - 1);
                startY = Math.Clamp(startY, 0, _pixelEditor.Height - 1);
                endX = Math.Clamp(endX, 0, _pixelEditor.Width - 1);
                endY = Math.Clamp(endY, 0, _pixelEditor.Height - 1);
                
                // Update final selection coordinates
                SelectionStartX = startX;
                SelectionStartY = startY;
                SelectionEndX = endX;
                SelectionEndY = endY;
                
                // Normalize the selection rect (make sure startX <= endX and startY <= endY)
                int left = Math.Min(startX, endX);
                int top = Math.Min(startY, endY);
                int right = Math.Max(startX, endX);
                int bottom = Math.Max(startY, endY);
                
                // Only process if there's an actual area selected
                bool hasCurrentArea = (left != right) || (top != bottom);
                
                if (hasCurrentArea)
                {
                    var newRegion = (left, top, right, bottom);
                    
                    // Handle different selection modes
                    switch (args.Mode)
                    {
                        case SelectionMode.Replace:
                            SelectionRegions.Clear();
                            SelectionRegions.Add(newRegion);
                            break;
                            
                        case SelectionMode.Add:
                            SelectionRegions.Add(newRegion);
                            break;
                            
                        case SelectionMode.Subtract:
                            // For subtraction, we need to implement this as multiple selection regions
                            // This is a simplified implementation that just removes intersecting regions
                            var regionsToKeep = new List<(int startX, int startY, int endX, int endY)>();
                            
                            foreach (var region in SelectionRegions)
                            {
                                // If the region doesn't overlap with the subtraction region, keep it
                                if (region.endX < left || region.startX > right || 
                                    region.endY < top || region.startY > bottom)
                                {
                                    regionsToKeep.Add(region);
                                }
                                else
                                {
                                    // Split the region into non-overlapping parts
                                    if (region.startX < left)
                                    {
                                        regionsToKeep.Add((region.startX, region.startY, left - 1, region.endY));
                                    }
                                    if (region.endX > right)
                                    {
                                        regionsToKeep.Add((right + 1, region.startY, region.endX, region.endY));
                                    }
                                    if (region.startY < top)
                                    {
                                        regionsToKeep.Add((Math.Max(left, region.startX), region.startY,
                                            Math.Min(right, region.endX), top - 1));
                                    }
                                    if (region.endY > bottom)
                                    {
                                        regionsToKeep.Add((Math.Max(left, region.startX), bottom + 1,
                                            Math.Min(right, region.endX), region.endY));
                                    }
                                }
                            }
                            
                            SelectionRegions = regionsToKeep;
                            break;
                    }
                    
                    HasSelection = SelectionRegions.Count > 0;
                    
                    if (HasSelection)
                    {
                        // For UI, we'll still use the last selection for display purposes
                        _pixelEditor.FinishSelection(left, top, right, bottom);
                        
                        int width = right - left + 1;
                        int height = bottom - top + 1;
                        
                        if (args.Mode == SelectionMode.Add)
                            StatusText = $"Added selection: {width}x{height}";
                        else if (args.Mode == SelectionMode.Subtract)
                            StatusText = $"Subtracted from selection: {width}x{height}";
                        else
                            StatusText = $"Selected area: {width}x{height}";
                    }
                }
                else if (SelectionRegions.Count == 0)
                {
                    _pixelEditor.ClearSelection();
                    HasSelection = false;
                    StatusText = "Selection canceled";
                }
            }
        }
        
        /// <summary>
        /// Converts a set of individual pixels into a minimal set of rectangular regions
        /// </summary>
        private List<(int startX, int startY, int endX, int endY)> ConvertPixelsToRectRegions(HashSet<(int x, int y)> pixels, int width, int height)
        {
            List<(int startX, int startY, int endX, int endY)> regions = new();
            
            if (pixels.Count == 0)
                return regions;
                
            // Create a 2D grid to mark selected pixels
            bool[,] selected = new bool[width, height];
            
            // Mark all selected pixels
            foreach (var (x, y) in pixels)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    selected[x, y] = true;
                }
            }
            
            // Convert the selection grid to rectangular regions
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Skip if this pixel is not selected or already processed
                    if (!selected[x, y]) 
                        continue;
                    
                    // Find the maximum width for this starting point
                    int maxWidth = 1;
                    while (x + maxWidth < width && selected[x + maxWidth, y])
                    {
                        maxWidth++;
                    }
                    
                    // Find the maximum height for this width
                    int maxHeight = 1;
                    bool canExtendDown = true;
                    
                    while (y + maxHeight < height && canExtendDown)
                    {
                        // Check if we can extend the entire width down
                        for (int i = 0; i < maxWidth; i++)
                        {
                            if (!selected[x + i, y + maxHeight])
                            {
                                canExtendDown = false;
                                break;
                            }
                        }
                        
                        if (canExtendDown)
                            maxHeight++;
                    }
                    
                    // Create a region from the found rectangle
                    regions.Add((x, y, x + maxWidth - 1, y + maxHeight - 1));
                    
                    // Mark these pixels as processed
                    for (int dy = 0; dy < maxHeight; dy++)
                    {
                        for (int dx = 0; dx < maxWidth; dx++)
                        {
                            selected[x + dx, y + dy] = false;
                        }
                    }
                    
                    // Skip the processed columns for this row
                    x += maxWidth - 1;
                }
            }
            
            return regions;
        }
        
        private void OnMagicWandSelect(object? parameter)
        {
            if (IsMagicWandToolSelected && parameter is PixelEventArgs args)
            {
                // Convert screen coordinates to canvas coordinates
                int canvasX = args.X;
                int canvasY = args.Y;
                
                // Ensure coordinates are within canvas bounds
                canvasX = Math.Clamp(canvasX, 0, _pixelEditor.Width - 1);
                canvasY = Math.Clamp(canvasY, 0, _pixelEditor.Height - 1);
                
                // Get the selected pixels using magic wand with current tolerance
                var selectedPixels = _pixelEditor.MagicWandSelect(canvasX, canvasY, MagicWandTolerance);
                
                if (selectedPixels.Count > 0)
                {
                    // Convert pixels to efficient rectangular regions
                    var newRegions = ConvertPixelsToRectRegions(selectedPixels, _pixelEditor.Width, _pixelEditor.Height);
                    
                    // Handle different selection modes
                    SelectionMode mode = args.IsCtrlPressed ? SelectionMode.Add : 
                                        args.IsAltPressed ? SelectionMode.Subtract : 
                                        SelectionMode.Replace;
                    
                    if (mode == SelectionMode.Replace && !args.IsCtrlPressed && !args.IsAltPressed)
                    {
                        // Clear existing selection regions if replacing
                        SelectionRegions.Clear();
                        SelectionRegions.AddRange(newRegions);
                    }
                    else if (mode == SelectionMode.Add)
                    {
                        // Simply add the new regions to the existing ones
                        SelectionRegions.AddRange(newRegions);
                    }
                    else if (mode == SelectionMode.Subtract)
                    {
                        // For subtraction, we need to handle region overlaps
                        var regionsToKeep = new List<(int startX, int startY, int endX, int endY)>();
                        
                        // Create a quick lookup set of all pixels in the new regions
                        HashSet<(int x, int y)> pixelsToRemove = new HashSet<(int x, int y)>();
                        foreach (var region in newRegions)
                        {
                            for (int y = region.startY; y <= region.endY; y++)
                            {
                                for (int x = region.startX; x <= region.endX; x++)
                                {
                                    pixelsToRemove.Add((x, y));
                                }
                            }
                        }
                        
                        // Process each existing region
                        foreach (var region in SelectionRegions)
                        {
                            // Collect pixels from this region that aren't in the subtraction set
                            HashSet<(int x, int y)> remainingPixels = new HashSet<(int x, int y)>();
                            
                            for (int y = region.startY; y <= region.endY; y++)
                            {
                                for (int x = region.startX; x <= region.endX; x++)
                                {
                                    if (!pixelsToRemove.Contains((x, y)))
                                    {
                                        remainingPixels.Add((x, y));
                                    }
                                }
                            }
                            
                            // Convert remaining pixels back to rectangle regions
                            if (remainingPixels.Count > 0)
                            {
                                regionsToKeep.AddRange(ConvertPixelsToRectRegions(
                                    remainingPixels, _pixelEditor.Width, _pixelEditor.Height));
                            }
                        }
                        
                        SelectionRegions = regionsToKeep;
                    }
                    
                    HasSelection = SelectionRegions.Count > 0;
                    
                    string modeText = mode == SelectionMode.Add ? "Added to" : 
                                    mode == SelectionMode.Subtract ? "Subtracted from" : "Created";
                    StatusText = $"{modeText} magic wand selection with {selectedPixels.Count} pixels in {newRegions.Count} region(s)";
                }
            }
        }
        
        private void OnUpdatePosition(object? parameter)
        {
            if (parameter is PixelEventArgs args)
            {
                // Convert screen coordinates to canvas coordinates
                int canvasX = args.X;
                int canvasY = args.Y;
                
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
                else if (IsMagicWandToolSelected)
                    StatusText = "Magic Wand Tool";
                else if (IsFillToolSelected)
                    StatusText = "Fill Tool";
                else if (IsEraserToolSelected)
                    StatusText = $"Eraser Tool (Tolerance: {MagicWandTolerance})";
            }
        }

        // Tool cycling methods
        public void CycleSelectionTools()
        {
            SelectedTool = SelectedTool switch
            {
                ToolType.Selection => ToolType.MagicWand,
                _ => ToolType.Selection
            };
            StatusText = SelectedTool == ToolType.Selection 
                ? "Rect Selection Tool [s] (hit again to switch to Magic Wand)"
                : "Magic Wand Tool [s] (hit again to switch to Rect Selection)";
        }

        public void CycleDrawingTools()
        {
            SelectedTool = SelectedTool switch
            {
                ToolType.Pencil => ToolType.Eraser,
                _ => ToolType.Pencil
            };
            StatusText = SelectedTool == ToolType.Pencil 
                ? "Pencil Tool [p] (hit again to switch to Eraser)"
                : "Eraser Tool [p] (hit again to switch to Pencil)";
        }

        public void SelectFillTool()
        {
            SelectedTool = ToolType.Fill;
            StatusText = "Flood Fill [f]";
        }
                    
        public void SelectColorPickerTool()
        {
            SelectedTool = ToolType.ColorPicker; 
            StatusText = "Color Picker [k]";
        }

        public void SwapSelectedColors() => (PrimaryColor, SecondaryColor) = (SecondaryColor, PrimaryColor);
        
        public Color GetPixelColor(int x, int y) => _pixelEditor.GetPixelColor(x, y);
    }

    public class PixelEventArgs
    {
        public int X { get; }
        public int Y { get; }
        public bool IsLeftButton { get; }
        public bool IsCtrlPressed { get; }
        public bool IsAltPressed { get; }
        public Color MouseDownColor { get; }

        public PixelEventArgs(int x, int y, bool isLeftButton, Color mouseDownColor, bool isCtrlPressed = false, bool isAltPressed = false)
        {
            X = x;
            Y = y;
            IsLeftButton = isLeftButton;
            IsCtrlPressed = isCtrlPressed;
            IsAltPressed = isAltPressed;
            MouseDownColor = mouseDownColor;
        }
    }

    public enum SelectionMode
    {
        Replace,  // Replace the current selection
        Add,      // Add to the current selection (Ctrl+drag)
        Subtract  // Subtract from the current selection (Alt+drag)
    }
    
    public class SelectionEventArgs
    {
        public int StartX { get; }
        public int StartY { get; }
        public int EndX { get; }
        public int EndY { get; }
        public SelectionMode Mode { get; }
    
        public SelectionEventArgs(int startX, int startY, int endX, int endY, SelectionMode mode = SelectionMode.Replace)
        {
            StartX = startX;
            StartY = startY;
            EndX = endX;
            EndY = endY;
            Mode = mode;
        }
    }
}