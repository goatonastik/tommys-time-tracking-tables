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
        d.Label("Portable backups contain timer history, settings, presets, images and sounds. Restoring replaces the current workspace; a recovery copy is saved first.");
        var list = new ListBox { Height = 210, Foreground = Brushes.Black, ItemsSource = store.Backups().Select(p => new BackupChoice(p, File.GetLastWriteTime(p).ToString("yyyy-MM-dd HH:mm:ss"))).ToArray() }; d.Fields.Children.Add(list);
        d.Actions(("Export portable backup…", () =>
        {
            var file = new SaveFileDialog { Filter = "T⁴ portable backup|*.t4backup", FileName = $"Tommy-Time-{DateTime.Now:yyyy-MM-dd}.t4backup" }; if (file.ShowDialog(d) != true) return;
            try { Engine.Checkpoint(); WorkspaceArchive.Export(file.FileName, Engine.Workspace); MessageBox.Show(d, "Portable backup exported."); } catch (Exception ex) { MessageBox.Show(d, ex.Message, "Backup failed"); }
        }), ("Import portable backup…", () =>
        {
            var file = new OpenFileDialog { Filter = "T⁴ portable backup|*.t4backup" }; if (file.ShowDialog(d) != true) return;
            try { var restored = WorkspaceArchive.Import(file.FileName, store.Folder); PreserveBeforeRestore(); d.Close(); ReplaceWorkspace(restored); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Restore failed"); }
        }), ("Restore selected snapshot", () =>
        {
            if (list.SelectedItem is not BackupChoice selected) return;
            try { var restored = WorkspaceStore.ReadDatabase(selected.Path); PreserveBeforeRestore(); d.Close(); ReplaceWorkspace(restored); } catch (Exception ex) { MessageBox.Show(this, ex.Message, "Restore failed"); }
        }), ("Close", () => d.Close())); d.ShowDialog();
    }
    private void PreserveBeforeRestore()
    {
        Engine.Checkpoint(); store.Save(Engine.Workspace, "before-restore"); store.Backup();
    }
    private sealed record BackupChoice(string Path, string Label) { public override string ToString() => Label; }
    private void ReplaceWorkspace(Workspace workspace)
    {
        refresh.Stop(); replacingWorkspace = true;
        try
        {
            audio.Dispose();
            foreach (var window in popouts.Values.ToArray()) window.Close();
            Engine = new TimerEngine(workspace);
            RecoverIfNeeded(); ApplySettings(); Render();
            foreach (var timer in workspace.Timers.Where(t => !t.Archived && t.Popout.IsOpen).ToArray()) PopOut(timer);
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
                suspended = true; audio.Dispose();
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
