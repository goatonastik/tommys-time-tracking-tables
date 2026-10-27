using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Windows.Media.Imaging;
using TimersApp.Core;

namespace TimersApp.Desktop;
internal sealed class AssetLibrary(string folder)
{
    public string Import(string path, bool image)
    {
        var file = new FileInfo(path);
        if (!file.Exists || file.Length > 60 * 1024 * 1024) throw new InvalidDataException("Choose a file smaller than 60 MB.");
        if (image)
        {
            using var input = file.OpenRead(); var decoder = BitmapDecoder.Create(input, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0]; if ((long)frame.PixelWidth * frame.PixelHeight > 100_000_000) throw new InvalidDataException("Choose an image smaller than 100 megapixels.");
        }
        else if (!new[] { ".wav", ".mp3", ".wma", ".m4a" }.Contains(file.Extension.ToLowerInvariant())) throw new InvalidDataException("Choose WAV, MP3, WMA, or M4A audio.");
        Directory.CreateDirectory(Path.Combine(folder, "assets"));
        var target = Path.Combine(folder, "assets", Guid.NewGuid().ToString("N") + file.Extension.ToLowerInvariant());
        File.Copy(path, target); return target;
    }
    public void ExportPreset(string path, StylePreset preset)
    {
        var copy = Appearance.Copy(preset);
        using var file = File.Create(path); using var zip = new ZipArchive(file, ZipArchiveMode.Create);
        if (copy.Style.Background.ImagePath is { } image && File.Exists(image))
        {
            var entry = zip.CreateEntry("background" + Path.GetExtension(image)); using var source = File.OpenRead(image); using var destination = entry.Open(); source.CopyTo(destination);
            copy.Style.Background.ImagePath = entry.FullName;
        }
        else copy.Style.Background.ImagePath = null;
        using var text = new StreamWriter(zip.CreateEntry("preset.json").Open()); text.Write(JsonSerializer.Serialize(copy, new JsonSerializerOptions { WriteIndented = true }));
    }
    public StylePreset ImportPreset(string path)
    {
        using var file = File.OpenRead(path); using var zip = new ZipArchive(file, ZipArchiveMode.Read);
        var entry = zip.GetEntry("preset.json") ?? throw new InvalidDataException("This file does not contain a timer preset.");
        if (entry.Length > 1024 * 1024) throw new InvalidDataException("Preset data is too large.");
        using var input = entry.Open(); var preset = JsonSerializer.Deserialize<StylePreset>(input) ?? throw new InvalidDataException("Preset is empty.");
        if (preset.Style is null || preset.Style.Background is null || !Visuals.ValidColor(preset.Style.TextColor) || !Visuals.ValidColor(preset.Style.AccentColor) || !Visuals.ValidColor(preset.Style.Background.Color)) throw new InvalidDataException("The preset contains invalid appearance values.");
        if(preset.Style.UseCustomFormat&&!Appearance.ValidFormat(preset.Style.CustomFormat))throw new InvalidDataException("The preset contains an invalid number layout.");
        if (!System.Windows.Media.Fonts.SystemFontFamilies.Any(f => f.Source.Equals(preset.Style.FontFamily, StringComparison.OrdinalIgnoreCase))) preset.Style.FontFamily = "Segoe UI";
        if (!Enum.IsDefined(preset.Style.Digits) || !Enum.IsDefined(preset.Style.Background.Fit) || preset.Style.Units == 0 || (preset.Style.Units & ~(TimeUnits.Years | TimeUnits.Days | TimeUnits.Hours | TimeUnits.Minutes | TimeUnits.Seconds | TimeUnits.Milliseconds)) != 0) throw new InvalidDataException("Preset display options are invalid.");
        if (!double.IsFinite(preset.Style.FontSize) || preset.Style.FontSize < 10 || preset.Style.FontSize > 200 || !double.IsFinite(preset.Style.Padding) || preset.Style.Padding < 0 || preset.Style.Padding > 80 || !double.IsFinite(preset.Style.CornerRadius) || preset.Style.CornerRadius < 0 || preset.Style.CornerRadius > 100) throw new InvalidDataException("Preset dimensions are outside the supported range.");
        var bg = preset.Style.Background;
        if (new[] { bg.AnchorX, bg.AnchorY, bg.CropX, bg.CropY, bg.CropWidth, bg.CropHeight, bg.ImageOpacity }.Any(x => !double.IsFinite(x) || x < 0 || x > 1) || !double.IsFinite(bg.Zoom) || bg.Zoom < .1 || bg.Zoom > 8 || bg.CropWidth <= 0 || bg.CropHeight <= 0) throw new InvalidDataException("Preset image settings are invalid.");
        string? image = bg.ImagePath; bg.ImagePath = null;
        if (image is not null)
        {
            var asset = zip.Entries.SingleOrDefault(x => x.FullName == image) ?? throw new InvalidDataException("Preset background is missing.");
            if (asset.Length > 60 * 1024 * 1024) throw new InvalidDataException("Preset image is too large.");
            Directory.CreateDirectory(Path.Combine(folder, "assets"));
            var temp = Path.Combine(folder, "assets", Guid.NewGuid().ToString("N") + ".tmp");
            try { using (var source = asset.Open()) using (var target = File.Create(temp)) source.CopyTo(target); bg.ImagePath = Import(temp, true); }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        preset.Id = Guid.NewGuid(); return preset;
    }
}
