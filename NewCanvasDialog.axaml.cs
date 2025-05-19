using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PixelEditorApp
{
    public partial class NewCanvasDialog : Window
    {
        public int CanvasWidth { get; private set; }
        public int CanvasHeight { get; private set; }

        public NewCanvasDialog()
        {
            InitializeComponent();
        }

        private void OnCreateClick(object sender, RoutedEventArgs e)
        {
            CanvasWidth = (int)WidthInput.Value;
            CanvasHeight = (int)HeightInput.Value;
            Close(true);
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}
