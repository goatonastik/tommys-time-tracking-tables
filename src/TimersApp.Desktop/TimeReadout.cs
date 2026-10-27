using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Automation.Peers;
using TimersApp.Core;

namespace TimersApp.Desktop;

/// <summary>Resolution-independent text and seven-segment drawing, sized by the containing Viewbox.</summary>
internal sealed class TimeReadout : FrameworkElement
{
    private string text = "00:00:00";
    private TimerStyle style = new();
    private Brush ink = Brushes.White;
    private string measureKey = "";
    private string layoutKey="";
    private string measurementText="00:00:00";
    protected override AutomationPeer OnCreateAutomationPeer() => new ReadoutPeer(this);
    private sealed class ReadoutPeer(TimeReadout owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetNameCore() => owner.text;
        protected override string GetClassNameCore() => nameof(TimeReadout);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;
    }
    public void Update(string value, TimerStyle appearance, Brush? color = null)
    {
        var nextInk=color??Visuals.Brush(appearance.TextColor);
        string key = $"{value}|{appearance.FontFamily}|{appearance.Digits}|{appearance.FontSize:R}";
        if(measureKey==key&&ReferenceEquals(ink,nextInk))return;
        string nextLayout=$"{new string(value.Select(c=>char.IsAsciiDigit(c)?'0':c).ToArray())}|{appearance.FontFamily}|{appearance.Digits}|{appearance.FontSize:R}";
        bool geometryChanged=layoutKey!=nextLayout;layoutKey=nextLayout;measureKey=key;
        text = value; style = appearance; ink = nextInk;
        if(geometryChanged)
        {
            char widest="0123456789".MaxBy(c=>Text(c.ToString(),style.FontSize).WidthIncludingTrailingWhitespace);
            measurementText=new string(value.Select(c=>char.IsAsciiDigit(c)?widest:c).ToArray());
        }
        if (geometryChanged) InvalidateMeasure();
        InvalidateVisual();
    }
    private FormattedText Text(string value, double size) => new(value, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
        new Typeface(new FontFamily(style.FontFamily), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal), size, ink, VisualTreeHelper.GetDpi(this).PixelsPerDip);
    protected override Size MeasureOverride(Size availableSize) => style.Digits == DigitStyle.Text
        ? new Size(Math.Max(1, Text(measurementText, style.FontSize).WidthIncludingTrailingWhitespace), Text(measurementText,style.FontSize).Height)
        : new Size(Math.Max(1, text.Sum(Advance)) * style.FontSize / 100, style.FontSize * 1.12);
    private static double Advance(char c) => char.IsDigit(c) ? 72 : c is ':' or '.' ? 25 : c == ' ' ? 20 : 37;
    protected override void OnRender(DrawingContext dc)
    {
        if (style.Digits == DigitStyle.Text) { dc.DrawText(Text(text, style.FontSize), new Point()); return; }
        dc.PushTransform(new ScaleTransform(style.FontSize / 100, style.FontSize / 100));
        double x = 0;
        int[] masks = [0x3F, 0x06, 0x5B, 0x4F, 0x66, 0x6D, 0x7D, 0x07, 0x7F, 0x6F];
        foreach (char c in text)
        {
            if (char.IsAsciiDigit(c))
            {
                (Point A, Point B)[] segments = [(new(x + 14, 8), new(x + 54, 8)), (new(x + 60, 15), new(x + 60, 49)),
                    (new(x + 60, 63), new(x + 60, 97)), (new(x + 14, 105), new(x + 54, 105)),
                    (new(x + 8, 63), new(x + 8, 97)), (new(x + 8, 15), new(x + 8, 49)), (new(x + 14, 56), new(x + 54, 56))];
                int mask = masks[c - '0'];
                for (int i = 0; i < 7; i++)
                {
                    dc.PushOpacity((mask & (1 << i)) != 0 ? 1 : .08);
                    var pen = new Pen(ink, 9) { StartLineCap = PenLineCap.Triangle, EndLineCap = PenLineCap.Triangle };
                    dc.DrawLine(pen, segments[i].A, segments[i].B); dc.Pop();
                }
            }
            else if (c == ':') { dc.DrawEllipse(ink, null, new Point(x + 12, 38), 4, 4); dc.DrawEllipse(ink, null, new Point(x + 12, 76), 4, 4); }
            else if (c == '.') dc.DrawEllipse(ink, null, new Point(x + 10, 103), 4, 4);
            else if (c != ' ') dc.DrawText(Text(c.ToString(), 40), new Point(x, 66));
            x += Advance(c);
        }
        dc.Pop();
    }
}
