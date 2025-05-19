using Avalonia.Media;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace PixelEditorApp.ViewModels
{
    public class ColorPaletteViewModel : ViewModelBase
    {
        private Color _selectedColor;

        public ObservableCollection<PaletteColor> Colors { get; } = new ObservableCollection<PaletteColor>();

        public Color SelectedColor
        {
            get => _selectedColor;
            set => SetProperty(ref _selectedColor, value);
        }
        
        public ICommand RemoveColorCommand { get; }
        public ICommand SelectColorCommand { get; }
        public ICommand LoadDefault16BitColorsCommand { get; }

        public event ColorChangedEventHandler? ColorSelected;

        public ColorPaletteViewModel()
        {
            RemoveColorCommand = new RelayCommand(RemoveColor);
            SelectColorCommand = new RelayCommand(SelectColor);
            LoadDefault16BitColorsCommand = new RelayCommand(_ => LoadDefault16BitColors());
            LoadDefault16BitColors();
        }

        private void SelectColor(object? parameter)
        {
            if (parameter is ColorSelectionData data)
            {
                SelectedColor = data.Color;
                ColorSelected?.Invoke(data.Color, data.IsLeftButton);
            }
        }

        private void RemoveColor(object? parameter)
        {
            if (parameter is PaletteColor paletteColor)
            {
                Colors.Remove(paletteColor);
            }
        }

        public void AddColor(Color color)
        {
            // Check if the color already exists
            foreach (var paletteColor in Colors)
            {
                if (paletteColor.Color.Equals(color))
                    return;
            }
            
            Colors.Add(new PaletteColor(color));
        }

        public void LoadDefault16BitColors()
        {
            Colors.Clear();
            
            AddColor(Color.FromArgb(0, 0, 0, 0));   // transparent
            // Black, White, Gray
            AddColor(Color.FromRgb(0, 0, 0));
            AddColor(Color.FromRgb(255, 255, 255));
            AddColor(Color.FromRgb(128, 128, 128));
            AddColor(Color.FromRgb(192, 192, 192));
            
            // Basic 16-bit colors (classic CGA/EGA/VGA palette)
            AddColor(Color.FromRgb(255, 0, 0));       // Red
            AddColor(Color.FromRgb(0, 255, 0));       // Green  
            AddColor(Color.FromRgb(0, 0, 255));       // Blue
            AddColor(Color.FromRgb(255, 255, 0));     // Yellow
            AddColor(Color.FromRgb(255, 0, 255));     // Magenta
            AddColor(Color.FromRgb(0, 255, 255));     // Cyan
            
            // Additional colors
            AddColor(Color.FromRgb(128, 0, 0));       // Dark Red
            AddColor(Color.FromRgb(0, 128, 0));       // Dark Green
            AddColor(Color.FromRgb(0, 0, 128));       // Dark Blue
            AddColor(Color.FromRgb(128, 128, 0));     // Dark Yellow/Olive
            AddColor(Color.FromRgb(128, 0, 128));     // Dark Magenta/Purple
            AddColor(Color.FromRgb(0, 128, 128));     // Dark Cyan/Teal
            
            // Common pixel art colors
            AddColor(Color.FromRgb(140, 80, 60));     // Brown
            AddColor(Color.FromRgb(255, 128, 0));     // Orange
            AddColor(Color.FromRgb(255, 192, 203));   // Pink
            AddColor(Color.FromRgb(173, 216, 230));   // Light Blue
        }
    }

    public class ColorSelectionData
    {
        public Color Color { get; set; }
        public bool IsLeftButton { get; set; }

        public ColorSelectionData(Color color, bool isLeftButton)
        {
            Color = color;
            IsLeftButton = isLeftButton;
        }
    }
}