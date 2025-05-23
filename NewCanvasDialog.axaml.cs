using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Classic.Avalonia.Theme;
using Classic.CommonControls.Dialogs;

namespace PixelEditor
{
    public partial class NewCanvasDialog : ClassicWindow
    {
        public int CanvasWidth { get; private set; }
        public int CanvasHeight { get; private set; }

        public NewCanvasDialog()
        {
            InitializeComponent();
        }

        private void OnCreateClick(object sender, RoutedEventArgs e)
        {
            try
            {
                CanvasWidth = (int)WidthInput.Value!;
                CanvasHeight = (int)HeightInput.Value!;
                Close(true);
            }
            catch (Exception)
            {
                if (Program.MainWindow != null)
                    MessageBox.ShowDialog(Program.MainWindow, "Error", "Invalid input. Please enter valid numbers.",
                        MessageBoxButtons.Ok, MessageBoxIcon.Error);
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Close(false);
        }

        private void OnUpDownLoaded(object? sender, RoutedEventArgs e)
        {
            if (sender is NumericUpDown n) n.Focus();
        }
    }
}
