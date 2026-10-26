using System.Windows;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;

namespace TimersApp.Desktop;
public partial class App : Application
{
    private Mutex? instance;
    private bool ownsMutex;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        string folder = Environment.GetEnvironmentVariable("TIMERSAPP_DATA_DIR") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TimersApp");
        string identity = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(folder).ToUpperInvariant())))[..24];
        instance = new Mutex(false, "Local\\TimersApp." + identity);
        try { ownsMutex = instance.WaitOne(0); } catch (AbandonedMutexException) { ownsMutex = true; }
        if (!ownsMutex) { MessageBox.Show("Tommy’s Time Tracking Tables is already open. Check your taskbar.", "T⁴"); Shutdown(); return; }
        try
        {
            try { var store = new WorkspaceStore(folder); _ = store.Load(); _ = store.Load("preferences"); }
            catch (Exception ex)
            {
                if (!RecoverStartup(folder, ex.Message)) { Shutdown(1); return; }
            }
            var window = new MainWindow(); MainWindow = window; window.Show(); ShutdownMode = ShutdownMode.OnMainWindowClose;
        }
        catch (Exception ex) { MessageBox.Show($"The app could not open. Saved data has been preserved.\n\n{ex.Message}", "Startup problem"); Shutdown(1); }
    }
    private bool RecoverStartup(string folder, string reason)
    {
        var d = new FormDialog(null, "Recover saved timers", 650);
        d.Label("The saved workspace could not be read. Restore a backup or start a new workspace. Existing database files will be preserved in a separate folder.");
        d.Label(reason);
        var backups = Directory.Exists(folder) ? Directory.GetFiles(folder, "backup-*.db").OrderDescending().ToArray() : [];
        var list = new ListBox { ItemsSource = backups, Height = 200, Foreground = Brushes.Black }; d.Fields.Children.Add(list);
        d.Actions(("Restore selected backup", () =>
        {
            if (list.SelectedItem is not string backup) return;
            try { WorkspaceStore.RecoverDatabase(folder, backup); d.DialogResult = true; } catch (Exception ex) { MessageBox.Show(d, ex.Message, "Recovery failed"); }
        }), ("Start new (preserve old files)", () => { try { WorkspaceStore.RecoverDatabase(folder, null); d.DialogResult = true; } catch (Exception ex) { MessageBox.Show(d, ex.Message); } }), ("Exit", () => d.Close()));
        return d.ShowDialog() == true;
    }
    protected override void OnExit(ExitEventArgs e) { if (ownsMutex) instance?.ReleaseMutex(); instance?.Dispose(); base.OnExit(e); }
}
