using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using TimersApp.Core;

namespace TimersApp.Desktop;
internal static class BackgroundEditor
{
    public static BackgroundStyle? Edit(Window owner, BackgroundStyle original, AssetLibrary assets)
    {
        var draft = Appearance.Copy(original);
        var d = new FormDialog(owner, "Background image and position", 940);
        var preview = new ImageBackdrop { Width = 350, Height = 230, Cursor = Cursors.Hand };
        var crop = new CropSelector { Width = 350, Height = 230 };
        var right = new StackPanel { Margin = new Thickness(8, 24, 20, 0) };
        right.Children.Add(new TextBlock { Text = "BACKGROUND PREVIEW · drag to position", Foreground = Brushes.LightSteelBlue, Margin = new Thickness(0, 0, 0, 10) }); right.Children.Add(preview);
        right.Children.Add(new TextBlock { Text = "CROP · drag a rectangle over the source image", Foreground = Brushes.LightSteelBlue, Margin = new Thickness(0, 20, 0, 10) }); right.Children.Add(crop); d.Preview(right, 380);
        d.Label("Background color"); var color = d.Text(draft.Color);
        var fileLabel = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brushes.LightSteelBlue, Margin = new Thickness(0, 12, 0, 8) }; d.Fields.Children.Add(fileLabel);
        var browse = new Button { Content = "Choose image…" }; var remove = new Button { Content = "Remove image", Margin = new Thickness(0, 8, 0, 0) }; d.Fields.Children.Add(browse); d.Fields.Children.Add(remove);
        d.Label("Image sizing"); var fit = d.Select(Enum.GetValues<ImageFit>(), draft.Fit);
        d.Label("Image anchor"); string[] anchors = ["Top left", "Top center", "Top right", "Center left", "Center", "Center right", "Bottom left", "Bottom center", "Bottom right", "Custom"];
        var anchor = d.Select(anchors, "Custom");
        d.Label("Horizontal anchor (0 = left, 100 = right)"); var x = Slider(d, draft.AnchorX * 100, 0, 100);
        d.Label("Vertical anchor (0 = top, 100 = bottom)"); var y = Slider(d, draft.AnchorY * 100, 0, 100);
        d.Label("Zoom (10–800%)"); var zoom = Slider(d, draft.Zoom * 100, 10, 800);
        d.Label("Image opacity (0–100%)"); var opacity = Slider(d, draft.ImageOpacity * 100, 0, 100);
        var reset = new Button { Content = "Reset crop and positioning", Margin = new Thickness(0, 16, 0, 0) }; d.Fields.Children.Add(reset);
        var error = new TextBlock { Foreground = Brushes.Salmon, TextWrapping = TextWrapping.Wrap }; d.Fields.Children.Add(error);
        void Refresh()
        {
            if (Visuals.ValidColor(color.Text)) { draft.Color = color.Text; error.Text = ""; } else error.Text = "Enter a valid background color.";
            draft.Fit = (ImageFit)fit.SelectedItem; draft.AnchorX = x.Value / 100; draft.AnchorY = y.Value / 100; draft.Zoom = zoom.Value / 100; draft.ImageOpacity = opacity.Value / 100;
            preview.Update(draft); crop.Update(draft); fileLabel.Text = draft.ImagePath is null ? "No image selected" : Path.GetFileName(draft.ImagePath);
            if (preview.ImageError is not null) error.Text = "Image could not be loaded. Choose another image. " + preview.ImageError;
        }
        color.TextChanged += (_, _) => Refresh(); fit.SelectionChanged += (_, _) => Refresh();
        foreach (var slider in new[] { x, y, zoom, opacity }) slider.ValueChanged += (_, _) => Refresh();
        anchor.SelectionChanged += (_, _) => { int i = anchor.SelectedIndex; if (i < 0 || i > 8) return; x.Value = i % 3 * 50; y.Value = i / 3 * 50; Refresh(); };
        browse.Click += (_, _) =>
        {
            var open = new OpenFileDialog { Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff" };
            if (open.ShowDialog(d) != true) return;
            try { draft.ImagePath = assets.Import(open.FileName, true); draft.CropX = draft.CropY = 0; draft.CropWidth = draft.CropHeight = 1; Refresh(); }
            catch (Exception ex) { MessageBox.Show(d, ex.Message, "Could not import image"); }
        };
        remove.Click += (_, _) => { draft.ImagePath = null; Refresh(); };
        reset.Click += (_, _) => { draft.CropX = draft.CropY = 0; draft.CropWidth = draft.CropHeight = 1; x.Value = y.Value = 50; zoom.Value = 100; Refresh(); };
        crop.CropChanged += () => { draft.CropX = crop.Selection.X; draft.CropY = crop.Selection.Y; draft.CropWidth = crop.Selection.Width; draft.CropHeight = crop.Selection.Height; preview.Update(draft); };
        Point? drag = null;
        preview.MouseLeftButtonDown += (_, e) => { drag = e.GetPosition(preview); preview.CaptureMouse(); };
        preview.MouseMove += (_, e) =>
        {
            if (drag is not { } previous) return;
            Point now = e.GetPosition(preview); var delta = now - previous; drag = now;
            var offset = preview.AnchorDelta(delta.X, delta.Y);
            anchor.SelectedItem = "Custom"; x.Value = Math.Clamp(x.Value + offset.X * 100, 0, 100); y.Value = Math.Clamp(y.Value + offset.Y * 100, 0, 100);
        };
        preview.MouseLeftButtonUp += (_, _) => { drag = null; preview.ReleaseMouseCapture(); };
        preview.LostMouseCapture += (_, _) => drag = null;
        preview.MouseWheel += (_, e) => { zoom.Value += e.Delta > 0 ? 10 : -10; e.Handled = true; };
        d.Actions(("Apply background", () => { if (Visuals.ValidColor(color.Text) && preview.ImageError is null) d.DialogResult = true; }), ("Cancel", () => d.Close()));
        Refresh(); return d.ShowDialog() == true ? draft : null;
    }
    private static Slider Slider(FormDialog d, double value, double min, double max)
    {
        var panel = new DockPanel(); var number = new TextBox { Width = 64, Text = value.ToString("0.##"), Margin = new Thickness(10, 0, 0, 0) };
        DockPanel.SetDock(number, Dock.Right); panel.Children.Add(number);
        var slider = new Slider { Minimum = min, Maximum = max, Value = value, VerticalAlignment = VerticalAlignment.Center };
        panel.Children.Add(slider); d.Fields.Children.Add(panel);
        slider.ValueChanged += (_, _) => number.Text = slider.Value.ToString("0.##");
        number.LostKeyboardFocus += (_, _) => { if (double.TryParse(number.Text, out var n) && double.IsFinite(n)) slider.Value = Math.Clamp(n, min, max); number.Text = slider.Value.ToString("0.##"); };
        return slider;
    }
}
internal sealed class CropSelector : FrameworkElement
{
    private BitmapSource? image;
    private string? path;
    private Point? start;
    private Rect imageRect;
    public Rect Selection { get; private set; } = new(0, 0, 1, 1);
    public event Action? CropChanged;
    public CropSelector()
    {
        Cursor = Cursors.Cross;
        MouseLeftButtonDown += (_, e) => { if (image is null || !imageRect.Contains(e.GetPosition(this))) return; start = Normalize(e.GetPosition(this)); CaptureMouse(); };
        MouseMove += (_, e) => { if (start is not { } a) return; var b = Normalize(e.GetPosition(this)); Selection = new Rect(a, b); if (Selection.Width >= .005 && Selection.Height >= .005) CropChanged?.Invoke(); InvalidateVisual(); };
        MouseLeftButtonUp += (_, _) => { start = null; ReleaseMouseCapture(); };
        LostMouseCapture += (_, _) => start = null;
    }
    public void Update(BackgroundStyle style)
    {
        Selection = new Rect(style.CropX, style.CropY, style.CropWidth, style.CropHeight);
        if (path != style.ImagePath)
        {
            path = style.ImagePath; image = null;
            if (path is not null) try { var bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad; bitmap.UriSource = new Uri(path); bitmap.DecodePixelWidth = 700; bitmap.EndInit(); bitmap.Freeze(); image = bitmap; } catch { }
        }
        InvalidateVisual();
    }
    private Point Normalize(Point p) => new(Math.Clamp((p.X - imageRect.X) / imageRect.Width, 0, 1), Math.Clamp((p.Y - imageRect.Y) / imageRect.Height, 0, 1));
    protected override void OnRender(DrawingContext dc)
    {
        dc.DrawRectangle(Brushes.Black, null, new Rect(RenderSize)); if (image is null) return;
        double ratio = Math.Min(ActualWidth / image.PixelWidth, ActualHeight / image.PixelHeight);
        imageRect = new Rect((ActualWidth - image.PixelWidth * ratio) / 2, (ActualHeight - image.PixelHeight * ratio) / 2, image.PixelWidth * ratio, image.PixelHeight * ratio);
        dc.DrawImage(image, imageRect);
        var selected = new Rect(imageRect.X + Selection.X * imageRect.Width, imageRect.Y + Selection.Y * imageRect.Height, Selection.Width * imageRect.Width, Selection.Height * imageRect.Height);
        var shade = new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(imageRect), new RectangleGeometry(selected));
        dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(140, 0, 0, 0)), null, shade); dc.DrawRectangle(null, new Pen(Brushes.Aquamarine, 2), selected);
    }
}
