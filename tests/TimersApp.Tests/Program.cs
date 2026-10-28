using ClosedXML.Excel;
using TimersApp.Core;
using TimersApp.Desktop;
using System.IO;

var failures = new List<string>();
int passed = 0;
void Test(string name, Action action)
{
    try { action(); passed++; Console.WriteLine("PASS " + name); }
    catch (Exception ex) { failures.Add(name + ": " + ex.Message); Console.WriteLine("FAIL " + name + ": " + ex); }
}
void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}, got {actual}"); }
var clock = new ManualClock(); var workspace = new Workspace(); var engine = new TimerEngine(workspace, clock);
var timer = engine.Add("Design", "First task"); var other = engine.Add("Review");
Test("Independent timers, stopped periods excluded", () =>
{
    engine.Start(timer); clock.Advance(60); engine.Start(other); clock.Advance(30); engine.Stop(timer); clock.Advance(300);
    Equal(90d, engine.Total(timer).TotalSeconds); Equal(330d, engine.Total(other).TotalSeconds);
    engine.Start(timer); clock.Advance(10); engine.Stop(timer); engine.Stop(other);
    Equal(100d, engine.Total(timer).TotalSeconds); Equal(2, timer.Segments.Count);
});
Test("Double start cannot create duplicate running segments", () => { engine.Start(timer); engine.Start(timer); Equal(1, timer.Segments.Count(s => s.End is null)); engine.Stop(timer); });
Test("Clock adjustment cannot inflate elapsed time", () =>
{
    engine.Reset(timer); engine.Start(timer); clock.Advance(20); clock.ShiftWallClock(3600); clock.Advance(10); engine.Stop(timer); Equal(30d, engine.Total(timer).TotalSeconds);
});
Test("Reset preserves old segments but clears displayed total", () => { int count = timer.Segments.Count; engine.Reset(timer); Equal(TimeSpan.Zero, engine.Total(timer)); Equal(count, timer.Segments.Count); });
Test("Renaming keeps historical name snapshot", () => { engine.Start(timer); engine.Rename(timer, "New name", "New description"); engine.Stop(timer); Equal("Design", timer.Segments.Last().Name); });
Test("Rounding boundaries and halfway upward", () =>
{
    Equal(TimeSpan.Zero, Duration.Round(TimeSpan.FromSeconds(449), 15));
    Equal(TimeSpan.FromMinutes(15), Duration.Round(TimeSpan.FromSeconds(450), 15));
    Equal(TimeSpan.FromMinutes(1), Duration.Round(TimeSpan.FromSeconds(30), 1));
    Equal(TimeSpan.FromHours(1), Duration.Round(TimeSpan.FromMinutes(30), 60));
});
Test("Recovery excludes downtime and creates separate resume segment", () =>
{
    engine.Start(timer); clock.Advance(42); engine.Checkpoint();
    var copy = WorkspaceStore.Copy(workspace); clock.Advance(600); var recovered = new TimerEngine(copy, clock); recovered.Recover(false, true);
    var restored = copy.Timers.Single(t => t.Id == timer.Id); Equal(42d, recovered.Total(restored).TotalSeconds); Equal(true, restored.IsRunning);
    clock.Advance(5); recovered.Stop(restored); Equal(47d, recovered.Total(restored).TotalSeconds);
});
Test("Recovery can explicitly include interruption", () =>
{
    var copy = WorkspaceStore.Copy(workspace); var recovered = new TimerEngine(copy, clock); recovered.Recover(true, false);
    Equal(647d, recovered.Total(copy.Timers.Single(t => t.Id == timer.Id)).TotalSeconds);
});
var reportWorkspace = new Workspace(); var reportTimer = new TimerRecord { Name = "Calendar test" }; reportWorkspace.Timers.Add(reportTimer);
reportTimer.Segments.Add(new Segment { RunId = reportTimer.RunId, Name = reportTimer.Name, Start = new DateTimeOffset(2024, 2, 29, 23, 50, 0, TimeSpan.Zero), End = new DateTimeOffset(2024, 3, 1, 0, 10, 0, TimeSpan.Zero), MeasuredThrough = new DateTimeOffset(2024, 3, 1, 0, 10, 0, TimeSpan.Zero), ElapsedTicks = TimeSpan.FromMinutes(20).Ticks });
Test("Leap day and month boundary split without losing duration", () =>
{
    var rows = Reports.Build(reportWorkspace, new(null, null), TimeZoneInfo.Utc); Equal(2, rows.Count); Equal(new DateOnly(2024, 2, 29), rows[0].Day); Equal(TimeSpan.FromMinutes(20).Ticks, rows.Sum(x => x.Ticks));
});
Test("Inclusive date range clips overlapping session", () =>
{
    var rows = Reports.Build(reportWorkspace, new(new DateOnly(2024, 3, 1), new DateOnly(2024, 3, 1)), TimeZoneInfo.Utc); Equal(1, rows.Count); Equal(TimeSpan.FromMinutes(10).Ticks, rows[0].Ticks); Equal(reportTimer.Segments[0].Start, rows[0].ActualStart);
});
Test("DST calendar day can contain 23 elapsed hours", () =>
{
    var w = new Workspace(); var t = new TimerRecord(); w.Timers.Add(t);
    t.Segments.Add(new Segment { Start = new DateTimeOffset(2024, 3, 10, 0, 0, 0, TimeSpan.FromHours(-5)), End = new DateTimeOffset(2024, 3, 11, 0, 0, 0, TimeSpan.FromHours(-4)), ElapsedTicks = TimeSpan.FromHours(23).Ticks });
    var rows = Reports.Build(w, new(null, null), TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")); Equal(1, rows.Count); Equal(TimeSpan.FromHours(23).Ticks, rows[0].Ticks);
});
Test("Active snapshots retain blank stop time", () =>
{
    var w = new Workspace(); var t = new TimerRecord(); w.Timers.Add(t); t.Segments.Add(new Segment { Start = clock.GetUtcNow().AddMinutes(-1), MeasuredThrough = clock.GetUtcNow(), ElapsedTicks = TimeSpan.FromMinutes(1).Ticks });
    var rows = Reports.Build(w, new(null, null), TimeZoneInfo.Utc); Equal<DateTimeOffset?>(null, rows[0].ActualStop); Equal(0, Reports.Build(w, new(null, null, IncludeRunning: false), TimeZoneInfo.Utc).Count);
});
string artifacts = Path.GetFullPath("artifacts/tests"); Directory.CreateDirectory(artifacts);
Test("SQLite roundtrip, baseline isolation, and backup", () =>
{
    var store = new WorkspaceStore(Path.Combine(artifacts, "store-" + Guid.NewGuid().ToString("N"))); store.Save(workspace); var loaded = store.Load()!;
    Equal(workspace.Timers.Count, loaded.Timers.Count); var original = WorkspaceStore.Copy(loaded); loaded.Timers.Clear(); store.Save(loaded); Equal(0, store.Load()!.Timers.Count); store.Save(original); Equal(workspace.Timers.Count, store.Load()!.Timers.Count); store.Backup(); Equal(1, Directory.GetFiles(store.Folder, "backup-*.db").Length);
});
Test("Workbook contains one table and numeric exact durations", () =>
{
    var path=Path.Combine(artifacts,"calendar.xlsx");ExcelExport.Write(path,reportWorkspace,new(null,null));using var book=new XLWorkbook(path);var sheet=book.Worksheet("Recorded time");
    Equal(1,book.Worksheets.Count);Equal(TimeSpan.FromMinutes(20).Ticks,sheet.RowsUsed().Skip(1).Sum(row=>row.Cell(1).GetTimeSpan().Ticks));Equal("[h]:mm:ss",sheet.Cell(2,1).Style.NumberFormat.Format);Equal(45d,sheet.Column(4).Width);
});
Test("Elapsed units roll up hidden units and never introduce months", () =>
{
    var duration = TimeSpan.FromDays(366) + TimeSpan.FromHours(2) + TimeSpan.FromMinutes(3) + TimeSpan.FromSeconds(4.005);
    Equal("1y 1d 02h 03min 04s 005ms", Appearance.Format(duration, TimeUnits.Years | TimeUnits.Days | TimeUnits.Hours | TimeUnits.Minutes | TimeUnits.Seconds | TimeUnits.Milliseconds));
    Equal("61min 005ms", Appearance.Format(TimeSpan.FromMinutes(61) + TimeSpan.FromMilliseconds(5), TimeUnits.Minutes | TimeUnits.Milliseconds));
    Equal("25:00:00", Appearance.Format(TimeSpan.FromHours(25), TimeUnits.Hours | TimeUnits.Minutes | TimeUnits.Seconds));
});
Test("Style inheritance and pop-out overrides are independent", () =>
{
    var w = new Workspace(); var t = new TimerRecord(); var preset = w.Presets[1]; t.PresetId = preset.Id;
    Equal(preset.Style, Appearance.Resolve(w, t, false)); Equal(preset.Style, Appearance.Resolve(w, t, true));
    w.Settings.SeparatePopoutStyle = true; Equal(w.Settings.PopoutStyle, Appearance.Resolve(w, t, true));
    t.SeparatePopoutStyle = false; Equal(preset.Style, Appearance.Resolve(w, t, true));
    t.Style = new TimerStyle { TextColor = "Orange" }; Equal("Orange", Appearance.Resolve(w, t, false).TextColor);
    t.SeparatePopoutStyle = true; t.PopoutStyle = new TimerStyle { TextColor = "Pink" }; Equal("Pink", Appearance.Resolve(w, t, true).TextColor); Equal("Orange", Appearance.Resolve(w, t, false).TextColor);
});
Test("Styling, alert settings and window placement survive database save", () =>
{
    var w = new Workspace(); var t = new TimerRecord { Style = new TimerStyle { Digits = DigitStyle.SevenSegment }, SeparatePopoutStyle = true, PopoutStyle = TimerStyle.Minimal(), Width = 420, Height = 130, Alert = new AlertSettings { Enabled = true, Flash = true, SnoozeMinutes = 12 } }; t.Popout.Left = 123; t.Popout.Top = 87; w.Timers.Add(t);
    var store = new WorkspaceStore(Path.Combine(artifacts, "appearance-" + Guid.NewGuid().ToString("N"))); store.Save(w); var saved = store.Load()!.Timers[0];
    Equal(DigitStyle.SevenSegment, saved.Style!.Digits); Equal(false, saved.PopoutStyle!.ShowControls); Equal(123d, saved.Popout.Left); Equal(420d, saved.Width); Equal(12, saved.Alert.SnoozeMinutes);
    var copy = Appearance.Copy(saved.Style); copy.Background.Color = "Red"; Equal("#182338", saved.Style.Background.Color);
});
Test("Interval alerts fire once and consolidate missed intervals", () =>
{
    var a = new AlertSettings { Enabled = true, IntervalSeconds = 60 }; var s = new AlertState(); var now = DateTimeOffset.UtcNow;
    Equal(false, Alerts.Evaluate(a, s, TimeSpan.FromSeconds(59), now, true)); Equal(true, Alerts.Evaluate(a, s, TimeSpan.FromSeconds(60), now, true));
    Equal(false, Alerts.Evaluate(a, s, TimeSpan.FromSeconds(60), now, true)); Alerts.Dismiss(s);
    Equal(true, Alerts.Evaluate(a, s, TimeSpan.FromSeconds(600), now, true)); Equal(10L, s.LastInterval); Equal(false, Alerts.Evaluate(a, s, TimeSpan.FromSeconds(600), now, true));
    Alerts.Dismiss(s); Equal(false, Alerts.Evaluate(a, s, TimeSpan.FromSeconds(700), now, false));
});
Test("Snooze restores alert only at deadline and disabling silences it", () =>
{
    var a = new AlertSettings { Enabled = true, IntervalSeconds = 10 }; var s = new AlertState { Active = true, LastInterval = 1 }; var now = DateTimeOffset.UtcNow;
    Alerts.Snooze(s, now, 5); Equal(false, s.Active); Equal(false, Alerts.Evaluate(a, s, TimeSpan.FromMinutes(3), now.AddMinutes(4), true));
    Equal(true, Alerts.Evaluate(a, s, TimeSpan.FromMinutes(4), now.AddMinutes(5), true)); a.Enabled = false;
    Equal(false, Alerts.Evaluate(a, s, TimeSpan.FromMinutes(6), now.AddMinutes(6), true)); Equal(false, s.Active);
});
Test("One-shot alerts do not rearm after dismiss", () =>
{
    var a = new AlertSettings { Enabled = true, Repeat = false, IntervalSeconds = 10 }; var s = new AlertState { LastInterval = 3 }; var now = DateTimeOffset.UtcNow;
    Equal(true, Alerts.Evaluate(a, s, TimeSpan.FromSeconds(40), now, true)); Alerts.Dismiss(s); Equal(false, Alerts.Evaluate(a, s, TimeSpan.FromSeconds(90), now, true));
});
ReliabilityChecks.Run(Test, artifacts);
FeedbackChecks.Run(Test, artifacts);
Feedback2Checks.Run(Test, artifacts);
Feedback3Checks.Run(Test, artifacts);
Feedback4Checks.Run(Test, artifacts);
Test("Render workspace with three independent timer cards", () =>
{
    Exception? error = null;
    var thread = new Thread(() =>
    {
        try
        {
            string folder = Path.Combine(artifacts, "render-" + Guid.NewGuid().ToString("N"));
            Environment.SetEnvironmentVariable("TIMERSAPP_DATA_DIR", folder);
            var data = new Workspace(); var e = new TimerEngine(data);
            foreach (var pair in new[] { ("Design exploration", "Sketching the next idea", 4120), ("Client review", "Notes and follow-up", 1860), ("Deep work", "One thing at a time", 905) })
            {
                var t = e.Add(pair.Item1, pair.Item2); t.Segments.Add(new Segment { RunId = t.RunId, Name = t.Name, Start = DateTimeOffset.Now.AddSeconds(-pair.Item3), End = DateTimeOffset.Now, ElapsedTicks = TimeSpan.FromSeconds(pair.Item3).Ticks });
            }
            new WorkspaceStore(folder).Save(data);
            var app = new App(); app.InitializeComponent();
            app.ShutdownMode=System.Windows.ShutdownMode.OnExplicitShutdown;
            string freshFolder=Path.Combine(artifacts,"first-session-"+Guid.NewGuid().ToString("N"));Environment.SetEnvironmentVariable("TIMERSAPP_DATA_DIR",freshFolder);
            var freshWindow=new MainWindow();var firstId=freshWindow.Engine.Workspace.Id;freshWindow.Engine.Workspace.Settings.ExitChoice=ExitChoice.Discard;freshWindow.Close();var freshStore=new WorkspaceStore(freshFolder);Equal(firstId,freshStore.Load()!.Id);
            var off=freshStore.Load()!;off.Settings.RestoreOnLaunch=false;freshStore.Save(off);for(int restart=0;restart<2;restart++){var scratch=new MainWindow();Equal(false,scratch.Engine.Workspace.Name=="default");scratch.Engine.Workspace.Settings.ExitChoice=ExitChoice.Discard;scratch.Close();}Equal(1,freshStore.SavedWorkspaces().Count);Equal(firstId,freshStore.Load()!.Id);
            var editedScratch=new MainWindow();editedScratch.Engine.Workspace.Settings.ShowStatusBar=true;editedScratch.AppearanceChanged(false);Equal(2,freshStore.SavedWorkspaces().Count);Equal(true,freshStore.Load()!.Settings.ShowStatusBar);editedScratch.Engine.Workspace.Settings.ExitChoice=ExitChoice.Save;editedScratch.Close();
            string transitionFolder=Path.Combine(artifacts,"session-switch-"+Guid.NewGuid().ToString("N"));Environment.SetEnvironmentVariable("TIMERSAPP_DATA_DIR",transitionFolder);var switchWindow=new MainWindow();var target=new Workspace{Name="Another workspace"};
            var transition=typeof(MainWindow).GetMethod("Transition",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;Equal(true,(bool)transition.Invoke(switchWindow,new object[]{target,false})!);switchWindow.Engine.Add("Discard this edit");switchWindow.Engine.Workspace.Settings.ExitChoice=ExitChoice.Discard;switchWindow.Close();var switched=new WorkspaceStore(transitionFolder).Load()!;Equal(target.Id,switched.Id);Equal(0,switched.Timers.Count);
            Environment.SetEnvironmentVariable("TIMERSAPP_DATA_DIR",folder);
            var window = new MainWindow(); window.RefreshWorkspaceView();
            var preservingTimer=window.Engine.Add("Preservation check");window.Engine.Start(preservingTimer);typeof(MainWindow).GetMethod("PreserveBeforeRestore",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.Invoke(window,null);var preserved=new WorkspaceStore(folder).Load("before-restore")!;Equal(false,preserved.Timers.Last().IsRunning);Equal(false,preserved.Timers.Last().Segments[0].Recovered);window.Engine.Stop(preservingTimer);window.Engine.Workspace.Timers.Remove(preservingTimer);
            var accessibleTimer = window.Engine.Workspace.Timers[0];
            var accessibleCard = new TimerCard(window, accessibleTimer, popout: true) { Width = 180, Height = 100 };
            var popoutHost = new System.Windows.Window { Content = accessibleCard };
            accessibleCard.SetPreview(TimerStyle.Minimal());
            Equal(true, accessibleCard.Focusable);
            var peer = System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(accessibleCard)!;
            Equal(System.Windows.Automation.Peers.AutomationControlType.Group, peer.GetAutomationControlType());
            Equal(true, peer.GetName().Contains("Design exploration"));
            window.Engine.Rename(accessibleTimer, "Accessible renamed timer", "");
            accessibleCard.Refresh(); Equal("Accessible renamed timer — T⁴", popoutHost.Title);
            Equal(true, peer.GetName().Contains("Accessible renamed timer"));
            window.Engine.Rename(accessibleTimer, "Design exploration", "Sketching the next idea");
            popoutHost.Content = null;
            var wideCard=new TimerCard(window,accessibleTimer,popout:true){Width=720,Height=160};
            wideCard.Measure(new System.Windows.Size(720,160));wideCard.Arrange(new System.Windows.Rect(0,0,720,160));wideCard.Refresh();wideCard.UpdateLayout();
            Equal(true,wideCard.HorizontalPresentation);Equal(true,wideCard.ControlsVisible);
            window.Engine.Workspace.Settings.PopoutScale=.75;window.Engine.Workspace.Settings.TimerScale=1.5;wideCard.Refresh();Equal(System.Windows.Media.Transform.Identity,wideCard.LayoutTransform);
            var initial=TimerSizing.InitialPopout(window.Engine.Workspace.Settings);Equal(292.5,initial.Width);
            window.Engine.Workspace.Settings.LinkPopoutScale=true;Equal(585d,TimerSizing.InitialPopout(window.Engine.Workspace.Settings).Width);
            window.Engine.Workspace.Settings.LinkPopoutScale=false;window.Engine.Workspace.Settings.TimerScale=1;window.Engine.Workspace.Settings.PopoutScale=1;
            var compactCard=new TimerCard(window,accessibleTimer,popout:true){Width=220,Height=90};compactCard.SetPreview(TimerStyle.Minimal());compactCard.Measure(new System.Windows.Size(220,90));compactCard.Arrange(new System.Windows.Rect(0,0,220,90));compactCard.Refresh();compactCard.UpdateLayout();Equal(false,compactCard.ControlsVisible);
            Feedback3Checks.Layout(window);
            Feedback2Checks.Layout(window,artifacts);Feedback4Checks.Layout(window,artifacts);
            var scaleControl=new ScaleInput{Value=1.1};int scaleEvents=0;scaleControl.ValueChanged+=(_,_)=>scaleEvents++;var plus=scaleControl.Children.OfType<System.Windows.Controls.Button>().Last();plus.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));Equal(1.25,scaleControl.Value);Equal(1,scaleEvents);
            var mainCard = new TimerCard(window, accessibleTimer);
            Equal(true, mainCard.ContextMenu.Items.OfType<System.Windows.Controls.MenuItem>().Any(item => Equals(item.Header, "Return to workspace")));
            var readout = new TimeReadout(); readout.Update("12:34:56", new TimerStyle { Digits = DigitStyle.SevenSegment });
            var digitPeer = System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(readout)!;
            Equal("12:34:56", digitPeer.GetName());
            readout.Update("12:34:57", new TimerStyle()); Equal("12:34:57", digitPeer.GetName());
            readout.Measure(new System.Windows.Size(double.PositiveInfinity,double.PositiveInfinity));readout.Arrange(new System.Windows.Rect(new System.Windows.Point(),readout.DesiredSize));Equal(true,readout.IsMeasureValid);readout.Update("12:34:58",new TimerStyle());Equal(true,readout.IsMeasureValid);
            var cachedClip=compactCard.Clip;compactCard.Refresh();Equal(true,ReferenceEquals(cachedClip,compactCard.Clip));
            var form = new FormDialog(null, "Accessible fields"); form.Label("Timer description"); var field = form.Text("");
            Equal("Timer description", ((System.Windows.Controls.TextBlock)System.Windows.Automation.AutomationProperties.GetLabeledBy(field)).Text);
            var content = (System.Windows.FrameworkElement)window.Content;
            content.Measure(new System.Windows.Size(1200, 740)); content.Arrange(new System.Windows.Rect(0, 0, 1200, 740)); content.UpdateLayout();
            var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(1200, 740, 96, 96, System.Windows.Media.PixelFormats.Pbgra32); bitmap.Render(content);
            var png = new System.Windows.Media.Imaging.PngBitmapEncoder(); png.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
            using var output = File.Create(Path.Combine(artifacts, "workspace-preview.png")); png.Save(output);
            var gallery = new System.Windows.Controls.Grid { Width = 1000, Height = 620, Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(14, 21, 37)) };
            gallery.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition()); gallery.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition());
            gallery.RowDefinitions.Add(new System.Windows.Controls.RowDefinition()); gallery.RowDefinitions.Add(new System.Windows.Controls.RowDefinition());
            TimerCard AddCard(TimerStyle style, string name, int column, int row, double width, double height)
            {
                var card = new TimerCard(window, new TimerRecord { Name = name, Description = "Customizable and independently sized" }, preview: true) { Width = width, Height = height, Margin = new System.Windows.Thickness(18) };
                card.SetPreview(style); System.Windows.Controls.Grid.SetColumn(card, column); System.Windows.Controls.Grid.SetRow(card, row); gallery.Children.Add(card); return card;
            }
            AddCard(data.Presets[1].Style, "Seven-segment display", 0, 0, 460, 270);
            AddCard(new TimerStyle { FontFamily = "Georgia", TextColor = "#FFDFA6", AccentColor = "#FFDFA6", Background = new() { Color = "#322A25" } }, "Selectable fonts", 1, 0, 460, 270);
            var tiny = AddCard(TimerStyle.Minimal(), "Time-only pop-out", 0, 1, 300, 80);
            Equal(true, tiny.ContextMenu.Items.Count > 10);
            AddCard(new TimerStyle { Digits = DigitStyle.SevenSegment, Units = TimeUnits.Hours | TimeUnits.Minutes | TimeUnits.Seconds | TimeUnits.Milliseconds, TextColor = "#B9BCFF", AccentColor = "#B9BCFF", Background = new() { Color = "#24243D" } }, "Milliseconds", 1, 1, 460, 230);
            gallery.Measure(new System.Windows.Size(1000, 620)); gallery.Arrange(new System.Windows.Rect(0, 0, 1000, 620)); gallery.UpdateLayout();
            var galleryImage = new System.Windows.Media.Imaging.RenderTargetBitmap(1000, 620, 96, 96, System.Windows.Media.PixelFormats.Pbgra32); galleryImage.Render(gallery);
            var galleryPng = new System.Windows.Media.Imaging.PngBitmapEncoder(); galleryPng.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(galleryImage));
            string imagePath = Path.Combine(artifacts, "appearance-preview.png"); using (var imageFile = File.Create(imagePath)) galleryPng.Save(imageFile);
            var assets = new AssetLibrary(folder); string managed = assets.Import(imagePath, true);
            var editor=StyleEditor.Create(window,accessibleTimer,new TimerStyle(),"Style editor render");
            var editorContent=(System.Windows.FrameworkElement)editor.Dialog.Content;editorContent.Margin=new System.Windows.Thickness();editorContent.Width=1150;editorContent.Height=690;editorContent.Measure(new System.Windows.Size(1150,690));editorContent.Arrange(new System.Windows.Rect(0,0,1150,690));editorContent.UpdateLayout();
            var editorBitmap=new System.Windows.Media.Imaging.RenderTargetBitmap(1150,690,96,96,System.Windows.Media.PixelFormats.Pbgra32);editorBitmap.Render(editorContent);var editorPng=new System.Windows.Media.Imaging.PngBitmapEncoder();editorPng.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(editorBitmap));using(var editorFile=File.Create(Path.Combine(artifacts,"style-editor-preview.png")))editorPng.Save(editorFile);
            var exportPreset = new StylePreset { Name = "With image", Style = new TimerStyle { Background = new() { ImagePath = managed, CropX = .1, CropWidth = .8, Fit = ImageFit.Fill, AnchorX = .25 } } };
            string presetPath = Path.Combine(artifacts, "portable.t4style"); assets.ExportPreset(presetPath, exportPreset);
            var imported = assets.ImportPreset(presetPath); Equal("With image", imported.Name); Equal(true, File.Exists(imported.Style.Background.ImagePath)); Equal(.1, imported.Style.Background.CropX); Equal(false, imported.Id == exportPreset.Id);
            data.Timers[0].Style = imported.Style;
            string archivePath = Path.Combine(artifacts, "portable.t4backup"); WorkspaceArchive.Export(archivePath, data);
            var portable = WorkspaceArchive.Import(archivePath, Path.Combine(folder, "restored")); Equal(3, portable.Timers.Count); Equal(true, File.Exists(portable.Timers[0].Style!.Background.ImagePath)); Equal(.1, portable.Timers[0].Style!.Background.CropX);
            var bg = new ImageBackdrop { Width = 500, Height = 250 }; bg.Update(imported.Style.Background); bg.Measure(new System.Windows.Size(500, 250)); bg.Arrange(new System.Windows.Rect(0, 0, 500, 250)); Equal<string?>(null, bg.ImageError);
            window.Show();window.UpdateLayout();Feedback3Checks.Settings(window,artifacts);
            Feedback4Checks.Windows(window,artifacts);Feedback2Checks.LiveWindows(window);
        }
        catch (Exception ex) { error = ex; }
    });
    thread.SetApartmentState(ApartmentState.STA); thread.Start(); thread.Join();
    if (error is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
});
Console.WriteLine($"{passed} passed, {failures.Count} failed");
return failures.Count == 0 ? 0 : 1;

sealed class ManualClock : TimeProvider
{
    private DateTimeOffset utc = new(2025, 1, 1, 12, 0, 0, TimeSpan.Zero); private long timestamp;
    public override DateTimeOffset GetUtcNow() => utc;
    public override long GetTimestamp() => timestamp;
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    public void Advance(int seconds) { utc = utc.AddSeconds(seconds); timestamp += TimeSpan.FromSeconds(seconds).Ticks; }
    public void ShiftWallClock(int seconds) => utc = utc.AddSeconds(seconds);
}
