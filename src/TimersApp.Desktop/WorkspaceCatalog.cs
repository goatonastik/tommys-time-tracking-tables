using Microsoft.Data.Sqlite;
using System.IO;
using System.Text.Json;
using TimersApp.Core;

namespace TimersApp.Desktop;

public sealed class BackupPolicy
{
    public bool DisableBackups { get; set; }
    public int MaxBackups { get; set; } = 32;
}
public sealed record WorkspaceInfo(Guid Id, string Name, bool Deleted);
public sealed record SnapshotWorkspace(Workspace Value,bool Deleted)
{
    public string DisplayName=>Value.Name+(Deleted?" (deleted)":"");
}

public sealed partial class WorkspaceStore
{
    private void InitializeCatalog(SqliteConnection db)
    {
        using var check = db.CreateCommand(); check.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='workspaces'";
        if (Convert.ToInt32(check.ExecuteScalar()) != 0) return;
        using var read = db.CreateCommand(); read.CommandText = "SELECT payload FROM snapshots WHERE name='current'";
        var payload = read.ExecuteScalar() as string;
        var legacy = payload is null ? null : Decode(payload);
        if(legacy is not null)
        {
            using var preferences=db.CreateCommand();preferences.CommandText="SELECT payload FROM snapshots WHERE name='preferences'";
            if(preferences.ExecuteScalar() is string preferenceText)
            {
                var savedPreferences=Decode(preferenceText);legacy.Settings.AutoSave=savedPreferences.Settings.AutoSave;legacy.Settings.RestoreOnLaunch=savedPreferences.Settings.RestoreOnLaunch;legacy.Settings.ExitChoice=savedPreferences.Settings.ExitChoice;
            }
        }
        if (legacy is not null)
        {
            string preserved = Path.Combine(Folder, "before-workspaces-" + Guid.NewGuid().ToString("N") + ".db");
            using var target = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = preserved, Pooling = false }.ToString()); target.Open(); db.BackupDatabase(target);
        }
        using var tx = db.BeginTransaction(); using var create = db.CreateCommand(); create.Transaction = tx;
        create.CommandText = "CREATE TABLE workspaces(id TEXT PRIMARY KEY, name TEXT NOT NULL, payload TEXT NOT NULL, deleted INTEGER NOT NULL DEFAULT 0); CREATE UNIQUE INDEX workspace_names ON workspaces(name COLLATE NOCASE) WHERE deleted=0; CREATE TABLE app_options(name TEXT PRIMARY KEY,payload TEXT NOT NULL);"; create.ExecuteNonQuery();
        if (legacy is not null)
        {
            UpsertWorkspace(db, tx, legacy);
            WriteSnapshot(db, tx, "current", JsonSerializer.Serialize(legacy, json));
            WriteSnapshot(db, tx, "preferences", PreferencePayload(legacy));
        }
        tx.Commit();
    }
    private static string PreferencePayload(Workspace value) => JsonSerializer.Serialize(new Workspace { Id = value.Id, Name = value.Name, Settings = new AppSettings { AutoSave = value.Settings.AutoSave, RestoreOnLaunch = value.Settings.RestoreOnLaunch, ExitChoice = value.Settings.ExitChoice } }, json);
    private static void WriteSnapshot(SqliteConnection db, SqliteTransaction tx, string slot, string payload)
    {
        using var c = db.CreateCommand(); c.Transaction = tx; c.CommandText = "INSERT OR REPLACE INTO snapshots(name,payload) VALUES($name,$payload)"; c.Parameters.AddWithValue("$name",slot); c.Parameters.AddWithValue("$payload",payload); c.ExecuteNonQuery();
    }
    private static void UpsertWorkspace(SqliteConnection db, SqliteTransaction tx, Workspace value)
    {
        string name = value.Name.Trim();
        if (name.Length is < 1 or > 100) throw new ArgumentException("Use a workspace name with 1–100 characters.");
        using var c = db.CreateCommand(); c.Transaction = tx;
        c.CommandText = "INSERT INTO workspaces(id,name,payload) VALUES($id,$name,$payload) ON CONFLICT(id) DO UPDATE SET name=excluded.name,payload=excluded.payload WHERE workspaces.deleted=0;";
        c.Parameters.AddWithValue("$id",value.Id.ToString("N")); c.Parameters.AddWithValue("$name",name); c.Parameters.AddWithValue("$payload",JsonSerializer.Serialize(value,json));
        if (c.ExecuteNonQuery() == 0) throw new InvalidOperationException("This workspace was deleted. Recover it as a new workspace first.");
    }
    public IReadOnlyList<WorkspaceInfo> SavedWorkspaces(bool deleted = false)
    {
        using var db = Open(); using var c = db.CreateCommand(); c.CommandText = "SELECT id,name,deleted FROM workspaces WHERE deleted=$deleted ORDER BY name COLLATE NOCASE"; c.Parameters.AddWithValue("$deleted",deleted ? 1 : 0);
        using var r = c.ExecuteReader(); var result = new List<WorkspaceInfo>(); while(r.Read()) result.Add(new(Guid.Parse(r.GetString(0)),r.GetString(1),r.GetInt32(2)!=0)); return result;
    }
    public Workspace LoadWorkspace(Guid id)
    {
        using var db = Open(); using var c = db.CreateCommand(); c.CommandText = "SELECT payload FROM workspaces WHERE id=$id"; c.Parameters.AddWithValue("$id",id.ToString("N")); return Decode(c.ExecuteScalar() as string ?? throw new InvalidDataException("Workspace not found."));
    }
    public void CommitTransition(Workspace leaving, Workspace entering, bool deleteLeaving = false,bool persistLeaving=true)
    {
        if (leaving.Id == entering.Id) throw new ArgumentException("Select a different workspace.");
        using var db = Open(); using var tx = db.BeginTransaction();
        if(persistLeaving)UpsertWorkspace(db,tx,leaving);
        if (deleteLeaving&&persistLeaving) { using var c = db.CreateCommand(); c.Transaction=tx; c.CommandText="UPDATE workspaces SET deleted=1 WHERE id=$id"; c.Parameters.AddWithValue("$id",leaving.Id.ToString("N")); c.ExecuteNonQuery(); }
        UpsertWorkspace(db,tx,entering);
        WriteSnapshot(db,tx,"current",JsonSerializer.Serialize(entering,json)); WriteSnapshot(db,tx,"preferences",PreferencePayload(entering)); tx.Commit();
    }
    public BackupPolicy LoadBackupPolicy()
    {
        using var db=Open(); using var c=db.CreateCommand(); c.CommandText="SELECT payload FROM app_options WHERE name='backups'";
        var policy=c.ExecuteScalar() is string text ? JsonSerializer.Deserialize<BackupPolicy>(text) ?? new() : new BackupPolicy(); policy.MaxBackups=Math.Clamp(policy.MaxBackups,1,1000); return policy;
    }
    public void SaveBackupPolicy(BackupPolicy policy)
    {
        if (policy.MaxBackups is < 1 or > 1000) throw new ArgumentException("Use 1–1000 backups.");
        using var db=Open(); using var c=db.CreateCommand(); c.CommandText="INSERT OR REPLACE INTO app_options(name,payload) VALUES('backups',$payload)"; c.Parameters.AddWithValue("$payload",JsonSerializer.Serialize(policy)); c.ExecuteNonQuery();
    }
    public static Workspace StoppedCopy(Workspace workspace)
    {
        var copy=Copy(workspace);foreach(var timer in copy.Timers){foreach(var segment in timer.Segments.Where(s=>s.End is null))segment.End=segment.MeasuredThrough;timer.ResumeAfterSleep=false;timer.Popout.IsOpen=false;Alerts.Dismiss(timer.AlertState);}copy.CleanShutdown=true;return copy;
    }
    public static IReadOnlyList<Workspace> ReadWorkspaces(string path)=>ReadWorkspaceChoices(path).Select(x=>x.Value).ToArray();
    public static IReadOnlyList<SnapshotWorkspace> ReadWorkspaceChoices(string path)
    {
        using var db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=path,Mode=SqliteOpenMode.ReadOnly,Pooling=false}.ToString());db.Open();using var c=db.CreateCommand();c.CommandText="SELECT COUNT(*) FROM sqlite_master WHERE name='workspaces'";
        if(Convert.ToInt32(c.ExecuteScalar())==0)return [new(ReadDatabase(path),false)];
        c.CommandText="SELECT payload,deleted FROM workspaces ORDER BY name COLLATE NOCASE";using var r=c.ExecuteReader();var items=new List<SnapshotWorkspace>();while(r.Read())items.Add(new(Decode(r.GetString(0)),r.GetInt32(1)!=0));return items.Count==0?[new(ReadDatabase(path),false)]:items;
    }
}
