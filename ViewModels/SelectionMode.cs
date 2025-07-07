namespace PixelEditor.ViewModels;

public enum SelectionMode
{
    Replace,  // Replace the current selection
    Add,      // Add to the current selection (Ctrl+drag)
    Subtract  // Subtract from the current selection (Alt+drag)
}