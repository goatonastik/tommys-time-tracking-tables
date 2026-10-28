using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using TimersApp.Core;

namespace TimersApp.Desktop;
public partial class MainWindow
{
    internal AssetLibrary Assets => new(store.Folder);
    public void AppearanceChanged(bool render = true) => Changed(render);
    public void Relayout() { Cards.InvalidateMeasure(); Cards.InvalidateArrange(); }
    public void EditTimerStyle(TimerRecord timer, bool popout)
    {
        var style = StyleEditor.Edit(this, timer, Appearance.Resolve(Engine.Workspace, timer, popout), popout ? "Pop-out style — " + timer.Name : "Timer style — " + timer.Name);
        if (style is null) return;
        SetStyle(timer, style, popout); Changed(false);
    }
    private void SetStyle(TimerRecord timer, TimerStyle style, bool popout)
    {
        if (popout) { timer.SeparatePopoutStyle = true; timer.PopoutStyle = style; }
        else { timer.Style = style; timer.PresetId = null; }
    }
    public void ApplyPreset(TimerRecord timer, StylePreset preset, bool popout)
    {
        if (popout) SetStyle(timer, Appearance.Copy(preset.Style), true);
        else { timer.Style = null; timer.PresetId = preset.Id; }
        Changed(false);
    }
    public void UseDefaultStyle(TimerRecord timer, bool popout)
    {
        if (popout) { timer.SeparatePopoutStyle = null; timer.PopoutStyle = null; }
        else { timer.Style = null; timer.PresetId = null; }
        Changed(false);
    }
    public void SetTimeOnly(TimerRecord timer, bool popout) { SetStyle(timer, TimerStyle.Minimal(), popout); Changed(false); }
    public void ToggleVisibility(TimerRecord timer, bool popout, string label)
    {
        var style = Appearance.Copy(Appearance.Resolve(Engine.Workspace, timer, popout));
        switch (label) { case "Title": style.ShowTitle = !style.ShowTitle; break; case "Description": style.ShowDescription = !style.ShowDescription; break; case "Status": style.ShowStatus = !style.ShowStatus; break; case "Controls": style.ShowControls = !style.ShowControls; break; case "Total details": style.ShowTotal = !style.ShowTotal; break; }
        SetStyle(timer, style, popout); Changed(false);
    }
    private void Appearance_Click(object sender, RoutedEventArgs e)
    {
        var d = new FormDialog(this, "Appearance and presets", 550);
        d.Label("Default styles apply to timers that have no custom style or preset.");
        var defaultButton = new Button { Content = "Default timer style…", Margin = new Thickness(0, 0, 0, 10) }; d.Fields.Children.Add(defaultButton);
        var separate = d.Check("Use a separate default style for pop-outs", Engine.Workspace.Settings.SeparatePopoutStyle);
        var popout = new Button { Content = "Default pop-out style…", Margin = new Thickness(0, 4, 0, 10) }; d.Fields.Children.Add(popout);
        var appBg = new Button { Content = "Application background…" }; d.Fields.Children.Add(appBg);
        d.Label("Saved timer styles"); var list = new ListBox { Height = 180, Foreground = Brushes.Black }; d.Fields.Children.Add(list);
        void RefreshPresets() { var selected = list.SelectedItem; list.ItemsSource = Engine.Workspace.Presets.ToArray(); list.SelectedItem = selected; }
        var row = new WrapPanel { Margin = new Thickness(0, 12, 0, 0) }; d.Fields.Children.Add(row);
        void Button(string text, Action action) { var button = new Button { Content = text, Margin = new Thickness(0, 0, 8, 8) }; button.Click += (_, _) => action(); row.Children.Add(button); }
        var sample = new TimerRecord { Name = "Timer preview", Description = "Your style, your workspace" };
        defaultButton.Click += (_, _) => { var style = StyleEditor.Edit(this, sample, Engine.Workspace.Settings.DefaultStyle, "Default timer style"); if (style is not null) { Engine.Workspace.Settings.DefaultStyle = style; Changed(false); } };
        popout.Click += (_, _) => { var style = StyleEditor.Edit(this, sample, Engine.Workspace.Settings.PopoutStyle, "Default pop-out style"); if (style is not null) { Engine.Workspace.Settings.PopoutStyle = style; Changed(false); } };
        separate.Checked += (_, _) => { Engine.Workspace.Settings.SeparatePopoutStyle = true; Changed(false); };
        separate.Unchecked += (_, _) => { Engine.Workspace.Settings.SeparatePopoutStyle = false; Changed(false); };
        appBg.Click += (_, _) => { var bg = BackgroundEditor.Edit(d, Engine.Workspace.Settings.AppBackground, Assets); if (bg is not null) { Engine.Workspace.Settings.AppBackground = bg; AppBackdrop.Update(bg); Changed(false); } };
        Button("New…", () => { var style = StyleEditor.Edit(this, sample, new TimerStyle(), "New style"); if (style is null) { RefreshPresets(); return; } NamePreset(d, null, style); RefreshPresets(); });
        Button("Edit…", () => { if (list.SelectedItem is not StylePreset p) return; var style = StyleEditor.Edit(this, sample, p.Style, "Edit preset — " + p.Name); if (style is not null) { p.Style = style; Changed(false); } RefreshPresets(); });
        Button("Rename…", () => { if (list.SelectedItem is StylePreset p) { NamePreset(d, p, p.Style); RefreshPresets(); } });
        Button("Duplicate", () => { if (list.SelectedItem is StylePreset p) { Engine.Workspace.Presets.Add(new StylePreset { Name = p.Name + " copy", Style = Appearance.Copy(p.Style) }); RefreshPresets(); Changed(false); } });
        Button("Remove", () =>
        {
            if (list.SelectedItem is not StylePreset p) return;
            foreach (var t in Engine.Workspace.Timers.Where(t => t.PresetId == p.Id && t.Style is null)) { t.Style = Appearance.Copy(p.Style); t.PresetId = null; }
            Engine.Workspace.Presets.Remove(p); RefreshPresets(); Changed(false);
        });
        Button("Import…", () =>
        {
            var file = new OpenFileDialog { Filter = "T⁴ style preset|*.t4style" }; if (file.ShowDialog(d) != true) return;
            try { Engine.Workspace.Presets.Add(Assets.ImportPreset(file.FileName)); RefreshPresets(); Changed(false); } catch (Exception ex) { MessageBox.Show(d, ex.Message, "Could not import preset"); }
        });
        Button("Export…", () =>
        {
            if (list.SelectedItem is not StylePreset p) return;
            var file = new SaveFileDialog { Filter = "T⁴ style preset|*.t4style", FileName = "Timer-style.t4style" }; if (file.ShowDialog(d) != true) return;
            try { Assets.ExportPreset(file.FileName, p); } catch (Exception ex) { MessageBox.Show(d, ex.Message, "Could not export preset"); }
        });
        d.Label("Changes in this panel apply immediately. Imported images are kept in the app’s data folder.");
        d.Actions(("Close", () => d.Close())); RefreshPresets(); d.ShowDialog();
    }
    private void NamePreset(Window owner, StylePreset? preset, TimerStyle style)
    {
        var d = new FormDialog(owner, "Preset name"); d.Label("Name"); var name = d.Text(preset?.Name ?? "My timer style");
        d.Actions(("Save", () => { if (string.IsNullOrWhiteSpace(name.Text)) return; if (preset is null) Engine.Workspace.Presets.Add(new StylePreset { Name = name.Text.Trim(), Style = Appearance.Copy(style) }); else preset.Name = name.Text.Trim(); Changed(false); d.DialogResult = true; }), ("Cancel", () => d.Close())); d.ShowDialog();
    }
    private void ManageNames(Window owner)
    {
        var d = new FormDialog(owner, "Timer-name suggestions");
        d.Label("Select names to remove from suggestions. Recorded timer names are preserved.");
        d.Label("Changes here apply immediately, including when opened from Workspace settings.");
        var disabled=d.Check("Do not save name suggestions",!Engine.Workspace.Settings.SaveNameSuggestions);d.Label("Existing suggestions remain until removed; this stops collecting new names.");disabled.Checked+=(_,_)=>{Engine.Workspace.Settings.SaveNameSuggestions=false;Changed(false);};disabled.Unchecked+=(_,_)=>{Engine.Workspace.Settings.SaveNameSuggestions=true;Changed(false);};
        var list = new ListBox { Height = 300, SelectionMode = SelectionMode.Extended, Foreground = Brushes.Black, ItemsSource = Engine.Workspace.NameHistory.ToArray() }; d.Fields.Children.Add(list);
        d.Actions(("Remove selected", () => { foreach (var name in list.SelectedItems.Cast<string>().ToArray()) Engine.Workspace.NameHistory.Remove(name); list.ItemsSource = Engine.Workspace.NameHistory.ToArray(); Changed(false); }), ("Remove all",()=>{Engine.Workspace.NameHistory.Clear();list.ItemsSource=Engine.Workspace.NameHistory.ToArray();Changed(false);}), ("Close", () => d.Close())); d.ShowDialog();
    }
    public void EditAlert(TimerRecord timer)
    {
        var a = Appearance.Copy(timer.Alert); var d = new FormDialog(this, "Interval alert — " + timer.Name, 520);
        var enabled = d.Check("Enable interval alert", a.Enabled);
        d.Label("Interval in minutes (fractions allowed)"); var interval = d.Text((a.IntervalSeconds / 60).ToString("0.####"));
        var repeat = d.Check("Repeat at every interval", a.Repeat);
        var color = d.Check("Change display color", a.ChangeColor); var flash = d.Check("Flash display", a.Flash);
        d.Label("Alert color"); var hex = d.Text(a.Color);
        var sound = d.Check("Play sound", a.PlaySound); var repeatSound = d.Check("Repeat sound until dismissed or snoozed", a.RepeatSound);
        var choices=SoundCatalog.Discover();
        if(a.SoundPath is null&&a.SystemSound is not null&&choices.All(x=>x.SystemFile!=a.SystemSound)){choices.Add(new SoundChoice("Unavailable Windows sound: "+a.SystemSound,a.SystemSound));d.Label("The saved Windows sound is unavailable here. Playback will use the built-in chime until it becomes available or you choose another sound.");}
        if(a.SoundPath is not null)choices.Add(new SoundChoice("Custom: "+(a.SoundName??System.IO.Path.GetFileName(a.SoundPath)),CustomPath:a.SoundPath));
        var selected=a.SoundPath is not null?choices.Last():choices.FirstOrDefault(x=>x.SystemFile==a.SystemSound)??choices.First();
        d.Label("Alert sound");
        var soundList=d.Select(choices,selected);
        var preview=new TimerRecord{AlertState=new(){Active=true}};
        soundList.SelectionChanged+=(_,_)=>{audio.Stop(preview.Id);if(soundList.SelectedItem is SoundChoice choice){choice.Select(a);}};
        var browse=new Button{Content="Choose custom WAV or MP3…"};d.Fields.Children.Add(browse);
        browse.Click+=(_,_)=>{var file=new OpenFileDialog{Filter="WAV or MP3 sound|*.wav;*.mp3"};if(file.ShowDialog(d)!=true)return;try{AlertAudio.Validate(file.FileName);audio.Stop(preview.Id);var choice=new SoundChoice("Custom: "+System.IO.Path.GetFileName(file.FileName),CustomPath:file.FileName,ImportOnApply:true);choices.RemoveAll(x=>x.ImportOnApply);choices.Add(choice);soundList.ItemsSource=null;soundList.ItemsSource=choices;soundList.SelectedItem=choice;}catch(Exception ex){MessageBox.Show(d,ex.Message,"Could not load sound");}};
        d.Closed+=(_,_)=>audio.Stop(preview.Id);
        d.Label("Volume"); var volume = new Slider { Minimum = 0, Maximum = 1, Value = a.Volume }; d.Fields.Children.Add(volume);
        System.Windows.Automation.AutomationProperties.SetName(volume,"Alert sound volume");
        var previewError=new TextBlock{TextWrapping=TextWrapping.Wrap,Foreground=Brushes.Orange};d.Fields.Children.Add(previewError);
        d.Label("Snooze minutes (1–1440)"); var snooze = d.Text(a.SnoozeMinutes.ToString());
        d.Label("Alerts follow exact running time. The next alert occurs at the next multiple of the chosen interval. Right-click a timer to dismiss or snooze.");
        d.Actions(("Apply", () =>
        {
            if (!double.TryParse(interval.Text, out double minutes) || !double.IsFinite(minutes) || minutes < 1d / 60 || minutes > 525600 || !int.TryParse(snooze.Text, out int sn) || sn < 1 || sn > 1440 || !Visuals.ValidColor(hex.Text)) { MessageBox.Show(d, "Check interval, color, and snooze values."); return; }
            a.Enabled = enabled.IsChecked == true; a.IntervalSeconds = minutes * 60; a.Repeat = repeat.IsChecked == true; a.ChangeColor = color.IsChecked == true; a.Flash = flash.IsChecked == true; a.Color = hex.Text; a.PlaySound = sound.IsChecked == true; a.RepeatSound = repeatSound.IsChecked == true; a.Volume = volume.Value; a.SnoozeMinutes = sn;
            try{if(soundList.SelectedItem is SoundChoice chosen&&chosen.ImportOnApply&&a.SoundPath is not null)a.SoundPath=Assets.Import(a.SoundPath,false);}catch(Exception ex){MessageBox.Show(d,ex.Message,"Could not import sound");return;}
            audio.Stop(timer.Id); timer.Alert = a; timer.AlertState = new AlertState { LastInterval = (long)(Engine.Total(timer).TotalSeconds / a.IntervalSeconds) }; Changed(false); d.DialogResult = true;
        }), ("Test sound", () => { previewError.Text="";preview.Alert=Appearance.Copy(a);preview.Alert.PlaySound=true;preview.Alert.Volume=volume.Value;preview.Alert.RepeatSound=false;audio.Play(preview,message=>previewError.Text=message); }), ("Cancel", () => d.Close()));
        d.ShowDialog();
    }
    public void DismissAlert(TimerRecord timer) { Alerts.Dismiss(timer.AlertState); audio.Stop(timer.Id); Changed(false); }
    public void SnoozeAlert(TimerRecord timer) { Alerts.Snooze(timer.AlertState, DateTimeOffset.Now, timer.Alert.SnoozeMinutes); audio.Stop(timer.Id); Changed(false); }
    internal double WorkspaceWidth => double.IsFinite(WorkspaceScroll.ViewportWidth)&&WorkspaceScroll.ViewportWidth>0?WorkspaceScroll.ViewportWidth:Math.Max(250,(ActualWidth>0?ActualWidth:Width)/Engine.Workspace.Settings.AppScale-50);
    private bool layoutQueued;
    private void QueueLayout()
    {
        if(layoutQueued)return;layoutQueued=true;
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded,new Action(()=>{layoutQueued=false;if(!exiting){UpdateGridColumns();Relayout();}}));
    }
    private void Workspace_SizeChanged(object sender, SizeChangedEventArgs e){if(ready)QueueLayout();}
    private void Workspace_ScrollChanged(object sender,ScrollChangedEventArgs e){if(ready&&(e.ViewportWidthChange!=0||e.ViewportHeightChange!=0))QueueLayout();}
    private void UpdateGridColumns()
    {
        var s=Engine.Workspace.Settings;
        var presenters=Cards.Children.OfType<TimerCardPresenter>().ToArray();
        foreach(var presenter in presenters)presenter.Configure(s,WorkspaceWidth);
        double factor=TimerSizing.PresentationScale(s),gap=(s.ListLayout?s.ListSpacing:s.Spacing)*factor;
        double largest=presenters.Select(c=>c.Card.Width*c.Factor).DefaultIfEmpty(Math.Max(150,s.CardWidth*TimerSizing.LogicalScale(s))*factor).Max();
        int columns=s.ListLayout?1:s.AutoColumns?Math.Max(1,(int)((WorkspaceWidth+gap)/(largest+gap))):s.Columns;
        Cards.Columns=columns;Cards.Spacing=gap;Cards.MinimumRows=s.ListLayout?0:s.Rows;
        Cards.EmptyRowHeight=s.FitCardHeight?presenters.Select(c=>c.Card.Height*c.Factor).DefaultIfEmpty(Math.Max(48,100*s.TimerScale)).Max():Math.Max(24,s.CardHeight*TimerSizing.LogicalScale(s))*factor;
        Relayout();
    }
}
