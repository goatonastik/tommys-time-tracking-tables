namespace TimersApp.Core;

public sealed class AlertSettings
{
    public bool Enabled { get; set; }
    public double IntervalSeconds { get; set; } = 900;
    public bool Repeat { get; set; } = true;
    public bool ChangeColor { get; set; } = true;
    public bool Flash { get; set; }
    public string Color { get; set; } = "#FFBD69";
    public bool PlaySound { get; set; }
    public string? SoundPath { get; set; }
    public double Volume { get; set; } = .6;
    public bool RepeatSound { get; set; }
    public int SnoozeMinutes { get; set; } = 5;
}
public sealed class AlertState
{
    public long LastInterval { get; set; }
    public bool Active { get; set; }
    public bool HasTriggered { get; set; }
    public DateTimeOffset? SnoozedUntil { get; set; }
}
public static class Alerts
{
    public static bool Evaluate(AlertSettings settings, AlertState state, TimeSpan total, DateTimeOffset now, bool running)
    {
        if (!settings.Enabled) { state.Active = false; state.SnoozedUntil = null; return false; }
        if (state.SnoozedUntil is { } until)
        {
            if (now < until) return false;
            if (double.IsFinite(settings.IntervalSeconds) && settings.IntervalSeconds >= 1)
                state.LastInterval = Math.Max(state.LastInterval, (long)(total.TotalSeconds / settings.IntervalSeconds));
            state.SnoozedUntil = null; state.Active = true; return true;
        }
        if (!running || !double.IsFinite(settings.IntervalSeconds) || settings.IntervalSeconds < 1) return false;
        long interval = (long)(total.TotalSeconds / settings.IntervalSeconds);
        if (interval <= state.LastInterval || interval < 1 || (!settings.Repeat && state.HasTriggered)) return false;
        state.LastInterval = interval;
        state.HasTriggered = true;
        bool wasActive = state.Active; state.Active = true;
        return !wasActive;
    }
    public static void Dismiss(AlertState state) { state.Active = false; state.SnoozedUntil = null; }
    public static void Snooze(AlertState state, DateTimeOffset now, int minutes) { if (!state.Active) return; state.Active = false; state.SnoozedUntil = now.AddMinutes(Math.Clamp(minutes, 1, 1440)); }
}
