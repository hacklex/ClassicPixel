using Avalonia.Media;

namespace PixelEditor.ViewModels;

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