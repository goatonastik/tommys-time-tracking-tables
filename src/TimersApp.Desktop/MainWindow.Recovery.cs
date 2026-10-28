using System.Windows;
using Microsoft.Win32;
using TimersApp.Core;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;

namespace TimersApp.Desktop;
public partial class MainWindow
{
    private void OnDisplaysChanged(object? sender, EventArgs e)
    {
        if (exiting || Dispatcher.HasShutdownStarted) return;
        Dispatcher.BeginInvoke(() => { WindowPlacementService.EnsureVisible(this); foreach (var window in popouts.Values.ToArray()) WindowPlacementService.EnsureVisible(window); });
    }
    private void Backups_Click(object sender, RoutedEventArgs e)
    {
        var d = new FormDialog(this, "Back up and restore", 650);
        var policy=store.LoadBackupPolicy();var disabled=d.Check("Disable backups",policy.DisableBackups);
        d.Label("Maximum automatic backups (1–1000)");var maximum=d.Text(policy.MaxBackups.ToString());
        var policyButton=new Button{Content="Apply backup settings"};d.Fields.Children.Add(policyButton);
        policyButton.Click+=(_,_)=>{if(!int.TryParse(maximum.Text,out int count)||count<1||count>1000){MessageBox.Show(d,"Use 1–1000 backups.");return;}try{store.SaveBackupPolicy(new(){DisableBackups=disabled.IsChecked==true,MaxBackups=count});Notify("Backup settings saved");}catch(Exception ex){MessageBox.Show(d,ex.Message);}};
        d.Label("These settings apply to all workspaces. Disabling automatic snapshots does not disable timer autosave, recovery copies or explicit portable exports.");
        d.Label("Portable backups contain timer history, settings, presets, images and sounds. Restoring replaces the current workspace; a recovery copy is saved first. Load saved can retrieve the most recently displaced workspace; the next restore replaces that recovery slot.");
        var list = new ListBox { Height = 210, Foreground = Brushes.Black, ItemsSource = store.Backups().Select(p => new BackupChoice(p, File.GetLastWriteTime(p).ToString("yyyy-MM-dd HH:mm:ss"))).ToArray() }; d.Fields.Children.Add(list);
        var delete=new Button{Content="Delete all backups",Margin=new Thickness(0,8,0,8)};d.Fields.Children.Add(delete);delete.Click+=(_,_)=>{if(MessageBox.Show(d,$"Permanently delete {store.Backups().Count} automatic backup files? Saved workspaces, recovery copies and portable exports are kept.","Delete all backups",MessageBoxButton.OKCancel)!=MessageBoxResult.OK)return;try{store.DeleteBackups();}catch(Exception ex){MessageBox.Show(d,ex.Message,"Some backups could not be deleted");}finally{list.ItemsSource=store.Backups().Select(p=>new BackupChoice(p,File.GetLastWriteTime(p).ToString("yyyy-MM-dd HH:mm:ss"))).ToArray();}};
        d.Label("Deleted workspaces (recover as a new workspace)");var deleted=d.Select(store.SavedWorkspaces(true),store.SavedWorkspaces(true).FirstOrDefault());deleted.DisplayMemberPath="Name";
        var recover=new Button{Content="Recover selected deleted workspace"};d.Fields.Children.Add(recover);recover.Click+=(_,_)=>{if(deleted.SelectedItem is not WorkspaceInfo info)return;try{var restored=store.LoadWorkspace(info.Id);restored.Id=Guid.NewGuid();restored.Name=UniqueWorkspaceName(restored.Name+" recovered");if(Transition(restored))d.Close();}catch(Exception ex){MessageBox.Show(d,ex.Message);}};
        var slots=new Button{Content="Load saved recovery copy",Margin=new Thickness(0,8,0,0)};slots.Click+=(_,_)=>Load_Click(slots,new RoutedEventArgs());d.Fields.Children.Add(slots);
        d.Actions(("Export portable backup…", () =>
        {
            var file = new SaveFileDialog { Filter = "T⁴ portable backup|*.t4backup", FileName = $"Tommy-Time-{DateTime.Now:yyyy-MM-dd}.t4backup" }; if (file.ShowDialog(d) != true) return;
            try { Engine.Checkpoint(); WorkspaceArchive.Export(file.FileName, Engine.Workspace); MessageBox.Show(d, "Portable backup exported."); } catch (Exception ex) { MessageBox.Show(d, ex.Message, "Backup failed"); }
        }), ("Import portable backup…", () =>
        {
            var file = new OpenFileDialog { Filter = "T⁴ portable backup|*.t4backup" }; if (file.ShowDialog(d) != true) return;
            try { var restored = WorkspaceArchive.Import(file.FileName, store.Folder); RestoreOrImport(restored,d); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Restore failed"); }
        }), ("Restore selected snapshot", () =>
        {
            if (list.SelectedItem is not BackupChoice selected) return;
            try { var choices=WorkspaceStore.ReadWorkspaceChoices(selected.Path);var pick=new FormDialog(d,"Workspace from backup");pick.Label("Select a workspace");var selectedWorkspace=pick.Select(choices,choices.FirstOrDefault(x=>x.Value.Id==Engine.Workspace.Id)??choices[0]);selectedWorkspace.DisplayMemberPath="DisplayName";pick.Actions(("Continue",()=>pick.DialogResult=true),("Cancel",()=>pick.Close()));if(pick.ShowDialog()==true)RestoreOrImport(((SnapshotWorkspace)selectedWorkspace.SelectedItem).Value,d); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Restore failed"); }
        }), ("Close", () => d.Close())); d.ShowDialog();
    }
    private string UniqueWorkspaceName(string name)
    {
        var names=store.SavedWorkspaces().Select(x=>x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);string root=name.Length>85?name[..85]:name;string result=root;int n=2;while(names.Contains(result))result=root+" "+n++;return result;
    }
    private void RestoreOrImport(Workspace restored,Window dialog)
    {
        var answer=MessageBox.Show(dialog,"Import as a new workspace? Choose No to replace the current workspace (a recovery copy is saved first).","Restore workspace",MessageBoxButton.YesNoCancel);
        if(answer==MessageBoxResult.Cancel)return;
        if(answer==MessageBoxResult.Yes){restored.Id=Guid.NewGuid();restored.Name=UniqueWorkspaceName(restored.Name+" imported");if(Transition(restored)&&dialog!=this)dialog.Close();}
        else{PreserveBeforeRestore();restored.Id=Engine.Workspace.Id;restored.Name=Engine.Workspace.Name;CarryLaunchPreferences(Engine.Workspace,restored);store.Save(restored);if(dialog!=this)dialog.Close();ReplaceWorkspace(restored);}
    }
    private void PreserveBeforeRestore()
    {
        var result = PersistenceResult.Run(() => { Engine.Checkpoint(); store.Save(WorkspaceStore.StoppedCopy(Engine.Workspace), "before-restore"); }, store.Backup);
        if (!result.Saved) throw new IOException("Could not preserve the current workspace: " + result.SaveError);
        lastBackup = System.Diagnostics.Stopwatch.GetTimestamp();
        backupWarning = result.BackupWarning is null ? null : "Recovery copy saved; secondary backup failed: " + result.BackupWarning;
        if (backupWarning is not null) Status.Text = backupWarning;
    }
    private sealed record BackupChoice(string Path, string Label) { public override string ToString() => Label; }
    private void ReplaceWorkspace(Workspace workspace)
    {
        refresh.Stop(); replacingWorkspace = true;CloseSizePanel();
        scaleSaveTimer.Stop();
        try
        {
            audio.StopTimers(Engine.Workspace.Timers.Select(t=>t.Id));
            foreach (var window in popouts.Values.ToArray()) window.Close();
            Engine = new TimerEngine(workspace);
            Engine.Workspace.CleanShutdown=false;
            RecoverIfNeeded(); ApplySettings(); Render();
            foreach (var timer in workspace.Timers.Where(t => !t.Archived && t.Popout.IsOpen).ToArray()) PopOut(timer, true);
        }
        finally { replacingWorkspace = false; refresh.Start(); }
        Changed(false);
    }
    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (exiting || Dispatcher.HasShutdownStarted) return;
        Dispatcher.Invoke(() =>
        {
            if (e.Mode == PowerModes.Suspend)
            {
                suspended = true; audio.StopAll();
                if (Engine.Workspace.Settings.PauseDuringSleep) Engine.SuspendForSleep();
                else Engine.Checkpoint();
                if (Engine.Workspace.Settings.AutoSave) Save(false);
            }
            else if (e.Mode == PowerModes.Resume && suspended)
            {
                suspended = false;
                Engine.ResumeFromSleep();
                foreach (var timer in Engine.Workspace.Timers.Where(t => !t.Archived && t.AlertState.Active && t.Alert.Enabled)) audio.Play(timer);
                Changed(false);
            }
        });
    }
}
