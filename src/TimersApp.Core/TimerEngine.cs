namespace TimersApp.Core;

/// <summary>Wall timestamps describe events; monotonic timestamps measure running duration.</summary>
public sealed class TimerEngine(Workspace workspace, TimeProvider? time = null)
{
    private readonly TimeProvider clock = time ?? TimeProvider.System;
    private readonly Dictionary<Guid, (long Timestamp, long BaseTicks)> active = [];
    private long lastStartSequence = Math.Max(0, workspace.Timers.SelectMany(t => t.Segments)
        .Select(s => s.StartSequence).DefaultIfEmpty().Max());
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
        var now = Now;
        var timestamp = clock.GetTimestamp();
        if (Workspace.Settings.OnlyOneTimerAtATime)
            foreach (var other in Workspace.Timers.Where(t => t.IsRunning && t.Id != timer.Id).ToArray()) StopAt(other, now, timestamp);
        var segment = new Segment { RunId = timer.RunId, Name = timer.Name,
            Description = timer.Description, Start = now, StartSequence = NextStartSequence(), MeasuredThrough = now };
        timer.Segments.Add(segment);
        active[segment.Id] = (timestamp, 0);
    }

    public void Stop(TimerRecord timer)
        => StopAt(timer, Now, clock.GetTimestamp());

    private void StopAt(TimerRecord timer, DateTimeOffset now, long timestamp)
    {
        timer.ResumeAfterSleep = false;
        var segment = timer.Segments.LastOrDefault(x => x.End is null);
        if (segment is null) return;
        CheckpointAt(segment, now, timestamp);
        segment.End = now;
        segment.MeasuredThrough = now;
        active.Remove(segment.Id);
    }

    /// <summary>When the setting is enabled mid-session, keep the newest running timer.</summary>
    public IReadOnlyList<TimerRecord> EnforceSingleRunning()
    {
        if (!Workspace.Settings.OnlyOneTimerAtATime) return [];
        var running = Workspace.Timers.Where(t => t.IsRunning).ToArray();
        if (running.Length <= 1) return [];
        var keep = running.Where(t => !t.Archived)
            .OrderBy(t => t.Segments.Last(s => s.End is null).StartSequence)
            .ThenBy(t => t.Segments.Last(s => s.End is null).Start).LastOrDefault();
        var stopped = running.Where(t => t != keep).ToArray();
        var now = Now;
        var timestamp = clock.GetTimestamp();
        foreach (var timer in stopped) StopAt(timer, now, timestamp);
        return stopped;
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
        => CheckpointAt(segment, Now, clock.GetTimestamp());

    private void CheckpointAt(Segment segment, DateTimeOffset now, long timestamp)
    {
        // Restored records have no live monotonic anchor. Do not move their recovery boundary.
        if (!active.TryGetValue(segment.Id, out var anchor)) return;
        segment.ElapsedTicks = anchor.BaseTicks + clock.GetElapsedTime(anchor.Timestamp, timestamp).Ticks;
        segment.MeasuredThrough = now;
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
        var candidates = Workspace.Timers.Where(t => t.ResumeAfterSleep && !t.Archived).ToArray();
        foreach (var timer in Workspace.Timers.Where(t => t.ResumeAfterSleep)) timer.ResumeAfterSleep = false;
        foreach (var timer in SelectForResume(candidates)) Start(timer);
        foreach (var timer in Workspace.Timers.Where(t => !t.IsRunning)) Alerts.Dismiss(timer.AlertState);
    }

    private IEnumerable<TimerRecord> SelectForResume(IEnumerable<TimerRecord> candidates)
    {
        var ordered = candidates.Where(t => !t.Archived)
            .OrderBy(t => t.Segments.LastOrDefault()?.StartSequence ?? 0)
            .ThenBy(t => t.Segments.LastOrDefault()?.Start ?? DateTimeOffset.MinValue).ToArray();
        if (!Workspace.Settings.OnlyOneTimerAtATime) return ordered;
        return ordered.Length == 0 ? [] : [ordered[^1]];
    }

    private long NextStartSequence()
    {
        if (lastStartSequence == long.MaxValue)
        {
            // A malformed imported value must not make Start or resume fail.
            var ordered = Workspace.Timers.SelectMany(t => t.Segments)
                .OrderBy(s => s.StartSequence).ThenBy(s => s.Start).ToArray();
            for (var i = 0; i < ordered.Length; i++) ordered[i].StartSequence = i + 1L;
            lastStartSequence = ordered.LongLength;
        }
        return ++lastStartSequence;
    }

    // Recovery closes the old segment at the last checkpoint, preserving a visible gap.
    // Including the interruption extends that segment; resuming creates a fresh segment.
    public void Recover(bool includeInterruption, bool resume)
    {
        var toResume = new List<TimerRecord>();
        foreach (var timer in Workspace.Timers)
        {
            if (timer.ResumeAfterSleep)
            {
                timer.ResumeAfterSleep = false;
                if (resume && !timer.Archived) toResume.Add(timer);
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
            if (resume && !timer.Archived) toResume.Add(timer);
        }
        foreach (var timer in SelectForResume(toResume)) Start(timer);
        foreach (var timer in Workspace.Timers.Where(t => !t.IsRunning)) Alerts.Dismiss(timer.AlertState);
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
    public static decimal DecimalHours(TimeSpan value, int minutes = 15) => (decimal)Round(value, minutes).Ticks / TimeSpan.TicksPerHour;
    public static string HoursText(TimeSpan value, int minutes = 15) => DecimalHours(value, minutes).ToString("0.00", System.Globalization.CultureInfo.CurrentCulture);
    public static TimeSpan Round(TimeSpan value, int minutes)
    {
        if (minutes is not (1 or 15 or 60)) throw new ArgumentOutOfRangeException(nameof(minutes));
        long step = TimeSpan.FromMinutes(minutes).Ticks;
        return TimeSpan.FromTicks(checked((value.Ticks / step + (value.Ticks % step >= step / 2 ? 1 : 0)) * step));
    }
    public static string Format(TimeSpan value) => $"{(long)value.TotalHours:00}:{value.Minutes:00}:{value.Seconds:00}";
    public static string FormatPrecise(TimeSpan value) => $"{Format(value)}.{value.Milliseconds:000}";
}
