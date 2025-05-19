using Avalonia;
using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace PixelEditorApp;

internal class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static Window? MainWindow => Application.Current?.ApplicationLifetime
        is IClassicDesktopStyleApplicationLifetime desktop
        ? desktop.MainWindow
        : null;
    
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}