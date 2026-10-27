using ClosedXML.Excel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using TimersApp.Core;
using Duration=TimersApp.Core.Duration;

namespace TimersApp.Desktop;
internal static class ExcelExport
{
    internal static readonly string[] Headers = ["Total time (HH:MM:SS)", "Daily total (hours, nearest 15 min)", "Timer name", "Timer description", "Date started", "Actual start", "Actual stop", "Status", "Segment ID"];
    public static void Write(string path, Workspace workspace, ReportOptions options)
    {
        var rows = ExportTable.Build(workspace, options, TimeZoneInfo.Local);
        if (Path.GetExtension(path).Equals(".csv", StringComparison.OrdinalIgnoreCase)) { Csv(path, rows); return; }
        using var book = new XLWorkbook(); var sheet = book.AddWorksheet("Recorded time");
        for (int c = 0; c < Headers.Length; c++) sheet.Cell(1, c + 1).Value = Headers[c];
        sheet.Cell(1, 1).CreateComment().AddText($"Workspace: {workspace.Name}\nReport time zone: {TimeZoneInfo.Local.Id}\nExact measured running duration allocated to selected calendar days. Actual timestamps retain recorded local time; offsets appear in cell comments.");
        sheet.Cell(1, 2).CreateComment().AddText("Each timer's exact calendar-day total is rounded once to the nearest quarter hour; midpoints round up. The value appears on the first detail row for that timer/day only. Blank cells are intentional. Filtering segment rows can hide the row carrying the daily total.");
        int r = 2;
        foreach (var line in rows)
        {
            var p = line.Portion;
            sheet.Cell(r, 1).Value = (double)p.Ticks / TimeSpan.TicksPerDay;
            if (line.DailyHours is { } rounded) sheet.Cell(r, 2).Value = (double)rounded;
            sheet.Cell(r, 3).Value = p.Name; sheet.Cell(r, 4).Value = p.Description;
            sheet.Cell(r, 5).Value = p.ActualStart.Date;
            sheet.Cell(r, 6).Value = p.ActualStart.DateTime;
            sheet.Cell(r, 6).CreateComment().AddText(p.ActualStart.ToString("O"));
            if (p.ActualStop is { } stop) { sheet.Cell(r, 7).Value = stop.DateTime; sheet.Cell(r, 7).CreateComment().AddText(stop.ToString("O")); }
            sheet.Cell(r, 8).Value = line.Status; sheet.Cell(r, 9).Value = line.ShortId;
            sheet.Cell(r, 9).CreateComment().AddText($"Segment: {p.SegmentId:N}\nTimer: {p.TimerId:N}\nAllocated day: {p.Day:yyyy-MM-dd}");
            r++;
        }
        sheet.Column(1).Style.NumberFormat.Format = "[h]:mm:ss";
        sheet.Column(2).Style.NumberFormat.Format = "0.00";
        sheet.Column(5).Style.DateFormat.Format = "mmmm d, yyyy";
        sheet.Columns(6, 7).Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss.000";
        var header = sheet.Range(1, 1, 1, Headers.Length);
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#182338"); header.Style.Font.FontColor = XLColor.White; header.Style.Font.Bold = true;
        header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        sheet.Columns(1, Headers.Length).AdjustToContents();
        foreach (var column in sheet.Columns(1, Headers.Length)) column.Width = Math.Clamp(column.Width + 2, 14, 60);
        sheet.Column(4).Width = 45; sheet.Column(8).Width = 52;
        sheet.Range(1, 1, Math.Max(1, r - 1), Headers.Length).Style.Alignment.WrapText = true;
        // ClosedXML does not reliably auto-fit wrapped text. Measure every cell at its final width.
        for (int row = 1; row < r; row++)
        {
            double height = 24;
            for (int col = 1; col <= Headers.Length; col++)
            {
                string value = sheet.Cell(row, col).GetFormattedString();
                var formatted = new FormattedText(value.Length==0?" ":value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Calibri"), FontStyles.Normal, row==1?FontWeights.Bold:FontWeights.Normal, FontStretches.Normal), 11*96d/72, Brushes.Black, 1)
                    { MaxTextWidth=Math.Max(10,sheet.Column(col).Width*7-8) };
                double wrappedHeight=0;
                foreach(string part in value.Split('\n'))
                {
                    var line=new FormattedText(part.Length==0?" ":part,CultureInfo.InvariantCulture,FlowDirection.LeftToRight,
                        new Typeface(new FontFamily("Calibri"),FontStyles.Normal,row==1?FontWeights.Bold:FontWeights.Normal,FontStretches.Normal),11*96d/72,Brushes.Black,1);
                    wrappedHeight+=Math.Max(1,Math.Ceiling(line.WidthIncludingTrailingWhitespace/formatted.MaxTextWidth))*line.Height;
                }
                height=Math.Max(height,Math.Max(formatted.Height,wrappedHeight)*72/96+8);
            }
            sheet.Row(row).Height = Math.Min(409, height);
        }
        sheet.SheetView.FreezeRows(1); sheet.Range(1, 1, Math.Max(1, r - 1), Headers.Length).SetAutoFilter();
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".xlsx";
        try { book.SaveAs(temporary); File.Move(temporary, path, true); } finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    private static void Csv(string path, List<ExportLine> rows)
    {
        static string Quote(string value) { if (value.Length > 0 && "=+-@\t\r".Contains(value[0])) value = "'" + value; return "\"" + value.Replace("\"", "\"\"") + "\""; }
        var text = new StringBuilder(); text.AppendLine(string.Join(",", Headers.Select(Quote)));
        foreach (var line in rows)
        {
            var p = line.Portion;
            string[] cells = [Duration.FormatPrecise(TimeSpan.FromTicks(p.Ticks)), line.DailyHours?.ToString("0.00", CultureInfo.InvariantCulture) ?? "", p.Name, p.Description,
                p.ActualStart.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture), p.ActualStart.ToString("O"), p.ActualStop?.ToString("O") ?? "", line.Status, line.ShortId];
            text.AppendLine(string.Join(",", cells.Select(Quote)));
        }
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temporary, text.ToString(), new UTF8Encoding(true)); File.Move(temporary, path, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
