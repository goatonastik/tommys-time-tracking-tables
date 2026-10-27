namespace TimersApp.Core;

/// <summary>Wall timestamps describe events; monotonic timestamps measure running duration.</summary>
public sealed class TimerEngine(Workspace workspace, TimeProvider? time = null)
{
    private readonly TimeProvider clock = time ?? TimeProvider.System;
    private readonly Dictionary<Guid, (long Timestamp, long BaseTicks)> active = [];
    public Workspace Workspace { get; } = workspace;
    public DateTimeOffset Now => clock.GetUtcNow().ToLocalTime();

    public TimerRecord Add(string name, string description = "")
    {
        var timer = new TimerRecord { Name = CleanName(name), Description = description.Trim() };
        Workspace.Timers.Add(timer);
        RememberName(timer.Name);
        return timer;
    }

    public void Rename(TimerRecord timer, string name, string description)
    {
        timer.Name = CleanName(name);
        timer.Description = description.Trim();
        RememberName(timer.Name);
    }

    public void Start(TimerRecord timer)
    {
        if (timer.Archived || timer.IsRunning) return;
        var segment = new Segment { RunId = timer.RunId, Name = timer.Name,
            Description = timer.Description, Start = Now, MeasuredThrough = Now };
        timer.Segments.Add(segment);
        active[segment.Id] = (clock.GetTimestamp(), 0);
    }

    public void Stop(TimerRecord timer)
    {
        timer.ResumeAfterSleep = false;
        var segment = timer.Segments.LastOrDefault(x => x.End is null);
        if (segment is null) return;
        Checkpoint(segment);
        segment.End = Now;
        segment.MeasuredThrough = segment.End.Value;
        active.Remove(segment.Id);
    }

    public void Reset(TimerRecord timer)
    {
        Stop(timer);
        timer.RunId = Guid.NewGuid();
    }

    public void Archive(TimerRecord timer) { Stop(timer); timer.Archived = true; }

    public TimeSpan Total(TimerRecord timer) => TimeSpan.FromTicks(timer.Segments
        .Where(x => x.RunId == timer.RunId).Sum(x => Elapsed(x).Ticks));

    public TimeSpan Elapsed(Segment segment)
    {
        if (!active.TryGetValue(segment.Id, out var anchor)) return TimeSpan.FromTicks(segment.ElapsedTicks);
        return TimeSpan.FromTicks(anchor.BaseTicks) + clock.GetElapsedTime(anchor.Timestamp);
    }

    public void Checkpoint()
    {
        foreach (var segment in Workspace.Timers.SelectMany(x => x.Segments).Where(x => x.End is null)) Checkpoint(segment);
        Workspace.SavedAt = Now;
    }

    private void Checkpoint(Segment segment)
    {
        // Restored records have no live monotonic anchor. Do not move their recovery boundary.
        if (!active.ContainsKey(segment.Id)) return;
        segment.ElapsedTicks = Elapsed(segment).Ticks;
        segment.MeasuredThrough = Now;
        if (Math.Abs((segment.MeasuredThrough - segment.Start).TotalSeconds - TimeSpan.FromTicks(segment.ElapsedTicks).TotalSeconds) > 1)
            segment.ClockAdjusted = true;
    }

    public void SuspendForSleep()
    {
        foreach (var timer in Workspace.Timers.Where(t => t.IsRunning && !t.Archived))
        {
            Stop(timer);
            timer.ResumeAfterSleep = true;
        }
        Workspace.SavedAt = Now;
    }

    public void ResumeFromSleep()
    {
        foreach (var timer in Workspace.Timers.Where(t => t.ResumeAfterSleep))
        {
            timer.ResumeAfterSleep = false;
            if (!timer.Archived) Start(timer);
        }
    }

    // Recovery closes the old segment at the last checkpoint, preserving a visible gap.
    // Including the interruption extends that segment; resuming creates a fresh segment.
    public void Recover(bool includeInterruption, bool resume)
    {
        foreach (var timer in Workspace.Timers)
        {
            if (timer.ResumeAfterSleep)
            {
                timer.ResumeAfterSleep = false;
                if (resume && !timer.Archived) Start(timer);
                continue;
            }
            var segment = timer.Segments.LastOrDefault(x => x.End is null);
            if (segment is null) continue;
            if (includeInterruption)
            {
                segment.ElapsedTicks += Math.Max(0, (Now - segment.MeasuredThrough).Ticks);
                segment.MeasuredThrough = Now;
            }
            segment.End = segment.MeasuredThrough;
            segment.Recovered = true;
            if (resume && !timer.Archived) Start(timer);
        }
    }

    private void RememberName(string name)
    {
        if (!Workspace.Settings.SaveNameSuggestions) return;
        Workspace.NameHistory.RemoveAll(x => x.Equals(name, StringComparison.OrdinalIgnoreCase));
        Workspace.NameHistory.Insert(0, name);
    }
    private static string CleanName(string name) => string.IsNullOrWhiteSpace(name) ? "Untitled timer" : name.Trim();
}

public static class Duration
{
    public static TimeSpan Round(TimeSpan value, int minutes)
    {
        if (minutes is not (1 or 15 or 60)) throw new ArgumentOutOfRangeException(nameof(minutes));
        long step = TimeSpan.FromMinutes(minutes).Ticks;
        return TimeSpan.FromTicks(checked((value.Ticks / step + (value.Ticks % step >= step / 2 ? 1 : 0)) * step));
    }
    public static string Format(TimeSpan value) => $"{(long)value.TotalHours:00}:{value.Minutes:00}:{value.Seconds:00}";
    public static string FormatPrecise(TimeSpan value) => $"{Format(value)}.{value.Milliseconds:000}";
}
