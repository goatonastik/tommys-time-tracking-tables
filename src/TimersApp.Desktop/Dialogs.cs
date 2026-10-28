using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Automation;

namespace TimersApp.Desktop;
internal sealed class FormDialog : Window
{
    public StackPanel Fields { get; } = new() { Margin = new Thickness(24) };
    private readonly DockPanel root = new();
    private readonly ScrollViewer scroll;
    public FormDialog(Window? owner, string title, double width = 480)
    {
        SetResourceReference(StyleProperty, typeof(Window));
        if (owner is not null) Owner = owner;
        Title = title; Width = Math.Min(width, SystemParameters.WorkArea.Width - 40); SizeToContent = SizeToContent.Height;
        MaxHeight = Math.Max(400, SystemParameters.WorkArea.Height - 70);
        WindowStartupLocation = owner is null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner;
        scroll = new ScrollViewer { Content = Fields, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        root.Children.Add(scroll); Content = root;
    }
    public void Label(string text) => Fields.Children.Add(new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 7), Foreground = Brushes.LightSteelBlue });
    public TextBox Text(string value, bool multiline = false)
    {
        var box = new TextBox { Text = value, AcceptsReturn = multiline, TextWrapping = TextWrapping.Wrap, MinHeight = multiline ? 75 : 0 };
        LabelControl(box); Fields.Children.Add(box); return box;
    }
    public CheckBox Check(string text, bool value)
    {
        var box = new CheckBox { Content = text, IsChecked = value }; Fields.Children.Add(box); return box;
    }
    public ComboBox Select<T>(IEnumerable<T> items, T value)
    {
        var box = new ComboBox { ItemsSource = items, SelectedItem = value, Padding = new Thickness(8), Foreground = Brushes.Black };
        LabelControl(box); Fields.Children.Add(box); return box;
    }
    private void LabelControl(FrameworkElement control)
    {
        if (Fields.Children.Count > 0 && Fields.Children[Fields.Children.Count - 1] is TextBlock label)
            AutomationProperties.SetLabeledBy(control, label);
    }
    public void Actions(params (string Label, Action Action)[] actions)
    {
        var panel = new WrapPanel { Margin = new Thickness(24, 12, 16, 16) };
        foreach (var action in actions) { var button = new Button { Content = action.Label, Margin = new Thickness(0, 0, 8, 8) }; button.Click += (_, _) => action.Action(); panel.Children.Add(button); }
        DockPanel.SetDock(panel, Dock.Bottom); root.Children.Insert(0, panel);
    }
    public void Preview(UIElement preview, double width = 340)
    {
        root.Children.Remove(scroll);
        var grid = new Grid(); grid.ColumnDefinitions.Add(new ColumnDefinition()); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(width) });
        grid.Children.Add(scroll); Grid.SetColumn(preview, 1); grid.Children.Add(preview); root.Children.Add(grid);
        Height = 730; SizeToContent = SizeToContent.Manual;
    }
    public void TwoColumns(int split)
    {
        var children=Fields.Children.Cast<UIElement>().ToArray();Fields.Children.Clear();
        var columns=new Grid();var left=new StackPanel();var right=new StackPanel();
        columns.ColumnDefinitions.Add(new());columns.ColumnDefinitions.Add(new());
        columns.RowDefinitions.Add(new(){Height=GridLength.Auto});columns.RowDefinitions.Add(new(){Height=GridLength.Auto});
        for(int i=0;i<children.Length;i++)(i<split?left:right).Children.Add(children[i]);
        columns.Children.Add(left);columns.Children.Add(right);Fields.Children.Add(columns);
        bool? previous=null;
        void Reflow(){bool wide=scroll.ActualWidth>=680;if(previous==wide)return;previous=wide;
            Grid.SetColumn(right,wide?1:0);Grid.SetRow(right,wide?0:1);
            columns.ColumnDefinitions[1].Width=wide?new GridLength(1,GridUnitType.Star):new GridLength(0);
            left.Margin=new Thickness(0,0,wide?18:0,0);right.Margin=new Thickness(wide?6:0,0,0,0);
        }
        scroll.SizeChanged+=(_,_)=>Reflow();Reflow();
    }
}
