using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;

namespace PixelEditor
{
    public class PixelEditor
    {
        private Color[,] _pixels;
        private readonly Color[,] _previewLayer;

        public int Width { get; private set; }
        public int Height { get; private set; }

        public bool IsWithinBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;
        
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

        public bool WritePixel(int x, int y, Color color) => IsWithinBounds(x,y) && (_pixels[x,y] = color) == color;
        public Color? ReadPixel(int x, int y) => IsWithinBounds(x, y) ? _pixels[x, y] : null;

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
                if (_pixels[cx, cy].IsSimilarTo(targetColor, tolerance))
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
                WritePixel(x, y, color);

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
            int xEnd = x0;
            float yEnd = y0 + gradient * (xEnd - x0);
            float xGap = 1.0f - ((x0 + 0.5f) - (float)Math.Floor(x0 + 0.5f));
            int xPxl1 = xEnd;
            int yPxl1 = (int)Math.Floor(yEnd);

            if (steep)
            {
                PlotAntialiased(yPxl1, xPxl1, color, (1.0f - (yEnd - Math.Floor(yEnd))) * xGap);
                PlotAntialiased(yPxl1 + 1, xPxl1, color, (yEnd - Math.Floor(yEnd)) * xGap);
            }
            else
            {
                PlotAntialiased(xPxl1, yPxl1, color, (1.0f - (yEnd - Math.Floor(yEnd))) * xGap);
                PlotAntialiased(xPxl1, yPxl1 + 1, color, (yEnd - Math.Floor(yEnd)) * xGap);
            }

            float interY = yEnd + gradient;

            // Handle second endpoint
            xEnd = x1;
            yEnd = y1 + gradient * (xEnd - x1);
            xGap = (x1 + 0.5f) - (float)Math.Floor(x1 + 0.5f);
            int xPxl2 = xEnd;
            int yPxl2 = (int)Math.Floor(yEnd);

            if (steep)
            {
                PlotAntialiased(yPxl2, xPxl2, color, (1.0f - (yEnd - Math.Floor(yEnd))) * xGap);
                PlotAntialiased(yPxl2 + 1, xPxl2, color, (yEnd - Math.Floor(yEnd)) * xGap);
            }
            else
            {
                PlotAntialiased(xPxl2, yPxl2, color, (1.0f - (yEnd - Math.Floor(yEnd))) * xGap);
                PlotAntialiased(xPxl2, yPxl2 + 1, color, (yEnd - Math.Floor(yEnd)) * xGap);
            }

            // Main loop
            for (int x = xPxl1 + 1; x < xPxl2; x++)
            {
                if (steep)
                {
                    PlotAntialiased((int)Math.Floor(interY), x, color, 1.0f - (interY - Math.Floor(interY)));
                    PlotAntialiased((int)Math.Floor(interY) + 1, x, color, interY - Math.Floor(interY));
                }
                else
                {
                    PlotAntialiased(x, (int)Math.Floor(interY), color, 1.0f - (interY - Math.Floor(interY)));
                    PlotAntialiased(x, (int)Math.Floor(interY) + 1, color, interY - Math.Floor(interY));
                }

                interY += gradient;
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
            int xEnd = x0;
            float yEnd = y0 + gradient * (xEnd - x0);
            float xGap = 1.0f - ((x0 + 0.5f) - (float)Math.Floor(x0 + 0.5f));
            int xPxl1 = xEnd;
            int yPxl1 = (int)Math.Floor(yEnd);

            if (steep)
            {
                PlotAntialiasedPreview(yPxl1, xPxl1, color, (1.0f - (yEnd - Math.Floor(yEnd))) * xGap);
                PlotAntialiasedPreview(yPxl1 + 1, xPxl1, color, (yEnd - Math.Floor(yEnd)) * xGap);
            }
            else
            {
                PlotAntialiasedPreview(xPxl1, yPxl1, color, (1.0f - (yEnd - Math.Floor(yEnd))) * xGap);
                PlotAntialiasedPreview(xPxl1, yPxl1 + 1, color, (yEnd - Math.Floor(yEnd)) * xGap);
            }

            float interY = yEnd + gradient;

            // Handle second endpoint
            xEnd = x1;
            yEnd = y1 + gradient * (xEnd - x1);
            xGap = (x1 + 0.5f) - (float)Math.Floor(x1 + 0.5f);
            int xPxl2 = xEnd;
            int yPxl2 = (int)Math.Floor(yEnd);

            if (steep)
            {
                PlotAntialiasedPreview(yPxl2, xPxl2, color, (1.0f - (yEnd - Math.Floor(yEnd))) * xGap);
                PlotAntialiasedPreview(yPxl2 + 1, xPxl2, color, (yEnd - Math.Floor(yEnd)) * xGap);
            }
            else
            {
                PlotAntialiasedPreview(xPxl2, yPxl2, color, (1.0f - (yEnd - Math.Floor(yEnd))) * xGap);
                PlotAntialiasedPreview(xPxl2, yPxl2 + 1, color, (yEnd - Math.Floor(yEnd)) * xGap);
            }

            // Main loop
            for (int x = xPxl1 + 1; x < xPxl2; x++)
            {
                if (steep)
                {
                    PlotAntialiasedPreview((int)Math.Floor(interY), x, color, 1.0f - (interY - Math.Floor(interY)));
                    PlotAntialiasedPreview((int)Math.Floor(interY) + 1, x, color, interY - Math.Floor(interY));
                }
                else
                {
                    PlotAntialiasedPreview(x, (int)Math.Floor(interY), color, 1.0f - (interY - Math.Floor(interY)));
                    PlotAntialiasedPreview(x, (int)Math.Floor(interY) + 1, color, interY - Math.Floor(interY));
                }

                interY += gradient;
            }
        }

        private void PlotAntialiased(int x, int y, Color color, double alpha)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                Color existingColor = _pixels[x, y];
                byte finalAlpha = (byte)Math.Round(color.A * alpha);

                if (existingColor.A == 0) _pixels[x, y] = color.WithAlphaTimes(alpha);
                else
                {
                    // Blend with existing color
                    var blendAlpha = finalAlpha / 255.0;
                    var r = (byte)Math.Round(color.R * blendAlpha + existingColor.R * (1 - blendAlpha));
                    var g = (byte)Math.Round(color.G * blendAlpha + existingColor.G * (1 - blendAlpha));
                    var b = (byte)Math.Round(color.B * blendAlpha + existingColor.B * (1 - blendAlpha));
                    var a = Math.Max(existingColor.A, finalAlpha);
                    _pixels[x, y] = Color.FromArgb(a, r, g, b);
                }
            }
        }

        private void PlotAntialiasedPreview(int x, int y, Color color, double alpha)
        {
            if (!IsWithinBounds(x, y)) return;
            _previewLayer[x, y] = color.WithAlphaTimes(alpha);
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
                        var distance = Math.Sqrt(i * i + j * j);
                        if (distance > brushSize / 2.0) continue;
                    }
                    
                    if (!IsWithinBounds(px, py)) continue; 
                    
                    _previewLayer[px, py] = color;
                }
            }
        }
        
        // Clear the preview layer (set all pixels to transparent)
        public void ClearPreview() => Array.Clear(_previewLayer);

        private void DrawEllipseInternal(Color[,] target, int x0, int y0, int x1, int y1, Color? borderColor, Color? fillColor, bool antialiased)
        {
            if (borderColor == null && fillColor == null) return;
            if (!antialiased)
            {
                DrawEllipseBresenhamTo(target, x0, y0, x1, y1, borderColor, fillColor);
                return;
            }
            using var bitmap = GetBitmap(target);
            bitmap.PaintOnCanvas(painter =>
            {
                var borderRect = new SKRect(x0+0.5f, y0+0.5f, x1+0.5f, y1+0.5f);
                if (fillColor != null)
                    painter.DrawOval(borderRect, new SKPaint()
                    {
                        IsAntialias = antialiased,
                        Color = new SKColor(fillColor.Value.ToUInt32()), IsStroke = false,
                    });
                if (borderColor != null)
                    painter.DrawOval(borderRect, new SKPaint()
                    {
                        StrokeWidth = 1,
                        IsAntialias = antialiased,
                        Color = new SKColor(borderColor.Value.ToUInt32()), IsStroke = true,
                    });
            });
            WritePixels(target, bitmap);
        }

        private WriteableBitmap GetBitmap(Color[,] pixels)
        {
            var bitmap = new WriteableBitmap(
                new PixelSize(Width, Height),
                new Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Unpremul);

            using var fb = bitmap.Lock();
            unsafe
            {
                var ptr = (uint*)fb.Address;
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        // Get the base pixel color
                        Color pixelColor = pixels[x, y];
       
                        // Convert to BGRA format
                        uint color = (uint)((pixelColor.A << 24) | (pixelColor.R << 16) | (pixelColor.G << 8) | pixelColor.B);
                        ptr[y * fb.RowBytes / 4 + x] = color;
                    }
                }
            }
            return bitmap;
        }

        private void WritePixels(Color[,] target, WriteableBitmap source)
        {
            if (source.PixelSize.Width != Width || source.PixelSize.Height != Height)
                throw new ArgumentException("Source bitmap size does not match editor size.");

            using var fb = source.Lock();
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
                        target[x, y] = Color.FromArgb(a, r, g, b);
                    }
                }
            }
        }
        
        public unsafe WriteableBitmap GetBitmap()
        {
            var bitmap = new WriteableBitmap(new PixelSize(Width, Height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
            using var fb = bitmap.Lock();
            var ptr = (uint*)fb.Address;
            for (var y = 0; y < Height; y++)
                for (var x = 0; x < Width; x++)
                {
                    // Get the base pixel color
                    var pixelColor = _pixels[x, y];

                    // Apply preview layer if available
                    if (_previewLayer != null)
                    {
                        Color previewColor = _previewLayer[x, y];
                        if (previewColor.A > 0)
                        {
                            pixelColor = previewColor.Over(pixelColor); //BlendColors(pixelColor, previewColor);
                        }
                    }
                    // Convert to BGRA format
                    var color = (uint)((pixelColor.A << 24) | (pixelColor.R << 16) | (pixelColor.G << 8) | pixelColor.B);
                    ptr[y * fb.RowBytes / 4 + x] = color;
                }
            return bitmap;
        }
        
        public void SaveToStream(Stream stream)
        {
            var bitmap = GetBitmap();
            bitmap.Save(stream);
        }

        public unsafe void LoadFromStream(Stream stream)
        {
            
            var bitmap = new Bitmap(stream);
            Width = bitmap.PixelSize.Width;
            Height = bitmap.PixelSize.Height;
            using var wb = new WriteableBitmap(bitmap.PixelSize, bitmap.Dpi, bitmap.Format, bitmap.AlphaFormat);
            bitmap.CopyPixels(wb.Lock(), AlphaFormat.Unpremul);
            
            _pixels = new Color[Width, Height];
            using var fb = wb.Lock();
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
                        WritePixel(x, y, fillColor);
                    }
                }
            }
            
            // Draw border (if enabled)
            if (drawBorder)
            {
                // Top and bottom edges
                for (int x = left; x <= right; x++)
                {
                    WritePixel(x, top, borderColor);
                    WritePixel(x, bottom, borderColor);
                }
                
                // Left and right edges
                for (int y = top; y <= bottom; y++)
                {
                    WritePixel(left, y, borderColor);
                    WritePixel(right, y, borderColor);
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

        // Draw an ellipse (optionally filled, optionally antialiased)
        private void DrawEllipseTo(Color[,] target, int x1, int y1, int x2, int y2, Color borderColor, Color fillColor, bool drawBorder, bool drawFill, bool antialiased)
        {
            if (target == _previewLayer) ClearPreview();
            // Normalize coordinates
            var left = Math.Min(x1, x2);
            var top = Math.Min(y1, y2);
            var right = Math.Max(x1, x2);
            var bottom = Math.Max(y1, y2);
            var width = right - left;
            var height = bottom - top;
            if (width == 0 || height == 0) return;

            DrawEllipseInternal(target, left, top, right, bottom, drawBorder ? borderColor : null,
                drawFill ? fillColor : null, antialiased);
             
        }

        public void DrawEllipse(int x1, int y1, int x2, int y2, Color borderColor, Color fillColor, bool drawBorder, bool drawFill, bool antialiased) 
            => DrawEllipseTo(_pixels, x1, y1, x2, y2, borderColor, fillColor, drawBorder, drawFill, antialiased);

        public void PreviewEllipse(int x1, int y1, int x2, int y2, Color borderColor, Color fillColor, bool drawBorder, bool drawFill, bool antialiased)
            => DrawEllipseTo(_previewLayer, x1, y1, x2, y2, borderColor, fillColor, drawBorder, drawFill, antialiased);


        static IEnumerable<(int x, int y)> PlotEllipseRect(int x0, int y0, int x1, int y1)
        {
            int Abs(int x) => x < 0 ? -x : x; /* absolute value */
            int a = Abs(x1 - x0), b = Abs(y1 - y0), b1 = b & 1; /* values of diameter */
            long dx = 4 * (1 - a) * b * b, dy = 4 * (b1 + 1) * a * a; /* error increment */
            var err = dx + dy + b1 * a * a; /* error of 1.step */

            if (x0 > x1)
            {
                x0 = x1;
                x1 += a;
            } /* if called with swapped points */

            if (y0 > y1) y0 = y1; /* then exchange them */
            y0 += (b + 1) / 2;
            y1 = y0 - b1; /* starting pixel */
            a *= 8 * a;
            b1 = 8 * b * b;

            do
            {
                yield return (x1, y0); /*   I. Quadrant */
                yield return (x0, y0); /*  II. Quadrant */
                yield return (x0, y1); /* III. Quadrant */
                yield return (x1, y1); /*  IV. Quadrant */
                var e2 = 2 * err;
                if (e2 <= dy)
                {
                    y0++;
                    y1--;
                    err += dy += a;
                } /* y step */

                if (e2 >= dx || 2 * err > dy)
                {
                    x0++;
                    x1--;
                    err += dx += b1;
                } /* x step */
            } while (x0 <= x1);

            while (y0 - y1 < b)
            {
                /* too early stop of flat ellipses a=1 */
                yield return (x0 - 1, y0); /* -> finish tip of ellipse */
                yield return (x1 + 1, y0++);
                yield return (x0 - 1, y1);
                yield return (x1 + 1, y1--);
            }
        }

        // Midpoint ellipse algorithm for border (non-antialiased)
        private void DrawEllipseBresenhamTo(Color[,] target, int x0, int y0, int x1, int y1, Color? borderColor, Color? fillColor)
        {
            if (borderColor is null && fillColor is null) return;
            var pointsByY = new Dictionary<int, List<int>>();
            foreach (var point in PlotEllipseRect(x0, y0, x1, y1))
            {
                int px = point.x;
                int py = point.y;
                if (!pointsByY.TryAdd(py, [px])) pointsByY[py].Add(px);
                if (IsWithinBounds(px, py))
                    PaintPixel(target, px, py, borderColor ?? fillColor ?? Colors.Transparent);
            }

            if (fillColor is null) return;
            var fillYs = pointsByY.Keys.OrderBy(y => y).ToList();
            foreach (var fillY in fillYs)
            {
                var fillXs = pointsByY[fillY].OrderBy(x => x).ToList();
                for (int fillX = fillXs[0]; fillX <= fillXs[^1]; fillX++)
                {
                    if (!fillXs.Contains(fillX) && IsWithinBounds(fillX, fillY))
                        PaintPixel(target, fillX, fillY, fillColor.Value);
                }
            }
        }
        
        private static void PaintPixel(Color[,] target, int x, int y, Color color)
        {
            if (!target.AcceptsIndices(x, y)) return;
            var existingColor = target[x, y];
            target[x, y] = color.Over(existingColor);
        }
    }
}
