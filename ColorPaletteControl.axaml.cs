using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Classic.Avalonia.Theme;
using PixelEditor.ViewModels;

namespace PixelEditor
{
    public class PaletteColor
    {
        public Color Color { get; set; }
        public IBrush ColorBrush => new SolidColorBrush(Color);

        public PaletteColor(Color color)
        {
            Color = color;
        }
    }
    
    public delegate void ColorChangedEventHandler(Color selectedColor, bool isLeftButton);

    public partial class ColorPaletteControl : UserControl
    {
        public ObservableCollection<PaletteColor> Colors { get; } = new ObservableCollection<PaletteColor>();

        private void OnColorPressed(object sender, PointerPressedEventArgs e)
        {
            if (sender is ClassicBorderDecorator border && border.DataContext is PaletteColor paletteColor && 
                DataContext is ColorPaletteViewModel viewModel)
            {
                var isLeftButton = e.GetCurrentPoint(border).Properties.IsLeftButtonPressed;
                viewModel.SelectColorCommand.Execute(new ColorSelectionData(paletteColor.Color, isLeftButton));
            }
        }

        public event ColorChangedEventHandler? ColorSelected;

        public ColorPaletteControl()
        {
            InitializeComponent(); 
        }

        private void ColorList_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.Source is Border border && border.DataContext is PaletteColor paletteColor)
            {
                var isLeftButton = e.GetCurrentPoint(border).Properties.IsLeftButtonPressed;
                ColorSelected?.Invoke(paletteColor.Color, isLeftButton);
            }
        }

        private void OnRemoveColorClick(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.CommandParameter is PaletteColor paletteColor)
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
            AddColor(Color.FromRgb(128, 0, 0));       // Dark Red
            AddColor(Color.FromRgb(255, 0, 0));       // Red
            
            AddColor(Color.FromRgb(128, 128, 0));     // Dark Yellow/Olive
            AddColor(Color.FromRgb(255, 255, 0));     // Yellow
            
            AddColor(Color.FromRgb(0, 128, 0));       // Dark Green
            AddColor(Color.FromRgb(0, 255, 0));       // Green  
            
            AddColor(Color.FromRgb(0, 128, 128));     // Dark Cyan
            AddColor(Color.FromRgb(0, 255, 255));     // Cyan
            
            AddColor(Color.FromRgb(0, 0, 128));       // Dark Blue
            AddColor(Color.FromRgb(0, 0, 255));       // Blue
            
            AddColor(Color.FromRgb(128, 0, 128));     // Dark Magenta/Purple
            AddColor(Color.FromRgb(255, 0, 255));     // Magenta
            
            // Common pixel art colors
            AddColor(Color.FromRgb(128, 128, 64));     // weak olive
            AddColor(Color.FromRgb(255, 255, 128));     // weak yellow
            
            AddColor(Color.FromRgb(0, 64, 64));     // Dark Teal
            AddColor(Color.FromRgb(0, 255, 128));   // Light Green
            
            AddColor(Color.FromRgb(0, 128, 255));     // Light Blue
            AddColor(Color.FromRgb(128, 255, 255));     // Sky Blue
            
            AddColor(Color.FromRgb(0, 64, 128));     // Deep Blue
            AddColor(Color.FromRgb(128, 128, 255));     // weak light blue
            
            AddColor(Color.FromRgb(64, 0, 255));     // deep purple
            AddColor(Color.FromRgb(255, 0, 128));     // intense pink
            
            AddColor(Color.FromRgb(128, 64, 0));     // brown
            AddColor(Color.FromRgb(255, 128, 64));     // light orange
            
            // Common pixel art colors
            AddColor(Color.FromRgb(140, 80, 60));     // light brown
            AddColor(Color.FromRgb(255, 128, 0));     // intense orange
            
            AddColor(Color.FromRgb(255, 192, 203));   // light pink
            AddColor(Color.FromRgb(173, 216, 230));   // very light blue
            
        }
    }
}
