using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
namespace PlayDo.Odometer {
 public sealed class Dashboard {
  public readonly Window Window;readonly Collector collector;readonly string settingsPath;readonly Settings settings;
  readonly DispatcherTimer ageTimer;volatile bool visible;int queued;string scope="Today",previous="",view="Dashboard",appearanceReturnView;
  bool paused,changingChoices,allModels;double fullWidth=1040,fullHeight=800;string actionMessage="";DateTime actionAt;
  readonly Dictionary<string,Button> themeCards=new Dictionary<string,Button>();
  public event Action ExitRequested;
  T Find<T>(string name) where T:class{return Window.FindName(name) as T;}
  void Text(string name,string value){Find<TextBlock>(name).Text=value;}
  void Click(string name,RoutedEventHandler handler){Find<Button>(name).Click+=handler;}
  static void Resource(FrameworkElement element,DependencyProperty property,string key){element.SetResourceReference(property,key);}
  TextBlock Label(string text,double size,string brush){var label=new TextBlock {Text=text,FontSize=size};Resource(label,TextBlock.ForegroundProperty,brush);return label;}
  void Choice(string name,string[] options,string selected){var box=Find<ComboBox>(name);box.ItemsSource=options;box.SelectedItem=selected;}
  public Dashboard(Collector c,string root,Settings prefs,string prefsPath) {
   collector=c;settings=prefs;settingsPath=prefsPath;
   using(var stream=typeof(Dashboard).Assembly.GetManifestResourceStream("MainWindow.xaml"))Window=(Window)XamlReader.Load(stream);
   string icon=Path.Combine(root,"assets","app-icon.png");if(File.Exists(icon))Window.Icon=new BitmapImage(new Uri(icon));
   settings.Normalize();Appearance.Apply(Window,settings);UpdateAppearanceNote();
   Choice("ThemeChoice",Appearance.Themes,settings.Theme);Choice("FontChoice",Appearance.Fonts,settings.Font);Choice("NumberFontChoice",Appearance.Fonts,settings.NumberFont);Choice("CounterChoice",Appearance.Counters,settings.Counter);Choice("ViewChoice",Appearance.Views,settings.View);BuildThemeGallery();
   Find<ComboBox>("ThemeChoice").SelectionChanged+=delegate{if(!changingChoices)SelectPreset(Convert.ToString(Find<ComboBox>("ThemeChoice").SelectedItem));};Find<ComboBox>("FontChoice").SelectionChanged+=delegate{AppearanceChanged();};Find<ComboBox>("NumberFontChoice").SelectionChanged+=delegate{AppearanceChanged();};Find<ComboBox>("CounterChoice").SelectionChanged+=delegate{AppearanceChanged();};
   Find<ComboBox>("ViewChoice").SelectionChanged+=delegate{if(!changingChoices){appearanceReturnView=null;SetView(Convert.ToString(Find<ComboBox>("ViewChoice").SelectedItem));}};
   Click("TodayButton",delegate{SetScope("Today");});Click("WeekButton",delegate{SetScope("7 days");});Click("AllButton",delegate{SetScope("Recorded");});
   Click("CompactButton",delegate{SetView("Compact");});Click("MiniButton",delegate{SetView("Mini");});Click("ExpandButton",delegate{SetView("Dashboard");});
   Click("CompactStyleButton",delegate{OpenAppearance();});
   Click("SettingsButton",delegate{var panel=Find<Border>("SettingsPanel");if(panel.Visibility==Visibility.Visible)CloseSettings();else{appearanceReturnView=null;OpenSettings();}});
   Click("CloseSettingsButton",delegate{CloseSettings();});
   Click("RestorePresetButton",delegate{SelectPreset(settings.Theme);});Click("CopyButton",delegate{CopyCurrentTotal();});Click("ExportButton",delegate{ExportCurrentScope();});
   var compactMenu=new ContextMenu{Resources=Window.Resources};foreach(string option in new[]{"Today","7 days","Recorded"}){string selected=option;var item=new MenuItem{Header=option};item.Click+=delegate{SetScope(selected);};compactMenu.Items.Add(item);}
   compactMenu.Items.Add(new Separator());var copyItem=new MenuItem{Header="Copy total    Ctrl+Shift+C"};copyItem.Click+=delegate{CopyCurrentTotal();};compactMenu.Items.Add(copyItem);var exportItem=new MenuItem{Header="Export CSV    Ctrl+E"};exportItem.Click+=delegate{ExportCurrentScope();};compactMenu.Items.Add(exportItem);var styleItem=new MenuItem{Header="Style studio    Ctrl+,"};styleItem.Click+=delegate{OpenAppearance();};compactMenu.Items.Add(styleItem);Find<Border>("CompactView").ContextMenu=compactMenu;
   Click("CompactMenuButton",delegate{compactMenu.PlacementTarget=Find<Button>("CompactMenuButton");compactMenu.Placement=PlacementMode.Bottom;compactMenu.IsOpen=true;});
   Window.PreviewKeyDown+=delegate(object sender,KeyEventArgs e){if(HandleShortcut(e.Key,Keyboard.Modifiers))e.Handled=true;};
   Text("AboutText","PlayDo's Token Odometer · "+typeof(Dashboard).Assembly.GetName().Version.ToString(3)+" · local, private and portable");
   Click("MoreModelsButton",delegate{allModels=!allModels;Render();});
   Click("PauseButton",delegate{TogglePause();});Click("RescanButton",delegate{collector.Refresh();});Click("FolderButton",delegate{System.Diagnostics.Process.Start("explorer.exe","\""+root+"\"");});Click("ExitButton",delegate{if(ExitRequested!=null)ExitRequested();});
   Find<CheckBox>("TopmostCheck").IsChecked=prefs.Topmost;Window.Topmost=prefs.Topmost;
   Find<CheckBox>("MotionCheck").IsChecked=prefs.ReduceMotion;Find<CheckBox>("StartupCheck").IsChecked=Shell.StartupEnabled();
   Find<CheckBox>("TopmostCheck").Click+=delegate{settings.Topmost=Find<CheckBox>("TopmostCheck").IsChecked==true;Window.Topmost=settings.Topmost;SaveSettings();};
   Find<CheckBox>("MotionCheck").Click+=delegate{settings.ReduceMotion=Find<CheckBox>("MotionCheck").IsChecked==true;SaveSettings();};
   Find<CheckBox>("StartupCheck").Click+=delegate{try{Shell.SetStartup(Find<CheckBox>("StartupCheck").IsChecked==true);Text("SettingsNote","Startup preference saved. Closing the window keeps tracking in the tray.");}catch{Find<CheckBox>("StartupCheck").IsChecked=Shell.StartupEnabled();Text("SettingsNote","Windows could not save the startup shortcut. You can still open the app normally.");}};
   ageTimer=new DispatcherTimer {Interval=TimeSpan.FromMinutes(1)};ageTimer.Tick+=delegate{Render();};
   Window.IsVisibleChanged+=delegate{visible=Window.IsVisible&&Window.WindowState!=WindowState.Minimized;if(visible){Render();ageTimer.Start();}else ageTimer.Stop();};
   Window.StateChanged+=delegate{visible=Window.IsVisible&&Window.WindowState!=WindowState.Minimized;if(visible){ageTimer.Start();Render();}else ageTimer.Stop();};
   collector.Changed+=delegate{if(!visible||Interlocked.Exchange(ref queued,1)!=0)return;Window.Dispatcher.BeginInvoke(new Action(delegate{Interlocked.Exchange(ref queued,0);if(visible)Render();}),DispatcherPriority.Background);};
   Window.SourceInitialized+=delegate{Shell.TitleBar(Window,Appearance.Get(settings.Theme).Dark);};Window.SizeChanged+=delegate{AdaptHeader();};
  }
  void BuildThemeGallery(){
   var gallery=Find<UniformGrid>("ThemeGallery");foreach(var preset in Appearance.Presets){
    string name=preset.Name;var previewSettings=new Settings();Appearance.ChoosePreset(previewSettings,name);var body=new StackPanel{Margin=new Thickness(14,10,14,11)};
    var title=new TextBlock{Text=name,FontFamily=Appearance.Font(preset.Font),Foreground=Appearance.Brush(preset.Colors[7]),FontSize=17,FontWeight=FontWeights.SemiBold};body.Children.Add(title);
    var number=new Viewbox{Child=Appearance.Counter(previewSettings,"12,345",true),Stretch=Stretch.Uniform,StretchDirection=StretchDirection.DownOnly,Height=46,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,6,0,5)};body.Children.Add(number);
    body.Children.Add(new TextBlock{Text=preset.Description,FontFamily=Appearance.Font(preset.Font),Foreground=Appearance.Brush(preset.Colors[8]),FontSize=11,TextWrapping=TextWrapping.Wrap,MinHeight=28});
    var design=new Grid();design.Children.Add(new Border{Background=Appearance.Detail(preset),IsHitTestVisible=false});design.Children.Add(body);
    var plate=new Border{Background=Appearance.Material(preset),CornerRadius=new CornerRadius(Math.Min(preset.Radius,16)),Child=design};
    var card=new Button{Content=plate,Style=(Style)Window.Resources["ThemeCardStyle"],Margin=new Thickness(3),BorderBrush=Appearance.Brush(preset.Colors[9]),Tag=name,MinHeight=135};AutomationProperties.SetName(card,name+" style: "+preset.Description);card.ToolTip=preset.Font+" + "+preset.NumberFont+" · "+preset.Counter;card.Click+=delegate{SelectPreset(name);};themeCards[name]=card;gallery.Children.Add(card);
   }UpdateGallerySelection();
  }
  void UpdateGallerySelection(){foreach(var pair in themeCards){pair.Value.BorderThickness=new Thickness(pair.Key==settings.Theme?3:1);AutomationProperties.SetItemStatus(pair.Value,pair.Key==settings.Theme?"Selected preset":"Apply preset");}}
  void SyncAppearanceChoices(){changingChoices=true;Find<ComboBox>("ThemeChoice").SelectedItem=settings.Theme;Find<ComboBox>("FontChoice").SelectedItem=settings.Font;Find<ComboBox>("NumberFontChoice").SelectedItem=settings.NumberFont;Find<ComboBox>("CounterChoice").SelectedItem=settings.Counter;changingChoices=false;}
  public void SelectPreset(string name){Appearance.ChoosePreset(settings,name);SyncAppearanceChoices();ApplyAppearance();}
  void ApplyAppearance(){Appearance.Apply(Window,settings);UpdateAppearanceNote();UpdateGallerySelection();previous="";SaveSettings();Render();}
  void OpenSettings(){Find<Border>("SettingsPanel").Visibility=Visibility.Visible;Find<ScrollViewer>("FullView").ScrollToTop();Button card;if(themeCards.TryGetValue(settings.Theme,out card))card.Focus();}
  public void OpenAppearance(){if(view!="Dashboard"){appearanceReturnView=view;ApplyView("Dashboard",false);changingChoices=true;Find<ComboBox>("ViewChoice").SelectedItem=settings.View;changingChoices=false;}else if(Find<Border>("SettingsPanel").Visibility!=Visibility.Visible)appearanceReturnView=null;Show();OpenSettings();}
  void AdaptHeader(){bool narrow=Window.ActualWidth<900;var actions=Find<StackPanel>("HeaderActions");Grid.SetRow(actions,narrow?1:0);Grid.SetColumn(actions,narrow?0:1);Grid.SetColumnSpan(actions,narrow?2:1);Grid.SetColumnSpan(Find<StackPanel>("HeaderBrand"),narrow?2:1);actions.HorizontalAlignment=narrow?HorizontalAlignment.Left:HorizontalAlignment.Right;actions.Margin=new Thickness(0,narrow?14:0,0,0);Find<UniformGrid>("ThemeGallery").Columns=narrow?2:3;Find<UniformGrid>("AppearanceChoices").Columns=narrow?2:3;}
  void CloseSettings(){Find<Border>("SettingsPanel").Visibility=Visibility.Collapsed;var returnTo=appearanceReturnView;appearanceReturnView=null;if(returnTo!=null&&returnTo!="Dashboard")SetView(returnTo);}
  void UpdateAppearanceNote(){string fallback=Appearance.Font(settings.Font).Source==settings.Font&&Appearance.Font(settings.NumberFont).Source==settings.NumberFont?"":" · unavailable fonts use Segoe UI";Text("AppearanceNote",settings.Theme+" · "+settings.Font+" / "+settings.NumberFont+" · "+settings.Counter+fallback);}
  void AppearanceChanged(){
   if(changingChoices)return;settings.Font=Appearance.FontName(Convert.ToString(Find<ComboBox>("FontChoice").SelectedItem));settings.NumberFont=Appearance.FontName(Convert.ToString(Find<ComboBox>("NumberFontChoice").SelectedItem));settings.Counter=Appearance.Choice(Convert.ToString(Find<ComboBox>("CounterChoice").SelectedItem),Appearance.Counters);ApplyAppearance();
  }
  public void SetScope(string requested){scope=requested=="7 days"||requested=="Recorded"?requested:"Today";actionMessage="";Render();}
  public string CurrentScope {get{return scope;}}
  void Feedback(string value){actionMessage=value;actionAt=DateTime.UtcNow;Text("ActionFeedback",value);Text("CompactStatus",value);}
  public void CopyCurrentTotal(){try{Clipboard.SetText(Convenience.CountText(collector.GetSnapshot(),scope));Feedback("Copied "+scope.ToLowerInvariant()+" total");}catch{Feedback("Clipboard is busy. Try Copy total again.");}}
  public void ExportCurrentScope(){try{var dialog=new Microsoft.Win32.SaveFileDialog{Title="Export aggregate token usage",Filter="CSV file (*.csv)|*.csv",DefaultExt=".csv",AddExtension=true,FileName="PlayDo-tokens-"+scope.ToLowerInvariant().Replace(" ","-")+"-"+DateTime.Today.ToString("yyyy-MM-dd")+".csv"};if(dialog.ShowDialog(Window)!=true)return;File.WriteAllText(dialog.FileName,Convenience.Csv(collector.GetSnapshot(),scope),new System.Text.UTF8Encoding(true));Feedback("CSV saved · "+scope.ToLowerInvariant());}catch{Feedback("Could not save CSV. Choose a writable folder and try again.");}}
  public bool HandleShortcut(Key key,ModifierKeys modifiers){
   if(key==Key.Escape&&modifiers==ModifierKeys.None&&Find<Border>("SettingsPanel").Visibility==Visibility.Visible){CloseSettings();return true;}
   if(modifiers==ModifierKeys.Control){if(key==Key.D1||key==Key.NumPad1){SetScope("Today");return true;}if(key==Key.D2||key==Key.NumPad2){SetScope("7 days");return true;}if(key==Key.D3||key==Key.NumPad3){SetScope("Recorded");return true;}if(key==Key.OemComma){OpenAppearance();return true;}if(key==Key.M){ToggleCompact();return true;}if(key==Key.E){ExportCurrentScope();return true;}}
   if(modifiers==(ModifierKeys.Control|ModifierKeys.Shift)){if(key==Key.C){CopyCurrentTotal();return true;}if(key==Key.M){SetView("Mini");return true;}}return false;
  }
  void SaveSettings(){try{settings.Save(settingsPath);}catch{Text("SettingsNote","Could not save preferences. Check the app folder permissions.");}}
  public void TogglePause(){paused=!paused;collector.Pause(paused);Find<Button>("PauseButton").Content=paused?"Resume tracking":"Pause tracking";}
  public void Show(){Window.Show();if(Window.WindowState==WindowState.Minimized)Window.WindowState=WindowState.Normal;Window.Activate();Render();}
  public void ToggleCompact(){SetView(view=="Dashboard"?"Compact":"Dashboard");}
  public void RestoreView(){ApplyView(settings.View,false);}
  public void SetView(string requested){appearanceReturnView=null;ApplyView(requested,true);}
  void ApplyView(string requested,bool save) {
   string next=Appearance.Choice(requested,Appearance.Views);if(view=="Dashboard"&&next!="Dashboard"){fullWidth=Window.Width;fullHeight=Window.Height;}
   view=next;bool full=next=="Dashboard",mini=next=="Mini";if(!full&&Window.WindowState!=WindowState.Normal)Window.WindowState=WindowState.Normal;
   Window.MinWidth=full?760:mini?340:380;Window.MinHeight=full?640:mini?164:220;
   Find<ScrollViewer>("FullView").Visibility=full?Visibility.Visible:Visibility.Collapsed;Find<Border>("CompactView").Visibility=full?Visibility.Collapsed:Visibility.Visible;
   Find<Border>("CompactView").Padding=new Thickness(mini?14:20,mini?10:16,mini?14:20,mini?10:16);Find<Viewbox>("CompactCounterBox").Margin=new Thickness(0,mini?5:14,0,mini?5:14);Find<TextBlock>("CompactStatus").FontSize=mini?11:12;
   Window.Width=full?fullWidth:mini?360:440;Window.Height=full?fullHeight:mini?174:260;
   changingChoices=true;Find<ComboBox>("ViewChoice").SelectedItem=next;changingChoices=false;
   if(save){settings.View=next;SaveSettings();}previous="";Render();
  }
  static string Count(long n){return n.ToString("N0",CultureInfo.CurrentCulture);}
  static string Short(long n){return n>=1000000000?(n/1e9).ToString("0.0")+"B":n>=1000000?(n/1e6).ToString("0.0")+"M":n>=1000?(n/1e3).ToString("0.0")+"K":n.ToString();}
  static string Age(DateTime at){double min=Math.Max(0,(DateTime.UtcNow-at.ToUniversalTime()).TotalMinutes);return min<1?"just now":min<60?((int)min)+"m ago":min<1440?((int)(min/60))+"h ago":((int)(min/1440))+"d ago";}
  Tokens Select(BreakdownView item){return item==null?new Tokens():scope=="Today"?item.Today:scope=="7 days"?item.Week:item.Recorded;}
  void RenderDigits(long n,bool hasData) {
   string value=hasData?Count(n):"—",key=view+"|"+settings.Theme+"|"+settings.Counter+"|"+settings.NumberFont+"|"+value;if(previous==key)return;previous=key;
   var row=Find<StackPanel>(view=="Dashboard"?"Digits":"CompactDigits");row.Children.Clear();row.Children.Add(Appearance.Counter(settings,value,false));
   AutomationProperties.SetName(row,hasData?Count(n)+" tokens":"No usage recorded yet");
  }
  void RenderQuota(Snapshot s) {
   var q=s.Quota;if(q==null||q.Windows.Count==0){Text("QuotaValue","Not yet reported");Text("QuotaTitle","Account allowance");Text("QuotaAge","No snapshot");Text("QuotaReset","A quota snapshot appears when Codex reports one.");Text("QuotaExtra","Reported separately from tokens");Find<ProgressBar>("QuotaBar").Value=0;return;}
   var w=q.Windows.OrderByDescending(x=>x.Minutes).First();bool expired=w.Reset>0&&DateTime.UtcNow>=new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).AddSeconds(w.Reset);bool stale=(DateTime.UtcNow-q.At).TotalMinutes>15;
   Text("QuotaTitle",w.Minutes==10080?"Weekly allowance":w.Minutes==300?"5-hour allowance":w.Minutes>0?w.Minutes+"-minute allowance":"Account allowance");Text("QuotaValue",expired?"Awaiting update":(100-w.Used).ToString("0.#")+"% remaining");Text("QuotaAge",(expired?"Expired snapshot · ":stale?"Older snapshot · ":"Reported ")+Age(q.At));Find<ProgressBar>("QuotaBar").Value=expired?0:100-w.Used;
   Text("QuotaReset",w.Reset>0?(expired?"Previous reset: ":"Resets ")+new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).AddSeconds(w.Reset).ToLocalTime().ToString("ddd, MMM d · h:mm tt") : "Reset time unavailable");var other=q.Windows.FirstOrDefault(x=>!Object.ReferenceEquals(x,w));bool otherExpired=other!=null&&other.Reset>0&&DateTime.UtcNow>=new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).AddSeconds(other.Reset);
   Text("QuotaExtra",other!=null?(other.Minutes==300?"5-hour":other.Minutes==10080?"Weekly":other.Minutes+"-minute")+": "+(otherExpired?"awaiting update":(100-other.Used).ToString("0.#")+"% last reported"):"Reported separately from tokens");
  }
  void RenderModels(Snapshot s,long total) {
   Text("ModelScope",scope);var list=Find<StackPanel>("ModelList");list.Children.Clear();var models=(s.Models??new List<BreakdownView>()).Where(x=>Select(x).Total>0).OrderByDescending(x=>Select(x).Total).ToList();
   int count=allModels?models.Count:Math.Min(8,models.Count);for(int i=0;i<count;i++) {
    var model=models[i];var tokens=Select(model);double share=total>0?tokens.Total/(double)total*100:0;var row=new StackPanel {Margin=new Thickness(0,0,0,13)};var header=new Grid();header.ColumnDefinitions.Add(new ColumnDefinition());header.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
    var label=Label(model.Label??model.Id,13,"Text");label.TextTrimming=TextTrimming.CharacterEllipsis;label.Margin=new Thickness(0,0,10,0);header.Children.Add(label);var usage=Label(Short(tokens.Total)+" · "+share.ToString("0.#")+"%",12,"Muted");usage.VerticalAlignment=VerticalAlignment.Center;Grid.SetColumn(usage,1);header.Children.Add(usage);row.Children.Add(header);
    var bar=new ProgressBar {Minimum=0,Maximum=100,Value=Math.Min(100,share),Height=4,BorderThickness=new Thickness(0),Margin=new Thickness(0,7,0,0)};Resource(bar,ProgressBar.BackgroundProperty,"Control");Resource(bar,ProgressBar.ForegroundProperty,i==0?"Accent":"ChartBar");row.Children.Add(bar);row.ToolTip=(model.Label??model.Id)+": "+Count(tokens.Total)+" tokens ("+share.ToString("0.#")+"% of "+scope.ToLowerInvariant()+")";AutomationProperties.SetName(row,row.ToolTip.ToString());list.Children.Add(row);
   }
   if(models.Count==0)list.Children.Add(Label(s.Indexing?"Reading model metadata…":"No model usage in this period",13,"Muted"));
   Find<Button>("MoreModelsButton").Visibility=models.Count>8?Visibility.Visible:Visibility.Collapsed;Find<Button>("MoreModelsButton").Content=allModels?"Show top 8":"Show all "+models.Count+" models";
   Text("ModelNote","Approximate attribution from local metadata. Unknown includes usage whose model was not reported.");
  }
  public void Render() {
   if(!visible||Window.WindowState==WindowState.Minimized)return;var s=collector.GetSnapshot();var totals=scope=="Today"?s.Today:scope=="7 days"?s.Week:s.Recorded;bool has=s.Recorded.Total>0||s.Events>0;
   Text("StatusText",s.Status);Text("ScopeLabel",scope=="Today"?"Today's reported tokens":scope=="7 days"?"Tokens across 7 days":"Tokens in local records");
   foreach(var pair in new[]{new[]{"TodayButton","Today"},new[]{"WeekButton","7 days"},new[]{"AllButton","Recorded"}}){var button=Find<Button>(pair[0]);Resource(button,Button.BackgroundProperty,scope==pair[1]?"Selected":"Control");Resource(button,Button.ForegroundProperty,scope==pair[1]?"Accent":"Text");}
   string feedback=actionMessage.Length>0&&(DateTime.UtcNow-actionAt).TotalSeconds<12?actionMessage:"";Text("ActionFeedback",feedback);
   Text("CompactScope",(view=="Mini"?"Tokens":"Token Odometer")+" · "+scope);Text("CompactStatus",feedback.Length>0?feedback:s.Paused?"Paused · Style → Resume tracking":s.Indexing?"Importing local history…":s.LastEvent==DateTime.MinValue?(has?"Saved history · awaiting new usage":"Waiting for Codex usage"):"Last usage "+Age(s.LastEvent));RenderDigits(totals.Total,has);if(view!="Dashboard")return;
   Text("TotalAccessible",has?(s.LastEvent==DateTime.MinValue?"Input + output · preserved local history; latest event unavailable":"Input + output · last usage "+Age(s.LastEvent)):"No usage imported yet. Keep Codex open while the collector starts.");Text("InputText",has?Count(totals.Input):"—");Text("OutputText",has?Count(totals.Output):"—");Text("CacheText",has?Count(totals.Cached):"—");RenderQuota(s);RenderModels(s,totals.Total);
   var chart=Find<Grid>("Chart");chart.Children.Clear();chart.ColumnDefinitions.Clear();long max=1;for(int i=0;i<7;i++){Tokens d;if(s.Days.TryGetValue(DateTime.Today.AddDays(i-6).ToString("yyyy-MM-dd"),out d))max=Math.Max(max,d.Total);}
   for(int i=0;i<7;i++){chart.ColumnDefinitions.Add(new ColumnDefinition());DateTime date=DateTime.Today.AddDays(i-6);Tokens d;long n=s.Days.TryGetValue(date.ToString("yyyy-MM-dd"),out d)?d.Total:0;var col=new Grid {Margin=new Thickness(3,0,3,0)};col.RowDefinitions.Add(new RowDefinition{Height=new GridLength(22)});col.RowDefinitions.Add(new RowDefinition());col.RowDefinitions.Add(new RowDefinition{Height=new GridLength(24)});var number=Label(Short(n),11,"Muted");number.HorizontalAlignment=HorizontalAlignment.Center;col.Children.Add(number);
    var bar=new Border {Height=n==0?2:Math.Max(4,n/(double)max*82),CornerRadius=new CornerRadius(3,3,0,0),VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(9,0,9,0)};Resource(bar,Border.BackgroundProperty,i==6?"Accent":"ChartBar");Grid.SetRow(bar,1);col.Children.Add(bar);var label=Label(i==6?"Today":date.ToString("ddd"),11,"Muted");label.HorizontalAlignment=HorizontalAlignment.Center;label.Margin=new Thickness(0,6,0,0);Grid.SetRow(label,2);col.Children.Add(label);col.ToolTip=date.ToString("MMM d")+": "+Count(n)+" tokens";AutomationProperties.SetName(col,col.ToolTip.ToString());Grid.SetColumn(col,i);chart.Children.Add(col);
   }
   Text("ChartSummary",has?Count(s.Week.Total)+" tokens recorded over 7 days":"History will appear as records are imported.");var list=Find<StackPanel>("SessionList");list.Children.Clear();foreach(var session in s.Sessions.Take(3)){var row=new Grid {Margin=new Thickness(0,0,0,10)};row.ColumnDefinitions.Add(new ColumnDefinition());row.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});row.Children.Add(Label(session.Id.Length>8?session.Id.Substring(0,8):session.Id,14,"Text"));var number=Label(Short(session.Total)+" tokens",13,"Muted");Grid.SetColumn(number,1);row.Children.Add(number);row.ToolTip=Count(session.Total)+" tokens · last usage "+Age(session.At);list.Children.Add(row);}
   if(s.Sessions.Count==0)list.Children.Add(Label("No sessions imported yet",13,"Muted"));Text("CoverageText",s.Status+" · "+s.Files+" local files · "+s.Compressed+" compressed archives excluded");
  }
  public void Capture(string path){Window.UpdateLayout();var bitmap=new RenderTargetBitmap((int)Window.ActualWidth,(int)Window.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(Window);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using(var file=File.Create(path))encoder.Save(file);}
 }
}
