using Microsoft.Data.Sqlite;
using System.IO;
using System.Text.Json;
using TimersApp.Core;

namespace TimersApp.Desktop;

public sealed class WorkspaceStore
{
    public string Folder { get; }
    private readonly string connectionString;
    private static readonly JsonSerializerOptions json = new() { WriteIndented = false };
    public WorkspaceStore(string? folder = null)
    {
        Folder = folder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TimersApp");
        Directory.CreateDirectory(Folder);
        connectionString = new SqliteConnectionStringBuilder { DataSource = Path.Combine(Folder, "workspace.db") }.ToString();
        using var db = Open();
        using var command = db.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL; CREATE TABLE IF NOT EXISTS snapshots (name TEXT PRIMARY KEY, payload TEXT NOT NULL);";
        command.ExecuteNonQuery();
    }
    private SqliteConnection Open() { var db = new SqliteConnection(connectionString); db.Open(); return db; }
    public Workspace? Load(string slot = "current")
    {
        using var db = Open();
        using var command = db.CreateCommand();
        command.CommandText = "SELECT payload FROM snapshots WHERE name=$name";
        command.Parameters.AddWithValue("$name", slot);
        var payload = command.ExecuteScalar() as string;
        if (payload is null) return null;
        return Decode(payload);
    }
    internal static Workspace Decode(string payload)
    {
        var value = JsonSerializer.Deserialize<Workspace>(payload, json) ?? throw new InvalidDataException("Saved workspace is empty.");
        if (value.SchemaVersion is < 1 or > 2) throw new InvalidDataException("This workspace needs a different version of TimersApp.");
        if (value.Settings is null || value.Timers is null || value.NameHistory is null || value.Presets is null) throw new InvalidDataException("Saved workspace structure is incomplete.");
        if (value.Timers.Any(t => t is null || t.Segments is null) || value.Timers.Select(t => t.Id).Distinct().Count() != value.Timers.Count) throw new InvalidDataException("Saved timer records are invalid.");
        foreach (var timer in value.Timers)
        {
            if (timer.Segments.Any(s => s is null || s.ElapsedTicks < 0) || timer.Segments.Count(s => s.End is null) > 1 || timer.Segments.Select(s => s.Id).Distinct().Count() != timer.Segments.Count) throw new InvalidDataException("Saved timer segments are invalid.");
            try { _ = timer.Segments.Sum(s => s.ElapsedTicks); } catch (OverflowException ex) { throw new InvalidDataException("A saved timer total is outside the supported range.", ex); }
            timer.Alert ??= new(); timer.AlertState ??= new(); timer.Popout ??= new();
            timer.Name ??= "Untitled timer"; timer.Description ??= "";
            timer.Width = timer.Width is { } width ? Math.Clamp(width, 170, 1500) : null;
            timer.Height = timer.Height is { } height ? Math.Clamp(height, 100, 1200) : null;
            timer.Popout.Width = Math.Clamp(timer.Popout.Width, 150, 1800); timer.Popout.Height = Math.Clamp(timer.Popout.Height, 100, 1400);
            timer.Alert.IntervalSeconds = Math.Clamp(timer.Alert.IntervalSeconds, 1, 365 * 86400); timer.Alert.Volume = Math.Clamp(timer.Alert.Volume, 0, 1); timer.Alert.SnoozeMinutes = Math.Clamp(timer.Alert.SnoozeMinutes, 1, 1440);
            if (timer.Style is not null) NormalizeStyle(timer.Style); if (timer.PopoutStyle is not null) NormalizeStyle(timer.PopoutStyle);
        }
        value.Settings.DefaultStyle ??= new(); value.Settings.PopoutStyle ??= TimerStyle.Minimal(); value.Settings.AppBackground ??= new();
        NormalizeStyle(value.Settings.DefaultStyle); NormalizeStyle(value.Settings.PopoutStyle); NormalizeBackground(value.Settings.AppBackground);
        foreach (var preset in value.Presets) { if (preset is null || preset.Style is null) throw new InvalidDataException("A saved style preset is invalid."); NormalizeStyle(preset.Style); }
        value.SchemaVersion = 2;
        if (value.Settings.RoundMinutes is not (1 or 15 or 60)) value.Settings.RoundMinutes = 15;
        value.Settings.AppScale = Math.Clamp(value.Settings.AppScale, .65, 1.6);
        value.Settings.TimerScale = Math.Clamp(value.Settings.TimerScale, .6, 1.8);
        value.Settings.Columns = Math.Clamp(value.Settings.Columns, 1, 8);
        value.Settings.CardWidth = Math.Clamp(value.Settings.CardWidth, 250, 1200); value.Settings.CardHeight = Math.Clamp(value.Settings.CardHeight, 220, 1200);
        value.Settings.Rows = Math.Clamp(value.Settings.Rows, 0, 100); value.Settings.Spacing = Math.Clamp(value.Settings.Spacing, 0, 80); value.Settings.ListRowHeight = Math.Clamp(value.Settings.ListRowHeight, 100, 1200);
        if (!Enum.IsDefined(value.Settings.ExitChoice)) value.Settings.ExitChoice = ExitChoice.Ask;
        if (!Enum.IsDefined(value.Settings.TotalDisplay)) value.Settings.TotalDisplay = TotalDisplay.Both;
        return value;
    }
    private static void NormalizeStyle(TimerStyle style)
    {
        style.Background ??= new(); NormalizeBackground(style.Background);
        style.FontFamily = string.IsNullOrWhiteSpace(style.FontFamily) || style.FontFamily.Contains('/') || style.FontFamily.Contains('\\') ? "Segoe UI" : style.FontFamily;
        style.FontSize = Math.Clamp(style.FontSize, 10, 200); style.Padding = Math.Clamp(style.Padding, 0, 80); style.CornerRadius = Math.Clamp(style.CornerRadius, 0, 100);
        if (!Enum.IsDefined(style.Digits)) style.Digits = DigitStyle.Text;
        style.Units &= TimeUnits.Years | TimeUnits.Days | TimeUnits.Hours | TimeUnits.Minutes | TimeUnits.Seconds | TimeUnits.Milliseconds;
        if (style.Units == 0) style.Units = TimeUnits.Hours | TimeUnits.Minutes | TimeUnits.Seconds;
    }
    private static void NormalizeBackground(BackgroundStyle style)
    {
        if (!Enum.IsDefined(style.Fit)) style.Fit = ImageFit.Fill;
        style.AnchorX = Math.Clamp(style.AnchorX, 0, 1); style.AnchorY = Math.Clamp(style.AnchorY, 0, 1); style.Zoom = Math.Clamp(style.Zoom, .1, 8); style.ImageOpacity = Math.Clamp(style.ImageOpacity, 0, 1);
        style.CropX = Math.Clamp(style.CropX, 0, .99); style.CropY = Math.Clamp(style.CropY, 0, .99); style.CropWidth = Math.Clamp(style.CropWidth, .001, 1 - style.CropX); style.CropHeight = Math.Clamp(style.CropHeight, .001, 1 - style.CropY);
    }
    public IReadOnlyList<string> Backups() => Directory.GetFiles(Folder, "backup-*.db").OrderDescending().ToArray();
    public static Workspace ReadDatabase(string path)
    {
        using var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString()); db.Open();
        using var command = db.CreateCommand(); command.CommandText = "SELECT payload FROM snapshots WHERE name='current'";
        return Decode(command.ExecuteScalar() as string ?? throw new InvalidDataException("The backup contains no saved workspace."));
    }
    public static void RecoverDatabase(string folder, string? backup)
    {
        var restored = backup is not null ? ReadDatabase(backup) : null;
        SqliteConnection.ClearAllPools();
        string preserved = Path.Combine(folder, "preserved-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-ffff"));
        Directory.CreateDirectory(preserved);
        foreach (var name in new[] { "workspace.db", "workspace.db-wal", "workspace.db-shm" })
        {
            string source = Path.Combine(folder, name); if (File.Exists(source)) File.Move(source, Path.Combine(preserved, name));
        }
        if (restored is not null) new WorkspaceStore(folder).Save(restored);
    }
    public void Save(Workspace value, string slot = "current")
    {
        var payload = JsonSerializer.Serialize(value, json);
        using var db = Open();
        using var transaction = db.BeginTransaction();
        using var command = db.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO snapshots(name,payload) VALUES($name,$payload) ON CONFLICT(name) DO UPDATE SET payload=excluded.payload";
        command.Parameters.AddWithValue("$name", slot);
        command.Parameters.AddWithValue("$payload", payload);
        command.ExecuteNonQuery();
        if (slot == "current")
        {
            command.Parameters["$name"].Value = "preferences";
            command.Parameters["$payload"].Value = JsonSerializer.Serialize(new Workspace { Settings = new AppSettings { AutoSave = value.Settings.AutoSave, RestoreOnLaunch = value.Settings.RestoreOnLaunch, ExitChoice = value.Settings.ExitChoice } }, json);
            command.ExecuteNonQuery();
        }
        transaction.Commit();
    }
    public void Backup()
    {
        using var source = Open();
        var file = Path.Combine(Folder, $"backup-{DateTime.Now:yyyyMMdd-HHmmss-ffff}.db");
        using var target = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = file }.ToString());
        target.Open();
        source.BackupDatabase(target);
        foreach (var old in Directory.GetFiles(Folder, "backup-*.db").OrderDescending().Skip(20)) File.Delete(old);
    }
    public static Workspace Copy(Workspace value) => JsonSerializer.Deserialize<Workspace>(JsonSerializer.Serialize(value, json), json)!;
}
