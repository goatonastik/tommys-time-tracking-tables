using System.Text.Json;

namespace TimersApp.Core;

public enum DigitStyle { Text, SevenSegment }
public enum ImageFit { Fit, Fill, Original, Stretch, Tile }
[Flags] public enum TimeUnits { Years = 1, Days = 2, Hours = 4, Minutes = 8, Seconds = 16, Milliseconds = 32 }

public sealed class BackgroundStyle
{
    public string Color { get; set; } = "#182338";
    public string? ImagePath { get; set; }
    public ImageFit Fit { get; set; } = ImageFit.Fill;
    public double AnchorX { get; set; } = .5;
    public double AnchorY { get; set; } = .5;
    public double Zoom { get; set; } = 1;
    public double ImageOpacity { get; set; } = .65;
    public double CropX { get; set; }
    public double CropY { get; set; }
    public double CropWidth { get; set; } = 1;
    public double CropHeight { get; set; } = 1;
}
public sealed class TimerStyle
{
    public DigitStyle Digits { get; set; }
    public string FontFamily { get; set; } = "Consolas";
    public double FontSize { get; set; } = 42;
    public bool FitDigitsToWindow { get; set; } = true;
    public string TextColor { get; set; } = "#EDF3FD";
    public string AccentColor { get; set; } = "#53D9B0";
    public BackgroundStyle Background { get; set; } = new();
    public TimeUnits Units { get; set; } = TimeUnits.Hours | TimeUnits.Minutes | TimeUnits.Seconds;
    public bool HideUnitLabels { get; set; }
    public bool UseCustomFormat { get; set; }
    public string CustomFormat { get; set; } = "H:M:S.MS";
    public bool ShowTitle { get; set; } = true;
    public bool ShowDescription { get; set; } = true;
    public bool ShowStatus { get; set; } = true;
    public bool ShowControls { get; set; } = true;
    public bool ShowTotal { get; set; } = true;
    public double Padding { get; set; }
    public double CornerRadius { get; set; }
    public static TimerStyle Minimal() => new() { ShowTitle = false, ShowDescription = false, ShowStatus = false, ShowControls = false, ShowTotal = false, Digits = DigitStyle.SevenSegment };
}
public sealed class StylePreset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "New preset";
    public TimerStyle Style { get; set; } = new();
    public override string ToString() => Name;
    public static List<StylePreset> BuiltIns() =>
    [new() { Name = "Classic", Style = new() },
     new() { Name = "Digital mint", Style = new() { Digits = DigitStyle.SevenSegment, TextColor = "#77F5C9", Background = new() { Color = "#10251F" } } },
     new() { Name = "Time only", Style = TimerStyle.Minimal() }];
}
public class PopoutGeometry
{
    public double? Left { get; set; }
    public double? Top { get; set; }
    public double? Width { get; set; }
    public double? Height { get; set; }
    public void Validate()
    {
        if (Left is not double x || Top is not double y || !double.IsFinite(x) || !double.IsFinite(y) || Math.Abs(x)>1_000_000 || Math.Abs(y)>1_000_000) Left = Top = null;
        if (Width is not double w || Height is not double h || !double.IsFinite(w) || !double.IsFinite(h) || w <= 0 || h <= 0 || w>1_000_000 || h>1_000_000) Width = Height = null;
    }
}
public sealed class WindowPlacement : PopoutGeometry
{
    public bool Topmost { get; set; }
    public bool IsOpen { get; set; }
}
public static class Appearance
{
    private static readonly (string Token, long Ticks, string Format)[] FormatTokens =
    [("Y", TimeSpan.TicksPerDay * 365, "00"), ("D", TimeSpan.TicksPerDay, "00"),
     ("H", TimeSpan.TicksPerHour, "00"), ("M", TimeSpan.TicksPerMinute, "00"),
     ("S", TimeSpan.TicksPerSecond, "00"), ("MS", TimeSpan.TicksPerMillisecond, "000")];
    public static bool ValidFormat(string? format)
    {
        if (string.IsNullOrWhiteSpace(format) || format.Length > 64) return false;
        var tokens = System.Text.RegularExpressions.Regex.Split(format.ToUpperInvariant(), "[:. ]");
        int previous = -1;
        foreach (var token in tokens)
        {
            int index = Array.FindIndex(FormatTokens, x => x.Token == token);
            if (index <= previous) return false;
            previous = index;
        }
        return previous >= 0;
    }
    public static string Format(TimeSpan value, TimerStyle style)
    {
        if (style.UseCustomFormat && ValidFormat(style.CustomFormat)) return FormatPattern(value, style.CustomFormat.ToUpperInvariant());
        if (!style.HideUnitLabels) return Format(value, style.Units);
        string pattern = string.Join(":", FormatTokens.Where((x, i) => style.Units.HasFlag((TimeUnits)(1 << i))).Select(x => x.Token));
        return FormatPattern(value, pattern.Length == 0 ? "H:M:S" : pattern.Replace(":MS", ".MS"));
    }
    private static string FormatPattern(TimeSpan value, string pattern)
    {
        long remaining = Math.Max(0, value.Ticks);
        return System.Text.RegularExpressions.Regex.Replace(pattern, "MS|Y|D|H|M|S", match =>
        {
            var step = FormatTokens.First(x => x.Token == match.Value);
            long count = remaining / step.Ticks; remaining %= step.Ticks;
            return count.ToString(step.Format, System.Globalization.CultureInfo.InvariantCulture);
        });
    }
    public static T Copy<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
    public static TimerStyle Resolve(Workspace workspace, TimerRecord timer, bool popout)
    {
        if (popout && (timer.SeparatePopoutStyle ?? workspace.Settings.SeparatePopoutStyle))
            return timer.PopoutStyle ?? workspace.Settings.PopoutStyle;
        return timer.Style ?? workspace.Presets.FirstOrDefault(p => p.Id == timer.PresetId)?.Style ?? workspace.Settings.DefaultStyle;
    }
    public static string Format(TimeSpan value, TimeUnits units)
    {
        if (units == 0) units = TimeUnits.Hours | TimeUnits.Minutes | TimeUnits.Seconds;
        if (units == (TimeUnits.Hours | TimeUnits.Minutes | TimeUnits.Seconds)) return Duration.Format(value);
        long remaining = Math.Max(0, value.Ticks);
        var parts = new List<string>();
        (TimeUnits Unit, long Ticks, string Label, string Format)[] steps =
        [(TimeUnits.Years, TimeSpan.TicksPerDay * 365, "y", "0"), (TimeUnits.Days, TimeSpan.TicksPerDay, "d", "0"),
         (TimeUnits.Hours, TimeSpan.TicksPerHour, "h", "00"), (TimeUnits.Minutes, TimeSpan.TicksPerMinute, "min", "00"),
         (TimeUnits.Seconds, TimeSpan.TicksPerSecond, "s", "00"), (TimeUnits.Milliseconds, TimeSpan.TicksPerMillisecond, "ms", "000")];
        foreach (var step in steps)
        {
            if (!units.HasFlag(step.Unit)) continue;
            long count = remaining / step.Ticks; remaining %= step.Ticks;
            parts.Add(count.ToString(step.Format) + step.Label);
        }
        return string.Join(" ", parts);
    }
}
