using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Linq;

namespace PixelEditor
{
    public class PixelEditor
    {
        private Color[,] _pixels;
        private int _animationFrame = 0;
        private int _selectionLeft, _selectionTop, _selectionRight, _selectionBottom;
        
        public int Width { get; private set; }
        public int Height { get; private set; }

        public PixelEditor(int width, int height)
        {
            Width = width;
            Height = height;
            _pixels = new Color[width, height];
            
            // Initialize with transparent white
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Use fully transparent pixels by default
                    _pixels[x, y] = Color.FromArgb(0, 255, 255, 255);
                }
            }
        }

        public void DrawPixel(int x, int y, Color color)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                _pixels[x, y] = color;
            }
        }
        
        public void FloodFill(int x, int y, Color fillColor)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return;

            Color targetColor = _pixels[x, y];
            
            // Don't fill if the colors are the same
            if (targetColor.Equals(fillColor))
                return;

            Stack<Point> pixels = new Stack<Point>();
            pixels.Push(new Point(x, y));

            while (pixels.Count > 0)
            {
                Point current = pixels.Pop();
                int cx = (int)current.X;
                int cy = (int)current.Y;

                if (cx < 0 || cx >= Width || cy < 0 || cy >= Height)
                    continue;

                if (!_pixels[cx, cy].Equals(targetColor))
                    continue;

                _pixels[cx, cy] = fillColor;

                pixels.Push(new Point(cx + 1, cy));
                pixels.Push(new Point(cx - 1, cy));
                pixels.Push(new Point(cx, cy + 1));
                pixels.Push(new Point(cx, cy - 1));
            }
        }
        
        public void EraseSimilarPixels(int x, int y, int tolerance)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return;
        
            Color targetColor = _pixels[x, y];
            Color transparentColor = Color.FromArgb(0, 255, 255, 255);
            
            // Don't erase if already transparent
            if (targetColor.A == 0)
                return;
                
            Stack<Point> pixels = new Stack<Point>();
            pixels.Push(new Point(x, y));
        
            while (pixels.Count > 0)
            {
                Point current = pixels.Pop();
                int cx = (int)current.X;
                int cy = (int)current.Y;
        
                if (cx < 0 || cx >= Width || cy < 0 || cy >= Height)
                    continue;
                    
                // Only erase if color is similar to the target color
                if (!IsColorSimilar(_pixels[cx, cy], targetColor, tolerance))
                    continue;
                    
                // Don't process already erased pixels
                if (_pixels[cx, cy].A == 0)
                    continue;
        
                _pixels[cx, cy] = transparentColor;
        
                pixels.Push(new Point(cx + 1, cy));
                pixels.Push(new Point(cx - 1, cy));
                pixels.Push(new Point(cx, cy + 1));
                pixels.Push(new Point(cx, cy - 1));
            }
        }
        
        public void ReplaceSimilarColors(int x, int y, Color replacementColor, int tolerance)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return;
        
            Color targetColor = _pixels[x, y];
            
            // Don't replace if colors are identical
            if (targetColor.Equals(replacementColor))
                return;
                
            Stack<Point> pixels = new Stack<Point>();
            pixels.Push(new Point(x, y));
        
            while (pixels.Count > 0)
            {
                Point current = pixels.Pop();
                int cx = (int)current.X;
                int cy = (int)current.Y;
        
                if (cx < 0 || cx >= Width || cy < 0 || cy >= Height)
                    continue;
                    
                // Only replace if color is similar to the target color
                if (!IsColorSimilar(_pixels[cx, cy], targetColor, tolerance))
                    continue;
                    
                // Don't process already replaced pixels
                if (_pixels[cx, cy].Equals(replacementColor))
                    continue;
        
                _pixels[cx, cy] = replacementColor;
        
                pixels.Push(new Point(cx + 1, cy));
                pixels.Push(new Point(cx - 1, cy));
                pixels.Push(new Point(cx, cy + 1));
                pixels.Push(new Point(cx, cy - 1));
            }
        }
        
        public HashSet<(int x, int y)> MagicWandSelect(int x, int y, int tolerance = 32)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return new HashSet<(int x, int y)>();
        
            Color targetColor = _pixels[x, y];
            HashSet<(int x, int y)> selectedPixels = new HashSet<(int x, int y)>();
            Stack<(int x, int y)> pixelsToCheck = new Stack<(int x, int y)>();
            
            pixelsToCheck.Push((x, y));
            
            while (pixelsToCheck.Count > 0)
            {
                var (cx, cy) = pixelsToCheck.Pop();
                
                if (cx < 0 || cx >= Width || cy < 0 || cy >= Height)
                    continue;
                
                if (selectedPixels.Contains((cx, cy)))
                    continue;
                
                // Check if the color is similar enough to the target color
                if (IsColorSimilar(_pixels[cx, cy], targetColor, tolerance))
                {
                    selectedPixels.Add((cx, cy));
                    
                    // Add the 4-connected neighbors to check
                    pixelsToCheck.Push((cx + 1, cy));
                    pixelsToCheck.Push((cx - 1, cy));
                    pixelsToCheck.Push((cx, cy + 1));
                    pixelsToCheck.Push((cx, cy - 1));
                }
            }
            
            return selectedPixels;
        }
        
        public Color GetPixelColor(int x, int y)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                return _pixels[x, y];
            }
            return Color.FromArgb(0, 0, 0, 0); // Return transparent black for out of bounds
        }
        
        public bool IsColorSimilar(Color a, Color b, int tolerance)
        {
            int rDiff = Math.Abs(a.R - b.R);
            int gDiff = Math.Abs(a.G - b.G);
            int bDiff = Math.Abs(a.B - b.B);
            int aDiff = Math.Abs(a.A - b.A);
            
            // Calculate color distance (simple Manhattan distance)
            int distance = rDiff + gDiff + bDiff + aDiff;
            
            return distance <= tolerance;
        }

        public WriteableBitmap GetBitmap()
        {
            var bitmap = new WriteableBitmap(
                new PixelSize(Width, Height),
                new Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Unpremul);

            using (var fb = bitmap.Lock())
            {
                unsafe
                {
                    var ptr = (uint*)fb.Address;
                    for (int y = 0; y < Height; y++)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            // Get the base pixel color
                            Color pixelColor = _pixels[x, y];

                            // Convert to BGRA format
                            uint color = (uint)((pixelColor.A << 24) | (pixelColor.R << 16) | (pixelColor.G << 8) | pixelColor.B);
                            ptr[y * fb.RowBytes / 4 + x] = color;
                        }
                    }
                }
            }

            return bitmap;
        }

        private Color BlendColors(Color background, Color overlay)
        {
            // Simple alpha blending
            float alpha = overlay.A / 255.0f;
            byte r = (byte)(overlay.R * alpha + background.R * (1 - alpha));
            byte g = (byte)(overlay.G * alpha + background.G * (1 - alpha));
            byte b = (byte)(overlay.B * alpha + background.B * (1 - alpha));
            return Color.FromRgb(r, g, b);
        }

        public void SaveToStream(Stream stream)
        {
            var bitmap = GetBitmap();
            bitmap.Save(stream);
        }

        public void LoadFromStream(Stream stream)
        {
            
            var bitmap = new Bitmap(stream);
            Width = bitmap.PixelSize.Width;
            Height = bitmap.PixelSize.Height;
            using var wb = new WriteableBitmap(bitmap.PixelSize, bitmap.Dpi, bitmap.Format, bitmap.AlphaFormat);
            bitmap.CopyPixels(wb.Lock(), AlphaFormat.Unpremul);
            
            _pixels = new Color[Width, Height];
            using (var fb = wb.Lock())
            {
                unsafe
                {
                    var ptr = (uint*)fb.Address;
                    
                    for (int y = 0; y < Height; y++)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            uint pixel = ptr[y * fb.RowBytes / 4 + x];
                            
                            byte b = (byte)(pixel & 0xFF);
                            byte g = (byte)((pixel >> 8) & 0xFF);
                            byte r = (byte)((pixel >> 16) & 0xFF);
                            byte a = (byte)((pixel >> 24) & 0xFF);
                            
                            _pixels[x, y] = Color.FromArgb(a, r, g, b);
                        }
                    }
                }
            }
        }
    }
}
