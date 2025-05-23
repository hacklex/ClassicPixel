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
        private Color[,] _selectionOverlay;
        private bool _hasSelection;
        private int _animationFrame = 0;
        private readonly Color[] _selectionColors = new[] 
        { 
            Color.FromArgb(255, 255, 255, 255), 
            Color.FromArgb(255, 230, 230, 230),
            Color.FromArgb(255, 200, 200, 200),
            Color.FromArgb(255, 170, 170, 170),
            Color.FromArgb(255, 140, 140, 140),
            Color.FromArgb(255, 110, 110, 110),
            Color.FromArgb(255, 80, 80, 80),
            Color.FromArgb(255, 50, 50, 50),
            Color.FromArgb(255, 0, 0, 0)
        };
        private int _selectionLeft, _selectionTop, _selectionRight, _selectionBottom;
        
        public int Width { get; private set; }
        public int Height { get; private set; }

        public PixelEditor(int width, int height)
        {
            Width = width;
            Height = height;
            _pixels = new Color[width, height];
            _selectionOverlay = new Color[width, height];
            
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

        public void StartSelection()
        {
            // Clear selection overlay
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _selectionOverlay[x, y] = Colors.Transparent;
                }
            }
            _hasSelection = true;
            _animationFrame = 0;
        }

        public void UpdateSelectionPreview(int startX, int startY, int endX, int endY)
        {
            // Reset overlay
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _selectionOverlay[x, y] = Colors.Transparent;
                }
            }

            // Calculate selection rectangle
            _selectionLeft = Math.Min(startX, endX);
            _selectionTop = Math.Min(startY, endY);
            _selectionRight = Math.Max(startX, endX);
            _selectionBottom = Math.Max(startY, endY);
        
            // Draw selection border using current animation frame color
            Color selectionColor = _selectionColors[_animationFrame % _selectionColors.Length];
        
            // Draw the selection border
            DrawSelectionBorder(_selectionLeft, _selectionTop, _selectionRight, _selectionBottom, selectionColor);
        }
        
        private void DrawSelectionBorder(int left, int top, int right, int bottom, Color color)
        {
            // Draw horizontal lines
            for (int x = left; x <= right; x++)
            {
                if (x >= 0 && x < Width)
                {
                    if (top >= 0 && top < Height)
                        _selectionOverlay[x, top] = color;
                    if (bottom >= 0 && bottom < Height)
                        _selectionOverlay[x, bottom] = color;
                }
            }
        
            // Draw vertical lines
            for (int y = top; y <= bottom; y++)
            {
                if (y >= 0 && y < Height)
                {
                    if (left >= 0 && left < Width)
                        _selectionOverlay[left, y] = color;
                    if (right >= 0 && right < Width)
                        _selectionOverlay[right, y] = color;
                }
            }
        }

        public void FinishSelection(int startX, int startY, int endX, int endY)
        {
            // Don't clear selection, just update the bounds
            _selectionLeft = Math.Min(startX, endX);
            _selectionTop = Math.Min(startY, endY);
            _selectionRight = Math.Max(startX, endX);
            _selectionBottom = Math.Max(startY, endY);
            
            // If there's no actual area selected (just a point), clear selection
            if (_selectionLeft == _selectionRight && _selectionTop == _selectionBottom)
            {
                ClearSelection();
                return;
            }
            
            _hasSelection = true;
            
            // Update with current animation frame
            UpdateSelectionAnimation();
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

        public WriteableBitmap GetBitmap()
        {
            var bitmap = new WriteableBitmap(
                new PixelSize(Width, Height),
                new Vector(96, 96),
                Avalonia.Platform.PixelFormat.Bgra8888,
                Avalonia.Platform.AlphaFormat.Premul);

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

                            // Apply selection overlay if needed
                            if (_hasSelection && _selectionOverlay[x, y].A > 0)
                            {
                                // Alpha blend the selection with the pixel
                                pixelColor = BlendColors(pixelColor, _selectionOverlay[x, y]);
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

        public void UpdateSelectionAnimation()
        {
            if (!_hasSelection)
                return;
                
            // Advance animation frame
            _animationFrame = (_animationFrame + 1) % _selectionColors.Length;
            
            // Clear previous frame
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _selectionOverlay[x, y] = Colors.Transparent;
                }
            }
            
            // Draw the selection border using the current frame's color
            Color selectionColor = _selectionColors[_animationFrame];
            DrawSelectionBorder(_selectionLeft, _selectionTop, _selectionRight, _selectionBottom, selectionColor);
        }
        
        public void ClearSelection()
        {
            _hasSelection = false;
            
            // Clear overlay
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    _selectionOverlay[x, y] = Colors.Transparent;
                }
            }
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
            _selectionOverlay = new Color[Width, Height];

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
                            _selectionOverlay[x, y] = Colors.Transparent;
                        }
                    }
                }
            }
        }
    }
}
