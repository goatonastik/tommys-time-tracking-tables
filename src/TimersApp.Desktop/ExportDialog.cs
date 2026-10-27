using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using TimersApp.Core;

namespace TimersApp.Desktop;
internal enum ExportMode { All, Today, Range, Custom }
internal static class ExportDialog
{
    internal static ReportOptions Options(ExportMode mode, DateOnly today, string from, string through, Guid[] ids, bool running)
    {
        DateOnly Parse(string text) => DateOnly.ParseExact(text.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var options = mode switch
        {
            ExportMode.Today => new ReportOptions(today, today, IncludeRunning: running),
            ExportMode.Range => new ReportOptions(Parse(from), Parse(through), IncludeRunning: running),
            ExportMode.Custom when ids.Length > 0 => new ReportOptions(null, null, IncludeRunning: running, TimerIds: ids),
            ExportMode.Custom => throw new ArgumentException("Select at least one timer."),
            _ => new ReportOptions(null, null, IncludeRunning: running)
        };
        if (options.From > options.Through) throw new ArgumentException("Start date must be on or before end date.");
        return options;
    }
    public static void Show(MainWindow host)
    {
        var d = new FormDialog(host, "Export recorded time", 620);
        d.Label($"Workspace: {host.Engine.Workspace.Name}. Export includes recorded history and archived timers in this workspace.");
        var modes = new WrapPanel(); d.Fields.Children.Add(modes); var selected = ExportMode.All;
        var range = new StackPanel(); var timers = new StackPanel();
        foreach (var (mode, label) in new[] { (ExportMode.All, "Export all"), (ExportMode.Today, "Only today's"), (ExportMode.Range, "Date range"), (ExportMode.Custom, "Custom") })
        {
            var radio = new RadioButton { Content = label, GroupName = "ExportMode", Margin = new Thickness(0, 8, 16, 8), IsChecked = mode == ExportMode.All };
            radio.Checked += (_, _) => { selected = mode; range.Visibility = mode == ExportMode.Range ? Visibility.Visible : Visibility.Collapsed; timers.Visibility = mode == ExportMode.Custom ? Visibility.Visible : Visibility.Collapsed; };
            modes.Children.Add(radio);
        }
        range.Children.Add(new TextBlock { Text = "Inclusive date range (YYYY-MM-DD)" });
        var from = new TextBox { Text = DateTime.Today.ToString("yyyy-MM-dd") }; var through = new TextBox { Text = from.Text }; range.Children.Add(from); range.Children.Add(through);
        System.Windows.Automation.AutomationProperties.SetName(from,"Start date, YYYY-MM-DD");System.Windows.Automation.AutomationProperties.SetName(through,"End date, inclusive, YYYY-MM-DD");
        timers.Children.Add(new TextBlock { Text = "Select timers (Ctrl/Shift selects several)" });
        var choices = host.Engine.Workspace.Timers.Select(t => new Choice(t.Id, t.Name + (t.Archived ? " (archived)" : "") + " · " + t.Id.ToString("N")[..8])).ToArray();
        var list = new ListBox { ItemsSource = choices, SelectionMode = SelectionMode.Extended, Height = 160, Foreground = Brushes.Black }; timers.Children.Add(list);
        System.Windows.Automation.AutomationProperties.SetName(list,"Select timers to export");
        range.Visibility = timers.Visibility = Visibility.Collapsed; d.Fields.Children.Add(range); d.Fields.Children.Add(timers);
        var running = d.Check("Include snapshots of currently running timers", true);
        d.Label("Exact segment/day details, plus each timer's daily total rounded once to the nearest 15 minutes in decimal hours. Rounded values appear once per timer/day; other cells are blank.");
        d.Actions(("Export…", () =>
        {
            try
            {
                var options = Options(selected, DateOnly.FromDateTime(DateTime.Today), from.Text, through.Text, list.SelectedItems.Cast<Choice>().Select(t => t.Id).ToArray(), running.IsChecked == true);
                string name = string.Concat(host.Engine.Workspace.Name.Select(c => System.IO.Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
                var save = new SaveFileDialog { Filter = "Excel workbook (*.xlsx)|*.xlsx|CSV table (*.csv)|*.csv", FileName = $"{name}-{DateTime.Now:yyyy-MM-dd}.xlsx" };
                if (save.ShowDialog(d) != true) return;
                host.Engine.Checkpoint(); ExcelExport.Write(save.FileName, host.Engine.Workspace, options); d.DialogResult = true;
            }
            catch (Exception ex) { MessageBox.Show(d, ex.Message, "Could not export"); }
        }), ("Cancel", () => d.Close()));
        d.ShowDialog();
    }
    private sealed record Choice(Guid Id, string Name) { public override string ToString() => Name; }
}
