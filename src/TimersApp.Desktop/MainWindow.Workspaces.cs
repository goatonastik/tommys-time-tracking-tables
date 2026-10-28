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
    private readonly HashSet<string> dismissedNotices=[];
    private string? toastCondition;
    internal void ClearNotice(string condition){noticeConditions.Remove(condition);dismissedNotices.Remove(condition);if(toastCondition==condition)HideToast(null,EventArgs.Empty);}
    internal void Notify(string text, bool persistent = false,string? condition=null,bool force=false)
    {
        if(condition is not null){bool same=noticeConditions.TryGetValue(condition,out var previous)&&previous==text;
            if(!force&&same&&(dismissedNotices.Contains(condition)||(toastCondition==condition&&Toast.Visibility==Visibility.Visible)))return;
            if(force||!same)dismissedNotices.Remove(condition);noticeConditions[condition]=text;}
        toastCondition=condition;
        ToastText.Text=text; Toast.Visibility=Visibility.Visible; toastTimer.Stop();
        var peer=System.Windows.Automation.Peers.UIElementAutomationPeer.FromElement(ToastText)??System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(ToastText);peer?.RaiseAutomationEvent(System.Windows.Automation.Peers.AutomationEvents.LiveRegionChanged);
        toastTimer.Tick -= HideToast; toastTimer.Tick += HideToast;
        Toast.MouseLeftButtonDown -= DismissToast; Toast.MouseLeftButtonDown += DismissToast;
        if(!persistent)toastTimer.Start();
    }
    private void HideToast(object? sender, EventArgs e){toastTimer.Stop();Toast.Visibility=Visibility.Collapsed;}
    private void DismissCurrentNotice(){if(toastCondition is not null)dismissedNotices.Add(toastCondition);HideToast(null,EventArgs.Empty);}
    private void DismissToast(object sender,System.Windows.Input.MouseButtonEventArgs e)=>DismissCurrentNotice();
    private void DismissToast_Click(object sender,RoutedEventArgs e)=>DismissCurrentNotice();
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
}
