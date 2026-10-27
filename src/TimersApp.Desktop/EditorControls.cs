using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Input;
using System.Windows.Media;

namespace TimersApp.Desktop;

public sealed class ScaleInput : StackPanel
{
    private readonly TextBox entry = new() { Width=60, Padding=new Thickness(5), VerticalContentAlignment=VerticalAlignment.Center };
    private double value=1;
    private readonly Slider slider = new() { Minimum=.25, Maximum=3, Width=90, SmallChange=.05, Margin=new Thickness(6,0,0,0) };
    public event EventHandler? ValueChanged;
    public double Value { get=>value; set { this.value=Math.Clamp(value,.25,3); entry.Text=(this.value*100).ToString("0.##"); slider.Value=this.value; } }
    public string Label { get=>AutomationProperties.GetName(entry); set {AutomationProperties.SetName(entry,value);AutomationProperties.SetName(slider,value);foreach(var button in Children.OfType<Button>())AutomationProperties.SetName(button,(Equals(button.Content,"+")?"Increase ":"Decrease ")+value);} }
    public ScaleInput()
    {
        Orientation=Orientation.Horizontal; VerticalAlignment=VerticalAlignment.Center;
        var minus=new Button { Content="−",Width=27,Padding=new Thickness(3),Margin=new Thickness(2),ToolTip="Previous 25% step" };
        var plus=new Button { Content="+",Width=27,Padding=new Thickness(3),Margin=new Thickness(2),ToolTip="Next 25% step" };
        Children.Add(minus); Children.Add(entry); Children.Add(new TextBlock {Text="%",Margin=new Thickness(3,0,3,0)}); Children.Add(plus);
        Children.Add(slider); slider.ValueChanged+=(_,_)=>{if(Math.Abs(slider.Value-value)>.000001)Set(slider.Value);};
        AutomationProperties.SetName(minus,"Decrease scale"); AutomationProperties.SetName(plus,"Increase scale");
        minus.Click+=(_,_)=>Step(-1); plus.Click+=(_,_)=>Step(1);
        entry.LostKeyboardFocus+=(_,_)=>Commit(); entry.KeyDown+=(_,e)=>{ if(e.Key==Key.Enter){Commit();e.Handled=true;} if(e.Key==Key.Escape){Value=value;e.Handled=true;} }; Value=1;
    }
    private void Step(int direction) { double target=direction>0 ? (Math.Floor(value*4+.000001)+1)/4 : (Math.Ceiling(value*4-.000001)-1)/4; Set(target); }
    private void Set(double number) { number=Math.Clamp(number,.25,3);bool changed=Math.Abs(value-number)>.000001; Value=number; if(changed)ValueChanged?.Invoke(this,EventArgs.Empty); }
    private void Commit()
    {
        if(!double.TryParse(entry.Text.Trim().TrimEnd('%'),out double n)||!double.IsFinite(n)||n<25||n>300){entry.ToolTip="Enter 25–300%.";Value=value;return;}
        entry.ToolTip=null;Set(n/100);
    }
}
internal sealed class NumberSlider : StackPanel
{
    private readonly Slider slider;
    public double Value {get=>slider.Value;set=>slider.Value=value;}
    public event EventHandler? ValueChanged;
    public NumberSlider(string label,double min,double max,double value,double step=1)
    {
        var caption=new TextBlock {Text=label,Margin=new Thickness(0,8,0,4)};Children.Add(caption);
        var row=new DockPanel();var number=new TextBlock {Width=45,TextAlignment=TextAlignment.Right};DockPanel.SetDock(number,Dock.Right);row.Children.Add(number);
        slider=new Slider {Minimum=min,Maximum=max,Value=value,TickFrequency=step,IsSnapToTickEnabled=true,SmallChange=step,Margin=new Thickness(0,0,8,0)};AutomationProperties.SetLabeledBy(slider,caption);row.Children.Add(slider);Children.Add(row);
        void Update(){number.Text=slider.Value.ToString("0.##");ValueChanged?.Invoke(this,EventArgs.Empty);}slider.ValueChanged+=(_,_)=>Update();Update();
    }
}
public sealed class ToolbarIcon : FrameworkElement
{
    public string Kind {get;set;}="Plus";
    public ToolbarIcon(){Width=21;Height=21;}
    protected override void OnRender(DrawingContext dc)
    {
        dc.PushTransform(new ScaleTransform(ActualWidth/21,ActualHeight/21));
        var pen=new Pen(Kind=="AddTimer"?Brushes.Black:Brushes.White,1.7){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};
        void L(double a,double b,double c,double d)=>dc.DrawLine(pen,new Point(a,b),new Point(c,d));
        if(Kind=="AddTimer"){dc.DrawEllipse(null,pen,new Point(10.5,11),8,8);L(3,2,6,1);L(15,1,18,2);L(5,18,3,20);L(16,18,18,20);L(7,11,14,11);L(10.5,7,10.5,15);}
        else if(Kind=="Save"){dc.DrawRectangle(null,pen,new Rect(2,2,17,17));dc.DrawRectangle(null,pen,new Rect(5,2,10,6));dc.DrawRectangle(null,pen,new Rect(5,13,11,6));}
        else if(Kind=="Rename"){L(1,18,6,3);L(6,3,11,18);L(3,12,9,12);L(12,15,18,7);L(14,17,20,9);L(12,15,12,18);L(12,18,14,17);}
        else if(Kind=="Copy"){dc.DrawRectangle(null,pen,new Rect(7,2,12,12));dc.DrawRectangle(Visuals.Brush("#293B55"),pen,new Rect(2,7,12,12));}
        else if(Kind=="Delete"){L(4,4,17,17);L(17,4,4,17);}
        else if(Kind=="Link"){dc.PushTransform(new RotateTransform(-35,10,10));dc.DrawRoundedRectangle(null,pen,new Rect(1,6,12,8),4,4);dc.DrawRoundedRectangle(null,pen,new Rect(8,6,12,8),4,4);dc.Pop();}
        else if(Kind is "Grid" or "List"){dc.DrawRectangle(null,pen,new Rect(2,2,17,17));if(Kind=="Grid"){L(8,2,8,19);L(13,2,13,19);L(2,8,19,8);L(2,13,19,13);}else for(int y=5;y<19;y+=4)L(5,y,16,y);}
        else{L(3,10.5,18,10.5);L(10.5,3,10.5,18);}
        dc.Pop();
    }
}
