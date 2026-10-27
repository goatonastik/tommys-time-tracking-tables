using Microsoft.Data.Sqlite;
using System.IO;
using System.Text.Json;
using TimersApp.Core;
using TimersApp.Desktop;

internal static class FeedbackChecks
{
    public static void Run(Action<string,Action> test,string artifacts)
    {
        static void Equal<T>(T expected,T actual){if(!EqualityComparer<T>.Default.Equals(expected,actual))throw new Exception($"Expected {expected}, got {actual}");}
        WorkspaceStore Store()=>new(Path.Combine(artifacts,"feedback-"+Guid.NewGuid().ToString("N")));
        test("Workspace transition is atomic; deletion cannot be resurrected by stale saves",()=>{
            var store=Store();var a=new Workspace{Name="Alpha"};a.Timers.Add(new(){Name="Preserved"});var b=new Workspace{Name="Beta"};store.Save(a);store.CommitTransition(a,b);
            Equal(b.Id,store.Load()!.Id);Equal(2,store.SavedWorkspaces().Count);Equal("Preserved",store.LoadWorkspace(a.Id).Timers[0].Name);
            var conflict=new Workspace{Name="ALPHA"};bool failed=false;try{store.CommitTransition(b,conflict);}catch(SqliteException){failed=true;}Equal(true,failed);Equal(b.Id,store.Load()!.Id);Equal(2,store.SavedWorkspaces().Count);
            store.CommitTransition(b,a,true);Equal(a.Id,store.Load("preferences")!.Id);Equal(1,store.SavedWorkspaces().Count);Equal(b.Id,store.SavedWorkspaces(true)[0].Id);
            failed=false;try{store.Save(b);}catch(InvalidOperationException){failed=true;}Equal(true,failed);Equal(a.Id,store.Load()!.Id);
        });
        test("Legacy workspace migrates once with stable identity and a consistent preserved database",()=>{
            var folder=Path.Combine(artifacts,"legacy-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);string path=Path.Combine(folder,"workspace.db");
            using(var db=new SqliteConnection($"Data Source={path};Pooling=False")){db.Open();using var c=db.CreateCommand();c.CommandText="CREATE TABLE snapshots(name TEXT PRIMARY KEY,payload TEXT NOT NULL);INSERT INTO snapshots VALUES('current',$payload)";c.Parameters.AddWithValue("$payload","{\"SchemaVersion\":2,\"Timers\":[],\"NameHistory\":[\"Original\"],\"Settings\":{},\"Presets\":[]}");c.ExecuteNonQuery();}
            var store=new WorkspaceStore(folder);var w=store.Load()!;Equal("default",w.Name);Equal("Original",w.NameHistory[0]);Equal(w.Id,new WorkspaceStore(folder).Load()!.Id);Equal(1,Directory.GetFiles(folder,"before-workspaces-*.db").Length);Equal(1,store.SavedWorkspaces().Count);
        });
        test("Database backup and disaster recovery preserve the entire workspace catalog",()=>{
            var store=Store();var a=new Workspace{Name="One"};var b=new Workspace{Name="Two"};store.Save(a);store.CommitTransition(a,b);store.Backup();string snapshot=store.Backups()[0];Equal(2,WorkspaceStore.ReadWorkspaces(snapshot).Count);
            var dest=Path.Combine(artifacts,"recover-catalog-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dest);WorkspaceStore.RecoverDatabase(dest,snapshot);Equal(2,new WorkspaceStore(dest).SavedWorkspaces().Count);
        });
        test("Backup policy defaults to 32, can be disabled independently of saving, and limits deletion scope",()=>{
            var store=Store();Equal(32,store.LoadBackupPolicy().MaxBackups);store.Save(new Workspace());for(int i=0;i<34;i++)store.Backup();Equal(32,store.Backups().Count);
            store.SaveBackupPolicy(new(){DisableBackups=true,MaxBackups=3});var w=store.Load()!;w.Name="Still saved";store.Save(w);store.Backup();Equal(32,store.Backups().Count);Equal("Still saved",store.Load()!.Name);
            store.SaveBackupPolicy(new(){MaxBackups=3});store.Backup();Equal(3,store.Backups().Count);File.WriteAllText(Path.Combine(store.Folder,"keep.txt"),"keep");Equal(3,store.DeleteBackups());Equal(true,File.Exists(Path.Combine(store.Folder,"workspace.db")));Equal(true,File.Exists(Path.Combine(store.Folder,"keep.txt")));
        });
        test("Custom duration layout rolls up units and validates token order",()=>{
            var style=new TimerStyle{UseCustomFormat=true,CustomFormat="H:M:S.MS"};Equal("25:02:03.004",Appearance.Format(TimeSpan.FromHours(25)+TimeSpan.FromMinutes(2)+TimeSpan.FromSeconds(3)+TimeSpan.FromMilliseconds(4),style));
            style.CustomFormat="m:s.ms";Equal("61:02.003",Appearance.Format(TimeSpan.FromMinutes(61)+TimeSpan.FromSeconds(2)+TimeSpan.FromMilliseconds(3),style));
            foreach(string bad in new[]{"","H:H","S:H","H::M","MONTH","H/M","H:M:"})Equal(false,Appearance.ValidFormat(bad));
            style.UseCustomFormat=false;style.HideUnitLabels=true;style.Units=TimeUnits.Hours|TimeUnits.Minutes|TimeUnits.Seconds|TimeUnits.Milliseconds;Equal("00:00:00.001",Appearance.Format(TimeSpan.FromMilliseconds(1),style));
        });
        test("Disabled name suggestions retain old suggestions but collect no new names",()=>{var w=new Workspace();var e=new TimerEngine(w);e.Add("Old");w.Settings.SaveNameSuggestions=false;var timer=e.Add("New");e.Rename(timer,"Changed","");Equal(1,w.NameHistory.Count);Equal("Old",w.NameHistory[0]);});
        test("Stopped workspace copies preserve totals and isolate duplicate data",()=>{var w=new Workspace();var clock=new ManualClock();var e=new TimerEngine(w,clock);var t=e.Add("Running");e.Start(t);clock.Advance(73);e.Checkpoint();var copy=WorkspaceStore.StoppedCopy(w);Equal(false,copy.Timers[0].IsRunning);Equal(73d,new TimerEngine(copy).Total(copy.Timers[0]).TotalSeconds);Equal(true,t.IsRunning);copy.Timers.Clear();Equal(1,w.Timers.Count);});
        test("Deliberate switches keep event classification and launch preferences",()=>{
            var w=new Workspace();w.Settings.AutoSave=false;w.Settings.RestoreOnLaunch=false;w.Settings.ExitChoice=ExitChoice.Discard;var clock=new ManualClock();var engine=new TimerEngine(w,clock);var timer=engine.Add("Deliberate stop");engine.Start(timer);clock.Advance(5);engine.Checkpoint();var stopped=WorkspaceStore.StoppedCopy(w);Equal(false,stopped.Timers[0].Segments[0].Recovered);
            var next=new Workspace{Name="Next"};MainWindow.CarryLaunchPreferences(w,next);Equal(false,next.Settings.AutoSave);Equal(false,next.Settings.RestoreOnLaunch);Equal(ExitChoice.Discard,next.Settings.ExitChoice);var store=Store();store.Save(stopped);store.CommitTransition(stopped,next);Equal(false,store.Load("preferences")!.Settings.AutoSave);
        });
        test("Snapshot workspace picker includes recoverable deleted workspaces",()=>{var store=Store();var a=new Workspace{Name="Deleted"};var b=new Workspace{Name="Current"};store.Save(a);store.CommitTransition(a,b,true);store.Backup();var all=WorkspaceStore.ReadWorkspaces(store.Backups()[0]);Equal(2,all.Count);Equal(true,all.Any(x=>x.Name=="Deleted"));Equal(true,WorkspaceStore.ReadWorkspaceChoices(store.Backups()[0]).Any(x=>x.Deleted&&x.DisplayName=="Deleted (deleted)"));});
        test("Stopped workspace retains spent alert intervals and skips empty temporary catalog entries",()=>{
            var w=new Workspace();var clock=new ManualClock();var engine=new TimerEngine(w,clock);var timer=engine.Add("Alert");timer.Alert.Enabled=true;engine.Start(timer);clock.Advance(1200);engine.Checkpoint();Equal(true,Alerts.Evaluate(timer.Alert,timer.AlertState,engine.Total(timer),clock.GetUtcNow(),true));Alerts.Dismiss(timer.AlertState);
            var stopped=WorkspaceStore.StoppedCopy(w);var t=stopped.Timers[0];Equal(1L,t.AlertState.LastInterval);Equal(true,t.AlertState.HasTriggered);Equal(false,Alerts.Evaluate(t.Alert,t.AlertState,new TimerEngine(stopped).Total(t),clock.GetUtcNow(),true));
            var store=Store();store.CommitTransition(new Workspace{Name="Temporary"},new Workspace{Name="Saved"},persistLeaving:false);Equal(1,store.SavedWorkspaces().Count);
        });
        test("Migration preserves explicitly committed launch preferences newer than current snapshot",()=>{
            string folder=Path.Combine(artifacts,"legacy-preferences-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            using(var db=new SqliteConnection($"Data Source={Path.Combine(folder,"workspace.db")};Pooling=False")){db.Open();using var c=db.CreateCommand();c.CommandText="CREATE TABLE snapshots(name TEXT PRIMARY KEY,payload TEXT NOT NULL);INSERT INTO snapshots VALUES('current',$current),('preferences',$preferences)";c.Parameters.AddWithValue("$current",JsonSerializer.Serialize(new Workspace{SchemaVersion=2}));c.Parameters.AddWithValue("$preferences",JsonSerializer.Serialize(new Workspace{SchemaVersion=2,Settings=new(){AutoSave=false,RestoreOnLaunch=false,ExitChoice=ExitChoice.Discard}}));c.ExecuteNonQuery();}
            var store=new WorkspaceStore(folder);Equal(false,store.Load()!.Settings.AutoSave);Equal(false,store.Load("preferences")!.Settings.RestoreOnLaunch);Equal(ExitChoice.Discard,store.Load("preferences")!.Settings.ExitChoice);
        });
        test("Discard of pre-recovery baseline retains crash classification when switching",()=>{
            var w=new Workspace();var time=new ManualClock();var e=new TimerEngine(w,time);var t=e.Add("Interrupted");e.Start(t);time.Advance(12);e.Checkpoint();var baseline=WorkspaceStore.Copy(w);e.Recover(false,false);
            var discarded=WorkspaceStore.StoppedCopy(ExitWorkspace.Prepare(w,baseline,ExitChoice.Discard,false));Equal(false,discarded.Timers[0].IsRunning);Equal(true,discarded.Timers[0].Segments[0].Recovered);Equal(12d,new TimerEngine(discarded).Total(discarded.Timers[0]).TotalSeconds);
        });
    }
}
