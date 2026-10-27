using System.Windows;
using TimersApp.Core;
namespace TimersApp.Desktop;
internal static class TimerSizing
{
    internal static Size Requested(AppSettings s, TimerRecord timer)
    {
        double w = s.AllowIndividualResize && !s.ListLayout ? timer.Width ?? s.CardWidth : s.CardWidth;
        double h = s.AllowIndividualResize && !s.ListLayout ? timer.Height ?? s.CardHeight : s.ListLayout ? s.ListRowHeight : s.CardHeight;
        return new Size(w * s.TimerScale, h * s.TimerScale);
    }
    internal static Size InitialPopout(AppSettings s) => new(Math.Clamp(390 * (s.LinkPopoutScale ? s.TimerScale : s.PopoutScale),150,1800), Math.Clamp(340 * (s.LinkPopoutScale ? s.TimerScale : s.PopoutScale),100,1400));
}
