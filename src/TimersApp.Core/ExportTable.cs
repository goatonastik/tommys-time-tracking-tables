namespace TimersApp.Core;

public sealed record ExportLine(ReportRow Portion, decimal? DailyHours, bool ShortIdCollision)
{
    public string ShortId => Portion.SegmentId.ToString("N")[..8];
    public string Status
    {
        get
        {
            var p = Portion;
            string value = p.ActualStop is null ? $"Running snapshot through {p.MeasuredThrough:yyyy-MM-dd HH:mm:ss.fff zzz}" : "Stopped";
            value += $"; allocated day {p.Day:yyyy-MM-dd}";
            if (p.PortionStart != p.ActualStart || p.PortionStop != (p.ActualStop ?? p.MeasuredThrough)) value += "; partial segment";
            if (DailyHours is not null) value += "; rounded hours cover all entries for this timer/day";
            if (p.Recovered) value += "; recovered";
            if (p.ClockAdjusted) value += "; clock changed, calendar allocation estimated";
            if (ShortIdCollision) value += $"; short ID collision, full segment {p.SegmentId:N}, timer {p.TimerId:N}";
            return value;
        }
    }
}

public static class ExportTable
{
    public static List<ExportLine> Build(Workspace workspace, ReportOptions options, TimeZoneInfo zone)
    {
        var rows = Reports.Build(workspace, options, zone).OrderBy(p => p.Day).ThenBy(p => p.Name)
            .ThenBy(p => p.TimerId).ThenBy(p => p.PortionStart).ThenBy(p => p.SegmentId).ToList();
        var collisions = rows.GroupBy(p => p.SegmentId.ToString("N")[..8])
            .Where(g => g.Select(p => (p.TimerId, p.SegmentId)).Distinct().Count() > 1).Select(g => g.Key).ToHashSet();
        var totals = rows.GroupBy(p => (p.TimerId, p.Day)).ToDictionary(g => g.Key,
            g => Duration.DecimalHours(TimeSpan.FromTicks(g.Sum(p => p.Ticks))));
        var seen = new HashSet<(Guid, DateOnly)>();
        return rows.Select(p => new ExportLine(p, seen.Add((p.TimerId, p.Day)) ? totals[(p.TimerId, p.Day)] : null,
            collisions.Contains(p.SegmentId.ToString("N")[..8]))).ToList();
    }
}
