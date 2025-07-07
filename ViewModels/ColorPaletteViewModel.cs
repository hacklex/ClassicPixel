using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PixelEditor.ViewModels
{
    public partial class ColorPaletteViewModel : ViewModelBase
    {
        [ObservableProperty] private Color _selectedColor;
        private Color? _colorUnderCursor;

        public Color? ColorUnderCursor
        {
            get => _colorUnderCursor;
            set
            {
                SetProperty(ref _colorUnderCursor, value);
                foreach (var paletteColor in Colors)
                    paletteColor.IsUnderCursor = paletteColor.Color == value || (paletteColor.Color.A == 0 && value is { A: 0 });
            }
        }

        public ObservableCollection<PaletteColor> Colors { get; } = new();
        
        public ICommand RemoveColorCommand { get; }
        public ICommand SelectColorCommand { get; }
        public ICommand LoadDefaultColorsCommand { get; }

        public event ColorChangedEventHandler? ColorSelected;

        public ColorPaletteViewModel()
        {
            RemoveColorCommand = new RelayCommand(RemoveColor);
            SelectColorCommand = new RelayCommand(SelectColor);
            LoadDefaultColorsCommand = new RelayCommand(_ => LoadDefaultColors());
            LoadDefaultColors();
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
            Colors.Add(new PaletteColor(color) { IsUnderCursor = (color == ColorUnderCursor) || (ColorUnderCursor is { A: 0 } && color.A == 0) });
        }

        public static ColorPaletteViewModel Dummy
        {
            get
            {
                var x = new ColorPaletteViewModel();
                x.LoadDefaultColors();
                return x;
            }
        }

        public void LoadDefaultColors()
        {
            Colors.Clear();

            
            AddColor(Color.FromRgb(0, 0, 0));
            AddColor(Color.FromRgb(255, 255, 255));
            
            AddColor(Color.FromRgb(128, 128, 128));
            AddColor(Color.FromRgb(192, 192, 192));
            
            // Basic 16-bit colors (classic CGA/EGA/VGA palette)
            AddColor(Color.FromRgb(128, 0, 0));       // Dark Red
            AddColor(Color.FromRgb(255, 0, 0));       // Red
            
            AddColor(Color.FromRgb(255, 0, 128));     // intense pink
            AddColor(Color.FromRgb(255, 192, 203));   // light pink
            
            AddColor(Color.FromRgb(128, 64, 0));     // brown
            AddColor(Color.FromRgb(255, 128, 0));     // intense orange
            
            AddColor(Color.FromRgb(140, 80, 60));     // light brown
            AddColor(Color.FromRgb(255, 128, 64));     // light orange
            
            AddColor(Color.FromRgb(128, 128, 0));     // Dark Yellow/Olive
            AddColor(Color.FromRgb(255, 255, 0));     // Yellow
            // Common pixel art colors
            AddColor(Color.FromRgb(128, 128, 64));     // weak olive
            AddColor(Color.FromRgb(255, 255, 128));     // weak yellow
            
            AddColor(Color.FromRgb(72, 114, 0));       // Dark Green
            AddColor(Color.FromRgb(80, 255, 0));  
            
            AddColor(Color.FromRgb(0, 128, 0));       // Dark Green
            AddColor(Color.FromRgb(0, 255, 0));       // Green  
            
            AddColor(Color.FromRgb(0, 128, 0));       // Dark Green
            AddColor(Color.FromRgb(0, 255, 0));       // Green  
            
            AddColor(Color.FromRgb(0, 96, 96));     // Dark Teal
            AddColor(Color.FromRgb(0, 255, 128));   // Light Green
            
            
            AddColor(Color.FromRgb(0, 128, 128));     // Dark Cyan
            AddColor(Color.FromRgb(0, 255, 255));     // Cyan
            
            AddColor(Color.FromRgb(0, 64, 128));     // Deep Blue
            AddColor(Color.FromRgb(128, 255, 255));     // Sky Blue
            
            AddColor(Color.FromRgb(0, 0, 128));       // Dark Blue
            AddColor(Color.FromRgb(0, 128, 255));     // Light Blue
            
            AddColor(Color.FromRgb(0, 0, 255));       // Blue
            AddColor(Color.FromRgb(56, 80, 255));   // lighter blue
            
            AddColor(Color.FromRgb(64, 0, 255));     // deep purple
            AddColor(Color.FromRgb(128, 128, 255));     // weak light blue
            
            AddColor(Color.FromRgb(128, 0, 128));     // Dark Magenta/Purple
            AddColor(Color.FromRgb(255, 0, 255));     // Magenta
            
            // //AddColor(Color.FromArgb(0, 0, 0, 0));   // transparent
            // // Black, White, Gray
            // AddColor(Color.FromRgb(0, 0, 0));
            // AddColor(Color.FromRgb(255, 255, 255));
            // AddColor(Color.FromRgb(128, 128, 128));
            // AddColor(Color.FromRgb(192, 192, 192));
            //
            // // Basic 16-bit colors (classic CGA/EGA/VGA palette)
            // AddColor(Color.FromRgb(255, 0, 0));       // Red
            // AddColor(Color.FromRgb(0, 255, 0));       // Green  
            // AddColor(Color.FromRgb(0, 0, 255));       // Blue
            // AddColor(Color.FromRgb(255, 255, 0));     // Yellow
            // AddColor(Color.FromRgb(255, 0, 255));     // Magenta
            // AddColor(Color.FromRgb(0, 255, 255));     // Cyan
            //
            // // Additional colors
            // AddColor(Color.FromRgb(128, 0, 0));       // Dark Red
            // AddColor(Color.FromRgb(0, 128, 0));       // Dark Green
            // AddColor(Color.FromRgb(0, 0, 128));       // Dark Blue
            // AddColor(Color.FromRgb(128, 128, 0));     // Dark Yellow/Olive
            // AddColor(Color.FromRgb(128, 0, 128));     // Dark Magenta/Purple
            // AddColor(Color.FromRgb(0, 128, 128));     // Dark Cyan/Teal
            //
            // // Common pixel art colors
            // AddColor(Color.FromRgb(140, 80, 60));     // Brown
            // AddColor(Color.FromRgb(255, 128, 0));     // Orange
            // AddColor(Color.FromRgb(255, 192, 203));   // Pink
            // AddColor(Color.FromRgb(173, 216, 230));   // Light Blue
        }
    }

    public record ColorSelectionData(Color Color, bool IsLeftButton);
}