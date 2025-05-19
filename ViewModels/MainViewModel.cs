using System;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.IO;
using System.Windows.Input;

namespace PixelEditorApp.ViewModels
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


            ColorPaletteViewModel.ColorSelected += OnColorSelected;
            UpdateCanvasBitmap();
        }
 
        public ICommand AddCurrentColorCommand { get; }

        public event EventHandler? ExitRequested;
        public event EventHandler<NewCanvasRequestEventArgs>? NewCanvasRequested;
        public event EventHandler<FileOperationEventArgs>? OpenRequested;
        public event EventHandler<FileOperationEventArgs>? SaveRequested;

        private void OnExit()
        {
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnNew()
        {
            NewCanvasRequested?.Invoke(this, new NewCanvasRequestEventArgs());
        }

        private void OnOpen()
        {
            OpenRequested?.Invoke(this, new FileOperationEventArgs());
        }

        private void OnSave()
        {
            SaveRequested?.Invoke(this, new FileOperationEventArgs());
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
            CanvasBitmap = _pixelEditor.GetBitmap();
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
                    _pixelEditor.DrawPixel(args.X, args.Y, args.IsLeftButton ? PrimaryColor : SecondaryColor);
                    UpdateCanvasBitmap();
                }
                else if (IsFillToolSelected)
                {
                    _pixelEditor.FloodFill(args.X, args.Y, args.IsLeftButton ? PrimaryColor : SecondaryColor);
                    UpdateCanvasBitmap();
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
                _pixelEditor.UpdateSelectionPreview(args.StartX, args.StartY, args.EndX, args.EndY);
                UpdateCanvasBitmap();
            }
        }

        private void OnSelectionEnd(object? parameter)
        {
            if (IsSelectionToolSelected && parameter is SelectionEventArgs args)
            {
                _pixelEditor.FinishSelection(args.StartX, args.StartY, args.EndX, args.EndY);
                UpdateCanvasBitmap();
            }
        }

        private void OnUpdatePosition(object? parameter)
        {
            if (parameter is PixelEventArgs args)
            {
                PositionText = $"Position: {args.X}, {args.Y}";
                
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

    public class NewCanvasRequestEventArgs : EventArgs { }
    
    public class FileOperationEventArgs : EventArgs { }
}