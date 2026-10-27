using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using TimersApp.Core;

namespace TimersApp.Desktop;
internal static class StyleEditor
{
    public static TimerStyle? Edit(MainWindow host,TimerRecord sample,TimerStyle original,string title)
    {
        var editor=Create(host,sample,original,title);return editor.Dialog.ShowDialog()==true?editor.Result():null;
    }
    internal static (Window Dialog,Func<TimerStyle> Result) Create(MainWindow host,TimerRecord sample,TimerStyle original,string title)
    {
        var draft=Appearance.Copy(original);
        var d=new Window{Title=title,Width=Math.Min(1190,SystemParameters.WorkArea.Width-40),Height=Math.Min(850,SystemParameters.WorkArea.Height-60),MinWidth=520,MinHeight=420,WindowStartupLocation=WindowStartupLocation.CenterOwner,Icon=host.Icon};d.SetResourceReference(FrameworkElement.StyleProperty,typeof(Window));if(host.IsLoaded)d.Owner=host;
        var root=new DockPanel{Margin=new Thickness(16),Background=Visuals.Brush("#0E1525")};d.Content=root;
        var top=new WrapPanel{Margin=new Thickness(0,0,0,10)};DockPanel.SetDock(top,Dock.Top);root.Children.Add(top);
        var preset=new ComboBox{Width=170,Foreground=Brushes.Black,Padding=new Thickness(6),Margin=new Thickness(0,0,8,4)};System.Windows.Automation.AutomationProperties.SetName(preset,"Style preset");top.Children.Add(preset);
        Button Button(Panel panel,string text,Action action){var b=new Button{Content=text,Margin=new Thickness(0,0,8,6),Padding=new Thickness(10,6,10,6)};b.Click+=(_,_)=>action();panel.Children.Add(b);return b;}
        var bottom=new WrapPanel{Margin=new Thickness(0,10,0,0)};DockPanel.SetDock(bottom,Dock.Bottom);root.Children.Add(bottom);
        var error=new TextBlock{Foreground=Brushes.Salmon,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,8)};DockPanel.SetDock(error,Dock.Bottom);root.Children.Add(error);
        var grid=new Grid();root.Children.Add(grid);for(int i=0;i<3;i++)grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(i==2?1.15:1,GridUnitType.Star)});
        StackPanel Column(int index){var p=new StackPanel{Margin=new Thickness(8)};var scroll=new ScrollViewer{Content=p,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};Grid.SetColumn(scroll,index);grid.Children.Add(scroll);return p;}
        var left=Column(0);var right=Column(1);var preview=Column(2);
        void Label(Panel p,string text){p.Children.Add(new TextBlock{Text=text,TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSteelBlue,Margin=new Thickness(0,9,0,5)});}
        TextBox Text(Panel p,string label,string value){Label(p,label);var box=new TextBox{Text=value};System.Windows.Automation.AutomationProperties.SetName(box,label);p.Children.Add(box);return box;}
        CheckBox Check(Panel p,string label,bool value){var box=new CheckBox{Content=new TextBlock{Text=label,TextWrapping=TextWrapping.Wrap},IsChecked=value};p.Children.Add(box);return box;}
        ComboBox Select<T>(Panel p,string label,IEnumerable<T> items,T value){Label(p,label);var box=new ComboBox{ItemsSource=items,SelectedItem=value,Foreground=Brushes.Black,Padding=new Thickness(6)};System.Windows.Automation.AutomationProperties.SetName(box,label);p.Children.Add(box);return box;}
        NumberSlider Slider(Panel p,string label,double min,double max,double value){var x=new NumberSlider(label,min,max,value);p.Children.Add(x);return x;}
        var digits=Select(left,"Digit display",Enum.GetValues<DigitStyle>(),draft.Digits);
        var fonts=Select(left,"Text font",Fonts.SystemFontFamilies.OrderBy(f=>f.Source).ToArray(),Fonts.SystemFontFamilies.FirstOrDefault(f=>f.Source.Equals(draft.FontFamily,StringComparison.OrdinalIgnoreCase)));fonts.DisplayMemberPath="";
        var template=new DataTemplate(typeof(FontFamily));var fontRow=new FrameworkElementFactory(typeof(DockPanel));
        var number=new FrameworkElementFactory(typeof(TextBlock));number.SetValue(DockPanel.DockProperty,Dock.Right);number.SetValue(TextBlock.TextProperty,"12:34:56.789");number.SetValue(FrameworkElement.MarginProperty,new Thickness(16,0,0,0));number.SetBinding(TextBlock.FontFamilyProperty,new Binding("."));fontRow.AppendChild(number);
        var fontName=new FrameworkElementFactory(typeof(TextBlock));fontName.SetBinding(TextBlock.TextProperty,new Binding("Source"));fontName.SetBinding(TextBlock.FontFamilyProperty,new Binding("."));fontRow.AppendChild(fontName);template.VisualTree=fontRow;fonts.ItemTemplate=template;fonts.SetValue(VirtualizingPanel.IsVirtualizingProperty,true);fonts.SetValue(ScrollViewer.CanContentScrollProperty,true);
        var size=Slider(left,"Font size",10,200,draft.FontSize);var padding=Slider(left,"Inner spacing",0,80,draft.Padding);var radius=Slider(left,"Corner radius",0,100,draft.CornerRadius);
        var fit=Check(left,"Scale digits to fill available space",draft.FitDigitsToWindow);var color=Text(left,"Text color",draft.TextColor);var accent=Text(left,"Accent color",draft.AccentColor);
        Label(right,"Elapsed units");var unitPanel=new WrapPanel();right.Children.Add(unitPanel);var units=new Dictionary<TimeUnits,CheckBox>();foreach(var unit in Enum.GetValues<TimeUnits>()){var box=Check(unitPanel,unit.ToString(),draft.Units.HasFlag(unit));box.Width=105;box.ToolTip=unit==TimeUnits.Years?"365 elapsed days per year":unit.ToString();units[unit]=box;}
        Label(right,"Visible content");var showTitle=Check(right,"Title",draft.ShowTitle);var description=Check(right,"Description",draft.ShowDescription);var status=Check(right,"Alert / popped-out indicators",draft.ShowStatus);var controls=Check(right,"Buttons",draft.ShowControls);var total=Check(right,"Exact / rounded total details",draft.ShowTotal);
        var hide=Check(right,"Hide unit labels (h, min, s…)",draft.HideUnitLabels);var custom=Check(right,"Use custom number layout",draft.UseCustomFormat);var pattern=Text(right,"Number layout",draft.CustomFormat);Label(right,"H:M:S.MS → 00:00:00.000\nY years · D days · H hours · M minutes · S seconds · MS milliseconds\nUse units once in descending order. Colons, periods and spaces are separators.");
        Label(preview,"LIVE PREVIEW");var card=new TimerCard(host,sample,preview:true){Height=260,HorizontalAlignment=HorizontalAlignment.Stretch};preview.Children.Add(card);Label(preview,"The number layout controls the selected units. Fit-to-space takes priority over nominal font size. Right-click a timer for controls hidden in compact layouts. Save as preset and Delete preset apply immediately; Cancel only discards this editor draft.");
        bool loading=false;
        bool Read()
        {
            if(loading)return true;
            if(!Visuals.ValidColor(color.Text)||!Visuals.ValidColor(accent.Text)){error.Text="Enter a valid text and accent color.";return false;}
            var selected=units.Where(x=>x.Value.IsChecked==true).Aggregate((TimeUnits)0,(a,b)=>a|b.Key);
            if(custom.IsChecked!=true&&selected==0){error.Text="Select at least one elapsed unit.";return false;}
            if(custom.IsChecked==true&&!Appearance.ValidFormat(pattern.Text)){error.Text="Invalid layout. Try H:M:S.MS; units must be unique and in descending order.";return false;}
            draft.Digits=(DigitStyle)digits.SelectedItem;draft.FontFamily=(fonts.SelectedItem as FontFamily)?.Source??draft.FontFamily;draft.FontSize=size.Value;draft.Padding=padding.Value;draft.CornerRadius=radius.Value;draft.FitDigitsToWindow=fit.IsChecked==true;draft.TextColor=color.Text;draft.AccentColor=accent.Text;draft.Units=selected==0?TimeUnits.Hours|TimeUnits.Minutes|TimeUnits.Seconds:selected;draft.ShowTitle=showTitle.IsChecked==true;draft.ShowDescription=description.IsChecked==true;draft.ShowStatus=status.IsChecked==true;draft.ShowControls=controls.IsChecked==true;draft.ShowTotal=total.IsChecked==true;draft.HideUnitLabels=hide.IsChecked==true;draft.UseCustomFormat=custom.IsChecked==true;draft.CustomFormat=pattern.Text.Trim().ToUpperInvariant();
            foreach(var unit in units.Values)unit.IsEnabled=!draft.UseCustomFormat;hide.IsEnabled=!draft.UseCustomFormat;pattern.IsEnabled=draft.UseCustomFormat;error.Text="";card.SetPreview(draft);return true;
        }
        void RefreshPresets(){var id=(preset.SelectedItem as StylePreset)?.Id;preset.ItemsSource=host.Engine.Workspace.Presets.ToArray();preset.SelectedItem=host.Engine.Workspace.Presets.FirstOrDefault(x=>x.Id==id);}
        void Populate()
        {
            loading=true;digits.SelectedItem=draft.Digits;var match=Fonts.SystemFontFamilies.FirstOrDefault(f=>f.Source.Equals(draft.FontFamily,StringComparison.OrdinalIgnoreCase));if(match is null){match=new FontFamily(draft.FontFamily);fonts.ItemsSource=Fonts.SystemFontFamilies.Append(match).OrderBy(f=>f.Source).ToArray();fonts.ToolTip="This font is not installed. Its name is preserved and Windows uses a fallback until it is installed.";}else fonts.ToolTip=null;fonts.SelectedItem=match;size.Value=draft.FontSize;padding.Value=draft.Padding;radius.Value=draft.CornerRadius;fit.IsChecked=draft.FitDigitsToWindow;color.Text=draft.TextColor;accent.Text=draft.AccentColor;
            foreach(var pair in units)pair.Value.IsChecked=draft.Units.HasFlag(pair.Key);showTitle.IsChecked=draft.ShowTitle;description.IsChecked=draft.ShowDescription;status.IsChecked=draft.ShowStatus;controls.IsChecked=draft.ShowControls;total.IsChecked=draft.ShowTotal;hide.IsChecked=draft.HideUnitLabels;custom.IsChecked=draft.UseCustomFormat;pattern.Text=draft.CustomFormat;loading=false;Read();
        }
        Button(top,"Load preset",()=>{if(preset.SelectedItem is StylePreset p){draft=Appearance.Copy(p.Style);Populate();}});
        Button(top,"Save as preset",()=>{if(!Read())return;var n=new FormDialog(d,"Save style preset");n.Label("Preset name");var name=n.Text("My timer style");n.Actions(("Save",()=>{if(string.IsNullOrWhiteSpace(name.Text))return;host.Engine.Workspace.Presets.Add(new(){Name=name.Text.Trim(),Style=Appearance.Copy(draft)});host.AppearanceChanged(false);n.DialogResult=true;}),("Cancel",()=>n.Close()));n.ShowDialog();RefreshPresets();});
        Button(top,"Delete preset",()=>{if(preset.SelectedItem is not StylePreset p)return;if(MessageBox.Show(d,$"Delete preset '{p.Name}'? Timers keep their current appearance.","Delete preset",MessageBoxButton.OKCancel)!=MessageBoxResult.OK)return;foreach(var timer in host.Engine.Workspace.Timers.Where(t=>t.PresetId==p.Id)){if(timer.Style is null)timer.Style=Appearance.Copy(p.Style);timer.PresetId=null;}host.Engine.Workspace.Presets.Remove(p);host.AppearanceChanged(false);RefreshPresets();});
        Button(left,"Background color / image",()=>{var result=BackgroundEditor.Edit(d,draft.Background,host.Assets);if(result is not null){draft.Background=result;Read();}});
        Button(bottom,"Apply",()=>{if(Read())d.DialogResult=true;});Button(bottom,"Cancel",()=>d.Close());
        foreach(var check in units.Values.Concat(new[]{fit,showTitle,description,status,controls,total,hide,custom})){check.Checked+=(_,_)=>Read();check.Unchecked+=(_,_)=>Read();}
        foreach(var box in new[]{color,accent,pattern})box.TextChanged+=(_,_)=>Read();foreach(var slider in new[]{size,padding,radius})slider.ValueChanged+=(_,_)=>Read();digits.SelectionChanged+=(_,_)=>Read();fonts.SelectionChanged+=(_,_)=>Read();
        // Reflow to a single scrolling column on narrow screens instead of clipping fields.
        var allScroll=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto};var stacked=new StackPanel();
        bool narrow=false;d.SizeChanged+=(_,_)=>{bool next=d.ActualWidth<950;if(next==narrow)return;narrow=next;if(next){root.Children.Remove(grid);foreach(var child in grid.Children.Cast<UIElement>().ToArray()){grid.Children.Remove(child);if(child is ScrollViewer sv){var panel=sv.Content;sv.Content=null;stacked.Children.Add((UIElement)panel);}}allScroll.Content=stacked;root.Children.Add(allScroll);}else{root.Children.Remove(allScroll);allScroll.Content=null;foreach(var panel in new[]{left,right,preview}){stacked.Children.Remove(panel);var sv=new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};Grid.SetColumn(sv,grid.Children.Count);grid.Children.Add(sv);}root.Children.Add(grid);}};
        RefreshPresets();Populate();return (d,()=>draft);
    }
}
