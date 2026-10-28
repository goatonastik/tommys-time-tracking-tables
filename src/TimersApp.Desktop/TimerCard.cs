using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Automation;
using System.Windows.Documents;
using System.Windows.Automation.Peers;
using TimersApp.Core;
using Duration = TimersApp.Core.Duration;

namespace TimersApp.Desktop;
internal sealed class TimerCard : Border
{
    private readonly TimerRecord timer;
    private readonly MainWindow host;
    private readonly bool popout;
    private readonly bool preview;
    private readonly TextBlock state = new() { FontSize = 11, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 10) };
    private readonly TimeReadout digits = new();
    private readonly Viewbox readout;
    private readonly TextBlock rounded = new() { TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 5, 0, 8) };
    private readonly Button start = new();
    private readonly StackPanel buttons = new() { Orientation=Orientation.Horizontal };
    private readonly StackPanel heading = new();
    private readonly WrapPanel alertControls = new() { Margin = new Thickness(0, 4, 0, 4) };
    private readonly Grid content = new();
    private readonly ImageBackdrop backdrop = new() { IsHitTestVisible = false };
    private readonly Thumb resize = new() { Width = 32, Height = 32, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Cursor = Cursors.SizeNWSE, Opacity = .4, ToolTip = "Drag to resize this timer" };
    private TimerStyle? previewStyle;
    private readonly MenuItem returnToWorkspace = new() { Header = "Return to workspace" };
    private Rect lastClip;
    private double lastRadius=-1;

    private readonly TextBlock identity = new() { TextWrapping=TextWrapping.Wrap };
    private readonly Grid band = new();
    private bool? horizontalLayout;
    internal bool IsList => !popout && !preview && host.Engine.Workspace.Settings.ListLayout;
    internal double DigitHeight => readout.ActualHeight;
    internal bool HorizontalPresentation => horizontalLayout==true;
    internal bool ControlsVisible => start.Visibility==Visibility.Visible;
    internal bool DescriptionVisible {get;private set;}
    private string identityKey="";
    private string titleMeasureKey="";
    private double cachedTitleWidth;
    private double cachedDescriptionWidth;
    private readonly TextBlock tooltipText=new(){TextWrapping=TextWrapping.Wrap,MaxWidth=420};
    private bool minimumClamped;
    private enum Presentation { List, Grid, CompactGrid, Popout, WidePopout }
    private Presentation? presentation;
    private double viewportWidth=900;

    public TimerCard(MainWindow host, TimerRecord timer, bool popout = false, bool preview = false)
    {
        this.host = host; this.timer = timer; this.popout = popout; this.preview = preview;
        Focusable = !preview;
        KeyboardNavigation.SetIsTabStop(this, !preview);
        AutomationProperties.SetHelpText(this, "Press Shift+F10 or the menu key for timer controls, styles and history.");
        if (preview) { IsHitTestVisible = false; Focusable = false; KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.None); }
        ClipToBounds = true;
        ToolTip=new ToolTip{Content=tooltipText};ToolTipOpening+=(_,_)=>{var total=host.Engine.Total(timer);tooltipText.Text=(minimumClamped?"Minimum readable size reached.\n":"")+timer.Name+(string.IsNullOrWhiteSpace(timer.Description)?"":"\n"+timer.Description)+"\nExact total: "+Duration.FormatPrecise(total)+"\nRight-click for controls, styles and history.";};
        var layers = new Grid(); Child = layers; layers.Children.Add(backdrop); layers.Children.Add(content);
        heading.Children.Add(state); heading.Children.Add(identity);
        var view = new Viewbox { Child = digits, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Stretch };
        readout = view;
        Add(alertControls, "Dismiss", "Restore normal appearance and stop sound", () => host.DismissAlert(timer));
        Add(alertControls, "Snooze", "Silence temporarily using this timer’s snooze interval", () => host.SnoozeAlert(timer));
        start.Click += (_, _) => host.Toggle(timer);
        band.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});band.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
        band.Children.Add(start);Grid.SetColumn(readout,1);Grid.SetRow(readout,0);band.Children.Add(readout);
        start.Padding=new Thickness(12,4,12,4);
        Add(buttons, "◷", "Segment history", () => host.History(timer));
        Add(buttons, popout ? "↙" : "↗", popout ? "Return to workspace" : "Pop out / focus window", () => { if (popout) host.Return(timer); else host.PopOut(timer); });
        Add(buttons, "⋯", "Timer options", () => { ContextMenu.IsOpen = true; });
        var menu = new ContextMenu(); ContextMenu = menu;
        Item(menu, "Start / stop", () => host.Toggle(timer));
        Item(menu, "Edit name and description…", () => host.Edit(timer));
        Item(menu, "Segment history / information…", () => host.History(timer));
        Item(menu, "Custom style…", () => host.EditTimerStyle(timer, popout));
        var presets = new MenuItem { Header = "Apply style preset" }; menu.Items.Add(presets);
        menu.Opened += (_, _) =>
        {
            presets.Items.Clear();
            foreach (var preset in host.Engine.Workspace.Presets)
            {
                var item = new MenuItem { Header = preset.Name }; item.Click += (_, _) => host.ApplyPreset(timer, preset, popout); presets.Items.Add(item);
            }
        };
        Item(menu, "Use default style", () => host.UseDefaultStyle(timer, popout));
        Item(menu, "Time-only style", () => host.SetTimeOnly(timer, popout));
        var visibility = new MenuItem { Header = "Show / hide" }; menu.Items.Add(visibility);
        foreach (var label in new[] { "Title", "Description", "Status", "Controls", "Total details" })
        {
            var item = new MenuItem { Header = label }; item.Click += (_, _) => host.ToggleVisibility(timer, popout, label); visibility.Items.Add(item);
        }
        Item(menu, "Interval alert…", () => host.EditAlert(timer));
        Item(menu, "Dismiss alert", () => host.DismissAlert(timer));
        Item(menu, "Snooze alert", () => host.SnoozeAlert(timer));
        Item(menu, "Reset total (keep history)", () => host.Reset(timer));
        Item(menu, popout ? "Return to workspace" : "Pop out / focus window", () => { if (popout) host.Return(timer); else host.PopOut(timer); });
        if (!popout) { returnToWorkspace.Click += (_, _) => host.Return(timer); menu.Items.Add(returnToWorkspace); }
        if (popout) Item(menu, "Toggle always on top", () => { var w = Window.GetWindow(this); w.Topmost = !w.Topmost; timer.Popout.Topmost = w.Topmost; host.AppearanceChanged(); });
        Item(menu, "Remove from workspace (keep history)", () => host.Remove(timer));
        if (!popout && !preview)
        {
            resize.Template = (ControlTemplate)FindResource("TimerResizeTemplate");
            layers.Children.Add(resize);
            resize.DragDelta += (_, e) => { double scale=host.Engine.Workspace.Settings.TimerScale; timer.Width=Math.Clamp((Width+e.HorizontalChange)/scale,170,1500);timer.Height=Math.Clamp((Height+e.VerticalChange)/scale,100,1200);ConfigureWorkspace(host.WorkspaceWidth);host.Relayout(); };
            resize.DragCompleted += (_, _) => host.AppearanceChanged(false);
        }
        SizeChanged += (_, _) => Refresh();
        Refresh();
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new TimerPeer(this);
    private sealed class TimerPeer(TimerCard owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetNameCore() => $"{owner.timer.Name}, {(owner.timer.IsRunning ? "running" : "stopped")}, exact elapsed {Duration.Format(owner.host.Engine.Total(owner.timer))}";
        protected override string GetClassNameCore() => nameof(TimerCard);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!preview && (e.Key == Key.Apps || (e.Key == Key.F10 && Keyboard.Modifiers == ModifierKeys.Shift)))
        {
            ContextMenu.PlacementTarget = this; ContextMenu.Placement = PlacementMode.Bottom;
            ContextMenu.IsOpen = true; e.Handled = true;
        }
        base.OnKeyDown(e);
    }
    public void SetPreview(TimerStyle style) { previewStyle = style; Refresh(); }
    private static void Add(Panel panel, string text, string tip, Action action) { var b = new Button { Content = text, ToolTip = tip, Padding = new Thickness(9, 4, 9, 4),Margin=new Thickness(0) }; AutomationProperties.SetName(b, tip); b.Click += (_, _) => action(); panel.Children.Add(b); }
    private static void Item(ContextMenu menu, string text, Action action) { var item = new MenuItem { Header = text }; item.Click += (_, _) => action(); menu.Items.Add(item); }
    private static Visibility Visible(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
    internal void ConfigureWorkspace(double viewport)
    {
        if(popout||preview)return;
        viewportWidth=viewport;
        Refresh();var requested=TimerSizing.Requested(host.Engine.Workspace.Settings,timer);
        Width=IsList?Math.Max(MinWidth,viewport):Math.Max(MinWidth,requested.Width);
        var s=host.Engine.Workspace.Settings;
        Height=!IsList&&s.FitCardHeight&&!(s.AllowIndividualResize&&timer.Height.HasValue)?MinHeight:Math.Max(MinHeight,requested.Height);
    }
    private void Compose(Presentation mode)
    {
        if(presentation==mode)return;presentation=mode;
        bool horizontal=mode is Presentation.List or Presentation.WidePopout;horizontalLayout=horizontal;
        content.Children.Clear();content.ColumnDefinitions.Clear();content.RowDefinitions.Clear();
        if(horizontal)
        {
            foreach(var width in new[]{GridLength.Auto,GridLength.Auto,new GridLength(1,GridUnitType.Star),GridLength.Auto})content.ColumnDefinitions.Add(new ColumnDefinition{Width=width});
            content.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
            AddAt(band,0,0);AddAt(rounded,0,1);AddAt(heading,0,2);AddAt(buttons,0,3);
            content.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});AddAt(alertControls,1,0,4);
            rounded.Margin=new Thickness(4,0,4,0);heading.Margin=new Thickness(0);heading.HorizontalAlignment=HorizontalAlignment.Stretch;
        }
        else
        {
            content.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(1,GridUnitType.Star)});
            content.RowDefinitions.Add(new RowDefinition{Height=mode==Presentation.Popout?new GridLength(1,GridUnitType.Star):GridLength.Auto});
            for(int i=0;i<4;i++)content.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});
            AddAt(band,0,0);AddAt(rounded,1,0);AddAt(heading,2,0);AddAt(buttons,3,0);AddAt(alertControls,4,0);
            rounded.Margin=new Thickness(0);heading.Margin=new Thickness(0);heading.HorizontalAlignment=HorizontalAlignment.Right;
        }
        identity.TextAlignment=TextAlignment.Left;heading.HorizontalAlignment=HorizontalAlignment.Stretch;heading.VerticalAlignment=VerticalAlignment.Center;buttons.VerticalAlignment=VerticalAlignment.Center;
        void AddAt(UIElement child,int row,int col,int span=1){Grid.SetRow(child,row);Grid.SetColumn(child,col);Grid.SetColumnSpan(child,span);Grid.SetRowSpan(child,1);content.Children.Add(child);}
    }
    public void Refresh()
    {
        var style=previewStyle??Appearance.Resolve(host.Engine.Workspace,timer,popout);
        var settings=host.Engine.Workspace.Settings;
        var requestedSize=TimerSizing.Requested(settings,timer);
        var text=Visuals.Brush(style.TextColor);var accent=Visuals.Brush(style.AccentColor);
        double h=ActualHeight>0?ActualHeight:double.IsFinite(Height)?Height:250;
        double w=popout||preview?(ActualWidth>0?ActualWidth:double.IsFinite(Width)?Width:330):IsList?viewportWidth:requestedSize.Width;
        var ownerWindow=popout&&!preview?Window.GetWindow(this):null;
        var chrome=ownerWindow is null?new Size():WindowPlacementService.Chrome(ownerWindow);
        if(ownerWindow is not null&&ownerWindow.ActualWidth>0){w=Math.Max(1,ownerWindow.ActualWidth-chrome.Width);h=Math.Max(1,ownerWindow.ActualHeight-chrome.Height);}
        bool wide=popout&&w>=620&&h<230;
        bool horizontal=IsList||wide;
        Compose(IsList?Presentation.List:wide?Presentation.WidePopout:popout?Presentation.Popout:requestedSize.Width<260?Presentation.CompactGrid:Presentation.Grid);
        double padding=Math.Max(popout?Math.Min(style.Padding,h*.045):style.Padding,style.CornerRadius*(1-1/Math.Sqrt(2)));
        content.Margin=new Thickness(padding);CornerRadius=new CornerRadius(style.CornerRadius);
        var bounds=new Rect(0,0,Math.Max(0,ActualWidth),Math.Max(0,ActualHeight));
        if(bounds!=lastClip||lastRadius!=style.CornerRadius){Clip=new RectangleGeometry(bounds,style.CornerRadius,style.CornerRadius);lastClip=bounds;lastRadius=style.CornerRadius;}
        backdrop.Update(style.Background);
        var total=preview?TimeSpan.FromSeconds(4120.375):host.Engine.Total(timer);
        bool active=!preview&&timer.Alert.Enabled&&timer.AlertState.Active;
        var color=active&&timer.Alert.ChangeColor?Visuals.Brush(timer.Alert.Color):text;
        string value=settings.TotalDisplay==TotalDisplay.Rounded?Duration.HoursText(total,settings.RoundMinutes):Appearance.Format(total,style);
        digits.Update(value,style,color);digits.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));
        double ratio=digits.DesiredSize.Width/Math.Max(1,digits.DesiredSize.Height);
        double target=horizontal?Math.Clamp(style.FontSize*(IsList?settings.TimerScale:1),24,120):popout?24:Math.Max(24,Math.Min(style.FontSize*settings.TimerScale,Math.Max(24,w-padding*2-(style.ShowControls?38:0))/Math.Max(.1,ratio)));
        readout.MinHeight=target;readout.MinWidth=ratio*target;
        readout.Height=popout&&!horizontal?double.NaN:target;readout.Width=horizontal?ratio*target:double.NaN;
        readout.StretchDirection=style.FitDigitsToWindow?StretchDirection.Both:StretchDirection.DownOnly;
        digits.Opacity=active&&timer.Alert.Flash?(DateTime.UtcNow.Millisecond<500?1:.15):1;
        start.Visibility=Visible(style.ShowControls);start.Background=Brushes.Transparent;start.Foreground=timer.IsRunning?Brushes.OrangeRed:Brushes.LimeGreen;
        start.Content=timer.IsRunning?"■":"▶";start.FontSize=Math.Clamp(target*.75,18,48);start.Padding=new Thickness(2);start.Margin=new Thickness(0,0,2,0);start.MinWidth=32;start.MinHeight=32;start.VerticalAlignment=VerticalAlignment.Center;
        start.ToolTip=timer.IsRunning?"Stop timer":"Start timer";AutomationProperties.SetName(start,timer.IsRunning?"Stop timer":"Start timer");AutomationProperties.SetAutomationId(start,"TimerToggle");
        rounded.Text=Duration.HoursText(total,settings.RoundMinutes);rounded.ToolTip=$"Hours, rounded to nearest {settings.RoundMinutes} minutes";rounded.Foreground=text;rounded.FontSize=horizontal?18:14;
        rounded.Visibility=Visible(style.ShowTotal&&settings.TotalDisplay==TotalDisplay.Both);
        bool popped=!popout&&host.IsPoppedOut(timer);
        state.Text=(popped?"↗ POPPED OUT":"")+(active?"  ◉ ALERT":"");state.Foreground=accent;state.Margin=new Thickness(0,0,0,3);state.Visibility=Visible(style.ShowStatus&&(popped||active));
        bool compact=popout&&!wide&&h<150;
        double titleWidth=0;
        if(horizontal)
        {
            double dpi=VisualTreeHelper.GetDpi(this).PixelsPerDip;string key=timer.Name+"|"+timer.Description+"|"+dpi;
            if(titleMeasureKey!=key){titleMeasureKey=key;cachedTitleWidth=new FormattedText(timer.Name,System.Globalization.CultureInfo.CurrentCulture,FlowDirection.LeftToRight,new Typeface(new FontFamily("Segoe UI"),FontStyles.Normal,FontWeights.SemiBold,FontStretches.Normal),14,text,dpi).WidthIncludingTrailingWhitespace;cachedDescriptionWidth=new FormattedText(" — "+timer.Description,System.Globalization.CultureInfo.CurrentCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),14,text,dpi).WidthIncludingTrailingWhitespace;}
            titleWidth=style.ShowTitle?cachedTitleWidth:0;
        }
        rounded.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));
        double roundedWidth=rounded.Visibility==Visibility.Visible?Math.Max(90,rounded.DesiredSize.Width):0;
        double bandMin=ratio*target+(style.ShowControls?38:0);
        double required=bandMin+roundedWidth+titleWidth+(style.ShowControls?145:0)+padding*2+40;
        bool showDescription=style.ShowDescription&&!string.IsNullOrWhiteSpace(timer.Description)&&!compact&&(!horizontal||w>=required+cachedDescriptionWidth);
        DescriptionVisible=showDescription;
        string nextIdentity=$"{timer.Name}|{timer.Description}|{style.ShowTitle}|{compact}|{showDescription}";
        if(identityKey!=nextIdentity){identityKey=nextIdentity;identity.Inlines.Clear();
        if(style.ShowTitle&&!compact)identity.Inlines.Add(new Run(timer.Name){FontWeight=FontWeights.SemiBold});
        if(showDescription)identity.Inlines.Add(new Run((style.ShowTitle?" — ":"")+timer.Description));}
        identity.Foreground=text;identity.Visibility=Visible(identity.Inlines.Count>0);identity.ToolTip=timer.Name+"\n"+timer.Description;
        identity.TextWrapping=horizontal?TextWrapping.NoWrap:TextWrapping.Wrap;identity.TextTrimming=horizontal&&!popout?TextTrimming.None:TextTrimming.CharacterEllipsis;identity.ClipToBounds=true;
        buttons.Visibility=Visible(style.ShowControls&&!compact);alertControls.Visibility=Visible(active&&style.ShowControls&&!compact);
        MinWidth=horizontal&&!popout?Math.Max(250,required):Math.Max(150,ratio*24+(style.ShowControls?38:0)+padding*2);
        // Measure the actual composed rows, including control templates and margins.
        content.Measure(new Size(Math.Max(MinWidth,w),double.PositiveInfinity));
        MinHeight=Math.Ceiling(content.DesiredSize.Height+BorderThickness.Top+BorderThickness.Bottom);
        resize.Visibility=Visible(!popout&&!preview&&!IsList&&settings.AllowIndividualResize);
        returnToWorkspace.IsEnabled=!preview&&popped;
        BorderBrush=popped?accent:Brushes.Transparent;BorderThickness=new Thickness(popped?1:0);
        var requested=TimerSizing.Requested(settings,timer);
        minimumClamped=!popout&&!preview&&(requested.Height<MinHeight||(!IsList&&requested.Width<MinWidth));
        if(ownerWindow is {} window){window.Title=timer.Name+" — T⁴";window.MinWidth=Math.Max(150,ratio*24+(style.ShowControls?38:0)+padding*2+chrome.Width);window.MinHeight=Math.Max(100,MinHeight+chrome.Height);}
    }
}
