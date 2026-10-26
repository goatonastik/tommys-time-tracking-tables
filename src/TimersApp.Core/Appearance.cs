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
    public bool ShowTitle { get; set; } = true;
    public bool ShowDescription { get; set; } = true;
    public bool ShowStatus { get; set; } = true;
    public bool ShowControls { get; set; } = true;
    public bool ShowTotal { get; set; } = true;
    public double Padding { get; set; } = 18;
    public double CornerRadius { get; set; } = 12;
    public static TimerStyle Minimal() => new() { ShowTitle = false, ShowDescription = false, ShowStatus = false, ShowControls = false, ShowTotal = false, Padding = 8, Digits = DigitStyle.SevenSegment };
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
public sealed class WindowPlacement
{
    public double? Left { get; set; }
    public double? Top { get; set; }
    public double Width { get; set; } = 390;
    public double Height { get; set; } = 340;
    public bool Topmost { get; set; }
    public bool IsOpen { get; set; }
}
public static class Appearance
{
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
