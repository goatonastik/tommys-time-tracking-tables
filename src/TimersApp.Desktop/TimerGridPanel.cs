using System.Windows;
using System.Windows.Controls;

namespace TimersApp.Desktop;
/// <summary>Variable-size cards in fixed columns; extra rows scroll instead of shrinking cards.</summary>
internal sealed class TimerGridPanel : Panel
{
    public int Columns { get; set; } = 3;
    public int MinimumRows { get; set; }
    public double Spacing { get; set; } = 14;
    public double EmptyRowHeight { get; set; } = 250;
    private double[] widths = [], heights = [];
    protected override Size MeasureOverride(Size availableSize)
    {
        int cols = Math.Max(1, Columns), rows = Math.Max(MinimumRows, (InternalChildren.Count + cols - 1) / cols);
        widths = new double[cols]; heights = new double[rows];
        for (int i = 0; i < InternalChildren.Count; i++)
        {
            var child = InternalChildren[i]; child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            widths[i % cols] = Math.Max(widths[i % cols], child.DesiredSize.Width); heights[i / cols] = Math.Max(heights[i / cols], child.DesiredSize.Height);
        }
        for (int r = 0; r < rows; r++) if (heights[r] == 0) heights[r] = EmptyRowHeight;
        return new Size(widths.Sum() + Math.Max(0, cols - 1) * Spacing, heights.Sum() + Math.Max(0, rows - 1) * Spacing);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        int cols = widths.Length;
        for (int i = 0; i < InternalChildren.Count && cols > 0; i++)
        {
            int c = i % cols, r = i / cols;
            double x = widths.Take(c).Sum() + c * Spacing, y = heights.Take(r).Sum() + r * Spacing;
            InternalChildren[i].Arrange(new Rect(x, y, widths[c], heights[r]));
        }
        return finalSize;
    }
}
