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
    private Pen? segmentPen;
    private Rect textBounds;
    private static readonly (Point A,Point B)[] Segments=[(new(14,8),new(54,8)),(new(60,15),new(60,49)),(new(60,63),new(60,97)),(new(14,105),new(54,105)),(new(8,63),new(8,97)),(new(8,15),new(8,49)),(new(14,56),new(54,56))];
    private static readonly int[] Masks=[0x3F,0x06,0x5B,0x4F,0x66,0x6D,0x7D,0x07,0x7F,0x6F];
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
        if(segmentPen is null||!ReferenceEquals(segmentPen.Brush,ink)){segmentPen=new Pen(ink,9){StartLineCap=PenLineCap.Triangle,EndLineCap=PenLineCap.Triangle};if(segmentPen.CanFreeze)segmentPen.Freeze();}
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
    protected override Size MeasureOverride(Size availableSize)
    {
        if(style.Digits!=DigitStyle.Text)return new Size(Math.Max(1,text.Sum(Advance))*style.FontSize/100,style.FontSize*1.12);
        var formatted=Text(measurementText,style.FontSize);
        static Rect DrawingBounds(FormattedText text){var drawing=new DrawingGroup();using(var dc=drawing.Open())dc.DrawText(text,new Point());return drawing.Bounds;}
        var ink=DrawingBounds(formatted);
        var digitsBounds=DrawingBounds(Text("0123456789",style.FontSize));
        double left=Math.Min(0,ink.Left),top=Math.Min(ink.Top,digitsBounds.Top);
        textBounds=new Rect(left,top,Math.Max(1,Math.Max(formatted.WidthIncludingTrailingWhitespace,ink.Right)-left),Math.Max(1,Math.Max(ink.Bottom,digitsBounds.Bottom)-top));
        return textBounds.Size;
    }
    private static double Advance(char c) => char.IsDigit(c) ? 72 : c is ':' or '.' ? 25 : c == ' ' ? 20 : 37;
    protected override void OnRender(DrawingContext dc)
    {
        if (style.Digits == DigitStyle.Text) { dc.DrawText(Text(text, style.FontSize), new Point(-textBounds.X,-textBounds.Y)); return; }
        dc.PushTransform(new ScaleTransform(style.FontSize / 100, style.FontSize / 100));
        double x = 0;
        foreach (char c in text)
        {
            if (char.IsAsciiDigit(c))
            {
                int mask = Masks[c - '0'];
                for (int i = 0; i < 7; i++)
                {
                    dc.PushOpacity((mask & (1 << i)) != 0 ? 1 : .08);
                    dc.DrawLine(segmentPen,new Point(x+Segments[i].A.X,Segments[i].A.Y),new Point(x+Segments[i].B.X,Segments[i].B.Y)); dc.Pop();
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
