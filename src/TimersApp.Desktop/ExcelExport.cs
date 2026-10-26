using ClosedXML.Excel;
using System.Globalization;
using System.IO;
using System.Text;
using TimersApp.Core;

namespace TimersApp.Desktop;
internal static class ExcelExport
{
    public static void Write(string path, Workspace workspace, ReportOptions options)
    {
        var rows = Reports.Build(workspace, options, TimeZoneInfo.Local);
        if (Path.GetExtension(path).Equals(".csv", StringComparison.OrdinalIgnoreCase)) { Csv(path, rows); return; }
        using var book = new XLWorkbook();
        var detail = book.AddWorksheet("Session detail");
        string[] headers = ["Month", "Day", "Timer", "Description", "Actual start (recorded local)", "Actual stop (recorded local)", "Measured through (recorded local)", "Allocated from (report zone)", "Allocated to (report zone)", "Exact duration", "Segment ID", "Status", "Start UTC offset", "Stop UTC offset", "Snapshot UTC offset", "Timer ID"];
        Header(detail, headers);
        int r = 2;
        foreach (var month in rows.GroupBy(x => x.Day.ToString("yyyy-MM")))
        {
            int monthStart = r;
            foreach (var day in month.GroupBy(x => x.Day))
            {
                int dayStart = r;
                foreach (var row in day)
                {
                    detail.Cell(r, 1).Value = month.Key; detail.Cell(r, 2).Value = row.Day.ToDateTime(TimeOnly.MinValue);
                    detail.Cell(r, 3).Value = row.Name; detail.Cell(r, 4).Value = row.Description;
                    detail.Cell(r, 5).Value = row.ActualStart.DateTime;
                    if (row.ActualStop is { } stop) detail.Cell(r, 6).Value = stop.DateTime;
                    detail.Cell(r, 7).Value = row.MeasuredThrough.DateTime;
                    detail.Cell(r, 8).Value = row.PortionStart.LocalDateTime; detail.Cell(r, 9).Value = row.PortionStop.LocalDateTime;
                    detail.Cell(r, 10).Value = TimeSpan.FromTicks(row.Ticks).TotalDays;
                    detail.Cell(r, 11).Value = row.SegmentId.ToString();
                    detail.Cell(r, 12).Value = (row.ActualStop is null ? "Running snapshot" : "Stopped") + (row.Recovered ? " / recovered" : "") + (row.ClockAdjusted ? " / clock changed; date allocation estimated" : "");
                    detail.Cell(r, 13).Value = row.ActualStart.ToString("zzz");
                    detail.Cell(r, 14).Value = row.ActualStop?.ToString("zzz") ?? "";
                    detail.Cell(r, 15).Value = row.MeasuredThrough.ToString("zzz"); detail.Cell(r, 16).Value = row.TimerId.ToString(); r++;
                }
                if (r > dayStart) detail.Rows(dayStart, r - 1).Group();
                detail.Cell(r, 1).Value = month.Key; detail.Cell(r, 2).Value = day.Key.ToDateTime(TimeOnly.MinValue);
                detail.Cell(r, 3).Value = "Day total"; detail.Cell(r, 10).Value = TimeSpan.FromTicks(day.Sum(x => x.Ticks)).TotalDays;
                detail.Row(r).Style.Font.Bold = true; detail.Row(r).Style.Fill.BackgroundColor = XLColor.FromHtml("#EDF4F7"); r++;
            }
            if (r > monthStart) detail.Rows(monthStart, r - 1).Group();
            detail.Cell(r, 1).Value = month.Key; detail.Cell(r, 3).Value = "Month total"; detail.Cell(r, 10).Value = TimeSpan.FromTicks(month.Sum(x => x.Ticks)).TotalDays;
            detail.Row(r).Style.Font.Bold = true; detail.Row(r).Style.Fill.BackgroundColor = XLColor.FromHtml("#D6E9E3"); r++;
        }
        detail.Column(2).Style.DateFormat.Format = "yyyy-mm-dd";
        detail.Columns(5, 9).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss.000";
        detail.Column(10).Style.NumberFormat.Format = "[h]:mm:ss.000";
        Finish(detail, r - 1, headers.Length);

        var names = workspace.Timers.ToDictionary(t => t.Id, t => t.Name);
        var daily = rows.GroupBy(x => (x.TimerId, x.Day)).Select(g => new { g.Key.TimerId, g.Key.Day, Name = names[g.Key.TimerId], Ticks = g.Sum(x => x.Ticks) }).ToList();
        var days = book.AddWorksheet("Daily totals");
        Header(days, ["Month", "Day", "Timer", "Exact total", "Rounded daily total", "Timer ID"]); r = 2;
        foreach (var day in daily)
        {
            days.Cell(r, 1).Value = day.Day.ToString("yyyy-MM"); days.Cell(r, 2).Value = day.Day.ToDateTime(TimeOnly.MinValue); days.Cell(r, 3).Value = day.Name;
            days.Cell(r, 4).Value = TimeSpan.FromTicks(day.Ticks).TotalDays;
            if (options.Basis == RoundingBasis.Daily) days.Cell(r, 5).Value = Duration.Round(TimeSpan.FromTicks(day.Ticks), options.RoundMinutes).TotalDays;
            days.Cell(r, 6).Value = day.TimerId.ToString();
            r++;
        }
        days.Column(2).Style.DateFormat.Format = "yyyy-mm-dd"; days.Columns(4, 5).Style.NumberFormat.Format = "[h]:mm:ss"; Finish(days, r - 1, 6);
        if (options.Totals == TotalDisplay.Rounded && options.Basis == RoundingBasis.Daily) days.Column(4).Hide();
        if (options.Totals == TotalDisplay.Exact || options.Basis == RoundingBasis.Monthly) days.Column(5).Hide();

        var months = book.AddWorksheet("Monthly totals"); Header(months, ["Month", "Timer", "Exact total", "Rounded total", "Date coverage", "Timer ID"]); r = 2;
        foreach (var group in daily.GroupBy(x => (x.TimerId, Month: x.Day.ToString("yyyy-MM"))))
        {
            var exact = TimeSpan.FromTicks(group.Sum(x => x.Ticks));
            var rounded = options.Basis == RoundingBasis.Monthly ? Duration.Round(exact, options.RoundMinutes) : TimeSpan.FromTicks(group.Sum(x => Duration.Round(TimeSpan.FromTicks(x.Ticks), options.RoundMinutes).Ticks));
            var first = new DateOnly(group.First().Day.Year, group.First().Day.Month, 1);
            var last = new DateOnly(first.Year, first.Month, DateTime.DaysInMonth(first.Year, first.Month));
            months.Cell(r, 1).Value = group.Key.Month; months.Cell(r, 2).Value = group.Last().Name; months.Cell(r, 3).Value = exact.TotalDays; months.Cell(r, 4).Value = rounded.TotalDays;
            months.Cell(r, 5).Value = options.From > first || options.Through < last ? "Partial month (selected range)" : "All recorded time";
            months.Cell(r, 6).Value = group.Key.TimerId.ToString(); r++;
        }
        months.Columns(3, 4).Style.NumberFormat.Format = "[h]:mm:ss"; Finish(months, r - 1, 6);
        if (options.Totals == TotalDisplay.Rounded) months.Column(3).Hide();
        if (options.Totals == TotalDisplay.Exact) months.Column(4).Hide();
        var info = book.AddWorksheet("Export information"); Header(info, ["Setting", "Value"]);
        string[][] settings = [["Application", "Tommy’s Time Tracking Tables"], ["Exported", DateTimeOffset.Now.ToString("O")], ["Time zone", TimeZoneInfo.Local.Id], ["Start date", options.From?.ToString() ?? "All"], ["End date", options.Through?.ToString() ?? "All"], ["Rounding", $"Nearest {options.RoundMinutes} minutes; midpoint upward; {options.Basis}"], ["Running timers", options.IncludeRunning ? "Included as snapshots; stop remains blank" : "Excluded"], ["Date allocation", "Running duration allocated across wall-clock calendar portions; exact duration remains independent of clock changes."], ["Archived timers", "Included when they match the selected timer filter"]];
        r = 2; foreach (var pair in settings) { info.Cell(r, 1).Value = pair[0]; info.Cell(r, 2).Value = pair[1]; r++; }
        info.Cell(r, 1).Value = "Selected timers"; info.Cell(r++, 2).Value = string.Join(", ", workspace.Timers.Where(t => (options.TimerId is null || t.Id == options.TimerId) && (options.TimerIds is null || options.TimerIds.Contains(t.Id))).Select(t => t.Name + " [" + t.Id + "]"));
        info.Cell(r, 1).Value = "Summary display"; info.Cell(r++, 2).Value = options.Totals.ToString();
        info.Cell(r, 1).Value = "Detail totals"; info.Cell(r++, 2).Value = "Day/month subtotal rows have blank Segment ID. Filter to populated Segment ID when summing detail rows yourself.";
        Finish(info, r - 1, 2); info.Column(2).Width = 80; info.Column(2).Style.Alignment.WrapText = true;
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".xlsx";
        try { book.SaveAs(temporary); File.Move(temporary, path, true); } finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    private static void Header(IXLWorksheet sheet, string[] headings) { for (int i = 0; i < headings.Length; i++) sheet.Cell(1, i + 1).Value = headings[i]; }
    private static void Finish(IXLWorksheet sheet, int last, int columns)
    {
        sheet.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#182338"); sheet.Row(1).Style.Font.FontColor = XLColor.White; sheet.Row(1).Style.Font.Bold = true;
        sheet.SheetView.FreezeRows(1); sheet.Range(1, 1, Math.Max(last, 1), columns).SetAutoFilter(); sheet.Columns(1, columns).AdjustToContents(1, Math.Max(1, Math.Min(last, 200)));
        foreach (var col in sheet.Columns(1, columns)) col.Width = Math.Clamp(col.Width + 2, 14, 45);
    }
    private static void Csv(string path, List<ReportRow> rows)
    {
        static string Quote(string value) { if (value.Length > 0 && "=+-@\t\r".Contains(value[0])) value = "'" + value; return "\"" + value.Replace("\"", "\"\"") + "\""; }
        var text = new StringBuilder("Month,Day,Timer,Description,Actual start,Actual stop,Measured through,Exact seconds,Segment ID\r\n");
        foreach (var row in rows) text.AppendLine(string.Join(",", new[] { row.Day.ToString("yyyy-MM"), row.Day.ToString("yyyy-MM-dd"), row.Name, row.Description, row.ActualStart.ToString("O"), row.ActualStop?.ToString("O") ?? "", row.MeasuredThrough.ToString("O"), TimeSpan.FromTicks(row.Ticks).TotalSeconds.ToString("R", CultureInfo.InvariantCulture), row.SegmentId.ToString() }.Select(Quote)));
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temporary, text.ToString(), new UTF8Encoding(true)); File.Move(temporary, path, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
