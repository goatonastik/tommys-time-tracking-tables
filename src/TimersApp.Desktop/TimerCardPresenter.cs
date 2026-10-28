using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TimersApp.Core;

namespace TimersApp.Desktop;

/// <summary>Measures a logical card and reserves only its uniformly scaled workspace footprint.</summary>
internal sealed class TimerCardPresenter(TimerCard card) : Decorator
{
    internal TimerCard Card { get; } = card;
    internal double Factor { get; private set; } = 1;
    internal void Configure(AppSettings settings, double viewport)
    {
        Child ??= Card;
        Factor = TimerSizing.PresentationScale(settings);
        Card.ConfigureWorkspace(viewport);
        Card.RenderTransformOrigin = new Point(0, 0);
        Card.RenderTransform = new ScaleTransform(Factor, Factor);
        HorizontalAlignment = HorizontalAlignment.Left; VerticalAlignment = VerticalAlignment.Top;
        InvalidateMeasure();
    }
    protected override Size MeasureOverride(Size constraint)
    {
        Card.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return new Size(Card.DesiredSize.Width * Factor, Card.DesiredSize.Height * Factor);
    }
    protected override Size ArrangeOverride(Size arrangeSize)
    {
        Card.Arrange(new Rect(new Point(), Card.DesiredSize));
        return arrangeSize;
    }
}
