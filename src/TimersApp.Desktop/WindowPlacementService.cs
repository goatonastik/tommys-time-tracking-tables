using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TimersApp.Desktop;
internal static class WindowPlacementService
{
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public Rect Monitor, Work; public uint Flags; }
    [DllImport("user32.dll")] private static extern nint MonitorFromWindow(nint window, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetWindowRect(nint window, out Rect rect);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetClientRect(nint window, out Rect rect);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetWindowPos(nint window, nint after, int x, int y, int width, int height, uint flags);
    internal static Size Chrome(Window window)
    {
        var handle=new WindowInteropHelper(window).Handle;
        if(handle!=0&&GetWindowRect(handle,out var outer)&&GetClientRect(handle,out var inner))
        {
            var dpi=System.Windows.Media.VisualTreeHelper.GetDpi(window);
            return new Size(Math.Max(0,(outer.Right-outer.Left-inner.Right+inner.Left)/dpi.DpiScaleX),Math.Max(0,(outer.Bottom-outer.Top-inner.Bottom+inner.Top)/dpi.DpiScaleY));
        }
        var frame=SystemParameters.WindowNonClientFrameThickness;return new Size(frame.Left+frame.Right,frame.Top+frame.Bottom+SystemParameters.WindowCaptionHeight);
    }
    public static void EnsureVisible(Window window)
    {
        if (window.WindowState != WindowState.Normal) return;
        nint handle = new WindowInteropHelper(window).Handle; if (handle == 0 || !GetWindowRect(handle, out var bounds)) return;
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(MonitorFromWindow(handle, 2), ref info)) return;
        int width = Math.Min(bounds.Right - bounds.Left, info.Work.Right - info.Work.Left);
        int height = Math.Min(bounds.Bottom - bounds.Top, info.Work.Bottom - info.Work.Top);
        if (width <= 0 || height <= 0) return;
        int x = Math.Clamp(bounds.Left, info.Work.Left, info.Work.Right - width), y = Math.Clamp(bounds.Top, info.Work.Top, info.Work.Bottom - height);
        if (x != bounds.Left || y != bounds.Top || width != bounds.Right - bounds.Left || height != bounds.Bottom - bounds.Top)
            SetWindowPos(handle, 0, x, y, width, height, 0x0004 | 0x0010);
    }
}
