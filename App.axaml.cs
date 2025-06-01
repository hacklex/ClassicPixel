using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Classic.CommonControls.Dialogs;

namespace PixelEditor
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void AppAbout_OnClick(object? sender, EventArgs e)
        {
            MessageBox.ShowDialog(Program.MainWindow!, $"ClassicPixel is an open-source Paint-inspired retro-styled image editor." +
                                                       $"{Environment.NewLine}{Environment.NewLine}" +
                                                       $"Created by Alex Rozanov aka hacklex in 2025." +
                                                       $"{Environment.NewLine}{Environment.NewLine}" +
                                                       $"Props to the Avalonia team for their amazing work on the Avalonia UI framework, " +
                                                       $"and to Claude Sonnet AI for doing the routine tasks :)",
                "About ClassicPixel", MessageBoxButtons.Ok, MessageBoxIcon.Information);
        }
    }
}
