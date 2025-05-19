using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Collections.ObjectModel;

namespace PixelEditorApp
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

    public partial class ColorPaletteControl : UserControl
    {
        public ObservableCollection<PaletteColor> Colors { get; } = new ObservableCollection<PaletteColor>();

        public event EventHandler<Color>? ColorSelected;

        public ColorPaletteControl()
        {
            InitializeComponent();
            ColorList.ItemsSource = Colors;
            ColorList.PointerPressed += ColorList_PointerPressed;
            LoadDefault16BitColors();
        }

        private void ColorList_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.Source is Border border && border.DataContext is PaletteColor paletteColor)
            {
                ColorSelected?.Invoke(this, paletteColor.Color);
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
            
            AddColor(Color.FromArgb(0, 0, 0, 0));   // Hot Pink
        }
    }
}
