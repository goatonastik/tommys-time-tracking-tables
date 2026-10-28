using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using TimersApp.Core;

namespace TimersApp.Desktop;
public partial class MainWindow
{
    private void Settings_Click(object sender,RoutedEventArgs e)
    {
        string before=System.Text.Json.JsonSerializer.Serialize(Engine.Workspace.Settings);
        var d=CreateSettingsDialog();if(d.ShowDialog()==true){ApplySettings();if(d.Tag is true||before!=System.Text.Json.JsonSerializer.Serialize(Engine.Workspace.Settings))Changed();}
    }
    internal FormDialog CreateSettingsDialog()
    {
        var original=Engine.Workspace.Settings;var s=Appearance.Copy(original);var d=new FormDialog(this,"Settings",820);
        var autosave=d.Check("Automatically save timers and settings",s.AutoSave);var restore=d.Check("Restore saved workspace on launch",s.RestoreOnLaunch);var sleep=d.Check("Pause timers while the PC sleeps",s.PauseDuringSleep);var status=d.Check("Show status bar",s.ShowStatusBar);var resize=d.Check("Allow individual timer resizing",s.AllowIndividualResize);
        d.Label("Application scale");var appScale=new ScaleInput{Value=s.AppScale,Label="Application scale percent"};d.Fields.Children.Add(appScale);appScale.ValueChanged+=(_,_)=>{AppTransform.ScaleX=AppTransform.ScaleY=appScale.Value;};var resetApp=new Button{Content="Reset application scale to 100%"};resetApp.Click+=(_,_)=>{appScale.Value=1;AppTransform.ScaleX=AppTransform.ScaleY=1;};d.Fields.Children.Add(resetApp);d.Closed+=(_,_)=>{AppTransform.ScaleX=AppTransform.ScaleY=Engine.Workspace.Settings.AppScale;};
        StartupSoundSettings startup;
        try{startup=Appearance.Copy(store.LoadStartupSound());}catch(Exception ex){startup=new();d.Label("Saved startup sound could not be read. Apply will replace that preference; Cancel preserves it. "+ex.Message);}
        bool importSound=false;var previewId=Guid.NewGuid();
        d.Label("Startup sound · applies to all workspaces");var startupEnabled=d.Check("Play a custom sound when the app opens",startup.Enabled);
        var soundName=new TextBlock{Text=startup.Name??"No sound selected",TextWrapping=TextWrapping.Wrap};d.Fields.Children.Add(soundName);
        var choose=new Button{Content="Choose WAV or MP3…",Margin=new Thickness(0,4,0,4)};d.Fields.Children.Add(choose);
        choose.Click+=(_,_)=>{var file=new OpenFileDialog{Filter="WAV or MP3 sound|*.wav;*.mp3"};if(file.ShowDialog(d)!=true)return;try{AlertAudio.Validate(file.FileName);audio.Stop(previewId);startup.Path=file.FileName;startup.Name=System.IO.Path.GetFileName(file.FileName);soundName.Text=startup.Name;importSound=true;}catch(Exception ex){MessageBox.Show(d,ex.Message,"Could not load sound");}};
        var startupVolume=new NumberSlider("Startup volume",0,1,startup.Volume,.05);d.Fields.Children.Add(startupVolume);
        var soundError=new TextBlock{TextWrapping=TextWrapping.Wrap,Foreground=Brushes.Orange};
        var soundActions=new WrapPanel();d.Fields.Children.Add(soundActions);
        void SoundButton(string label,Action action){var b=new Button{Content=label,Padding=new Thickness(8,5,8,5),Margin=new Thickness(0,0,4,4)};b.Click+=(_,_)=>action();soundActions.Children.Add(b);}
        SoundButton("Test",()=>{soundError.Text="";if(startup.Path is null){soundError.Text="Choose a sound first.";return;}audio.PlayOnce(previewId,startup.Path,startupVolume.Value,message=>soundError.Text=message);});
        SoundButton("Stop",()=>audio.Stop(previewId));SoundButton("Disable / reset",()=>{audio.Stop(previewId);startupEnabled.IsChecked=false;startup.Path=null;startup.Name=null;importSound=false;startupVolume.Value=.7;soundName.Text="No sound selected";soundError.Text="";});d.Fields.Children.Add(soundError);
        d.Label("Saves on Apply. Workspace Discard does not undo this app-wide preference. Cancel keeps the previous choice.");d.Closed+=(_,_)=>audio.Stop(previewId);
        int split=d.Fields.Children.Count;
        d.Label("Exit decision");var exit=d.Select(Enum.GetValues<ExitChoice>(),s.ExitChoice);d.Label("Round to nearest (minutes)");var round=d.Select(new[]{1,15,60},s.RoundMinutes);d.Label("Total display");var display=d.Select(Enum.GetValues<TotalDisplay>(),s.TotalDisplay);
        d.Label("Grid columns");var columns=d.Select(Enumerable.Range(1,8),s.Columns);var automatic=d.Check("Automatically fit columns",s.AutoColumns);d.Label("Minimum grid rows (0 = automatic)");var rows=d.Text(s.Rows.ToString());
        NumberSlider Slider(string label,double min,double max,double value){var control=new NumberSlider(label,min,max,value);d.Fields.Children.Add(control);return control;}
        var fit=d.Check("Fit card height to content",s.FitCardHeight);
        var gap=Slider("Space between timers",0,80,s.Spacing);var width=Slider("Card width",250,1200,s.CardWidth);var height=Slider("Manual card height",48,1200,s.CardHeight);height.IsEnabled=!s.FitCardHeight;fit.Checked+=(_,_)=>height.IsEnabled=false;fit.Unchecked+=(_,_)=>height.IsEnabled=true;var list=Slider("List row height",48,300,s.ListRowHeight);
        var compactStyle=new Button{Content="Use compact default timer style",Margin=new Thickness(0,8,0,4)};compactStyle.Click+=(_,_)=>{s.DefaultStyle.Padding=0;s.DefaultStyle.CornerRadius=0;s.PopoutStyle.Padding=0;s.PopoutStyle.CornerRadius=0;fit.IsChecked=true;};d.Fields.Children.Add(compactStyle);d.Label("Removes default timer/pop-out padding and rounding. Font, colors, backgrounds and custom presets are kept. Applies when you choose Apply.");
        var resetSizes=d.Check("Forget saved individual card sizes",false);
        d.Label("Pop-out memory · this workspace");
        var rememberLocation=d.Check("Save popped-out window location",s.RememberPopoutLocation);
        var locationPerTimer=d.Check("Save location per timer",s.PopoutLocationPerTimer);locationPerTimer.Margin=new Thickness(18,0,0,0);
        var rememberSize=d.Check("Save popped-out window size",s.RememberPopoutSize);
        var sizePerTimer=d.Check("Save size per timer",s.PopoutSizePerTimer);sizePerTimer.Margin=new Thickness(18,0,0,0);
        void UpdateMemory(){locationPerTimer.IsEnabled=rememberLocation.IsChecked==true;sizePerTimer.IsEnabled=rememberSize.IsChecked==true;}
        rememberLocation.Checked+=(_,_)=>UpdateMemory();rememberLocation.Unchecked+=(_,_)=>UpdateMemory();rememberSize.Checked+=(_,_)=>UpdateMemory();rememberSize.Unchecked+=(_,_)=>UpdateMemory();UpdateMemory();
        d.Label("Reuses saved values on every reopening. Turn off per timer to use this workspace’s shared memory. Each mode keeps its previous values; an empty memory uses defaults. Turning saving off keeps previous memories.");
        var names=new Button{Content="Timer name suggestions",Margin=new Thickness(0,12,0,0)};names.Click+=(_,_)=>ManageNames(d);d.Fields.Children.Add(names);
        d.Actions(("Reset to defaults",()=>{var defaults=new AppSettings();autosave.IsChecked=defaults.AutoSave;restore.IsChecked=defaults.RestoreOnLaunch;sleep.IsChecked=defaults.PauseDuringSleep;status.IsChecked=false;exit.SelectedItem=defaults.ExitChoice;round.SelectedItem=defaults.RoundMinutes;display.SelectedItem=defaults.TotalDisplay;columns.SelectedItem=defaults.Columns;automatic.IsChecked=true;rows.Text="0";gap.Value=defaults.Spacing;width.Value=defaults.CardWidth;height.Value=defaults.CardHeight;list.Value=defaults.ListRowHeight;appScale.Value=1;AppTransform.ScaleX=AppTransform.ScaleY=1;resize.IsChecked=false;fit.IsChecked=true;rememberSize.IsChecked=true;rememberLocation.IsChecked=true;sizePerTimer.IsChecked=true;locationPerTimer.IsChecked=true;}),("Apply",()=>{
            if(!int.TryParse(rows.Text,out int n)||n<0||n>100){MessageBox.Show(d,"Use 0–100 rows.");return;}
            s.AutoSave=autosave.IsChecked==true;s.RestoreOnLaunch=restore.IsChecked==true;s.PauseDuringSleep=sleep.IsChecked==true;s.ShowStatusBar=status.IsChecked==true;s.ExitChoice=(ExitChoice)exit.SelectedItem;s.RoundMinutes=(int)round.SelectedItem;s.TotalDisplay=(TotalDisplay)display.SelectedItem;s.Columns=(int)columns.SelectedItem;s.AutoColumns=automatic.IsChecked==true;s.Rows=n;s.Spacing=gap.Value;s.CardWidth=width.Value;s.CardHeight=height.Value;s.ListRowHeight=list.Value;s.AppScale=appScale.Value;s.AllowIndividualResize=resize.IsChecked==true;
            s.SaveNameSuggestions=Engine.Workspace.Settings.SaveNameSuggestions;
            s.FitCardHeight=fit.IsChecked==true;s.RememberPopoutSize=rememberSize.IsChecked==true;s.RememberPopoutLocation=rememberLocation.IsChecked==true;s.PopoutSizePerTimer=sizePerTimer.IsChecked==true;s.PopoutLocationPerTimer=locationPerTimer.IsChecked==true;
            startup.Enabled=startupEnabled.IsChecked==true;startup.Volume=startupVolume.Value;
            string? imported=null;
            try{
                if(startup.Enabled&&string.IsNullOrWhiteSpace(startup.Path))throw new InvalidOperationException("Choose a startup sound or disable startup playback.");
                if(importSound&&startup.Path is not null)AlertAudio.Validate(startup.Path);
                var soundToSave=Appearance.Copy(startup);
                if(importSound&&startup.Path is not null){imported=Assets.Import(startup.Path,false);soundToSave.Path=imported;}
                var candidate=new Workspace{Id=Engine.Workspace.Id,Name=Engine.Workspace.Name,Settings=s};store.SaveSettingsPreferences(candidate,soundToSave);
            }catch(Exception ex){if(imported is not null){try{System.IO.File.Delete(imported);}catch{}}MessageBox.Show(d,ex.Message,"Settings were not applied");return;}
            Engine.Workspace.Settings=s;
            if(resetSizes.IsChecked==true){foreach(var timer in Engine.Workspace.Timers){timer.Width=null;timer.Height=null;}d.Tag=true;}
            if(startup.Enabled&&!System.IO.File.Exists(startup.Path))Notify("Startup sound file is missing. Startup playback will be skipped until you choose an available sound.",true,"startup-sound",true);
            else ClearNotice("startup-sound");
            d.DialogResult=true;
        }),("Cancel",()=>d.Close()));
        foreach(var field in new FrameworkElement[]{exit,round,display,columns,rows}){field.Width=220;field.HorizontalAlignment=HorizontalAlignment.Left;}
        foreach(var label in d.Fields.Children.OfType<TextBlock>())label.Margin=new Thickness(0,6,0,3);
        foreach(var input in d.Fields.Children.OfType<ComboBox>())input.Padding=new Thickness(5);
        foreach(var input in d.Fields.Children.OfType<TextBox>())input.Padding=new Thickness(5);
        d.TwoColumns(split);return d;
    }
}
