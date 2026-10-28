using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using TimersApp.Core;

namespace TimersApp.Desktop;

/// <summary>Normal bounds belong to the opening workspace; only actual move/resize sessions replace memory.</summary>
internal sealed class PopoutSession : IDisposable
{
    private readonly Workspace workspace;
    private readonly TimerRecord timer;
    private readonly Window window;
    private HwndSource? source;
    private PopoutGeometry observed = new();
    private bool initialized, closed, suppressed, moveActive, endMovePending;
    private int boundsVersion, suppressionVersion;
    private static long sequence;
    private sealed class SharedOrder { internal long Size,Location; }
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Workspace,SharedOrder> sharedOrders=new();
    private long sizeOrder,locationOrder;
    private long endMoveOrder;
    internal PopoutSession(Workspace workspace, TimerRecord timer, Window window)
    {
        this.workspace=workspace;this.timer=timer;this.window=window;
        window.SourceInitialized+=OnSource;
        window.Loaded+=OnLoaded;
        window.LocationChanged+=OnLocation;
        window.SizeChanged+=OnSize;
    }
    private void OnSource(object? sender,EventArgs e)
    {
        source=HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);source?.AddHook(Hook);
    }
    private void OnLoaded(object sender,RoutedEventArgs e)
    {
        Suppress(()=>WindowPlacementService.EnsureVisible(window));
        window.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle,new Action(()=>
        {
            if(closed)return;
            observed=Bounds();initialized=true;
            PopoutPlacementPolicy.Capture(workspace,timer,observed,false,false,true);
        }));
    }
    private void OnLocation(object? sender,EventArgs e)=>BoundsChanged(false);
    private void OnSize(object sender,SizeChangedEventArgs e)=>BoundsChanged(true);
    private void BoundsChanged(bool size)
    {
        int version=++boundsVersion;
        if(!initialized||closed)return;
        if(suppressed&&!moveActive)return;
        long order=System.Threading.Interlocked.Increment(ref sequence);if(size)sizeOrder=order;else locationOrder=order;
        if(suppressed)return;
        // Queue the latest event, rather than reusing the first event's place in the queue:
        // changes across several windows must commit in their actual event order.
        window.Dispatcher.BeginInvoke(DispatcherPriority.Background,new Action(()=>
        {
            if(version==boundsVersion)Flush();
        }));
    }
    internal void Suppress(Action change)
    {
        if(closed)return;
        if(!suppressed)Flush();
        suppressed=true;++boundsVersion;int version=++suppressionVersion;
        change();
        window.Dispatcher.BeginInvoke(DispatcherPriority.Loaded,new Action(()=>
        {
            if(closed||version!=suppressionVersion)return;
            suppressed=false;
            if(moveActive||endMovePending)
            {
                // A minimum-size correction during a native drag is part of that drag's
                // final legal bounds. Preserve it even if WM_EXITSIZEMOVE arrived first.
                var bounds=Bounds();
                if(endMovePending&&(bounds.Width!=observed.Width||bounds.Height!=observed.Height))sizeOrder=Math.Max(sizeOrder,endMoveOrder);
                if(endMovePending&&(bounds.Left!=observed.Left||bounds.Top!=observed.Top))locationOrder=Math.Max(locationOrder,endMoveOrder);
                Flush();endMovePending=false;
            }
            else observed=Bounds();
        }));
    }
    private nint Hook(nint hwnd,int message,nint wParam,nint lParam,ref bool handled)
    {
        if(message==0x0231)BeginMove(); // WM_ENTERSIZEMOVE
        if(message==0x0232)EndMove();   // WM_EXITSIZEMOVE
        return 0;
    }
    internal void BeginMove(){if(initialized&&!closed){Flush();observed=Bounds();moveActive=true;}}
    internal void EndMove(){if(suppressed){endMovePending=true;endMoveOrder=System.Threading.Interlocked.Increment(ref sequence);}else Flush();moveActive=false;}
    internal void Flush()
    {
        if(!initialized||closed||suppressed||window.WindowState!=WindowState.Normal)return;
        var bounds=Bounds();
        bool size=bounds.Width!=observed.Width||bounds.Height!=observed.Height,location=bounds.Left!=observed.Left||bounds.Top!=observed.Top;
        var order=sharedOrders.GetOrCreateValue(workspace);var settings=workspace.Settings;
        if(!settings.PopoutSizePerTimer){size&=sizeOrder>=order.Size;if(size&&settings.RememberPopoutSize)order.Size=sizeOrder;}
        if(!settings.PopoutLocationPerTimer){location&=locationOrder>=order.Location;if(location&&settings.RememberPopoutLocation)order.Location=locationOrder;}
        PopoutPlacementPolicy.Capture(workspace,timer,bounds,size,location);
        observed=bounds;
    }
    private PopoutGeometry Bounds()=>new(){Left=window.Left,Top=window.Top,Width=window.ActualWidth,Height=window.ActualHeight};
    public void Dispose()
    {
        if(closed)return;Flush();closed=true;
        source?.RemoveHook(Hook);window.SourceInitialized-=OnSource;window.Loaded-=OnLoaded;window.LocationChanged-=OnLocation;window.SizeChanged-=OnSize;
    }
}
