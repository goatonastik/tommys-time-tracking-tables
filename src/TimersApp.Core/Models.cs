using System.Text.Json.Serialization;

namespace TimersApp.Core;

public enum TotalDisplay { Both, Exact, Rounded }
public enum ExitChoice { Ask, Save, Discard }

public sealed class Workspace
{
    public int SchemaVersion { get; set; } = 3;
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "default";
    public List<TimerRecord> Timers { get; set; } = [];
    public List<string> NameHistory { get; set; } = [];
    public AppSettings Settings { get; set; } = new();
    public List<StylePreset> Presets { get; set; } = StylePreset.BuiltIns();
    public DateTimeOffset SavedAt { get; set; }
    public bool CleanShutdown { get; set; }
}

public sealed class AppSettings
{
    public bool AutoSave { get; set; } = true;
    public bool RestoreOnLaunch { get; set; } = true;
    public ExitChoice ExitChoice { get; set; }
    public double AppScale { get; set; } = 1;
    public double TimerScale { get; set; } = 1;
    public double PopoutScale { get; set; } = 1;
    public bool LinkPopoutScale { get; set; }
    public bool AllowIndividualResize { get; set; }
    public bool ShowStatusBar { get; set; }
    public bool SaveNameSuggestions { get; set; } = true;
    public bool ListLayout { get; set; }
    public int Columns { get; set; } = 3;
    public bool AutoColumns { get; set; } = true;
    public int Rows { get; set; }
    public double Spacing { get; set; } = 14;
    public double ListRowHeight { get; set; } = 72;
    public double CardWidth { get; set; } = 330;
    public double CardHeight { get; set; } = 250;
    public bool FitCardHeight { get; set; } = true;
    public int RoundMinutes { get; set; } = 15;
    public TotalDisplay TotalDisplay { get; set; } = TotalDisplay.Both;
    public TimerStyle DefaultStyle { get; set; } = new();
    public bool SeparatePopoutStyle { get; set; }
    public TimerStyle PopoutStyle { get; set; } = TimerStyle.Minimal();
    public BackgroundStyle AppBackground { get; set; } = new() { Color = "#0E1525" };
    public bool PauseDuringSleep { get; set; } = true;
}

public sealed class TimerRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Untitled timer";
    public string Description { get; set; } = "";
    public bool Archived { get; set; }
    public Guid RunId { get; set; } = Guid.NewGuid();
    public List<Segment> Segments { get; set; } = [];
    public Guid? PresetId { get; set; }
    public TimerStyle? Style { get; set; }
    public bool? SeparatePopoutStyle { get; set; }
    public TimerStyle? PopoutStyle { get; set; }
    public WindowPlacement Popout { get; set; } = new();
    public double? Width { get; set; }
    public double? Height { get; set; }
    public AlertSettings Alert { get; set; } = new();
    public AlertState AlertState { get; set; } = new();
    public bool ResumeAfterSleep { get; set; }
    [JsonIgnore] public bool IsRunning => Segments.Any(x => x.End is null);
}

public sealed class Segment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RunId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset? End { get; set; }
    public DateTimeOffset MeasuredThrough { get; set; }
    public string TimeZoneId { get; set; } = TimeZoneInfo.Local.Id;
    public long ElapsedTicks { get; set; }
    public bool Recovered { get; set; }
    public bool ClockAdjusted { get; set; }
}
