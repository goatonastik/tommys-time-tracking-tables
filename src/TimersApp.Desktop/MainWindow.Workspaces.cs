using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TimersApp.Core;

namespace TimersApp.Desktop;
public partial class MainWindow
{
    private bool selectingWorkspace;
    private readonly DispatcherTimer toastTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly Dictionary<string,string> noticeConditions=[];
    private string? toastCondition;
    internal void ClearNotice(string condition){noticeConditions.Remove(condition);if(toastCondition==condition)HideToast(null,EventArgs.Empty);}
    internal void Notify(string text, bool persistent = false,string? condition=null,bool force=false)
    {
        if(condition is not null){if(!force&&noticeConditions.TryGetValue(condition,out var previous)&&previous==text)return;noticeConditions[condition]=text;}
        toastCondition=condition;
        ToastText.Text=text; Toast.Visibility=Visibility.Visible; toastTimer.Stop();
        var peer=System.Windows.Automation.Peers.UIElementAutomationPeer.FromElement(ToastText)??System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(ToastText);peer?.RaiseAutomationEvent(System.Windows.Automation.Peers.AutomationEvents.LiveRegionChanged);
        toastTimer.Tick -= HideToast; toastTimer.Tick += HideToast;
        Toast.MouseLeftButtonDown -= DismissToast; Toast.MouseLeftButtonDown += DismissToast;
        if(!persistent)toastTimer.Start();
    }
    private void HideToast(object? sender, EventArgs e){toastTimer.Stop();Toast.Visibility=Visibility.Collapsed;}
    private void DismissToast(object sender,System.Windows.Input.MouseButtonEventArgs e)=>HideToast(null,EventArgs.Empty);
    private void DismissToast_Click(object sender,RoutedEventArgs e)=>HideToast(null,EventArgs.Empty);
    private void RefreshWorkspacePicker()
    {
        selectingWorkspace=true;
        try { var entries=store.SavedWorkspaces().ToList(); if(entries.All(x=>x.Id!=Engine.Workspace.Id))entries.Add(new(Engine.Workspace.Id,Engine.Workspace.Name,false)); WorkspacePicker.ItemsSource=entries;WorkspacePicker.SelectedValue=Engine.Workspace.Id; }
        finally { selectingWorkspace=false; }
    }
    private string? AskWorkspaceName(string title,string initial,bool rename=false)
    {
        var d=new FormDialog(this,title);d.Label("Workspace name");var name=d.Text(initial);
        d.Actions(("Save",()=>{string text=name.Text.Trim();if(text.Length is <1 or >100 || (!rename&&Engine.Workspace.Name.Equals(text,StringComparison.OrdinalIgnoreCase)) || store.SavedWorkspaces().Any(x=>(!rename||x.Id!=Engine.Workspace.Id)&&x.Name.Equals(text,StringComparison.OrdinalIgnoreCase))){MessageBox.Show(d,"Use a unique name with 1–100 characters.");return;}d.DialogResult=true;}),("Cancel",()=>d.Close()));return d.ShowDialog()==true?name.Text.Trim():null;
    }
    private Workspace? PrepareLeaving(out bool discarded)
    {
        discarded=false;
        Engine.Checkpoint();
        bool running=Engine.Workspace.Timers.Any(x=>x.IsRunning);
        if(running || !Engine.Workspace.Settings.AutoSave)
        {
            var d=new FormDialog(this,"Switch workspace");d.Label("Only the selected workspace runs. Saving and switching stops its timers at the switch; returning will not add time spent away.");
            d.Label("Discard restores this workspace to how it was when you entered it.");ExitChoice choice=ExitChoice.Ask;
            d.Actions(("Save and switch",()=>{choice=ExitChoice.Save;d.DialogResult=true;}),("Discard and switch",()=>{choice=ExitChoice.Discard;d.DialogResult=true;}),("Cancel",()=>d.Close()));if(d.ShowDialog()!=true)return null;
            discarded=choice==ExitChoice.Discard;
            Engine.Checkpoint();return WorkspaceStore.StoppedCopy(discarded?ExitWorkspace.Prepare(Engine.Workspace,baseline,ExitChoice.Discard,false):Engine.Workspace);
        }
        return WorkspaceStore.StoppedCopy(Engine.Workspace);
    }
    private bool Transition(Workspace target,bool delete=false)
    {
        try
        {
            bool discarded=false;var leaving=delete?WorkspaceStore.StoppedCopy(Engine.Workspace):PrepareLeaving(out discarded);if(leaving is null)return false;
            target=WorkspaceStore.StoppedCopy(target);
            CarryLaunchPreferences(leaving,target);
            store.CommitTransition(leaving,target,delete,!scratchWorkspace||!discarded&&(Engine.Workspace.Timers.Count>0||scratchHasEdits));
            scratchWorkspace=false;baseline=WorkspaceStore.Copy(target);ReplaceWorkspace(target);RefreshWorkspacePicker();return true;
        }
        catch(Exception ex){MessageBox.Show(this,ex.Message,"Workspace change failed");return false;}
    }
    internal static void CarryLaunchPreferences(Workspace source,Workspace target)
    {
        target.Settings.AutoSave=source.Settings.AutoSave;target.Settings.RestoreOnLaunch=source.Settings.RestoreOnLaunch;target.Settings.ExitChoice=source.Settings.ExitChoice;
    }
    private void Workspace_Selected(object sender,SelectionChangedEventArgs e)
    {
        if(!ready||selectingWorkspace||WorkspacePicker.SelectedItem is not WorkspaceInfo info||info.Id==Engine.Workspace.Id)return;
        try { Transition(store.LoadWorkspace(info.Id)); } catch(Exception ex){MessageBox.Show(this,ex.Message,"Could not load workspace");}finally{RefreshWorkspacePicker();}
    }
    private void NewWorkspace_Click(object sender,RoutedEventArgs e)
    {
        string? name=AskWorkspaceName("New workspace","New workspace");if(name is not null)Transition(new Workspace{Name=name});
    }
    private void RenameWorkspace_Click(object sender,RoutedEventArgs e)
    {
        string? name=AskWorkspaceName("Rename workspace",Engine.Workspace.Name,true);if(name is null)return;
        string old=Engine.Workspace.Name;Engine.Workspace.Name=name;if(!Save(true))Engine.Workspace.Name=old;ApplySettings();
    }
    private void DuplicateWorkspace_Click(object sender,RoutedEventArgs e)
    {
        string? name=AskWorkspaceName("Duplicate workspace",Engine.Workspace.Name+" copy");if(name is null)return;
        Engine.Checkpoint();var copy=WorkspaceStore.StoppedCopy(Engine.Workspace);copy.Id=Guid.NewGuid();copy.Name=name;Transition(copy);
    }
    private void DeleteWorkspace_Click(object sender,RoutedEventArgs e)
    {
        if(MessageBox.Show(this,$"Delete '{Engine.Workspace.Name}'? A recoverable copy will remain in Backups.","Delete workspace",MessageBoxButton.OKCancel)!=MessageBoxResult.OK)return;
        try{Engine.Checkpoint();var next=store.SavedWorkspaces().FirstOrDefault(x=>x.Id!=Engine.Workspace.Id);Transition(next is null?new Workspace():store.LoadWorkspace(next.Id),true);}catch(Exception ex){MessageBox.Show(this,ex.Message,"Delete failed");}
    }
    private void Settings_Click(object sender,RoutedEventArgs e)
    {
        var original=Engine.Workspace.Settings;var s=Appearance.Copy(original);var d=new FormDialog(this,"Workspace settings",560);
        var autosave=d.Check("Automatically save timers and settings",s.AutoSave);var restore=d.Check("Restore saved workspace on launch",s.RestoreOnLaunch);var sleep=d.Check("Pause timers while the PC sleeps",s.PauseDuringSleep);var status=d.Check("Show status bar",s.ShowStatusBar);var resize=d.Check("Allow individual timer resizing",s.AllowIndividualResize);
        d.Label("Application scale");var appScale=new ScaleInput{Value=s.AppScale,Label="Application scale percent"};d.Fields.Children.Add(appScale);appScale.ValueChanged+=(_,_)=>{AppTransform.ScaleX=AppTransform.ScaleY=appScale.Value;};var resetApp=new Button{Content="Reset application scale to 100%"};resetApp.Click+=(_,_)=>{appScale.Value=1;AppTransform.ScaleX=AppTransform.ScaleY=1;};d.Fields.Children.Add(resetApp);d.Closed+=(_,_)=>{AppTransform.ScaleX=AppTransform.ScaleY=Engine.Workspace.Settings.AppScale;};
        d.Label("Exit decision");var exit=d.Select(Enum.GetValues<ExitChoice>(),s.ExitChoice);d.Label("Round to nearest (minutes)");var round=d.Select(new[]{1,15,60},s.RoundMinutes);d.Label("Total display");var display=d.Select(Enum.GetValues<TotalDisplay>(),s.TotalDisplay);
        d.Label("Grid columns");var columns=d.Select(Enumerable.Range(1,8),s.Columns);var automatic=d.Check("Automatically fit columns",s.AutoColumns);d.Label("Minimum grid rows (0 = automatic)");var rows=d.Text(s.Rows.ToString());
        NumberSlider Slider(string label,double min,double max,double value){var control=new NumberSlider(label,min,max,value);d.Fields.Children.Add(control);return control;}
        var gap=Slider("Space between timers",0,80,s.Spacing);var width=Slider("Card width",250,1200,s.CardWidth);var height=Slider("Default card height",220,1200,s.CardHeight);var list=Slider("List row height",48,300,s.ListRowHeight);
        var resetSizes=d.Check("Forget saved individual timer sizes",false);
        var names=new Button{Content="Timer name suggestions",Margin=new Thickness(0,12,0,0)};names.Click+=(_,_)=>ManageNames(d);d.Fields.Children.Add(names);
        d.Actions(("Reset to defaults",()=>{var defaults=new AppSettings();autosave.IsChecked=defaults.AutoSave;restore.IsChecked=defaults.RestoreOnLaunch;sleep.IsChecked=defaults.PauseDuringSleep;status.IsChecked=false;exit.SelectedItem=defaults.ExitChoice;round.SelectedItem=defaults.RoundMinutes;display.SelectedItem=defaults.TotalDisplay;columns.SelectedItem=defaults.Columns;automatic.IsChecked=true;rows.Text="0";gap.Value=defaults.Spacing;width.Value=defaults.CardWidth;height.Value=defaults.CardHeight;list.Value=defaults.ListRowHeight;appScale.Value=1;AppTransform.ScaleX=AppTransform.ScaleY=1;resize.IsChecked=false;}),("Apply",()=>{
            if(!int.TryParse(rows.Text,out int n)||n<0||n>100){MessageBox.Show(d,"Use 0–100 rows.");return;}
            s.AutoSave=autosave.IsChecked==true;s.RestoreOnLaunch=restore.IsChecked==true;s.PauseDuringSleep=sleep.IsChecked==true;s.ShowStatusBar=status.IsChecked==true;s.ExitChoice=(ExitChoice)exit.SelectedItem;s.RoundMinutes=(int)round.SelectedItem;s.TotalDisplay=(TotalDisplay)display.SelectedItem;s.Columns=(int)columns.SelectedItem;s.AutoColumns=automatic.IsChecked==true;s.Rows=n;s.Spacing=gap.Value;s.CardWidth=width.Value;s.CardHeight=height.Value;s.ListRowHeight=list.Value;s.AppScale=appScale.Value;s.AllowIndividualResize=resize.IsChecked==true;
            s.SaveNameSuggestions=Engine.Workspace.Settings.SaveNameSuggestions;
            Engine.Workspace.Settings=s;try{Preferences();}catch(Exception ex){Engine.Workspace.Settings=original;MessageBox.Show(d,ex.Message);return;}
            if(resetSizes.IsChecked==true)foreach(var timer in Engine.Workspace.Timers){timer.Width=null;timer.Height=null;}d.DialogResult=true;
        }),("Cancel",()=>d.Close()));if(d.ShowDialog()==true){ApplySettings();Changed();}
    }
}
