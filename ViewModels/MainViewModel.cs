using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PixelEditor.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        // Using ObservableProperty attributes to auto-generate property code
        [ObservableProperty] private WriteableBitmap? _canvasBitmap;
        
        [ObservableProperty] private Color _primaryColor = Colors.Black;
        [ObservableProperty] private Color _secondaryColor = Colors.White;
        
        [ObservableProperty] private ToolType _selectedTool = ToolType.Pencil;
        
        [ObservableProperty] private string _statusText = "Ready";
        [ObservableProperty] private string _positionText = "Position: 0, 0";
        [ObservableProperty] private string _canvasSizeText = "Size: 32x32";
        
        [ObservableProperty] private int _magicWandTolerance = 32;
        
        [ObservableProperty] private int _selectionStartX;
        [ObservableProperty] private int _selectionStartY;
        [ObservableProperty] private int _selectionEndX;
        [ObservableProperty] private int _selectionEndY;
        
        [ObservableProperty] private bool _hasSelection;
        
        private PixelEditor _pixelEditor; 

        private bool ShouldShowToleranceSetup(ToolType currentTool) => currentTool is ToolType.MagicWand or ToolType.Eraser or ToolType.Selection or ToolType.Fill;
        public bool IsToleranceSetupVisible => ShouldShowToleranceSetup(SelectedTool);

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(MagicWandTolerance)) StatusText = $"Magic Wand Tolerance: {MagicWandTolerance}";
            if (e.PropertyName == nameof(SelectedTool)) OnPropertyChanged(nameof(IsToleranceSetupVisible));
        }
        
        // Convenience properties for backward compatibility and readability in code
        public bool IsPencilToolSelected => SelectedTool == ToolType.Pencil;
        public bool IsSelectionToolSelected => SelectedTool == ToolType.Selection;
        public bool IsMagicWandToolSelected => SelectedTool == ToolType.MagicWand;
        public bool IsFillToolSelected => SelectedTool == ToolType.Fill;
        public bool IsEraserToolSelected => SelectedTool == ToolType.Eraser;
        public bool IsColorPickerToolSelected => SelectedTool == ToolType.ColorPicker;
       
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
        public ICommand PreviewBrushCommand { get; }
        
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
            PreviewBrushCommand = new RelayCommand(OnPreviewBrush);
            AddCurrentColorCommand = new RelayCommand(_ => ColorPaletteViewModel.AddColor(PrimaryColor));
            ColorPaletteViewModel.ColorSelected += OnColorSelected;
            UpdateCanvasBitmap();
        }
 
        public ICommand AddCurrentColorCommand { get; }

        private void OnExit() => Environment.Exit(0);

        private async void OnNew()
        {
            try
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
            catch (Exception e)
            {
                // Handle any exceptions that occur during the dialog display
                StatusText = $"Error creating new canvas: {e.Message}";
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
                        
        private void OnPreviewBrush(object? parameter)
        {
            if (parameter is PixelEventArgs args)
            {
                int x = args.X;
                int y = args.Y;
                
                // Only show preview in drawing tools
                if (IsPencilToolSelected || IsEraserToolSelected)
                {
                    // Choose appropriate color based on tool and mouse button
                    Color previewColor;
                    
                    if (IsEraserToolSelected)
                    {
                        // For eraser, use a semi-transparent white color to indicate erasure
                        previewColor = Color.FromArgb(128, 255, 255, 255);
                    }
                    else
                    {
                        // For pencil, use the primary or secondary color based on which mouse button will be used
                        previewColor = PrimaryColor;
                    }
                    
                    // Default brush size is 1 for now, can be extended later for different brush sizes
                    int brushSize = 1;
                    
                    // Update the preview
                    _pixelEditor.UpdatePreview(x, y, previewColor, brushSize);
                }
                else
                {
                    // Clear preview for non-drawing tools
                    _pixelEditor.ClearPreview();
                }
                
                // Update the canvas to show the preview
                UpdateCanvasBitmap();
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

        public void ClearPreview() => _pixelEditor.ClearPreview();
        
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

        public void DeleteSelection()
        {
            if (HasSelection)
            {
                foreach (var region in SelectionRegions)
                {
                    // Erase the selected area by setting pixels to transparent
                    for (int y = region.startY; y <= region.endY; y++)
                    {
                        for (int x = region.startX; x <= region.endX; x++)
                        {
                            _pixelEditor.DrawPixel(x, y, Color.FromArgb(0, 0, 0, 0));
                        }
                    }
                }
                UpdateCanvasBitmap();
                HasSelection = false;
                SelectionRegions.Clear();
                StatusText = "Erased selection";
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
                
                // Update status text based on the active tool
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