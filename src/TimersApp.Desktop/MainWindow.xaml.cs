using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using TimersApp.Core;
using Duration = TimersApp.Core.Duration;

namespace TimersApp.Desktop;
public partial class MainWindow : Window
{
    private readonly WorkspaceStore store;
    private Workspace baseline;
    public TimerEngine Engine { get; private set; }
    private readonly Dictionary<Guid, Window> popouts = [];
    private readonly List<TimerCard> cardViews = [];
    private readonly DispatcherTimer refresh = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private long lastSave = System.Diagnostics.Stopwatch.GetTimestamp();
    private long lastBackup = System.Diagnostics.Stopwatch.GetTimestamp();
    private bool replacingWorkspace;
    private bool suspended;
    private bool ready;
    private bool exiting;
    private bool saveProblem;
    private string? backupWarning;
    private bool applyingSettings;
    private bool scratchWorkspace;
    private bool scratchHasEdits;
    private readonly AlertAudio audio;

    public MainWindow()
    {
        store = new WorkspaceStore(Environment.GetEnvironmentVariable("TIMERSAPP_DATA_DIR"));
        var saved = store.Load();
        baseline = WorkspaceStore.Copy(saved ?? new Workspace());
        var prefs = store.Load("preferences");
        var settings = prefs?.Settings ?? saved?.Settings ?? new AppSettings();
        baseline.Settings.AutoSave = settings.AutoSave; baseline.Settings.RestoreOnLaunch = settings.RestoreOnLaunch; baseline.Settings.ExitChoice = settings.ExitChoice;
        if (!settings.RestoreOnLaunch && saved is not null && saved.Timers.Count > 0)
            store.Save(saved, "previous-workspace");
        var workspace = settings.RestoreOnLaunch ? saved ?? new Workspace() : new Workspace();
        scratchWorkspace=!settings.RestoreOnLaunch;
        if(scratchWorkspace){workspace.Name=UniqueWorkspaceName("default");baseline=WorkspaceStore.Copy(workspace);}
        workspace.Settings.AutoSave = settings.AutoSave; workspace.Settings.RestoreOnLaunch = settings.RestoreOnLaunch; workspace.Settings.ExitChoice = settings.ExitChoice;
        baseline=WorkspaceStore.Copy(workspace);
        Engine = new TimerEngine(workspace);
        InitializeComponent();
        AppScaleInput.ValueChanged += (_, _) => ScaleChanged(); TimerScaleInput.ValueChanged += (_, _) => ScaleChanged();
        audio = new AlertAudio(store.Folder, message => { Status.Text = message; saveProblem = true; Notify(message, true); });
        Loaded += (_, _) =>
        {
            RecoverIfNeeded();
            ready = true; RefreshWorkspaceView();
            Engine.Workspace.CleanShutdown = false;
            if (Engine.Workspace.Settings.AutoSave) Save(false);
            foreach (var timer in Engine.Workspace.Timers.Where(t => !t.Archived && t.Popout.IsOpen).ToArray()) PopOut(timer);
            refresh.Tick += (_, _) => Tick(); refresh.Start();
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            SystemEvents.DisplaySettingsChanged += OnDisplaysChanged;
        };
        Closing += OnClosing;
    }
    internal void RefreshWorkspaceView() { ApplySettings(); Render(); }
    private void RecoverIfNeeded()
    {
        if (!Engine.Workspace.Timers.Any(t => t.IsRunning || t.ResumeAfterSleep)) return;
        var dialog = new FormDialog(this, "Restore running timers");
        dialog.Label(Engine.Workspace.CleanShutdown ? "Some timers were running when this workspace was saved." : "The last session ended with running timers. Choose how to restore them.");
        dialog.Label("Keep saved time excludes the time since the last save. Include interruption adds that interval to the recorded running time. Timers remain stopped unless you choose to resume.");
        var include = dialog.Check("Include time since the last save (sleep-paused timers stay excluded)", false);
        var resume = dialog.Check("Resume the previously running timers", false);
        dialog.Actions(("Restore", () => dialog.DialogResult = true));
        dialog.ShowDialog();
        Engine.Recover(include.IsChecked == true, resume.IsChecked == true);
    }
    private void Tick()
    {
        if (replacingWorkspace || suspended || exiting) return;
        foreach (var timer in Engine.Workspace.Timers.Where(t => !t.Archived))
        {
            if (Alerts.Evaluate(timer.Alert, timer.AlertState, Engine.Total(timer), DateTimeOffset.Now, timer.IsRunning)) audio.Play(timer);
        }
        foreach (var card in cardViews.ToArray()) card.Refresh();
        if (!saveProblem) Status.Text = $"{Engine.Workspace.Timers.Count(t => !t.Archived)} timers  ·  {Engine.Workspace.Timers.Count(t => t.IsRunning)} running  ·  " + (Engine.Workspace.Settings.AutoSave ? "Autosave on" : "Autosave off — use Save now") + (backupWarning is null ? "" : "  ·  " + backupWarning);
        if (Engine.Workspace.Settings.AutoSave && System.Diagnostics.Stopwatch.GetElapsedTime(lastSave) > TimeSpan.FromSeconds(5)) Save(false);
    }
    private void ApplySettings()
    {
        applyingSettings = true;
        try
        {
        var s = Engine.Workspace.Settings;
        AppScaleInput.Value = s.AppScale; TimerScaleInput.Value = s.TimerScale;
        AppTransform.ScaleX = AppTransform.ScaleY = s.AppScale;
        var layout = new StackPanel { Orientation = Orientation.Horizontal }; layout.Children.Add(new ToolbarIcon { Kind = s.ListLayout ? "Grid" : "List" }); layout.Children.Add(new TextBlock { Text = s.ListLayout ? "Switch to grid" : "Switch to list", Margin = new Thickness(6,0,0,0) }); LayoutButton.Content = layout;
        StatusBar.Visibility = s.ShowStatusBar ? Visibility.Visible : Visibility.Collapsed;
        Title = Engine.Workspace.Name + " — T⁴";
        RefreshWorkspacePicker();
        AppBackdrop.Update(s.AppBackground);
        }
        finally { applyingSettings = false; }
    }
    private void Render()
    {
        cardViews.RemoveAll(card => Cards.Children.Contains(card)); Cards.Children.Clear();
        var s = Engine.Workspace.Settings; UpdateGridColumns();
        Cards.MinimumRows = s.ListLayout ? 0 : s.Rows; Cards.EmptyRowHeight = s.CardHeight * s.TimerScale; Cards.Spacing = s.Spacing;
        foreach (var timer in Engine.Workspace.Timers.Where(t => !t.Archived))
        {
            var card = new TimerCard(this, timer) { Width = timer.Width ?? (s.ListLayout ? Math.Max(s.CardWidth, 580) : s.CardWidth), Height = timer.Height ?? (s.ListLayout ? s.ListRowHeight : s.CardHeight), LayoutTransform = new ScaleTransform(s.TimerScale, s.TimerScale) };
            Cards.Children.Add(card); cardViews.Add(card);
        }
        EmptyState.Visibility = Cards.Children.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
    private void Changed(bool render = true)
    {
        if(scratchWorkspace)scratchHasEdits=true;
        if (render) Render(); foreach (var view in cardViews) view.Refresh();
        if (!replacingWorkspace && Engine.Workspace.Settings.AutoSave) Save(false);
    }
    private bool Save(bool announce, bool clean = false)
    {
        if(scratchWorkspace&&!scratchHasEdits&&Engine.Workspace.Timers.Count==0&&!announce)return true;
        bool backupDue = announce || System.Diagnostics.Stopwatch.GetElapsedTime(lastBackup) > TimeSpan.FromHours(1);
        var result = PersistenceResult.Run(() =>
        {
            Engine.Checkpoint(); Engine.Workspace.CleanShutdown = clean; store.Save(Engine.Workspace);
        }, backupDue ? store.Backup : null);
        lastSave = System.Diagnostics.Stopwatch.GetTimestamp();
        if (!result.Saved)
        {
            saveProblem = true; Status.Text = "Save failed — your current timers are still in memory. " + result.SaveError;
            Notify(Status.Text,true);
            if (announce || clean) MessageBox.Show(this, Status.Text, "Could not save");
            return false;
        }
        saveProblem = false;
        scratchWorkspace=false;
        if (backupDue)
        {
            // A backup failure must not misreport a successful primary save, or retry every five seconds.
            lastBackup = System.Diagnostics.Stopwatch.GetTimestamp();
            backupWarning = result.BackupWarning is null ? null : "Timers saved; backup failed: " + result.BackupWarning;
            if (backupWarning is not null) Status.Text = backupWarning;
        }
        if (announce) { RefreshWorkspacePicker(); Notify(backupWarning ?? "Saved", backupWarning is not null); }
        else if (backupWarning is not null) Notify(backupWarning, true);
        return true;
    }
    private void Preferences() => store.Save(new Workspace { Id=Engine.Workspace.Id,Name=Engine.Workspace.Name,Settings = new AppSettings { AutoSave = Engine.Workspace.Settings.AutoSave, RestoreOnLaunch = Engine.Workspace.Settings.RestoreOnLaunch, ExitChoice = Engine.Workspace.Settings.ExitChoice } }, "preferences");
    public void Toggle(TimerRecord timer) { if (timer.IsRunning) Engine.Stop(timer); else Engine.Start(timer); Changed(false); }
    public void Reset(TimerRecord timer)
    {
        if (MessageBox.Show(this, "Start a fresh total? Previous segments will remain in history and exports.", "Reset total", MessageBoxButton.OKCancel) != MessageBoxResult.OK) return;
        Engine.Reset(timer); timer.AlertState = new(); audio.Stop(timer.Id); Changed();
    }
    public void Remove(TimerRecord timer)
    {
        if (MessageBox.Show(this, "Remove this timer from the workspace? Its recorded history will remain available in exports.", "Remove timer", MessageBoxButton.OKCancel) != MessageBoxResult.OK) return;
        Return(timer); audio.Stop(timer.Id); Engine.Archive(timer); Changed();
    }
    public void Edit(TimerRecord? timer = null)
    {
        var dialog = new FormDialog(this, timer is null ? "Add timer" : "Edit timer");
        dialog.Label("Name");
        var name = new ComboBox { IsEditable = true, IsTextSearchEnabled = true, ItemsSource = Engine.Workspace.NameHistory.ToArray(), Text = timer?.Name ?? "", Padding = new Thickness(8), Foreground = Brushes.Black };
        System.Windows.Automation.AutomationProperties.SetName(name, "Timer name");
        dialog.Fields.Children.Add(name);
        dialog.Label("Description (optional)"); var desc = dialog.Text(timer?.Description ?? "", true);
        dialog.Actions((timer is null ? "Add timer" : "Save changes", () => { if (timer is null) Engine.Add(name.Text, desc.Text); else Engine.Rename(timer, name.Text, desc.Text); dialog.DialogResult = true; }), ("Cancel", () => dialog.Close()));
        if (dialog.ShowDialog() == true) Changed();
    }
    public void History(TimerRecord timer)
    {
        Engine.Checkpoint();
        var dialog = new FormDialog(this, $"Segment history — {timer.Name}", 850);
        dialog.Label($"Current total: {Duration.FormatPrecise(Engine.Total(timer))}   •   {timer.Segments.Count} recorded segments (including earlier resets)");
        var grid = new DataGrid { IsReadOnly = true, AutoGenerateColumns = true, Height = 360, Foreground = Brushes.Black, Background = Brushes.White, CanUserAddRows = false,
            ItemsSource = timer.Segments.OrderByDescending(x => x.Start).Select(x => new { Started = x.Start.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"), Stopped = x.End?.ToString("yyyy-MM-dd HH:mm:ss.fff zzz") ?? "Running — snapshot", Duration = Core.Duration.FormatPrecise(Engine.Elapsed(x)), Run = x.RunId == timer.RunId ? "Current" : "Archived total", Recovery = x.Recovered ? "Recovered" : "" }).ToList() };
        dialog.Fields.Children.Add(grid); dialog.Label("This is a snapshot. Reopen history to refresh an active segment."); dialog.Actions(("Close", () => dialog.Close())); dialog.ShowDialog();
    }
    public bool IsPoppedOut(TimerRecord timer) => popouts.ContainsKey(timer.Id);
    public void PopOut(TimerRecord timer)
    {
        if (popouts.TryGetValue(timer.Id, out var existing)) { existing.WindowState = WindowState.Normal; existing.Activate(); return; }
        var position = timer.Popout;
        var window = new Window { Title = timer.Name + " — T⁴", Icon = Icon, Width = Math.Clamp(position.Width, 150, 1800), Height = Math.Clamp(position.Height, 100, 1400), MinWidth = 150, MinHeight = 100, Topmost = position.Topmost };
        if (position.Left is { } left && position.Top is { } top)
        {
            var virtualArea = new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
            var titleArea = new Rect(left, top, window.Width, 40);
            if (virtualArea.IntersectsWith(titleArea)) { window.Left = left; window.Top = top; }
            else { window.Left = SystemParameters.WorkArea.Left + 40; window.Top = SystemParameters.WorkArea.Top + 40; }
        }
        var card = new TimerCard(this, timer, true); cardViews.Add(card);
        window.Content = card;
        window.ContextMenu = card.ContextMenu;
        popouts[timer.Id] = window;
        void Remember() { if (window.WindowState != WindowState.Normal) return; position.Left = window.Left; position.Top = window.Top; position.Width = window.Width; position.Height = window.Height; }
        window.LocationChanged += (_, _) => Remember(); window.SizeChanged += (_, _) => Remember();
        window.Loaded += (_, _) => WindowPlacementService.EnsureVisible(window);
        window.Closed += (_, _) => { popouts.Remove(timer.Id); cardViews.Remove(card); if (!exiting && !replacingWorkspace) { position.IsOpen = false; Changed(); } };
        position.IsOpen = true; window.Show(); Remember(); Changed();
    }
    public void Return(TimerRecord timer) { if (popouts.TryGetValue(timer.Id, out var window)) window.Close(); }
    private void Add_Click(object sender, RoutedEventArgs e) => Edit();
    private void Save_Click(object sender, RoutedEventArgs e) => Save(true);
    private void Load_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var owner=sender is DependencyObject element?Window.GetWindow(element)??this:this;
            var dialog = new FormDialog(owner, "Load saved workspace");
            dialog.Label("Choose a saved copy, then import it as a new workspace or explicitly replace the current one. Replacing saves a recovery copy first; that slot keeps only the most recently displaced workspace.");
            var choices = new List<LoadChoice> { new("current", "Latest saved workspace") };
            foreach(var slot in new[]{("previous-workspace","Previous workspace kept when launch restoration was off"),("before-restore","Workspace preserved before the last restore")})
            {
                try{if(store.Load(slot.Item1) is { } snapshot)choices.Add(new(slot.Item1,$"{slot.Item2} · {snapshot.SavedAt.LocalDateTime:yyyy-MM-dd HH:mm}"));}
                catch(Exception ex){dialog.Label($"Recovery copy '{slot.Item1}' could not be read: {ex.Message}. Other saved copies remain available.");}
            }
            var selected = dialog.Select(choices, choices[0]);
            dialog.Actions(("Load workspace", () => dialog.DialogResult = true), ("Cancel", () => dialog.Close()));
            if (dialog.ShowDialog() != true) return;
            var saved = store.Load(((LoadChoice)selected.SelectedItem).Slot); if (saved is null) return;
            RestoreOrImport(saved,owner);
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Load failed"); }
    }
    private void ScaleChanged()
    {
        if (!ready || applyingSettings) return;
        var s = Engine.Workspace.Settings; s.AppScale = AppScaleInput.Value; s.TimerScale = TimerScaleInput.Value;
        AppTransform.ScaleX = AppTransform.ScaleY = s.AppScale;
        foreach (TimerCard card in Cards.Children) card.LayoutTransform = new ScaleTransform(s.TimerScale, s.TimerScale);
        UpdateGridColumns(); Relayout();
        Changed(false);
    }
    private void Layout_Click(object sender, RoutedEventArgs e) { Engine.Workspace.Settings.ListLayout = !Engine.Workspace.Settings.ListLayout; ApplySettings(); Changed(); }
    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var d = new FormDialog(this, "Export recorded time");
        d.Label("Start date (blank = all history, YYYY-MM-DD)"); var from = d.Text("");
        d.Label("End date (inclusive, blank = all history)"); var through = d.Text("");
        var allTimers = d.Check("Export all timers, including archived", true);
        d.Label("Or select timers (Ctrl/Shift to select several)");
        var choice = new ListBox { Height = 130, SelectionMode = SelectionMode.Extended, IsEnabled = false, Foreground = Brushes.Black,
            ItemsSource = Engine.Workspace.Timers.Select(t => new TimerChoice(t.Id, $"{t.Name}{(t.Archived ? " (archived)" : "")} · {t.Id.ToString()[..8]}")).ToArray() };
        d.Fields.Children.Add(choice); allTimers.Checked += (_, _) => choice.IsEnabled = false; allTimers.Unchecked += (_, _) => choice.IsEnabled = true;
        d.Label("Rounding basis"); var basis = d.Select(Enum.GetValues<RoundingBasis>(), RoundingBasis.Daily);
        d.Label("Nearest interval (minutes)"); var round = d.Select(new[] { 1, 15, 60 }, Engine.Workspace.Settings.RoundMinutes);
        var running = d.Check("Include snapshots of currently running timers", true);
        d.Label("Summary totals to show"); var totals = d.Select(Enum.GetValues<TotalDisplay>(), TotalDisplay.Both);
        d.Label("Excel retains exact segment detail. Unselected summary columns are hidden. With monthly rounding, daily totals remain exact. CSV contains exact segment detail only.");
        d.Actions(("Export…", () =>
        {
            try
            {
                DateOnly? Parse(string value) => string.IsNullOrWhiteSpace(value) ? null : DateOnly.ParseExact(value.Trim(), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                var ids = allTimers.IsChecked == true ? null : choice.SelectedItems.Cast<TimerChoice>().Select(t => t.Id!.Value).ToArray();
                if (ids is { Length: 0 }) throw new ArgumentException("Select at least one timer or choose Export all timers.");
                var options = new ReportOptions(Parse(from.Text), Parse(through.Text), (int)round.SelectedItem, (RoundingBasis)basis.SelectedItem, running.IsChecked == true, TimerIds: ids, Totals: (TotalDisplay)totals.SelectedItem);
                if (options.From > options.Through) throw new ArgumentException("Start date must be on or before end date.");
                var save = new SaveFileDialog { Filter = "Excel workbook (*.xlsx)|*.xlsx|CSV detail (*.csv)|*.csv", FileName = $"Tommy-Time-{DateTime.Now:yyyy-MM-dd}.xlsx" };
                if (save.ShowDialog(d) != true) return;
                Engine.Checkpoint(); ExcelExport.Write(save.FileName, Engine.Workspace, options);
                d.DialogResult = true; MessageBox.Show(this, "Your time report has been exported.", "Export complete");
            }
            catch (Exception ex) { MessageBox.Show(d, ex.Message, "Could not export"); }
        }), ("Cancel", () => d.Close())); d.ShowDialog();
    }
    private sealed record TimerChoice(Guid? Id, string Name) { public override string ToString() => Name; }
    private sealed record LoadChoice(string Slot, string Name) { public override string ToString() => Name; }
    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (exiting) return;
        var choice = Engine.Workspace.Settings.ExitChoice;
        bool remember = false;
        if (choice == ExitChoice.Ask)
        {
            var dialog = new FormDialog(this, "Save before exiting?", 560);
            dialog.Label("Save timers and workspace before exiting?");
            dialog.Label("Discard restores the workspace from the beginning of this app session, including its settings and timer history. Time while the app is closed is not added automatically.");
            var check = dialog.Check("Remember this decision", false);
            dialog.Actions(("Save and exit", () => { choice = ExitChoice.Save; dialog.DialogResult = true; }), ("Discard changes and exit", () => { choice = ExitChoice.Discard; dialog.DialogResult = true; }), ("Cancel", () => dialog.Close()));
            if (dialog.ShowDialog() != true) { e.Cancel = true; return; }
            remember = check.IsChecked == true;
        }
        try
        {
            Engine.Checkpoint();
            var committed = ExitWorkspace.Prepare(Engine.Workspace, baseline, choice, remember);
            if(scratchWorkspace&&(choice==ExitChoice.Discard||(!scratchHasEdits&&Engine.Workspace.Timers.Count==0)))store.Save(committed,"preferences");else store.Save(committed);
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Could not complete save/discard"); e.Cancel = true; return; }
        try { store.Backup(); } catch { /* The committed workspace is durable even if a secondary backup fails. */ }
        exiting = true; refresh.Stop(); SystemEvents.PowerModeChanged -= OnPowerModeChanged; SystemEvents.DisplaySettingsChanged -= OnDisplaysChanged; audio.Dispose(); foreach (var window in popouts.Values.ToArray()) window.Close();
    }
}
