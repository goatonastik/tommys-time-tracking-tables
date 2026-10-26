using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TimersApp.Core;

namespace TimersApp.Desktop;
internal static class Visuals
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Brush> brushes = new();
    public static Brush Brush(string color)
    {
        try { if (brushes.TryGetValue(color, out var cached)) return cached; var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)); brush.Freeze(); if (brushes.Count > 256) brushes.Clear(); brushes[color] = brush; return brush; }
        catch { return Brushes.SlateGray; }
    }
    public static bool ValidColor(string color) { try { _ = (Color)ColorConverter.ConvertFromString(color); return true; } catch { return false; } }
}
internal sealed class ImageBackdrop : FrameworkElement
{
    private static readonly Dictionary<string, WeakReference<BitmapSource>> images = [];
    private BackgroundStyle style = new();
    private BitmapSource? bitmap;
    private string? loadedPath;
    private string crop = "";
    public string? ImageError { get; private set; }
    public Vector AnchorDelta(double dx, double dy)
    {
        if (bitmap is null) return new Vector();
        if (style.Fit == ImageFit.Tile) return new Vector(dx / Math.Max(1, ActualWidth), dy / Math.Max(1, ActualHeight));
        double iw = bitmap.PixelWidth * 96 / bitmap.DpiX, ih = bitmap.PixelHeight * 96 / bitmap.DpiY;
        double ratio = style.Fit switch { ImageFit.Fit => Math.Min(ActualWidth / iw, ActualHeight / ih), ImageFit.Fill => Math.Max(ActualWidth / iw, ActualHeight / ih), _ => 1 };
        double w = (style.Fit == ImageFit.Stretch ? ActualWidth : iw * ratio) * style.Zoom, h = (style.Fit == ImageFit.Stretch ? ActualHeight : ih * ratio) * style.Zoom;
        return new Vector(Math.Abs(ActualWidth - w) > .1 ? dx / (ActualWidth - w) : 0, Math.Abs(ActualHeight - h) > .1 ? dy / (ActualHeight - h) : 0);
    }
    public void Update(BackgroundStyle value)
    {
        style = value;
        string key = $"{value.CropX:R},{value.CropY:R},{value.CropWidth:R},{value.CropHeight:R}";
        if (loadedPath != value.ImagePath || crop != key)
        {
            bitmap = null; ImageError = null; loadedPath = value.ImagePath; crop = key;
            if (!string.IsNullOrWhiteSpace(loadedPath))
            {
                try
                {
                    string imageKey = Path.GetFullPath(loadedPath) + "|" + key;
                    if (images.TryGetValue(imageKey, out var existing) && existing.TryGetTarget(out var cached)) { bitmap = cached; InvalidateVisual(); return; }
                    var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad;
                    image.UriSource = new Uri(Path.GetFullPath(loadedPath)); image.EndInit(); image.Freeze();
                    int x = (int)(Math.Clamp(value.CropX, 0, .99) * image.PixelWidth), y = (int)(Math.Clamp(value.CropY, 0, .99) * image.PixelHeight);
                    int w = Math.Clamp((int)(value.CropWidth * image.PixelWidth), 1, image.PixelWidth - x), h = Math.Clamp((int)(value.CropHeight * image.PixelHeight), 1, image.PixelHeight - y);
                    bitmap = new CroppedBitmap(image, new Int32Rect(x, y, w, h)); bitmap.Freeze();
                    if (images.Count > 256) foreach (var old in images.Where(x => !x.Value.TryGetTarget(out _)).Select(x => x.Key).ToArray()) images.Remove(old);
                    images[imageKey] = new WeakReference<BitmapSource>(bitmap);
                }
                catch (Exception ex) { ImageError = ex.Message; }
            }
        }
        InvalidateVisual();
    }
    protected override void OnRender(DrawingContext dc)
    {
        dc.DrawRectangle(Visuals.Brush(style.Color), null, new Rect(RenderSize));
        if (bitmap is null || ActualWidth <= 0 || ActualHeight <= 0) return;
        dc.PushClip(new RectangleGeometry(new Rect(RenderSize))); dc.PushOpacity(Math.Clamp(style.ImageOpacity, 0, 1));
        double iw = bitmap.PixelWidth * 96 / bitmap.DpiX, ih = bitmap.PixelHeight * 96 / bitmap.DpiY;
        double zoom = Math.Clamp(style.Zoom, .1, 8);
        if (style.Fit == ImageFit.Tile)
        {
            var brush = new ImageBrush(bitmap) { TileMode = TileMode.Tile, ViewportUnits = BrushMappingMode.Absolute,
                Viewport = new Rect(ActualWidth * style.AnchorX, ActualHeight * style.AnchorY, iw * zoom, ih * zoom), Stretch = Stretch.Fill };
            dc.DrawRectangle(brush, null, new Rect(RenderSize));
        }
        else
        {
            double ratio = style.Fit switch { ImageFit.Fit => Math.Min(ActualWidth / iw, ActualHeight / ih), ImageFit.Fill => Math.Max(ActualWidth / iw, ActualHeight / ih), _ => 1 };
            double w = (style.Fit == ImageFit.Stretch ? ActualWidth : iw * ratio) * zoom;
            double h = (style.Fit == ImageFit.Stretch ? ActualHeight : ih * ratio) * zoom;
            dc.DrawImage(bitmap, new Rect((ActualWidth - w) * Math.Clamp(style.AnchorX, 0, 1), (ActualHeight - h) * Math.Clamp(style.AnchorY, 0, 1), w, h));
        }
        dc.Pop(); dc.Pop();
    }
}
