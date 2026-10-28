using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Data.Sqlite;
using TimersApp.Core;
using TimersApp.Desktop;

internal static class Feedback3Checks
{
    internal static void Run(Action<string,Action> test,string artifacts)
    {
        test("Startup sound defaults off and preference writes roll back together",()=>
        {
            var folder=Path.Combine(artifacts,"startup-"+Guid.NewGuid().ToString("N"));var store=new WorkspaceStore(folder);
            if(store.LoadStartupSound().Enabled)throw new Exception("Startup enabled by default");
            var w=new Workspace();store.SaveSettingsPreferences(w,new(){Enabled=true,Path="managed.wav",Name="Chime",Volume=.4});
            if(store.LoadStartupSound().Path!="managed.wav"||store.Load("preferences")!.Settings.ExitChoice!=ExitChoice.Ask)throw new Exception("Preference roundtrip failed");
            using var db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=Path.Combine(folder,"workspace.db")}.ToString());db.Open();using var c=db.CreateCommand();
            c.CommandText="CREATE TRIGGER reject_startup BEFORE INSERT ON app_options BEGIN SELECT RAISE(ABORT,'injected failure'); END";c.ExecuteNonQuery();
            w.Settings.ExitChoice=ExitChoice.Discard;bool failed=false;try{store.SaveSettingsPreferences(w,new());}catch(SqliteException){failed=true;}
            if(!failed||store.Load("preferences")!.Settings.ExitChoice!=ExitChoice.Ask||!store.LoadStartupSound().Enabled)throw new Exception("Partial preference commit");
        });
        test("One-shot startup playback stays independent and has no fallback",()=>
        {
            Exception? failure=null;var thread=new Thread(()=>{try{
                var outputs=new List<Feedback2Checks.FakeOutput>();var errors=new List<string>();using var audio=new AlertAudio(Path.Combine(artifacts,"startup-audio"),errors.Add,()=>{var x=new Feedback2Checks.FakeOutput();outputs.Add(x);return x;});
                var t=new TimerRecord{Alert=new(){PlaySound=true,RepeatSound=true},AlertState=new(){Active=true}};audio.Play(t);
                var id=Guid.NewGuid();audio.PlayOnce(id,audio.BuiltInSound(),.5);audio.StopTimers([t.Id]);
                if(!outputs[0].Disposed||outputs[1].Disposed)throw new Exception("Workspace stopped unrelated audio");
                var bytes=new byte[100000];if(outputs[1].Provider!.Read(bytes,0,bytes.Length)>=bytes.Length)throw new Exception("One-shot looped");audio.Stop(id);
                audio.PlayOnce(id,Path.Combine(artifacts,"missing-startup.mp3"),.5);
                if(outputs.Count!=2||errors.Count!=1)throw new Exception("Missing startup sound fell back");
            }catch(Exception ex){failure=ex;}});thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();if(failure is not null)throw failure;
        });
    }
    internal static IEnumerable<T> Descendants<T>(DependencyObject root) where T:DependencyObject
    {
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)
        {
            var child=VisualTreeHelper.GetChild(root,i);
            if(child is T value)yield return value;
            foreach(var nested in Descendants<T>(child))yield return nested;
        }
    }
    internal static void Layout(MainWindow host)
    {
        var s=host.Engine.Workspace.Settings;var saved=Appearance.Copy(s);
        try
        {
            foreach(bool list in new[]{true,false})foreach(double scale in new[]{.25,.5,.75,1,1.25,1.5})foreach(var digitStyle in Enum.GetValues<DigitStyle>())foreach(var totalDisplay in Enum.GetValues<TotalDisplay>())foreach(string font in new[]{"Consolas","Segoe UI","Georgia"})foreach(bool longFormat in new[]{false,true})
            {
                s.ListLayout=list;s.TimerScale=scale;s.AllowIndividualResize=false;s.DefaultStyle.Digits=digitStyle;s.TotalDisplay=totalDisplay;
                s.DefaultStyle.FontFamily=font;s.DefaultStyle.UseCustomFormat=longFormat;s.DefaultStyle.CustomFormat="Y:D:H:M:S.MS";
                s.DefaultStyle.CornerRadius=font=="Georgia"?20:0;
                var timer=new TimerRecord{Name="Measured timer",Description="Title and controls must not overlap"};timer.Segments.Add(new Segment{RunId=timer.RunId,Start=DateTimeOffset.Now,End=DateTimeOffset.Now,ElapsedTicks=TimeSpan.FromDays(1234.56789).Ticks});
                var card=new TimerCard(host,timer);card.ConfigureWorkspace(1000);
                for(int i=0;i<4;i++){card.Measure(new Size(card.Width,card.Height));card.Arrange(new Rect(0,0,card.Width,card.Height));card.UpdateLayout();card.Refresh();}
                var digits=Descendants<TimeReadout>(card).Single();
                Rect ink=digits.TransformToAncestor(card).TransformBounds(VisualTreeHelper.GetDrawing(digits)!.Bounds);
                if(ink.Top<-.1||ink.Left<-.1||ink.Bottom>card.Height+.1||ink.Right>card.Width+.1)
                    throw new Exception($"Time clipped: list={list}, scale={scale}, ink={ink}, card={card.Width}x{card.Height}");
                if(card.DigitHeight<23.99)throw new Exception("Readout fell below readable floor");
                foreach(var point in new[]{ink.TopLeft,ink.TopRight,ink.BottomLeft,ink.BottomRight})if(!card.Clip.FillContains(point))throw new Exception("Rounded clip cut time drawing");
                double oldHeight=card.MinHeight;for(int i=0;i<4;i++){card.Refresh();card.UpdateLayout();}if(Math.Abs(oldHeight-card.MinHeight)>.1)throw new Exception("Layout did not settle");
            }
        }
        finally{host.Engine.Workspace.Settings=saved;host.RefreshWorkspaceView();}
    }
    internal static void Settings(MainWindow host,string artifacts)
    {
        var before=Appearance.Copy(host.Engine.Workspace.Settings);
        var dialog=host.CreateSettingsDialog();dialog.Show();dialog.UpdateLayout();
        var first=Descendants<CheckBox>(dialog).First();var exit=Descendants<ComboBox>(dialog).First();
        if(exit.TransformToAncestor(dialog).Transform(new Point()).X<=first.TransformToAncestor(dialog).Transform(new Point()).X+200)throw new Exception("Settings did not use two columns");
        Render(dialog,"notes3-settings-wide.png",artifacts);
        dialog.Width=430;dialog.UpdateLayout();Render(dialog,"notes3-settings-narrow.png",artifacts);
        if(Math.Abs(exit.TransformToAncestor(dialog).Transform(new Point()).X-first.TransformToAncestor(dialog).Transform(new Point()).X)>2)throw new Exception("Settings did not reflow");
        var scale=Descendants<ScaleInput>(dialog).Single();var plus=scale.Children.OfType<Button>().Last();plus.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        dialog.Close();if(host.Engine.Workspace.Settings.AppScale!=before.AppScale)throw new Exception("Cancel retained settings draft");
        if(((ScaleTransform)host.FindName("AppTransform")).ScaleX!=before.AppScale)throw new Exception("Cancel retained scale preview");
        var root=(FrameworkElement)host.Content;
        var size=(Button)host.FindName("SizeButton");size.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));root.UpdateLayout();
        if(((UIElement)host.FindName("SizeOverlay")).Visibility!=Visibility.Visible)throw new Exception("Size panel did not open");
        Render(root,"notes3-size-panel.png",artifacts);
        size.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        host.Engine.Workspace.Settings.AppScale=3;host.RefreshWorkspaceView();root.UpdateLayout();size.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));root.UpdateLayout();
        var panel=(FrameworkElement)host.FindName("SizePanel");var bounds=panel.TransformToAncestor(root).TransformBounds(new Rect(0,0,panel.ActualWidth,panel.ActualHeight));
        if(bounds.Left<0||bounds.Top<0||bounds.Right>root.ActualWidth+.1||bounds.Bottom>root.ActualHeight+.1)throw new Exception("Size panel outside the window at 300% app scale");
        Render(root,"notes3-size-panel-high-scale.png",artifacts);size.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        host.Engine.Workspace.Settings.AppScale=before.AppScale;host.RefreshWorkspaceView();
        size.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));root.UpdateLayout();var percentage=(ScaleInput)host.FindName("TimerScaleInput");var edit=percentage.Children.OfType<TextBox>().Single();edit.Focus();double originalScale=host.Engine.Workspace.Settings.TimerScale;edit.Text="275";
        edit.RaiseEvent(new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice,PresentationSource.FromVisual(edit)!,Environment.TickCount,System.Windows.Input.Key.Escape){RoutedEvent=System.Windows.Input.Keyboard.PreviewKeyDownEvent});
        if(host.Engine.Workspace.Settings.TimerScale!=originalScale)throw new Exception("Escape committed pending percentage");
        size.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));double originalWidth=host.Width;host.Width=520;host.UpdateLayout();
        bounds=panel.TransformToAncestor(root).TransformBounds(new Rect(0,0,panel.ActualWidth,panel.ActualHeight));if(bounds.Right>root.ActualWidth+.1||bounds.Bottom>root.ActualHeight+.1)throw new Exception("Size overlay did not refit on resize");
        size.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));host.Width=originalWidth;host.UpdateLayout();
        var apply=host.CreateSettingsDialog();Exception? error=null;
        host.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded,new Action(()=>{try{
            Descendants<CheckBox>(apply).Single(x=>Equals(x.Content,"Fit card height to content")).IsChecked=false;
            Descendants<Button>(apply).Single(x=>Equals(x.Content,"Use compact default timer style")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Descendants<Button>(apply).Single(x=>Equals(x.Content,"Apply")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }catch(Exception ex){error=ex;apply.Close();}}));
        if(apply.ShowDialog()!=true||error is not null)throw new Exception("Settings Apply failed",error);
        if(!host.Engine.Workspace.Settings.FitCardHeight||host.Engine.Workspace.Settings.DefaultStyle.Padding!=0||host.Engine.Workspace.Settings.DefaultStyle.FontFamily!=before.DefaultStyle.FontFamily)throw new Exception("Compact settings changed custom font or did not apply");
        host.Engine.Workspace.Settings=before;host.RefreshWorkspaceView();
        var savedFolder=Environment.GetEnvironmentVariable("TIMERSAPP_DATA_DIR");
        try
        {
            string folder=Path.Combine(artifacts,"startup-launch-"+Guid.NewGuid().ToString("N"));Environment.SetEnvironmentVariable("TIMERSAPP_DATA_DIR",folder);
            var store=new WorkspaceStore(folder);var seed=new Workspace();seed.Settings.ExitChoice=ExitChoice.Save;store.Save(seed);store.SaveSettingsPreferences(seed,new(){Enabled=true,Path=Path.Combine(folder,"missing.wav")});
            var startup=new MainWindow();startup.Show();startup.UpdateLayout();startup.Dispatcher.Invoke(()=>{},System.Windows.Threading.DispatcherPriority.ContextIdle);var message=(TextBlock)startup.FindName("ToastText");
            if(!message.Text.Contains("Startup sound:"))throw new Exception("Missing startup sound did not notify");
            ((Button)startup.FindName("ToastDismiss")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            typeof(MainWindow).GetMethod("PlayStartupSound",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.Invoke(startup,null);
            if(((UIElement)startup.FindName("Toast")).Visibility==Visibility.Visible)throw new Exception("Startup playback retried");
            ApplyDialog(startup.CreateSettingsDialog());if(!store.LoadStartupSound().Enabled)throw new Exception("Unrelated Apply disabled missing startup sound");
            using(var db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=Path.Combine(folder,"workspace.db")}.ToString())){db.Open();using var c=db.CreateCommand();c.CommandText="UPDATE app_options SET payload='broken json' WHERE name='startup-sound'";c.ExecuteNonQuery();}
            ApplyDialog(startup.CreateSettingsDialog());if(store.LoadStartupSound().Enabled)throw new Exception("Corrupt preference was not repaired");if(((UIElement)startup.FindName("Toast")).Visibility==Visibility.Visible)throw new Exception("Repaired startup setting retained stale warning");startup.Close();
            string scratchFolder=Path.Combine(artifacts,"startup-scratch-"+Guid.NewGuid().ToString("N"));Environment.SetEnvironmentVariable("TIMERSAPP_DATA_DIR",scratchFolder);
            var scratchStore=new WorkspaceStore(scratchFolder);var scratchSeed=new Workspace();scratchSeed.Settings.RestoreOnLaunch=false;scratchSeed.Settings.AutoSave=false;scratchSeed.Settings.ExitChoice=ExitChoice.Save;scratchStore.Save(scratchSeed);
            var scratch=new MainWindow();scratch.Show();scratch.UpdateLayout();
            scratch.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded,new Action(()=>{var settings=Application.Current.Windows.Cast<Window>().Single(w=>w.Title=="Settings");Descendants<NumberSlider>(settings).First().Value=.25;Descendants<Button>(settings).Single(x=>Equals(x.Content,"Apply")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}));
            typeof(MainWindow).GetMethod("Settings_Click",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.Invoke(scratch,[scratch,new RoutedEventArgs()]);scratch.Close();
            if(scratchStore.SavedWorkspaces().Count!=1||scratchStore.LoadStartupSound().Volume!=.25)throw new Exception("Global-only settings created a scratch workspace");
        }
        finally{Environment.SetEnvironmentVariable("TIMERSAPP_DATA_DIR",savedFolder);}
        host.Notify("Save failure",true,"test-save");host.Notify("Another message");host.Notify("Save failure",true,"test-save");
        if(((TextBlock)host.FindName("ToastText")).Text!="Save failure")throw new Exception("Overwritten save notice remained suppressed");host.ClearNotice("test-save");
    }
    private static void ApplyDialog(FormDialog dialog)
    {
        Exception? error=null;dialog.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded,new Action(()=>{try{Descendants<Button>(dialog).Single(x=>Equals(x.Content,"Apply")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}catch(Exception ex){error=ex;dialog.Close();}}));
        if(dialog.ShowDialog()!=true||error is not null)throw new Exception("Dialog Apply failed",error);
    }
    internal static void Render(FrameworkElement element,string name,string artifacts)
    {
        var visual=element is Window window?(FrameworkElement)window.Content:element;
        int w=Math.Max(1,(int)Math.Ceiling(visual.ActualWidth)),h=Math.Max(1,(int)Math.Ceiling(visual.ActualHeight));
        var bitmap=new RenderTargetBitmap(w,h,96,96,PixelFormats.Pbgra32);
        if(element is Window owner){var backdrop=new DrawingVisual();using(var dc=backdrop.RenderOpen())dc.DrawRectangle(owner.Background,null,new Rect(0,0,w,h));bitmap.Render(backdrop);}
        bitmap.Render(visual);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(Path.Combine(artifacts,name));encoder.Save(file);
    }
}
