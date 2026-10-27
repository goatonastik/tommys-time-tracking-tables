using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Automation;
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
    private readonly TextBlock title = new() { FontSize = 18, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly TextBlock description = new() { TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 5, 0, 0) };
    private readonly TextBlock state = new() { FontSize = 11, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 10) };
    private readonly TimeReadout digits = new();
    private readonly Viewbox readout;
    private readonly TextBlock rounded = new() { TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 5, 0, 8) };
    private readonly Button start = new();
    private readonly WrapPanel buttons = new();
    private readonly StackPanel heading = new();
    private readonly StackPanel footer = new();
    private readonly WrapPanel alertControls = new() { Margin = new Thickness(0, 4, 0, 4) };
    private readonly Grid content = new();
    private readonly ImageBackdrop backdrop = new() { IsHitTestVisible = false };
    private readonly Thumb resize = new() { Width = 15, Height = 15, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Cursor = Cursors.SizeNWSE, Opacity = .4, ToolTip = "Drag to resize this timer" };
    private TimerStyle? previewStyle;
    private readonly MenuItem returnToWorkspace = new() { Header = "Return to workspace" };
    public TimerCard(MainWindow host, TimerRecord timer, bool popout = false, bool preview = false)
    {
        this.host = host; this.timer = timer; this.popout = popout; this.preview = preview;
        Focusable = !preview;
        KeyboardNavigation.SetIsTabStop(this, !preview);
        AutomationProperties.SetHelpText(this, "Press Shift+F10 or the menu key for timer controls, styles and history.");
        if (preview) { IsHitTestVisible = false; Focusable = false; KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.None); }
        ClipToBounds = true;
        var layers = new Grid(); Child = layers; layers.Children.Add(backdrop); layers.Children.Add(content);
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        heading.Children.Add(state); heading.Children.Add(title); heading.Children.Add(description); content.Children.Add(heading);
        var view = new Viewbox { Child = digits, Stretch = Stretch.Uniform, Margin = new Thickness(0, 5, 0, 5), HorizontalAlignment = HorizontalAlignment.Stretch };
        readout = view;
        Grid.SetRow(view, 1); content.Children.Add(view);
        footer.Children.Add(rounded); footer.Children.Add(buttons); Grid.SetRow(footer, 2); content.Children.Add(footer);
        Add(alertControls, "Dismiss", "Restore normal appearance and stop sound", () => host.DismissAlert(timer));
        Add(alertControls, "Snooze", "Silence temporarily using this timer’s snooze interval", () => host.SnoozeAlert(timer));
        footer.Children.Add(alertControls);
        start.Click += (_, _) => host.Toggle(timer); buttons.Children.Add(start);
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
            resize.DragDelta += (_, e) => { Width = Math.Clamp(Width + e.HorizontalChange, 170, 1500); Height = Math.Clamp(Height + e.VerticalChange, 100, 1200); timer.Width = Width; timer.Height = Height; host.Relayout(); };
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
    private static void Add(Panel panel, string text, string tip, Action action) { var b = new Button { Content = text, ToolTip = tip, Padding = new Thickness(9, 7, 9, 7) }; AutomationProperties.SetName(b, tip); b.Click += (_, _) => action(); panel.Children.Add(b); }
    private static void Item(ContextMenu menu, string text, Action action) { var item = new MenuItem { Header = text }; item.Click += (_, _) => action(); menu.Items.Add(item); }
    private static Visibility Visible(bool value) => value ? Visibility.Visible : Visibility.Collapsed;
    public void Refresh()
    {
        var style = previewStyle ?? Appearance.Resolve(host.Engine.Workspace, timer, popout);
        var text = Visuals.Brush(style.TextColor); var accent = Visuals.Brush(style.AccentColor);
        content.Margin = new Thickness(style.Padding); CornerRadius = new CornerRadius(style.CornerRadius);
        if (ActualWidth > 0 && ActualHeight > 0) Clip = new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight), style.CornerRadius, style.CornerRadius);
        backdrop.Update(style.Background);
        title.Text = timer.Name; title.ToolTip = timer.Name; title.Foreground = text;
        returnToWorkspace.IsEnabled = !preview && host.IsPoppedOut(timer);
        if (popout && !preview && Window.GetWindow(this) is { } window) window.Title = timer.Name + " — T⁴";
        description.Text = timer.Description; description.ToolTip = timer.Description; description.Foreground = text; description.Opacity = .75;
        var total = preview ? TimeSpan.FromSeconds(4120.375) : host.Engine.Total(timer); var settings = host.Engine.Workspace.Settings;
        bool active = !preview && timer.Alert.Enabled && timer.AlertState.Active;
        var color = active && timer.Alert.ChangeColor ? Visuals.Brush(timer.Alert.Color) : text;
        digits.Update(Appearance.Format(settings.TotalDisplay == TotalDisplay.Rounded ? Duration.Round(total, settings.RoundMinutes) : total, style.Units), style, color);
        readout.StretchDirection = style.FitDigitsToWindow ? StretchDirection.Both : StretchDirection.DownOnly;
        digits.Opacity = active && timer.Alert.Flash ? (DateTime.UtcNow.Millisecond < 500 ? 1 : .15) : 1;
        rounded.Text = settings.TotalDisplay switch { TotalDisplay.Both => $"≈ {Appearance.Format(Duration.Round(total, settings.RoundMinutes), style.Units)} · nearest {settings.RoundMinutes} min", TotalDisplay.Rounded => $"Rounded · nearest {settings.RoundMinutes} min", _ => "Exact running total" };
        rounded.Foreground = text; rounded.Opacity = .75;
        state.Text = (timer.IsRunning ? "● RUNNING" : "● STOPPED") + (!popout && host.IsPoppedOut(timer) ? "    ↗ POPPED OUT" : "") + (active ? "    ◉ ALERT" : "");
        state.Foreground = timer.IsRunning ? accent : text;
        start.Background = accent; start.Foreground = Brushes.Black;
        start.Content = timer.IsRunning ? "Stop" : timer.Segments.Any(x => x.RunId == timer.RunId) ? "Resume" : "Start";
        double height = ActualHeight > 0 ? ActualHeight : Height;
        bool compact = double.IsFinite(height) && height < 170;
        title.Visibility = Visible(style.ShowTitle && (!compact || height > 125));
        description.Visibility = Visible(style.ShowDescription && !compact && !string.IsNullOrWhiteSpace(timer.Description));
        state.Visibility = Visible(style.ShowStatus && !compact);
        rounded.Visibility = Visible(style.ShowTotal && !compact);
        buttons.Visibility = Visible(style.ShowControls && !compact && (ActualWidth == 0 || ActualWidth > 245));
        alertControls.Visibility = Visible(active && style.ShowControls && !compact);
        BorderBrush = !popout && host.IsPoppedOut(timer) ? accent : Brushes.Transparent;
        BorderThickness = new Thickness(!popout && host.IsPoppedOut(timer) ? 1 : 0);
        ToolTip = $"{timer.Name}\n{timer.Description}\nExact total: {Duration.Format(total)}\nRight-click for controls, styles and history.";
    }
}
