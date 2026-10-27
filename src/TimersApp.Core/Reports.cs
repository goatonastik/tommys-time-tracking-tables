using System.Numerics;

namespace TimersApp.Core;

public sealed record ReportOptions(DateOnly? From, DateOnly? Through, bool IncludeRunning = true, Guid? TimerId = null,
    IReadOnlyCollection<Guid>? TimerIds = null);
public sealed record ReportRow(Guid TimerId, Guid SegmentId, string Name, string Description, DateOnly Day,
    DateTimeOffset ActualStart, DateTimeOffset? ActualStop, DateTimeOffset MeasuredThrough,
    DateTimeOffset PortionStart, DateTimeOffset PortionStop, long Ticks, bool Recovered, bool ClockAdjusted);

public static class Reports
{
    public static List<ReportRow> Build(Workspace workspace, ReportOptions options, TimeZoneInfo zone)
    {
        if (options.From > options.Through) throw new ArgumentException("Start date must be on or before end date.");
        var rows = new List<ReportRow>();
        foreach (var timer in workspace.Timers.Where(t => (options.TimerId is null || t.Id == options.TimerId) && (options.TimerIds is null || options.TimerIds.Contains(t.Id))))
        foreach (var segment in timer.Segments.Where(s => s.End is not null || options.IncludeRunning))
        {
            var end = segment.End ?? segment.MeasuredThrough;
            var start = segment.Start;
            if (end <= start)
            {
                var day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(start, zone).DateTime);
                if (InRange(day, options)) rows.Add(Row(timer, segment, day, start, end, segment.ElapsedTicks));
                continue;
            }
            var cursor = start;
            if (options.From is { } from) { var bound = Midnight(from, zone); if (bound > cursor) cursor = bound; }
            var limit = end;
            if (options.Through is { } through && through != DateOnly.MaxValue) { var bound = Midnight(through.AddDays(1), zone); if (bound < limit) limit = bound; }
            while (cursor < limit)
            {
                var day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(cursor, zone).DateTime);
                var boundary = day == DateOnly.MaxValue ? limit : Midnight(day.AddDays(1), zone);
                var next = boundary < limit ? boundary : limit;
                if (next <= cursor) throw new InvalidOperationException("Could not resolve a calendar boundary.");
                // Cumulative allocation avoids rounding loss when dividing a segment across days.
                // Multi-year tick products exceed decimal capacity; integer allocation also preserves remainders.
                long before = Allocate((cursor - start).Ticks, segment.ElapsedTicks, (end - start).Ticks);
                long after = next == end ? segment.ElapsedTicks : Allocate((next - start).Ticks, segment.ElapsedTicks, (end - start).Ticks);
                if (InRange(day, options)) rows.Add(Row(timer, segment, day, cursor, next, after - before));
                cursor = next;
            }
        }
        return rows.OrderBy(r => r.Day).ThenBy(r => r.Name).ThenBy(r => r.PortionStart).ToList();
    }
    private static bool InRange(DateOnly day, ReportOptions options) => (options.From is null || day >= options.From) && (options.Through is null || day <= options.Through);
    private static long Allocate(long portion, long elapsed, long wall) => (long)((BigInteger)portion * elapsed / wall);
    private static DateTimeOffset Midnight(DateOnly day, TimeZoneInfo zone)
    {
        var local = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        while (zone.IsInvalidTime(local)) local = local.AddMinutes(1);
        if (zone.IsAmbiguousTime(local)) return new DateTimeOffset(local, zone.GetAmbiguousTimeOffsets(local).Max());
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zone), TimeSpan.Zero);
    }
    private static ReportRow Row(TimerRecord timer, Segment s, DateOnly day, DateTimeOffset from, DateTimeOffset to, long ticks) =>
        new(timer.Id, s.Id, s.Name, s.Description, day, s.Start, s.End, s.MeasuredThrough, from, to, ticks, s.Recovered, s.ClockAdjusted);
}
