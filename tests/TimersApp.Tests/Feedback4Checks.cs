using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using TimersApp.Core;
using TimersApp.Desktop;

internal static class Feedback4Checks
{
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern nint SendMessage(nint window,int message,nint wParam,nint lParam);
    private static void Move(Window window,Action change,bool pump=false)
    {
        nint handle=new System.Windows.Interop.WindowInteropHelper(window).Handle;
        SendMessage(handle,0x0231,0,0);change();window.UpdateLayout();
        if(pump)Dispatcher.CurrentDispatcher.Invoke(()=>{},DispatcherPriority.Loaded);
        SendMessage(handle,0x0232,0,0);
    }
    private static void Require(bool value,string message){if(!value)throw new Exception(message);}
    private static void Near(double a,double b,string message,double tolerance=.2){Require(Math.Abs(a-b)<tolerance,$"{message}: {a} vs {b}");}
    internal static void Run(Action<string,Action> test,string artifacts)
    {
        test("Pop-out geometry resolves independent off/shared/per-timer policies without erasing memory",()=>
        {
            var w=new Workspace();var a=new TimerRecord();var b=new TimerRecord();w.Timers.AddRange([a,b]);
            Require(PopoutPlacementPolicy.Resolve(w,a).Width is null,"New timer has false saved size");
            var first=new PopoutGeometry{Left=100,Top=110,Width=600,Height=350};
            PopoutPlacementPolicy.Capture(w,a,first,true,true);
            var second=new PopoutGeometry{Left=200,Top=210,Width=800,Height=450};
            PopoutPlacementPolicy.Capture(w,b,second,true,true);
            w.SharedPopoutGeometry=new(){Left=300,Top=310,Width=900,Height=550};
            foreach(int size in new[]{0,1,2})foreach(int location in new[]{0,1,2})
            {
                var s=w.Settings;s.RememberPopoutSize=size>0;s.PopoutSizePerTimer=size==1;s.RememberPopoutLocation=location>0;s.PopoutLocationPerTimer=location==1;
                var resolved=PopoutPlacementPolicy.Resolve(w,a);
                Require(resolved.Width==(size==0?null:size==1?600d:900d),"Size policy mismatch");
                Require(resolved.Left==(location==0?null:location==1?100d:300d),"Location policy mismatch");
            }
            w.Settings.RememberPopoutSize=false;w.Settings.RememberPopoutLocation=false;PopoutPlacementPolicy.Capture(w,a,second,true,true);
            Require(w.SharedPopoutGeometry.Width==900&&a.Popout.Width==600,"Disabled policy overwrote memory");
            w.Settings.RememberPopoutSize=w.Settings.RememberPopoutLocation=true;w.Settings.PopoutSizePerTimer=w.Settings.PopoutLocationPerTimer=false;
            PopoutPlacementPolicy.Capture(w,b,second,true,true);PopoutPlacementPolicy.Capture(w,a,first,false,false,true);
            Require(w.SharedPopoutGeometry.Width==800&&w.SharedPopoutGeometry.Left==200,"Old opening/close overwrote shared memory");
            var snapshot=Appearance.Copy(w);w.SharedPopoutGeometry.Width=2800;w.SharedPopoutGeometry.Height=1600;
            var store=new WorkspaceStore(Path.Combine(artifacts,"notes4-geometry-"+Guid.NewGuid().ToString("N")));store.Save(w);var loaded=store.Load()!;
            Require(loaded.SharedPopoutGeometry.Width==2800&&loaded.SharedPopoutGeometry.Height==1600,"Large size truncated");
            Require(snapshot.SharedPopoutGeometry.Width==800,"Geometry copy aliases original");
            var malformed=new PopoutGeometry{Left=double.NaN,Top=2,Width=4,Height=null};malformed.Validate();Require(malformed.Left is null&&malformed.Width is null,"Incomplete/nonfinite pairs retained");
        });
    }
    private static void Idle(){Dispatcher.CurrentDispatcher.Invoke(()=>{},DispatcherPriority.ContextIdle);}
    private static void Arrange(FrameworkElement element)
    {
        element.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));element.Arrange(new Rect(new Point(),element.DesiredSize));element.UpdateLayout();
    }
    internal static void Layout(MainWindow host,string artifacts)
    {
        var original=Appearance.Copy(host.Engine.Workspace.Settings);var s=host.Engine.Workspace.Settings;
        try
        {
            foreach(bool list in new[]{false,true})
            {
                s.ListLayout=list;s.AllowIndividualResize=false;s.TimerScale=.65;
                var t=new TimerRecord{Name="Scale boundary",Description="Description remains part of the uniformly scaled scene"};
                var baseline=new TimerCardPresenter(new TimerCard(host,t));baseline.Configure(s,1000);Arrange(baseline);
                double width=baseline.DesiredSize.Width,height=baseline.DesiredSize.Height;
                foreach(double scale in new[]{.64,.5,.25})
                {
                    s.TimerScale=scale;var p=new TimerCardPresenter(new TimerCard(host,t));p.Configure(s,1000);Arrange(p);
                    Near(width*scale/.65,p.DesiredSize.Width,"Whole card width");Near(height*scale/.65,p.DesiredSize.Height,"Whole card height");
                    var time=Feedback3Checks.Descendants<TimeReadout>(p).Single();var ink=time.TransformToAncestor(p).TransformBounds(VisualTreeHelper.GetDrawing(time)!.Bounds);
                    Require(ink.Left>=-.1&&ink.Top>=-.1&&ink.Right<=p.DesiredSize.Width+.1&&ink.Bottom<=p.DesiredSize.Height+.1,"Transformed ink clipped");
                    Feedback3Checks.Render(p,$"notes4-{(list?"list":"grid")}-{scale*100:0}.png",artifacts);
                }
            }
            s.ListLayout=true;s.TimerScale=1.5;var row=new TimerCard(host,new(){Name=new string('T',100),Description="A description should remain visible even when only part fits."});row.ConfigureWorkspace(1100);Arrange(row);
            Require(row.DescriptionVisible,"Description removed at larger scale");
            var description=Feedback3Checks.Descendants<TextBlock>(row).Single(x=>x.Text.StartsWith("A description"));Require(description.ActualWidth>20&&description.TextTrimming==TextTrimming.CharacterEllipsis,"Description has no allocation/ellipsis");
            Feedback3Checks.Render(row,"notes4-list-description.png",artifacts);
        }
        finally{host.Engine.Workspace.Settings=original;host.RefreshWorkspaceView();}
    }
    private static void WindowNear(double a,double b,string message)=>Near(a,b,message,1);
    internal static void Windows(MainWindow host,string artifacts)
    {
        var s=host.Engine.Workspace.Settings;s.AutoSave=false;s.ExitChoice=ExitChoice.Save;s.TimerScale=1;s.ListLayout=false;host.RefreshWorkspaceView();host.UpdateLayout();Idle();
        var center=(FrameworkElement)host.FindName("ToolbarCenter");var square=(Button)host.FindName("SizeButton");
        WindowNear(center.ActualHeight,square.ActualHeight,"Square height");WindowNear(square.ActualHeight,square.ActualWidth,"Square width");
        var commands=(Panel)host.FindName("CommandButtons");var delete=(Button)host.FindName("DeleteWorkspaceButton");
        var last=commands.Children.OfType<Button>().Last();WindowNear(last.TransformToAncestor(center).Transform(new Point(last.ActualWidth,0)).X,delete.TransformToAncestor(center).Transform(new Point(delete.ActualWidth,0)).X,"Toolbar right edges");
        Require(commands.Children.OfType<Button>().Select(x=>Math.Round(x.ActualWidth,1)).Distinct().Count()==1,"Command widths unequal");
        Feedback3Checks.Render((FrameworkElement)host.Content,"notes4-main.png",artifacts);
        double originalWidth=host.Width;host.Width=520;host.UpdateLayout();Idle();
        Require(Grid.GetRow((FrameworkElement)host.FindName("ToolbarSquares"))==1,"Narrow toolbar failed to wrap squares");
        Require(((ScrollViewer)host.FindName("ToolbarScroll")).ExtentWidth>=center.ActualWidth,"Narrow toolbar commands not scrollable");
        Feedback3Checks.Render((FrameworkElement)host.Content,"notes4-main-narrow.png",artifacts);host.Width=originalWidth;host.UpdateLayout();Idle();
        var timer=host.Engine.Workspace.Timers[0];timer.SeparatePopoutStyle=true;timer.PopoutStyle=new();timer.Popout=new(){Width=420,Height=340};
        host.Engine.Start(timer);host.PopOut(timer);Idle();
        var window=Application.Current.Windows.Cast<Window>().Single(x=>x.Content is TimerCard&&x.IsVisible);var card=(TimerCard)window.Content;
        foreach(var size in new[]{new Size(900,180),new Size(300,180),new Size(420,340)})
        {
            double oldMin=((TimerCard)window.Content).MinHeight;
            Move(window,()=>{window.Width=size.Width;window.Height=size.Height;});Idle();window.UpdateLayout();
            Require(card.HorizontalPresentation==(size.Width==900),"Per-timer threshold did not switch layout");
            if(size.Height==180)Require(Math.Abs(oldMin-((TimerCard)window.Content).MinHeight)>.01,"Per-timer threshold did not adjust minimum");
            WindowNear(window.ActualWidth,timer.Popout.Width??0,"Threshold resize width not remembered");
            WindowNear(window.ActualHeight,timer.Popout.Height??0,"Threshold resize height not remembered");
        }
        Rect Ink()=>Feedback3Checks.Descendants<TimeReadout>(card).Single().TransformToAncestor(card).TransformBounds(VisualTreeHelper.GetDrawing(Feedback3Checks.Descendants<TimeReadout>(card).Single())!.Bounds);
        Rect Toggle()=>Feedback3Checks.Descendants<Button>(card).Single(x=>System.Windows.Automation.AutomationProperties.GetAutomationId(x)=="TimerToggle").TransformToAncestor(card).TransformBounds(new Rect(0,0,32,32));
        window.UpdateLayout();var small=Ink();var smallButton=Toggle();window.Width=840;window.Height=680;window.UpdateLayout();Idle();window.UpdateLayout();
        var large=Ink();var largeButton=Toggle();Require(large.Height>small.Height*1.3,"Digits failed to grow");WindowNear(large.Height/small.Height,largeButton.Height/smallButton.Height,"Play/stop scale differs from digits");
        Feedback3Checks.Render(card,"notes4-popout-large.png",artifacts);
        window.Width=1000;window.Height=190;window.UpdateLayout();card.Refresh();window.UpdateLayout();Idle();
        Require(card.HorizontalPresentation,"Wide pop-out layout missing");Feedback3Checks.Render(card,"notes4-popout-wide.png",artifacts);
        var mainCard=Feedback3Checks.Descendants<TimerCard>((DependencyObject)host.FindName("Cards")).First();
        var badge=Feedback3Checks.Descendants<Button>(mainCard).Single(x=>Equals(x.Content,"↙ Popped out"));Require(badge.Visibility==Visibility.Visible,"Missing return badge");badge.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Require(!host.IsPoppedOut(timer)&&timer.IsRunning,"Return changed running state or left pop-out");
        timer.PopoutStyle=TimerStyle.Minimal();s.PopoutSizePerTimer=false;s.PopoutLocationPerTimer=false;host.Engine.Workspace.SharedPopoutGeometry=new(){Width=480,Height=350,Left=100,Top=100};host.PopOut(timer);Idle();
        window=Application.Current.Windows.Cast<Window>().Single(x=>x.Content is TimerCard&&x.IsVisible);WindowNear(480,window.ActualWidth,"Shared size not restored");
        var sessions=(Dictionary<Guid,PopoutSession>)typeof(MainWindow).GetField("popoutSessions",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(host)!;
        timer.PopoutStyle=new();host.AppearanceChanged(false);Idle();
        foreach(var size in new[]{new Size(900,180),new Size(300,180)})
        {
            double oldMin=((TimerCard)window.Content).MinHeight;
            Move(window,()=>{window.Width=size.Width;window.Height=size.Height;},pump:true);Idle();window.UpdateLayout();
            Require(((TimerCard)window.Content).HorizontalPresentation==(size.Width==900),"Shared threshold did not switch layout");
            Require(Math.Abs(oldMin-((TimerCard)window.Content).MinHeight)>.01,"Shared threshold did not adjust minimum");
            WindowNear(window.ActualWidth,host.Engine.Workspace.SharedPopoutGeometry.Width??0,"Shared threshold resize width lost");
            WindowNear(window.ActualHeight,host.Engine.Workspace.SharedPopoutGeometry.Height??0,"Shared threshold resize height lost");
        }
        timer.PopoutStyle=TimerStyle.Minimal();host.AppearanceChanged(false);Idle();
        Move(window,()=>{sessions[timer.Id].Suppress(()=>{});window.Width=680;});Idle();
        WindowNear(window.ActualWidth,host.Engine.Workspace.SharedPopoutGeometry.Width??0,"User resize during suppression lost");
        Move(window,()=>{window.Width=620;window.Left=130;});
        WindowNear(620,host.Engine.Workspace.SharedPopoutGeometry.Width??0,"Resize not captured");
        sessions[timer.Id].Suppress(()=>window.Width=660);window.UpdateLayout();Idle();
        WindowNear(620,host.Engine.Workspace.SharedPopoutGeometry.Width??0,"Automatic correction overwrote saved size");
        window.Left=140;window.UpdateLayout();Idle();WindowNear(140,host.Engine.Workspace.SharedPopoutGeometry.Left??0,"Position change outside a drag not captured");
        var settings=host.CreateSettingsDialog();Exception? settingsError=null;
        settings.Dispatcher.BeginInvoke(DispatcherPriority.Loaded,new Action(()=>{try{
            settings.UpdateLayout();sessions[timer.Id].BeginMove();window.Left=160;window.UpdateLayout();sessions[timer.Id].EndMove();
            Feedback3Checks.Descendants<Button>(settings).Single(x=>Equals(x.Content,"Apply")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }catch(Exception ex){settingsError=ex;settings.Close();}}));
        Require(settings.ShowDialog()==true&&settingsError is null,"Settings Apply failed: "+settingsError);
        WindowNear(160,host.Engine.Workspace.SharedPopoutGeometry.Left??0,"Settings Apply lost live shared location");
        host.Return(timer);host.PopOut(timer);Idle();window=Application.Current.Windows.Cast<Window>().Single(x=>x.Content is TimerCard&&x.IsVisible);WindowNear(620,window.ActualWidth,"Reopen lost shared size");WindowNear(160,window.Left,"Reopen lost shared location");
        var other=host.Engine.Workspace.Timers[1];other.SeparatePopoutStyle=true;other.PopoutStyle=TimerStyle.Minimal();host.PopOut(other);Idle();
        var newer=Application.Current.Windows.Cast<Window>().Single(x=>x.Content is TimerCard&&x.IsVisible&&x!=window);
        sessions[other.Id].BeginMove();newer.Width=700;newer.Left=180;newer.UpdateLayout();sessions[other.Id].EndMove();
        host.Return(timer);WindowNear(700,host.Engine.Workspace.SharedPopoutGeometry.Width??0,"Closing older window overwrote newer size");WindowNear(180,host.Engine.Workspace.SharedPopoutGeometry.Left??0,"Closing older window overwrote newer location");
        sessions[other.Id].BeginMove();newer.Width=720;newer.UpdateLayout();
        typeof(MainWindow).GetMethod("Save",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.Invoke(host,[false,false]);
        var persisted=new WorkspaceStore(Environment.GetEnvironmentVariable("TIMERSAPP_DATA_DIR")).Load()!;WindowNear(720,persisted.SharedPopoutGeometry.Width??0,"Snapshot missed pending geometry");sessions[other.Id].EndMove();host.Return(other);
        s=host.Engine.Workspace.Settings;s.RememberPopoutSize=false;s.RememberPopoutLocation=false;s.PopoutScale=1;s.LinkPopoutScale=false;
        host.PopOut(timer);Idle();window=Application.Current.Windows.Cast<Window>().Single(x=>x.Content is TimerCard&&x.IsVisible);WindowNear(390,window.ActualWidth,"Disabled memory ignored fallback");host.Return(timer);WindowNear(720,host.Engine.Workspace.SharedPopoutGeometry.Width??0,"Disabled memory erased saved size");
        s.RememberPopoutSize=s.RememberPopoutLocation=s.PopoutSizePerTimer=s.PopoutLocationPerTimer=true;host.Engine.Stop(timer);
    }
}
