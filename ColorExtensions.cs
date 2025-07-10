using System;
using Avalonia.Media;

namespace PixelEditor;

public static class ColorExtensions
{
    public static bool IsSimilarTo(this Color a, Color b, int tolerance)
    {
        int rDiff = Math.Abs(a.R - b.R);
        int gDiff = Math.Abs(a.G - b.G);
        int bDiff = Math.Abs(a.B - b.B);
        int aDiff = Math.Abs(a.A - b.A);
            
        // Calculate color distance (simple Manhattan distance)
        int distance = rDiff + gDiff + bDiff + aDiff;
            
        return distance <= tolerance;
    }

    public static Color WithAlpha(this Color color, double alpha) => new Color((byte)Math.Round(alpha * 255), color.R, color.G, color.B);
    public static Color WithAlpha(this Color color, byte alpha) => new Color(alpha, color.R, color.G, color.B);
    public static Color WithAlphaTimes(this Color color, double alphaTimes)
    {
        double alpha = color.A / 255.0 * alphaTimes;
        return new Color((byte)Math.Round(alpha * 255), color.R, color.G, color.B);
    }

    public static Color Over(this Color a, Color b)
    { 
        if (a.A == 0) return b;
        if (b.A == 0) return a;
        double alpha = a.A / 255.0 + b.A / 255.0 * (1 - a.A / 255.0);
        if (alpha == 0) return Colors.Transparent;
        double r = (a.R * a.A / 255.0 + b.R * b.A / 255.0 * (1 - a.A / 255.0)) / alpha;
        double g = (a.G * a.A / 255.0 + b.G * b.A / 255.0 * (1 - a.A / 255.0)) / alpha;
        double bValue = (a.B * a.A / 255.0 + b.B * b.A / 255.0 * (1 - a.A / 255.0)) / alpha;
        return new Color((byte)Math.Round(alpha * 255), (byte)Math.Round(r), (byte)Math.Round(g), (byte)Math.Round(bValue));
    }
}

public static class ArrayExtensions
{
    public static bool AcceptsIndices<T>(this T[,]? array, int index1, int index2)
    {
        if (array == null) return false;
        return index1 >= 0 && index1 < array.GetLength(0) && index2 >= 0 && index2 < array.GetLength(1);
    }
}