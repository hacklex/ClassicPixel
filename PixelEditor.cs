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
        private Color[,]? _previewLayer;
        private int _animationFrame = 0;
        private int _selectionLeft, _selectionTop, _selectionRight, _selectionBottom;

        public int Width { get; private set; }
        public int Height { get; private set; }

        // Indicates whether the preview layer is currently being used
        public bool HasPreview => _previewLayer != null;

        public PixelEditor(int width, int height)
        {
            Width = width;
            Height = height;
            _pixels = new Color[width, height];
            _previewLayer = new Color[width, height];

            // Initialize with transparent white
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Use fully transparent pixels by default
                    _pixels[x, y] = Color.FromArgb(0, 255, 255, 255);
                    _previewLayer[x, y] = Color.FromArgb(0, 255, 255, 255);
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

        public void DrawLine(int x0, int y0, int x1, int y1, Color color, bool antialiased = false)
        {
            if (antialiased)
            {
                DrawLineAntialiased(x0, y0, x1, y1, color);
            }
            else
            {
                DrawLineBresenham(x0, y0, x1, y1, color);
            }
        }

        public void PreviewLine(int x0, int y0, int x1, int y1, Color color, bool antialiased = false)
        {
            // Clear previous preview first
            ClearPreview();

            if (antialiased)
            {
                PreviewLineAntialiased(x0, y0, x1, y1, color);
            }
            else
            {
                PreviewLineBresenham(x0, y0, x1, y1, color);
            }
        }

        private void DrawLineBresenham(int x0, int y0, int x1, int y1, Color color)
        {
            // Bresenham's line algorithm for pixel-perfect lines
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            int x = x0;
            int y = y0;

            while (true)
            {
                DrawPixel(x, y, color);

                if (x == x1 && y == y1) break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }
        }

        private void PreviewLineBresenham(int x0, int y0, int x1, int y1, Color color)
        {
            // Bresenham's line algorithm for preview layer
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            int x = x0;
            int y = y0;

            while (true)
            {
                if (x >= 0 && x < Width && y >= 0 && y < Height)
                {
                    // Use semi-transparent version for preview
                    byte alpha = Math.Min((byte)128, color.A);
                    _previewLayer[x, y] = Color.FromArgb(alpha, color.R, color.G, color.B);
                }

                if (x == x1 && y == y1) break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y += sy;
                }
            }
        }

        private void DrawLineAntialiased(int x0, int y0, int x1, int y1, Color color)
        {
            // Wu's line algorithm for antialiased lines
            bool steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);

            if (steep)
            {
                // Swap x and y coordinates
                (x0, y0) = (y0, x0);
                (x1, y1) = (y1, x1);
            }

            if (x0 > x1)
            {
                // Swap points to ensure x0 < x1
                (x0, x1) = (x1, x0);
                (y0, y1) = (y1, y0);
            }

            float dx = x1 - x0;
            float dy = y1 - y0;
            float gradient = dy / dx;

            // Handle first endpoint
            int xend = x0;
            float yend = y0 + gradient * (xend - x0);
            float xgap = 1.0f - ((x0 + 0.5f) - (float)Math.Floor(x0 + 0.5f));
            int xpxl1 = xend;
            int ypxl1 = (int)Math.Floor(yend);

            if (steep)
            {
                PlotAntialiased(ypxl1, xpxl1, color, (1.0f - (yend - Math.Floor(yend))) * xgap);
                PlotAntialiased(ypxl1 + 1, xpxl1, color, (yend - Math.Floor(yend)) * xgap);
            }
            else
            {
                PlotAntialiased(xpxl1, ypxl1, color, (1.0f - (yend - Math.Floor(yend))) * xgap);
                PlotAntialiased(xpxl1, ypxl1 + 1, color, (yend - Math.Floor(yend)) * xgap);
            }

            float intery = yend + gradient;

            // Handle second endpoint
            xend = x1;
            yend = y1 + gradient * (xend - x1);
            xgap = (x1 + 0.5f) - (float)Math.Floor(x1 + 0.5f);
            int xpxl2 = xend;
            int ypxl2 = (int)Math.Floor(yend);

            if (steep)
            {
                PlotAntialiased(ypxl2, xpxl2, color, (1.0f - (yend - Math.Floor(yend))) * xgap);
                PlotAntialiased(ypxl2 + 1, xpxl2, color, (yend - Math.Floor(yend)) * xgap);
            }
            else
            {
                PlotAntialiased(xpxl2, ypxl2, color, (1.0f - (yend - Math.Floor(yend))) * xgap);
                PlotAntialiased(xpxl2, ypxl2 + 1, color, (yend - Math.Floor(yend)) * xgap);
            }

            // Main loop
            for (int x = xpxl1 + 1; x < xpxl2; x++)
            {
                if (steep)
                {
                    PlotAntialiased((int)Math.Floor(intery), x, color, 1.0f - (intery - Math.Floor(intery)));
                    PlotAntialiased((int)Math.Floor(intery) + 1, x, color, intery - Math.Floor(intery));
                }
                else
                {
                    PlotAntialiased(x, (int)Math.Floor(intery), color, 1.0f - (intery - Math.Floor(intery)));
                    PlotAntialiased(x, (int)Math.Floor(intery) + 1, color, intery - Math.Floor(intery));
                }

                intery += gradient;
            }
        }

        private void PreviewLineAntialiased(int x0, int y0, int x1, int y1, Color color)
        {
            // Wu's line algorithm for antialiased line preview
            bool steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);

            if (steep)
            {
                // Swap x and y coordinates
                (x0, y0) = (y0, x0);
                (x1, y1) = (y1, x1);
            }

            if (x0 > x1)
            {
                // Swap points to ensure x0 < x1
                (x0, x1) = (x1, x0);
                (y0, y1) = (y1, y0);
            }

            float dx = x1 - x0;
            float dy = y1 - y0;
            float gradient = dy / dx;

            // Handle first endpoint
            int xend = x0;
            float yend = y0 + gradient * (xend - x0);
            float xgap = 1.0f - ((x0 + 0.5f) - (float)Math.Floor(x0 + 0.5f));
            int xpxl1 = xend;
            int ypxl1 = (int)Math.Floor(yend);

            if (steep)
            {
                PlotAntialiastedPreview(ypxl1, xpxl1, color, (1.0f - (yend - Math.Floor(yend))) * xgap);
                PlotAntialiastedPreview(ypxl1 + 1, xpxl1, color, (yend - Math.Floor(yend)) * xgap);
            }
            else
            {
                PlotAntialiastedPreview(xpxl1, ypxl1, color, (1.0f - (yend - Math.Floor(yend))) * xgap);
                PlotAntialiastedPreview(xpxl1, ypxl1 + 1, color, (yend - Math.Floor(yend)) * xgap);
            }

            float intery = yend + gradient;

            // Handle second endpoint
            xend = x1;
            yend = y1 + gradient * (xend - x1);
            xgap = (x1 + 0.5f) - (float)Math.Floor(x1 + 0.5f);
            int xpxl2 = xend;
            int ypxl2 = (int)Math.Floor(yend);

            if (steep)
            {
                PlotAntialiastedPreview(ypxl2, xpxl2, color, (1.0f - (yend - Math.Floor(yend))) * xgap);
                PlotAntialiastedPreview(ypxl2 + 1, xpxl2, color, (yend - Math.Floor(yend)) * xgap);
            }
            else
            {
                PlotAntialiastedPreview(xpxl2, ypxl2, color, (1.0f - (yend - Math.Floor(yend))) * xgap);
                PlotAntialiastedPreview(xpxl2, ypxl2 + 1, color, (yend - Math.Floor(yend)) * xgap);
            }

            // Main loop
            for (int x = xpxl1 + 1; x < xpxl2; x++)
            {
                if (steep)
                {
                    PlotAntialiastedPreview((int)Math.Floor(intery), x, color, 1.0f - (intery - Math.Floor(intery)));
                    PlotAntialiastedPreview((int)Math.Floor(intery) + 1, x, color, intery - Math.Floor(intery));
                }
                else
                {
                    PlotAntialiastedPreview(x, (int)Math.Floor(intery), color, 1.0f - (intery - Math.Floor(intery)));
                    PlotAntialiastedPreview(x, (int)Math.Floor(intery) + 1, color, intery - Math.Floor(intery));
                }

                intery += gradient;
            }
        }

        private void PlotAntialiased(int x, int y, Color color, double alpha)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                Color existingColor = _pixels[x, y];
                byte finalAlpha = (byte)(color.A * alpha);

                if (existingColor.A == 0)
                {
                    // No existing color, just use the new color with alpha
                    _pixels[x, y] = Color.FromArgb(finalAlpha, color.R, color.G, color.B);
                }
                else
                {
                    // Blend with existing color
                    float blendAlpha = finalAlpha / 255.0f;
                    byte r = (byte)(color.R * blendAlpha + existingColor.R * (1 - blendAlpha));
                    byte g = (byte)(color.G * blendAlpha + existingColor.G * (1 - blendAlpha));
                    byte b = (byte)(color.B * blendAlpha + existingColor.B * (1 - blendAlpha));
                    byte a = (byte)Math.Max(existingColor.A, finalAlpha);

                    _pixels[x, y] = Color.FromArgb(a, r, g, b);
                }
            }
        }

        private void PlotAntialiastedPreview(int x, int y, Color color, double alpha)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                byte finalAlpha = (byte)(Math.Min((byte)128, color.A) * alpha); // Keep preview semi-transparent
                _previewLayer[x, y] = Color.FromArgb(finalAlpha, color.R, color.G, color.B);
            }
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
        
        // Update the preview layer to show a brush tip at the specified position
        public void UpdatePreview(int x, int y, Color color, int brushSize = 1)
        {
            // Clear previous preview
            ClearPreview();
            
            // Create a simple circular/square brush preview based on brush size
            for (int i = -brushSize / 2; i <= brushSize / 2; i++)
            {
                for (int j = -brushSize / 2; j <= brushSize / 2; j++)
                {
                    int px = x + i;
                    int py = y + j;
                    
                    // Simple circular brush check (for brushSize > 1)
                    if (brushSize > 1)
                    {
                        double distance = Math.Sqrt(i * i + j * j);
                        if (distance > brushSize / 2.0)
                            continue;
                    }
                    
                    if (px >= 0 && px < Width && py >= 0 && py < Height)
                    {
                        // Use a semi-transparent version of the color for preview
                        byte alpha = Math.Min((byte)128, color.A);
                        _previewLayer[px, py] = Color.FromArgb(alpha, color.R, color.G, color.B);
                    }
                }
            }
        }
        
        // Clear the preview layer (set all pixels to transparent)
        public void ClearPreview()
        {
            if (_previewLayer == null)
                return;
                
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _previewLayer[x, y] = Color.FromArgb(0, 255, 255, 255);
                }
            }
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

                            // Apply preview layer if available
                            if (_previewLayer != null)
                            {
                                Color previewColor = _previewLayer[x, y];
                                if (previewColor.A > 0)
                                {
                                    pixelColor = BlendColors(pixelColor, previewColor);
                                }
                            }
        
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

        public void DrawRectangle(int x1, int y1, int x2, int y2, Color borderColor, Color fillColor, bool drawBorder, bool drawFill)
        {
            // Normalize coordinates
            int left = Math.Min(x1, x2);
            int top = Math.Min(y1, y2);
            int right = Math.Max(x1, x2);
            int bottom = Math.Max(y1, y2);
            
            // Draw fill first (if enabled)
            if (drawFill)
            {
                for (int x = left; x <= right; x++)
                {
                    for (int y = top; y <= bottom; y++)
                    {
                        DrawPixel(x, y, fillColor);
                    }
                }
            }
            
            // Draw border (if enabled)
            if (drawBorder)
            {
                // Top and bottom edges
                for (int x = left; x <= right; x++)
                {
                    DrawPixel(x, top, borderColor);
                    DrawPixel(x, bottom, borderColor);
                }
                
                // Left and right edges
                for (int y = top; y <= bottom; y++)
                {
                    DrawPixel(left, y, borderColor);
                    DrawPixel(right, y, borderColor);
                }
            }
        }
        
        public void PreviewRectangle(int x1, int y1, int x2, int y2, Color borderColor, Color fillColor, bool drawBorder, bool drawFill)
        {
            // Clear previous preview first
            ClearPreview();
            
            // Normalize coordinates
            int left = Math.Min(x1, x2);
            int top = Math.Min(y1, y2);
            int right = Math.Max(x1, x2);
            int bottom = Math.Max(y1, y2);
            
            // Draw fill first (if enabled)
            if (drawFill)
            {
                for (int x = left; x <= right; x++)
                {
                    for (int y = top; y <= bottom; y++)
                    {
                        if (x >= 0 && x < Width && y >= 0 && y < Height)
                        {
                            byte alpha = Math.Min((byte)128, fillColor.A);
                            _previewLayer[x, y] = Color.FromArgb(alpha, fillColor.R, fillColor.G, fillColor.B);
                        }
                    }
                }
            }
            
            // Draw border (if enabled)
            if (drawBorder)
            {
                // Top and bottom edges
                for (int x = left; x <= right; x++)
                {
                    if (x >= 0 && x < Width && top >= 0 && top < Height)
                    {
                        byte alpha = Math.Min((byte)128, borderColor.A);
                        _previewLayer[x, top] = Color.FromArgb(alpha, borderColor.R, borderColor.G, borderColor.B);
                    }
                    if (x >= 0 && x < Width && bottom >= 0 && bottom < Height)
                    {
                        byte alpha = Math.Min((byte)128, borderColor.A);
                        _previewLayer[x, bottom] = Color.FromArgb(alpha, borderColor.R, borderColor.G, borderColor.B);
                    }
                }
                
                // Left and right edges
                for (int y = top; y <= bottom; y++)
                {
                    if (left >= 0 && left < Width && y >= 0 && y < Height)
                    {
                        byte alpha = Math.Min((byte)128, borderColor.A);
                        _previewLayer[left, y] = Color.FromArgb(alpha, borderColor.R, borderColor.G, borderColor.B);
                    }
                    if (right >= 0 && right < Width && y >= 0 && y < Height)
                    {
                        byte alpha = Math.Min((byte)128, borderColor.A);
                        _previewLayer[right, y] = Color.FromArgb(alpha, borderColor.R, borderColor.G, borderColor.B);
                    }
                }
            }
        }
    }
}
