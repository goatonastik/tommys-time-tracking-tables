using System.IO;
using System.Text.Json;
using ClosedXML.Excel;
using TimersApp.Core;
using TimersApp.Desktop;

internal static class ReliabilityChecks
{
    public static void Run(Action<string, Action> test, string artifacts)
    {
        static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}"); }
        test("Loading checkpoint cannot erase recovery gap", () =>
        {
            var time = new ManualClock(); var w = new Workspace(); var engine = new TimerEngine(w, time); var t = engine.Add("Gap"); engine.Start(t); time.Advance(20); engine.Checkpoint();
            var copy = Appearance.Copy(w); var saved = copy.Timers[0].Segments[0].MeasuredThrough; time.Advance(3600);
            var restored = new TimerEngine(copy, time); restored.Checkpoint(); Equal(saved, copy.Timers[0].Segments[0].MeasuredThrough);
            restored.Recover(true, false); Equal(3620d, restored.Total(copy.Timers[0]).TotalSeconds);
        });
        test("Sleep excludes sleeping time and preserves exact segments", () =>
        {
            var time = new ManualClock(); var w = new Workspace(); var engine = new TimerEngine(w, time); var t = engine.Add("Sleep"); engine.Start(t); time.Advance(60);
            engine.SuspendForSleep(); Equal(false, t.IsRunning); Equal(true, t.ResumeAfterSleep); time.Advance(7200); engine.ResumeFromSleep(); time.Advance(30); engine.Stop(t);
            Equal(90d, engine.Total(t).TotalSeconds); Equal(2, t.Segments.Count); Equal(7200d, (t.Segments[1].Start - t.Segments[0].End!.Value).TotalSeconds);
        });
        test("Crash while sleep-paused does not count the sleep gap", () =>
        {
            var time = new ManualClock(); var w = new Workspace(); var engine = new TimerEngine(w, time); var t = engine.Add("Sleep crash"); engine.Start(t); time.Advance(10); engine.SuspendForSleep();
            var copy = Appearance.Copy(w); time.Advance(3000); var restored = new TimerEngine(copy, time); restored.Recover(true, true);
            Equal(10d, restored.Total(copy.Timers[0]).TotalSeconds); Equal(true, copy.Timers[0].IsRunning);
        });
        test("Discard restores session baseline and remembers only chosen decision", () =>
        {
            var baseline = new Workspace(); baseline.Settings.AutoSave = false; baseline.Settings.AppScale = .9;
            var current = Appearance.Copy(baseline); current.Settings.AutoSave = true; current.Settings.AppScale = 1.4; current.Timers.Add(new TimerRecord());
            var discard = ExitWorkspace.Prepare(current, baseline, ExitChoice.Discard, true);
            Equal(0, discard.Timers.Count); Equal(false, discard.Settings.AutoSave); Equal(.9, discard.Settings.AppScale); Equal(ExitChoice.Discard, discard.Settings.ExitChoice); Equal(true, discard.CleanShutdown);
            var save = ExitWorkspace.Prepare(current, baseline, ExitChoice.Save, false); Equal(1, save.Timers.Count); Equal(ExitChoice.Ask, save.Settings.ExitChoice); Equal(false, current.CleanShutdown);
        });
        test("Current workspace and launch preferences commit together", () =>
        {
            var store = new WorkspaceStore(Path.Combine(artifacts, "commit-" + Guid.NewGuid().ToString("N"))); var w = new Workspace();
            w.Settings.AutoSave = false; w.Settings.RestoreOnLaunch = false; w.Settings.ExitChoice = ExitChoice.Discard; store.Save(w);
            var prefs = store.Load("preferences")!; Equal(false, prefs.Settings.AutoSave); Equal(false, prefs.Settings.RestoreOnLaunch); Equal(ExitChoice.Discard, prefs.Settings.ExitChoice);
        });
        test("Schema migration and malformed segment rejection", () =>
        {
            var old = new Workspace { SchemaVersion = 1 }; Equal(2, WorkspaceStore.Decode(JsonSerializer.Serialize(old)).SchemaVersion);
            var bad = new Workspace(); var t = new TimerRecord(); bad.Timers.Add(t); t.Segments.Add(new Segment()); t.Segments.Add(new Segment());
            bool rejected = false; try { WorkspaceStore.Decode(JsonSerializer.Serialize(bad)); } catch (InvalidDataException) { rejected = true; } Equal(true, rejected);
        });
        test("Database recovery preserves damaged files and restores valid data", () =>
        {
            string folder = Path.Combine(artifacts, "recovery-" + Guid.NewGuid().ToString("N")); var store = new WorkspaceStore(folder); var w = new Workspace(); w.Timers.Add(new TimerRecord { Name = "Safe" }); store.Save(w); store.Backup();
            var backup = store.Backups()[0]; Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.WriteAllText(Path.Combine(folder, "workspace.db"), "damaged fixture");
            WorkspaceStore.RecoverDatabase(folder, backup); Equal("Safe", new WorkspaceStore(folder).Load()!.Timers[0].Name); Equal(1, Directory.GetDirectories(folder, "preserved-*").Length);
        });
        test("Fall-back DST day has 25 hours and selection keeps separate IDs", () =>
        {
            var w = new Workspace(); var a = new TimerRecord { Name = "Same name" }; var b = new TimerRecord { Name = "Same name" }; w.Timers.AddRange([a, b]);
            a.Segments.Add(new Segment { Start = new(2024, 11, 3, 0, 0, 0, TimeSpan.FromHours(-4)), End = new(2024, 11, 4, 0, 0, 0, TimeSpan.FromHours(-5)), ElapsedTicks = TimeSpan.FromHours(25).Ticks });
            b.Segments.Add(new Segment { Start = a.Segments[0].Start, End = a.Segments[0].End, ElapsedTicks = 123 });
            var rows = Reports.Build(w, new(null, null, TimerIds: [a.Id]), TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")); Equal(1, rows.Count); Equal(TimeSpan.FromHours(25).Ticks, rows[0].Ticks); Equal(a.Id, rows[0].TimerId);
        });
        test("Excel preserves recorded offset, active blank stop, and subtotals", () =>
        {
            var w = new Workspace(); var t = new TimerRecord { Name = "=2+2" }; w.Timers.Add(t);
            var start = new DateTimeOffset(2025, 6, 5, 12, 30, 0, TimeSpan.FromHours(9)); t.Segments.Add(new Segment { Name = t.Name, Start = start, MeasuredThrough = start.AddMinutes(7), ElapsedTicks = TimeSpan.FromMinutes(7).Ticks });
            string path = Path.Combine(artifacts, "offsets.xlsx"); ExcelExport.Write(path, w, new(null, null, Totals: TotalDisplay.Exact)); using var book = new XLWorkbook(path); var detail = book.Worksheet("Session detail");
            Equal(start.DateTime, detail.Cell(2, 5).GetDateTime()); Equal("+09:00", detail.Cell(2, 13).GetString()); Equal(true, detail.Cell(2, 6).IsEmpty()); Equal(false, detail.Cell(2, 3).HasFormula);
            Equal(true, detail.RowsUsed().Any(row => row.Cell(3).GetString() == "Day total")); Equal(true, detail.RowsUsed().Any(row => row.Cell(3).GetString() == "Month total")); Equal(true, book.Worksheet("Monthly totals").Column(4).IsHidden);
        });
        test("Multi-year export preserves every tick and clips consistently", () =>
        {
            var w = new Workspace(); var t = new TimerRecord(); w.Timers.Add(t);
            var start = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero); var end = start.AddYears(4);
            long ticks = (end - start).Ticks - 17;
            t.Segments.Add(new Segment { Start = start, End = end, MeasuredThrough = end, ElapsedTicks = ticks });
            var rows = Reports.Build(w, new(null, null), TimeZoneInfo.Utc);
            Equal(1461, rows.Count); Equal(ticks, rows.Sum(r => r.Ticks));
            var from = new DateOnly(2021, 1, 1); var through = new DateOnly(2022, 12, 31);
            var clipped = Reports.Build(w, new(from, through), TimeZoneInfo.Utc);
            Equal(rows.Where(r => r.Day >= from && r.Day <= through).Sum(r => r.Ticks), clipped.Sum(r => r.Ticks));
        });
        test("Last supported calendar month exports without date overflow", () =>
        {
            var w = new Workspace(); var t = new TimerRecord(); w.Timers.Add(t);
            var start = new DateTimeOffset(9999, 12, 30, 12, 0, 0, TimeSpan.Zero);
            t.Segments.Add(new Segment { Start = start, End = start.AddHours(1), MeasuredThrough = start.AddHours(1), ElapsedTicks = TimeSpan.TicksPerHour });
            string path = Path.Combine(artifacts, "last-month.xlsx"); ExcelExport.Write(path, w, new(null, DateOnly.MaxValue));
            using var book = new XLWorkbook(path); Equal("9999-12", book.Worksheet("Monthly totals").Cell(2, 1).GetString());
        });
        test("CSV replacement failure preserves the existing report and cleans temporary files", () =>
        {
            var w = new Workspace(); var t = new TimerRecord(); w.Timers.Add(t);
            var start = new DateTimeOffset(2025, 4, 1, 12, 0, 0, TimeSpan.Zero);
            t.Segments.Add(new Segment { Name = "=1+1", Description = "Quoted \"note\", with comma", Start = start, End = start.AddSeconds(65), MeasuredThrough = start.AddSeconds(65), ElapsedTicks = TimeSpan.FromSeconds(65).Ticks });
            string folder = Path.Combine(artifacts, "csv-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "report.csv"); File.WriteAllText(path, "existing report");
            using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                bool failed = false; try { ExcelExport.Write(path, w, new(null, null)); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { failed = true; }
                Equal(true, failed); Equal("existing report", File.ReadAllText(path)); Equal(1, Directory.GetFiles(folder).Length);
            }
            ExcelExport.Write(path, w, new(null, null)); string csv = File.ReadAllText(path);
            Equal(true, csv.Contains("\"'=1+1\"")); Equal(true, csv.Contains("Quoted \"\"note\"\", with comma")); Equal(1, Directory.GetFiles(folder).Length);
        });
        test("Empty export and fifty simultaneous timers remain valid", () =>
        {
            var w = new Workspace(); string path = Path.Combine(artifacts, "empty.xlsx"); ExcelExport.Write(path, w, new(null, null));
            var clock = new ManualClock(); var engine = new TimerEngine(w, clock); for (int i = 0; i < 50; i++) engine.Start(engine.Add("Timer " + i)); clock.Advance(3661);
            foreach (var t in w.Timers) Equal(3661d, engine.Total(t).TotalSeconds); engine.Checkpoint(); Equal(50, w.Timers.Count(t => t.IsRunning));
        });
    }
}
