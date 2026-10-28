using System.Windows;
using TimersApp.Core;
namespace TimersApp.Desktop;
internal static class TimerSizing
{
    internal static double LogicalScale(AppSettings s) => Math.Max(.65, s.TimerScale);
    internal static double PresentationScale(AppSettings s) => Math.Min(1, s.TimerScale / .65);
    internal static Size Requested(AppSettings s, TimerRecord timer)
    {
        double w = s.AllowIndividualResize && !s.ListLayout ? timer.Width ?? s.CardWidth : s.CardWidth;
        double h = s.AllowIndividualResize && !s.ListLayout ? timer.Height ?? s.CardHeight : s.ListLayout ? s.ListRowHeight : s.CardHeight;
        return new Size(w * LogicalScale(s), h * LogicalScale(s));
    }
    internal static Size InitialPopout(AppSettings s) => new(Math.Clamp(390 * (s.LinkPopoutScale ? s.TimerScale : s.PopoutScale),150,1800), Math.Clamp(340 * (s.LinkPopoutScale ? s.TimerScale : s.PopoutScale),100,1400));
}
