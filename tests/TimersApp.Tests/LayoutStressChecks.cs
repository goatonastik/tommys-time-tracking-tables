using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TimersApp.Core;
using TimersApp.Desktop;

internal static class LayoutStressChecks
{
    internal static void Run(string artifacts)
    {
        string? previous = Environment.GetEnvironmentVariable("TIMERSAPP_DATA_DIR");
        string folder = Path.Combine(artifacts, "layout-stress-" + Guid.NewGuid().ToString("N"));
        var seed = new Workspace { Name = "Twelve timer layout test" };
        seed.Settings.AutoSave = false;
        seed.Settings.ExitChoice = ExitChoice.Save;
        string[] names =
        [
            "Short", "Client meeting and follow-up decisions for the entire project",
            "Research", "Invoices", "Design review", "Extended display",
            "Planning", "Administrative work", "Deep work", "Testing",
            "Notes", "Final check"
        ];
        string[] descriptions =
        [
            "Quick note", "A short description", "Research sources, compare approaches, and record the outcome for the weekly report.",
            "DocumentationAndResearchWithoutSpacesShouldStillStayInsideTheTimerCardEvenWhenTheWindowIsNarrow",
            "First line of a note\nSecond line with more detail\nThird line for the handoff",
            "Years, days, hours, minutes, seconds and milliseconds", "",
            "A description with punctuation: estimates, invoices; and follow-ups.",
            "Concentrated work on the primary task.", "Verify several timer sizes and layouts.",
            "This description is deliberately long enough to need wrapping in a narrow card. The full wording should remain available from the timer tooltip or editor.",
            "End of the twelve-timer group."
        ];
        for (int i = 0; i < names.Length; i++)
        {
            var timer = new TimerRecord { Name = names[i], Description = descriptions[i] };
            TimeSpan elapsed = i == 5 ? TimeSpan.FromDays(400) + TimeSpan.FromHours(3) + TimeSpan.FromMinutes(4) + TimeSpan.FromSeconds(5) + TimeSpan.FromMilliseconds(678)
                : TimeSpan.FromSeconds(65 + i * 3671);
            timer.Segments.Add(new Segment { RunId = timer.RunId, Name = timer.Name, Description = timer.Description,
                Start = DateTimeOffset.Now - elapsed, End = DateTimeOffset.Now, ElapsedTicks = elapsed.Ticks });
            if (i == 5) timer.Style = new TimerStyle { Digits = DigitStyle.SevenSegment,
                Units = TimeUnits.Years | TimeUnits.Days | TimeUnits.Hours | TimeUnits.Minutes | TimeUnits.Seconds | TimeUnits.Milliseconds };
            seed.Timers.Add(timer);
        }
        new WorkspaceStore(folder).Save(seed);
        Environment.SetEnvironmentVariable("TIMERSAPP_DATA_DIR", folder);
        MainWindow? window = null;
        try
        {
            window = new MainWindow();
            var root = (FrameworkElement)window.Content;
            var panel = (TimerGridPanel)window.FindName("Cards");
            var scroll = (ScrollViewer)window.FindName("WorkspaceScroll");
            var cases = new (string Name, int Width, int Height, bool List, double Scale)[]
            {
                ("grid-normal", 1220, 820, false, 1),
                ("grid-narrow", 480, 620, false, 1),
                ("grid-small-timers", 1220, 820, false, .65),
                ("grid-min-timers", 1220, 820, false, .25),
                ("list-normal", 1220, 820, true, 1),
                ("list-medium", 760, 620, true, 1),
                ("list-narrow", 480, 620, true, 1),
                ("list-small-timers", 480, 620, true, .65)
            };
            var reports = new List<object>();
            foreach (var scenario in cases)
            {
                window.Engine.Workspace.Settings.ListLayout = scenario.List;
                window.Engine.Workspace.Settings.TimerScale = scenario.Scale;
                window.RefreshWorkspaceView();
                root.Width = scenario.Width; root.Height = scenario.Height;
                root.Measure(new Size(scenario.Width, scenario.Height));
                root.Arrange(new Rect(0, 0, scenario.Width, scenario.Height));
                root.UpdateLayout();
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
                root.UpdateLayout();
                var cards = panel.Children.Cast<TimerCardPresenter>().Select(p => p.Card).ToArray();
                if (cards.Length != 12) throw new Exception($"Expected 12 cards in {scenario.Name}, got {cards.Length}");
                if (cards.Any(card => card.ActualWidth + .1 < card.MinWidth || card.ActualHeight + .1 < card.MinHeight || card.DigitHeight < 23.5))
                    throw new Exception($"Card minimum or digit height violated in {scenario.Name}");
                var cardReports = cards.Select((card, index) =>
                {
                    var title = Field<TextBlock>(card, "titleText");
                    var description = Field<TextBlock>(card, "descriptionText");
                    var identity = Field<TextBlock>(card, "identity");
                    var band = Field<FrameworkElement>(card, "bandView");
                    return new
                    {
                        index = index + 1, name = names[index], cardWidth = Math.Round(card.ActualWidth, 1),
                        cardHeight = Math.Round(card.ActualHeight, 1), minimumWidth = Math.Round(card.MinWidth, 1),
                        minimumHeight = Math.Round(card.MinHeight, 1), digitHeight = Math.Round(card.DigitHeight, 1),
                        bandWidth = Math.Round(band.ActualWidth, 1), titleWidth = Math.Round(title.ActualWidth, 1),
                        titleLikelyTruncated = Truncated(title, false), descriptionWidth = Math.Round(description.ActualWidth, 1),
                        descriptionLikelyTruncated = Truncated(description, false),
                        identityWidth = Math.Round(identity.ActualWidth, 1),
                        identityLikelyTruncated = Truncated(identity, true),
                        descriptionVisible = card.DescriptionVisible,
                        unusedCardHeight = Math.Round(Math.Max(0, card.ActualHeight - card.MinHeight), 1)
                    };
                }).ToArray();
                reports.Add(new
                {
                    scenario = scenario.Name, scenario.Width, scenario.Height, scenario.Scale,
                    columns = panel.Columns, viewportWidth = Math.Round(scroll.ViewportWidth, 1),
                    panelWidth = Math.Round(panel.ActualWidth, 1), panelHeight = Math.Round(panel.ActualHeight, 1),
                    horizontalOverflow = panel.ActualWidth > scroll.ViewportWidth + 1,
                    cards = cardReports
                });
                Render(root, scenario.Width, scenario.Height, Path.Combine(artifacts, "stress-" + scenario.Name + ".png"));
                Render(panel, (int)Math.Ceiling(panel.ActualWidth), (int)Math.Ceiling(panel.ActualHeight),
                    Path.Combine(artifacts, "stress-" + scenario.Name + "-all.png"));
            }
            File.WriteAllText(Path.Combine(artifacts, "stress-layout-report.json"), JsonSerializer.Serialize(reports, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally
        {
            if (window is not null) window.Close();
            Environment.SetEnvironmentVariable("TIMERSAPP_DATA_DIR", previous);
        }
    }

    private static T Field<T>(object owner, string name) where T : class =>
        (T)typeof(TimerCard).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;

    private static bool Truncated(TextBlock text, bool wrap)
    {
        string content = string.IsNullOrEmpty(text.Text) ? string.Concat(text.Inlines.OfType<Run>().Select(run => run.Text)) : text.Text;
        if (text.Visibility != Visibility.Visible || text.ActualWidth <= 0 || string.IsNullOrEmpty(content)) return false;
        var copy = new TextBlock { Text = content, FontFamily = text.FontFamily, FontSize = text.FontSize,
            FontWeight = text.FontWeight, TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap };
        copy.Measure(new Size(wrap ? text.ActualWidth : double.PositiveInfinity, double.PositiveInfinity));
        return wrap ? copy.DesiredSize.Height > text.ActualHeight + 1 : copy.DesiredSize.Width > text.ActualWidth + 1;
    }

    private static void Render(FrameworkElement element, int width, int height, string path)
    {
        if (width <= 0 || height <= 0 || width > 4000 || height > 4000) throw new Exception($"Unexpected render size {width}x{height}");
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(path); encoder.Save(file);
    }
}
