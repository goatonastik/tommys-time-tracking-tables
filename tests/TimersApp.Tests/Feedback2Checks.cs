using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ClosedXML.Excel;
using NAudio.Wave;
using TimersApp.Core;
using TimersApp.Desktop;
using Duration=TimersApp.Core.Duration;

internal static class Feedback2Checks
{
    private static void Equal<T>(T expected,T actual){if(!EqualityComparer<T>.Default.Equals(expected,actual))throw new Exception($"Expected {expected}, got {actual}");}
    public static void Run(Action<string,Action> test,string artifacts)
    {
        test("Custom sound choice survives switching to system sound and back",()=>
        {
            var draft=new AlertSettings();var custom=new SoundChoice("Custom: My alert.mp3",CustomPath:"C:/test/managed.mp3");var system=new SoundChoice("Windows: Notify","notify.wav");custom.Select(draft);system.Select(draft);Equal<string?>(null,draft.SoundPath);Equal("notify.wav",draft.SystemSound);custom.Select(draft);Equal("C:/test/managed.mp3",draft.SoundPath);Equal<string?>(null,draft.SystemSound);Equal("My alert.mp3",draft.SoundName);
        });
        test("Daily rounded hours aggregate segments once and distinguish same-name timers",()=>
        {
            var w=new Workspace();var t=new TimerRecord{Name="Same"};var other=new TimerRecord{Name="Same"};w.Timers.AddRange([t,other]);
            var start=new DateTimeOffset(2026,1,31,12,0,0,TimeSpan.Zero);
            for(int i=0;i<3;i++)t.Segments.Add(new Segment{Name=t.Name,Start=start.AddMinutes(i*10),End=start.AddMinutes(i*10+5),ElapsedTicks=TimeSpan.FromMinutes(5).Ticks});
            other.Segments.Add(new Segment{Name=other.Name,Start=start,End=start.AddMinutes(5),ElapsedTicks=TimeSpan.FromMinutes(5).Ticks});
            t.Segments.Add(new Segment{Name=t.Name,Start=start.AddDays(1),End=start.AddDays(1).AddMinutes(10),ElapsedTicks=TimeSpan.FromMinutes(10).Ticks});
            var rows=ExportTable.Build(w,new(null,null),TimeZoneInfo.Utc);Equal(5,rows.Count);Equal(3,rows.Count(x=>x.DailyHours is not null));Equal(.50m,rows.Sum(x=>x.DailyHours??0));
            Equal(.25m,rows.Single(x=>x.Portion.TimerId==t.Id&&x.Portion.Day==new DateOnly(2026,1,31)&&x.DailyHours is not null).DailyHours);
            Equal(0m,rows.Single(x=>x.Portion.TimerId==other.Id).DailyHours);Equal(2,rows.Count(x=>x.DailyHours is null));
        });
        test("Overnight export portions preserve exact allocation and short ID collisions",()=>
        {
            var w=new Workspace();var t=new TimerRecord();w.Timers.Add(t);var start=new DateTimeOffset(2026,1,31,23,50,0,TimeSpan.Zero);
            t.Segments.Add(new Segment{Id=Guid.Parse("abcdef01-0000-0000-0000-000000000001"),Start=start,End=start.AddMinutes(20),ElapsedTicks=TimeSpan.FromMinutes(20).Ticks});
            t.Segments.Add(new Segment{Id=Guid.Parse("abcdef01-0000-0000-0000-000000000002"),Start=start.AddHours(2),MeasuredThrough=start.AddHours(2).AddMinutes(5),ElapsedTicks=TimeSpan.FromMinutes(5).Ticks});
            var rows=ExportTable.Build(w,new(null,null),TimeZoneInfo.Utc);Equal(3,rows.Count);Equal(TimeSpan.FromMinutes(25).Ticks,rows.Sum(x=>x.Portion.Ticks));Equal(true,rows.All(x=>x.ShortIdCollision));Equal(true,rows[0].Status.Contains("partial segment"));
            var clipped=ExportTable.Build(w,new(new DateOnly(2026,2,1),new DateOnly(2026,2,1)),TimeZoneInfo.Utc);Equal(2,clipped.Count);Equal(.25m,clipped.Sum(x=>x.DailyHours??0));Equal(1,clipped.Count(x=>x.DailyHours is null));Equal(start,clipped[0].Portion.ActualStart);
        });
        test("Export modes ignore hidden filters and reject empty custom selection",()=>
        {
            var today=new DateOnly(2026,10,27);var all=ExportDialog.Options(ExportMode.All,today,"bad","bad",[],true);Equal<DateOnly?>(null,all.From);
            var current=ExportDialog.Options(ExportMode.Today,today,"bad","bad",[],true);Equal<DateOnly?>(today,current.From);Equal<DateOnly?>(today,current.Through);
            var id=Guid.NewGuid();var custom=ExportDialog.Options(ExportMode.Custom,today,"bad","bad",[id],false);Equal(id,custom.TimerIds!.Single());Equal<DateOnly?>(null,custom.From);
            bool rejected=false;try{ExportDialog.Options(ExportMode.Custom,today,"","",[],true);}catch(ArgumentException){rejected=true;}Equal(true,rejected);
            rejected=false;try{ExportDialog.Options(ExportMode.Range,today,"2026-10-28","2026-10-27",[],true);}catch(ArgumentException){rejected=true;}Equal(true,rejected);
        });
        test("Export headers, wrapped rows, fixed description width and numeric daily hours",()=>
        {
            var w=new Workspace{Name="Workbook check"};var t=new TimerRecord();w.Timers.Add(t);var start=new DateTimeOffset(2026,1,31,12,0,0,TimeSpan.Zero);
            for(int i=0;i<3;i++)t.Segments.Add(new Segment{Name="=A1",Description=string.Join("\n",Enumerable.Repeat(new string('W',90),4)),Start=start.AddMinutes(i*6),End=start.AddMinutes(i*6+5),ElapsedTicks=TimeSpan.FromMinutes(5).Ticks});
            string path=Path.Combine(artifacts,"notes2-export.xlsx");ExcelExport.Write(path,w,new(null,null));using var book=new XLWorkbook(path);var s=book.Worksheet("Recorded time");Equal(1,book.Worksheets.Count);Equal(9,s.ColumnsUsed().Count());Equal(4,s.RowsUsed().Count());Equal(45d,s.Column(4).Width);Equal(.25,s.Cell(2,2).GetDouble());Equal(true,s.Cell(3,2).IsEmpty());Equal(true,s.Cell(4,2).IsEmpty());Equal(false,s.Cell(2,3).HasFormula);Equal(8,s.Cell(2,9).GetString().Length);Equal(XLAlignmentHorizontalValues.Center,s.Cell(1,4).Style.Alignment.Horizontal);if(s.Row(2).Height<=80)throw new Exception($"Wrapped row height {s.Row(2).Height}");Equal("mmmm d, yyyy",s.Cell(2,5).Style.NumberFormat.Format);
        });
        test("WAV decoder and alert playback ownership need no media player",()=>
        {
            Exception? failure=null;var thread=new Thread(()=>{try{
                var outputs=new List<FakeOutput>();var errors=new List<string>();using var audio=new AlertAudio(Path.Combine(artifacts,"audio"),errors.Add,()=>{var output=new FakeOutput();outputs.Add(output);return output;});string sound=audio.BuiltInSound();AlertAudio.Validate(sound);
                var a=new TimerRecord{Alert=new(){PlaySound=true,RepeatSound=true,SoundPath=sound},AlertState=new(){Active=true}};var b=new TimerRecord{Alert=new(){PlaySound=true,SoundPath=sound}};
                audio.Play(a);audio.Play(b);Equal(2,outputs.Count);Equal(true,outputs.All(x=>x.State==PlaybackState.Playing));
                var bytes=new byte[100000];Equal(bytes.Length,outputs[0].Provider!.Read(bytes,0,bytes.Length));
                audio.Play(a);Equal(true,outputs[0].Disposed);Equal(false,outputs[1].Disposed);audio.Stop(a.Id);Equal(true,outputs[2].Disposed);audio.StopAll();Equal(true,outputs.All(x=>x.Disposed));Equal(0,errors.Count);
                a.Alert.SoundPath=Path.Combine(artifacts,"missing-audio.wav");audio.Play(a);Equal(1,errors.Count);Equal(PlaybackState.Playing,outputs[^1].State);audio.StopAll();
                string invalid=Path.Combine(artifacts,"invalid.wav");File.WriteAllText(invalid,"not wave");bool rejected=false;try{AlertAudio.Validate(invalid);}catch{rejected=true;}Equal(true,rejected);
                string mp3=Path.GetFullPath("artifacts/audio-fixtures/silence.mp3");if(File.Exists(mp3)){using var decoded=AlertAudio.Open(mp3);Equal(WaveFormatEncoding.IeeeFloat,decoded.WaveFormat.Encoding);var pcm=new byte[32768];Equal(true,decoded.Read(pcm,0,pcm.Length)>0);decoded.Position=0;Equal(true,decoded.Read(pcm,0,pcm.Length)>0);Console.WriteLine("Verified local MP3 fixture via managed decoder");}
            }catch(Exception ex){failure=ex;}});thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();if(failure is not null)throw failure;
        });
        test("Background audio reads stop safely even when the device reports a failure",()=>
        {
            Exception? error=null;var thread=new Thread(()=>{try{
                var outputs=new List<FakeOutput>();var errors=new List<string>();using var audio=new AlertAudio(Path.Combine(artifacts,"threaded-audio"),errors.Add,()=>{var result=new FakeOutput{ReadInBackground=true};outputs.Add(result);return result;});
                var timer=new TimerRecord{Alert=new(){PlaySound=true,RepeatSound=true},AlertState=new(){Active=true}};
                for(int i=0;i<8;i++){audio.Play(timer);Thread.Sleep(5);if(i==3)outputs[^1].FailStop=true;if(i==5)outputs[^1].FailDispose=true;audio.Stop(timer.Id);}
                Equal(true,outputs.All(x=>x.Disposed));Equal(true,outputs.All(x=>x.ReadError is null));Equal(2,errors.Count);
            }catch(Exception ex){error=ex;}});thread.SetApartmentState(ApartmentState.STA);thread.Start();thread.Join();if(error is not null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error).Throw();
        });
    }
    internal static void Layout(MainWindow host,string artifacts)
    {
        var s=host.Engine.Workspace.Settings;var t=host.Engine.Workspace.Timers[0];s.TimerScale=.25;s.AllowIndividualResize=false;t.Width=1400;t.Height=1100;
        var card=new TimerCard(host,t);card.ConfigureWorkspace(900);card.Measure(new Size(card.Width,card.Height));card.Arrange(new Rect(0,0,card.Width,card.Height));card.UpdateLayout();if(card.DigitHeight<23.99)throw new Exception($"Digit height {card.DigitHeight}, card {card.Width}x{card.Height}, minimum {card.MinWidth}x{card.MinHeight}");Equal(true,card.Width<1000);
        s.TimerScale=1;s.ListLayout=true;card=new TimerCard(host,t);card.ConfigureWorkspace(1000);card.Measure(new Size(card.Width,card.Height));card.Arrange(new Rect(0,0,card.Width,card.Height));card.Refresh();card.UpdateLayout();Equal(1000d,card.Width);Equal(true,card.HorizontalPresentation);Equal(true,card.DescriptionVisible);Equal(true,card.ControlsVisible);
        Render(card,artifacts,"notes2-list.png",1000,(int)card.Height);
        var widePreview=new TimerCard(host,new TimerRecord{Name="A long project name that must stay clear of the buttons",Description="Wide pop-out preview"},popout:true){Width=640,Height=160};widePreview.Measure(new Size(640,160));widePreview.Arrange(new Rect(0,0,640,160));widePreview.Refresh();widePreview.UpdateLayout();Render(widePreview,artifacts,"notes2-wide-popout.png",640,160);
        card.ConfigureWorkspace(700);card.Measure(new Size(card.Width,card.Height));card.Arrange(new Rect(0,0,card.Width,card.Height));card.Refresh();card.UpdateLayout();Equal(false,card.DescriptionVisible);
        s.ListLayout=false;s.TimerScale=1;t.Width=null;t.Height=null;host.RefreshWorkspaceView();
        var root=(FrameworkElement)host.Content;root.Measure(new Size(480,620));root.Arrange(new Rect(0,0,480,620));root.UpdateLayout();host.RefreshWorkspaceView();System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(()=>{},System.Windows.Threading.DispatcherPriority.ContextIdle);root.UpdateLayout();Render(root,artifacts,"notes2-narrow.png",480,620);
        root.Measure(new Size(1000,700));root.Arrange(new Rect(0,0,1000,700));root.UpdateLayout();
        int original=host.Engine.Workspace.Timers.Count;
        for(int i=0;i<10;i++)host.Engine.Add("Layout test "+i);
        var toggle=typeof(MainWindow).GetMethod("Layout_Click",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
        toggle.Invoke(host,[host,new RoutedEventArgs()]);root.UpdateLayout();
        var scroll=(ScrollViewer)host.FindName("WorkspaceScroll");scroll.ScrollToBottom();root.UpdateLayout();
        toggle.Invoke(host,[host,new RoutedEventArgs()]);System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(()=>{},System.Windows.Threading.DispatcherPriority.ContextIdle);root.UpdateLayout();
        var panel=(TimerGridPanel)host.FindName("Cards");Equal(true,panel.Columns>1);Equal(0d,scroll.VerticalOffset);
        host.Engine.Workspace.Timers.RemoveRange(original,10);host.RefreshWorkspaceView();
        s.AppScale=3;host.RefreshWorkspaceView();root.Measure(new Size(480,620));root.Arrange(new Rect(0,0,480,620));root.UpdateLayout();System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(()=>{},System.Windows.Threading.DispatcherPriority.ContextIdle);root.UpdateLayout();Render(root,artifacts,"notes2-high-scale.png",480,620);s.AppScale=1;host.RefreshWorkspaceView();
        var seed=WorkspaceStore.Copy(host.Engine.Workspace);seed.Settings.ExitChoice=ExitChoice.Save;seed.Settings.AutoSave=false;var seedStore=new WorkspaceStore(Path.Combine(artifacts,"notes2-ui-seed"));seed.Id=seedStore.Load()?.Id??seed.Id;seedStore.Save(seed);
        host.Notify("Test backup warning",true,"test");var toast=(FrameworkElement)host.FindName("Toast");Equal(Visibility.Visible,toast.Visibility);((Button)host.FindName("ToastDismiss")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Equal(Visibility.Collapsed,toast.Visibility);host.Notify("Test backup warning",true,"test");Equal(Visibility.Collapsed,toast.Visibility);host.ClearNotice("test");host.Notify("New warning",true,"test");Equal(Visibility.Visible,toast.Visibility);host.ClearNotice("test");Equal(Visibility.Collapsed,toast.Visibility);
    }
    private static void Render(FrameworkElement element,string artifacts,string name,int width,int height)
    {
        var bitmap=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);bitmap.Render(element);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var output=File.Create(Path.Combine(artifacts,name));encoder.Save(output);
    }
    internal static void LiveWindows(MainWindow host)
    {
        var s=host.Engine.Workspace.Settings;s.AutoSave=false;s.ExitChoice=ExitChoice.Save;s.PopoutScale=.25;s.LinkPopoutScale=false;
        var timer=host.Engine.Workspace.Timers[0];timer.SeparatePopoutStyle=true;timer.PopoutStyle=TimerStyle.Minimal();
        host.Show();host.UpdateLayout();host.PopOut(timer);
        var detached=Application.Current.Windows.Cast<Window>().Single(w=>w!=host&&w.IsVisible&&w.Content is TimerCard);
        detached.UpdateLayout();Equal(true,detached.ActualWidth+.01>=detached.MinWidth);Equal(true,detached.ActualHeight+.01>=detached.MinHeight);
        double width=detached.ActualWidth,height=detached.ActualHeight;
        s.PopoutScale=2;host.AppearanceChanged(false);detached.UpdateLayout();Equal(width,detached.ActualWidth);Equal(height,detached.ActualHeight);
        s.TimerScale=2;s.LinkPopoutScale=true;host.AppearanceChanged(false);detached.UpdateLayout();Equal(width,detached.ActualWidth);
        timer.PopoutStyle=new TimerStyle();detached.Width=900;detached.Height=180;host.AppearanceChanged(false);detached.UpdateLayout();
        detached.Width=300;detached.UpdateLayout();((TimerCard)detached.Content).Refresh();detached.UpdateLayout();Equal(true,detached.ActualWidth<620);Equal(false,((TimerCard)detached.Content).HorizontalPresentation);Equal(true,detached.ActualHeight+.01>=detached.MinHeight);
        host.Return(timer);timer.PopoutStyle=TimerStyle.Minimal();timer.Popout.Width=500;timer.Popout.Height=320;host.PopOut(timer,true);detached=Application.Current.Windows.Cast<Window>().Single(w=>w!=host&&w.IsVisible&&w.Content is TimerCard);detached.UpdateLayout();Equal(500d,detached.ActualWidth);host.Return(timer);
        s.PopoutScale=1;s.LinkPopoutScale=false;host.PopOut(timer);detached=Application.Current.Windows.Cast<Window>().Single(w=>w!=host&&w.IsVisible&&w.Content is TimerCard);detached.UpdateLayout();Equal(390d,detached.ActualWidth);host.Return(timer);
        s.Rows=3;s.TimerScale=1;host.RefreshWorkspaceView();var panel=(TimerGridPanel)host.FindName("Cards");var first=panel.Children[0];var scale=(ScaleInput)host.FindName("TimerScaleInput");scale.Children.OfType<Button>().Last().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(()=>{},System.Windows.Threading.DispatcherPriority.ContextIdle);Equal(true,ReferenceEquals(first,panel.Children[0]));Equal(panel.Children.Cast<TimerCard>().Max(c=>c.Height),panel.EmptyRowHeight);host.Close();
    }
    internal sealed class FakeOutput:IWavePlayer
    {
        public IWaveProvider? Provider;public bool Disposed;public volatile PlaybackState State;
        public bool ReadInBackground,FailStop,FailDispose;public Exception? ReadError;private Thread? reader;
        public event EventHandler<StoppedEventArgs>? PlaybackStopped;
        public PlaybackState PlaybackState=>State;public float Volume{get;set;}
        public WaveFormat OutputWaveFormat=>Provider?.WaveFormat??new WaveFormat();
        public void Init(IWaveProvider provider)=>Provider=provider;
        public void Play(){State=PlaybackState.Playing;if(ReadInBackground){reader=new Thread(()=>{try{var buffer=new byte[4096];while(State==PlaybackState.Playing){if(Provider!.Read(buffer,0,buffer.Length)==0)break;Thread.Sleep(1);}}catch(Exception ex){ReadError=ex;}finally{PlaybackStopped?.Invoke(this,new StoppedEventArgs(ReadError));}});reader.Start();}}
        public void Pause()=>State=PlaybackState.Paused;
        public void Stop(){State=PlaybackState.Stopped;PlaybackStopped?.Invoke(this,new StoppedEventArgs());if(FailStop)throw new IOException("Device stopped responding");}
        public void Dispose(){State=PlaybackState.Stopped;reader?.Join();Disposed=true;if(FailDispose)throw new IOException("Device disconnected");}
    }
}
