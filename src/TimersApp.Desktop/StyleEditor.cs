using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TimersApp.Core;

namespace TimersApp.Desktop;
internal static class StyleEditor
{
    public static TimerStyle? Edit(MainWindow host, TimerRecord sample, TimerStyle original, string title)
    {
        var draft = Appearance.Copy(original);
        var d = new FormDialog(host, title, 880);
        var previewCard = new TimerCard(host, sample, preview: true) { Height = 270, Width = 300 };
        var preview = new StackPanel { Margin = new Thickness(8, 24, 20, 20) };
        preview.Children.Add(new TextBlock { Text = "LIVE PREVIEW", Foreground = Brushes.LightSteelBlue, Margin = new Thickness(0, 0, 0, 16) }); preview.Children.Add(previewCard);
        preview.Children.Add(new TextBlock { Text = "Resize a card or pop-out to scale its display. Right-click always provides controls, including in time-only mode.", TextWrapping = TextWrapping.Wrap, Foreground = Brushes.LightSteelBlue, Margin = new Thickness(0, 18, 0, 0) });
        d.Preview(preview);
        d.Label("Start from a preset");
        var presets = new ComboBox { ItemsSource = host.Engine.Workspace.Presets, Padding = new Thickness(8), Foreground = Brushes.Black }; d.Fields.Children.Add(presets);
        var load = new Button { Content = "Load preset into editor", Margin = new Thickness(0, 8, 0, 0) }; d.Fields.Children.Add(load);
        d.Label("Digit display"); var digits = d.Select(Enum.GetValues<DigitStyle>(), draft.Digits);
        d.Label("Text font"); var fonts = d.Select(Fonts.SystemFontFamilies.Select(f => f.Source).Order().ToArray(), draft.FontFamily); fonts.IsEditable = true;
        d.Label("Text size (10–200)"); var size = d.Text(draft.FontSize.ToString());
        var fit = d.Check("Scale digits to fill the available space", draft.FitDigitsToWindow);
        d.Label("Text color (name or #RRGGBB)"); var textColor = d.Text(draft.TextColor);
        d.Label("Accent color"); var accent = d.Text(draft.AccentColor);
        var bgButton = new Button { Content = "Background color / image…", Margin = new Thickness(0, 16, 0, 4) }; d.Fields.Children.Add(bgButton);
        d.Label("Elapsed units — months are excluded");
        var units = new Dictionary<TimeUnits, CheckBox>();
        foreach (var unit in Enum.GetValues<TimeUnits>()) units[unit] = d.Check(unit == TimeUnits.Years ? "Years (365 elapsed days per year)" : unit.ToString(), draft.Units.HasFlag(unit));
        d.Label("Visible content");
        var showTitle = d.Check("Title", draft.ShowTitle); var description = d.Check("Description", draft.ShowDescription);
        var status = d.Check("Running / popped-out status", draft.ShowStatus); var controls = d.Check("Buttons", draft.ShowControls); var total = d.Check("Exact / rounded total details", draft.ShowTotal);
        d.Label("Inner spacing (0–80)"); var padding = d.Text(draft.Padding.ToString());
        d.Label("Corner radius (0–100)"); var radius = d.Text(draft.CornerRadius.ToString());
        var error = new TextBlock { Foreground = Brushes.Salmon, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0) }; d.Fields.Children.Add(error);
        bool Read()
        {
            if (!double.TryParse(size.Text, out double n) || !double.IsFinite(n) || n < 10 || n > 200 || !double.TryParse(padding.Text, out double p) || !double.IsFinite(p) || p < 0 || p > 80 || !double.TryParse(radius.Text, out double r) || !double.IsFinite(r) || r < 0 || r > 100 || !Visuals.ValidColor(textColor.Text) || !Visuals.ValidColor(accent.Text))
            { error.Text = "Check the color and size values before applying."; return false; }
            var selectedUnits = units.Where(x => x.Value.IsChecked == true).Aggregate((TimeUnits)0, (a, b) => a | b.Key);
            if (selectedUnits == 0) { error.Text = "Select at least one time unit."; return false; }
            if (!Fonts.SystemFontFamilies.Any(f => f.Source.Equals(fonts.Text, StringComparison.OrdinalIgnoreCase))) { error.Text = "Choose one of the installed fonts."; return false; }
            draft.Digits = (DigitStyle)digits.SelectedItem; draft.FontFamily = fonts.Text; draft.FontSize = n; draft.FitDigitsToWindow = fit.IsChecked == true;
            draft.TextColor = textColor.Text; draft.AccentColor = accent.Text; draft.Units = selectedUnits; draft.Padding = p; draft.CornerRadius = r;
            draft.ShowTitle = showTitle.IsChecked == true; draft.ShowDescription = description.IsChecked == true; draft.ShowStatus = status.IsChecked == true; draft.ShowControls = controls.IsChecked == true; draft.ShowTotal = total.IsChecked == true;
            error.Text = ""; previewCard.SetPreview(draft); return true;
        }
        bool loading = false;
        void Update() { if (!loading) Read(); }
        void Populate()
        {
            loading = true;
            digits.SelectedItem = draft.Digits; fonts.Text = draft.FontFamily; size.Text = draft.FontSize.ToString(); fit.IsChecked = draft.FitDigitsToWindow;
            textColor.Text = draft.TextColor; accent.Text = draft.AccentColor;
            foreach (var pair in units) pair.Value.IsChecked = draft.Units.HasFlag(pair.Key);
            showTitle.IsChecked = draft.ShowTitle; description.IsChecked = draft.ShowDescription; status.IsChecked = draft.ShowStatus; controls.IsChecked = draft.ShowControls; total.IsChecked = draft.ShowTotal;
            padding.Text = draft.Padding.ToString(); radius.Text = draft.CornerRadius.ToString(); loading = false; Read();
        }
        load.Click += (_, _) => { if (presets.SelectedItem is StylePreset preset) { draft = Appearance.Copy(preset.Style); Populate(); } };
        foreach (var box in new[] { size, textColor, accent, padding, radius }) box.TextChanged += (_, _) => Update();
        foreach (var check in units.Values.Concat(new[] { fit, showTitle, description, status, controls, total })) { check.Checked += (_, _) => Update(); check.Unchecked += (_, _) => Update(); }
        digits.SelectionChanged += (_, _) => Update(); fonts.LostKeyboardFocus += (_, _) => Update(); fonts.SelectionChanged += (_, _) => Update();
        bgButton.Click += (_, _) => { var result = BackgroundEditor.Edit(d, draft.Background, host.Assets); if (result is not null) { draft.Background = result; Read(); } };
        d.Actions(("Apply", () => { if (Read()) d.DialogResult = true; }), ("Save as preset…", () =>
        {
            if (!Read()) return;
            var nameDialog = new FormDialog(d, "Save style preset"); nameDialog.Label("Preset name"); var name = nameDialog.Text("My timer style");
            nameDialog.Actions(("Save preset", () => { if (string.IsNullOrWhiteSpace(name.Text)) return; host.Engine.Workspace.Presets.Add(new StylePreset { Name = name.Text.Trim(), Style = Appearance.Copy(draft) }); host.AppearanceChanged(false); nameDialog.DialogResult = true; }), ("Cancel", () => nameDialog.Close())); nameDialog.ShowDialog();
        }), ("Cancel", () => d.Close()));
        Populate(); return d.ShowDialog() == true ? draft : null;
    }
}
