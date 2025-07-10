using System;
using Avalonia.Media.Imaging;
using SkiaSharp;

namespace PixelEditor;

public static class WriteableBitmapExtensions
{
    public static void PaintOnCanvas(this WriteableBitmap writeableBitmap, Action<SKCanvas> painter)
    {
        if (writeableBitmap is null)
            throw new ArgumentNullException(nameof(writeableBitmap));
  
        using var lockedBitmap = writeableBitmap.Lock();
        var info = new SKImageInfo(writeableBitmap.PixelSize.Width, writeableBitmap.PixelSize.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var skBitmap = new SKBitmap();
        skBitmap.InstallPixels(info, lockedBitmap.Address, info.RowBytes);
        using var canvas = new SKCanvas(skBitmap);
        painter(canvas);
    }
    
}