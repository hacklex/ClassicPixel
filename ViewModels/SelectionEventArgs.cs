namespace PixelEditor.ViewModels;

public class SelectionEventArgs
{
    public int StartX { get; }
    public int StartY { get; }
    public int EndX { get; }
    public int EndY { get; }
    public SelectionMode Mode { get; }
    
    public SelectionEventArgs(int startX, int startY, int endX, int endY, SelectionMode mode = SelectionMode.Replace)
    {
        StartX = startX;
        StartY = startY;
        EndX = endX;
        EndY = endY;
        Mode = mode;
    }
}