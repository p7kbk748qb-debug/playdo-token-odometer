using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using PlayDo.Odometer;

namespace PlayDo.Desktop;
public partial class MainWindow:Window {
 readonly TrackerService service;readonly DispatcherTimer refresh=new(){Interval=TimeSpan.FromSeconds(1)};
 readonly DispatcherTimer motion=new(){Interval=TimeSpan.FromMilliseconds(33)};readonly System.Diagnostics.Stopwatch clock=new();
 readonly Dictionary<string,TextBlock> providerStatus=new();bool initializing=true,exiting;string page="Overview",numberContext="";
 long shown,target,from;bool hasNumber;string lastModels="";
 static readonly string[] Layouts={"Dashboard","Compact","Mini","Tiny"},Scopes={"Today","7 days","Recorded"},Corners={"Free","Top left","Top right","Bottom left","Bottom right"};
 T Find<T>(string name)where T:Control=>this.FindControl<T>(name)!;
 TextBlock Label(string text,double size=13,string? color=null)=>new(){Text=text,FontSize=size,Foreground=color==null?Foreground:color=="#A9B6C6"&&Resources.TryGetResource("Muted",ActualThemeVariant,out var muted)?(IBrush)muted!:Brush.Parse(color)};
 public MainWindow():this(new TrackerService(new[]{"--data",Path.Combine(Path.GetTempPath(),"odometer-design-"+Guid.NewGuid().ToString("N"))})){}
 public MainWindow(TrackerService tracker){
  InitializeComponent();service=tracker;if(Program.Arguments.Contains("--smoke"))service.Settings.Scope="Recorded";
  Find<ComboBox>("LayoutChoice").ItemsSource=Layouts;Find<ComboBox>("LayoutChoice").SelectedItem=Layouts.Contains(service.Settings.Layout)?service.Settings.Layout:"Dashboard";
  Find<ComboBox>("ScopeChoice").ItemsSource=Scopes;Find<ComboBox>("ScopeChoice").SelectedItem=Scopes.Contains(service.Settings.Scope)?service.Settings.Scope:"Today";
  Find<ComboBox>("CornerChoice").ItemsSource=Corners;Find<ComboBox>("CornerChoice").SelectedItem=Corners.Contains(service.Settings.Corner)?service.Settings.Corner:"Free";
  Find<ComboBox>("LayoutChoice").SelectionChanged+=(_,_)=>{if(!initializing)SetLayout(Find<ComboBox>("LayoutChoice").SelectedItem as string??"Dashboard");};
  Find<ComboBox>("ScopeChoice").SelectionChanged+=(_,_)=>{if(!initializing){service.Settings.Scope=Find<ComboBox>("ScopeChoice").SelectedItem as string??"Today";service.Save();Render();}};
  Find<ComboBox>("CornerChoice").SelectionChanged+=(_,_)=>{if(!initializing){service.Settings.Corner=Find<ComboBox>("CornerChoice").SelectedItem as string??"Free";service.Save();Place();}};
  foreach(string name in new[]{"Overview","Providers","Appearance","Settings"}){string selected=name;Find<Button>(name+"Nav").Click+=(_,_)=>Navigate(selected);}
  Find<Button>("WidgetExpand").Click+=(_,_)=>SetLayout("Dashboard");Find<Button>("TinyExpand").Click+=(_,_)=>SetLayout("Dashboard");
  Find<Button>("WidgetSettings").Click+=(_,_)=>{SetLayout("Dashboard");Navigate("Settings");};
  Find<Button>("WidgetHide").Click+=(_,_)=>Hide();Find<Button>("TinyHide").Click+=(_,_)=>Hide();
  Find<Border>("Widget").PointerPressed+=(_,e)=>{
   if(!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)return;
   var node=e.Source as Avalonia.Visual;while(node!=null){if(node is Button)return;node=node.GetVisualParent();}
   if(e.ClickCount==2){SetLayout("Dashboard");return;}
   service.Settings.Corner="Free";service.Save();BeginMoveDrag(e);
  };
  var menu=new ContextMenu();foreach(string name in Layouts){string layout=name;var item=new MenuItem{Header=layout};item.Click+=(_,_)=>SetLayout(layout);menu.Items.Add(item);}
  menu.Items.Add(new Separator());foreach(string period in Scopes){string selected=period;var scopeItem=new MenuItem{Header=period};scopeItem.Click+=(_,_)=>{service.Settings.Scope=selected;Find<ComboBox>("ScopeChoice").SelectedItem=selected;service.Save();Render();};menu.Items.Add(scopeItem);}
  var providerItem=new MenuItem{Header="Providers"};providerItem.Click+=(_,_)=>{SetLayout("Dashboard");Navigate("Providers");};menu.Items.Add(providerItem);var appearanceItem=new MenuItem{Header="Appearance"};appearanceItem.Click+=(_,_)=>{SetLayout("Dashboard");Navigate("Appearance");};menu.Items.Add(appearanceItem);
  menu.Items.Add(new Separator());var copy=new MenuItem{Header="Copy token total"};copy.Click+=async(_,_)=>await Copy();menu.Items.Add(copy);Find<Border>("Widget").ContextMenu=menu;
  Find<Button>("CopyButton").Click+=async(_,_)=>await Copy();
  Find<Button>("ExportButton").Click+=async(_,_)=>await Export();
  Find<Button>("DataButton").Click+=(_,_)=>System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo{FileName=service.DataFolder,UseShellExecute=true});
  Find<Button>("ExitButton").Click+=(_,_)=>{exiting=true;(Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.Shutdown();};
  Closing+=(_,e)=>{if(!exiting&&!Program.Arguments.Contains("--smoke")){e.Cancel=true;Hide();}};
  PropertyChanged+=(_,e)=>{if(e.Property==IsVisibleProperty||e.Property==WindowStateProperty){if(IsVisible&&WindowState!=WindowState.Minimized){refresh.Start();Render();}else{refresh.Stop();Settle();}}};
  BindCheck("TopmostCheck",()=>service.Settings.Topmost,v=>{service.Settings.Topmost=v;Topmost=v;});
  BindCheck("PauseCheck",()=>service.Settings.Paused,v=>service.Pause(v));
  BindCheck("TransparentCheck",()=>service.Settings.Transparent,v=>{service.Settings.Transparent=v;ApplyTheme();});
  BindCheck("QuotaCheck",()=>service.Settings.ShowAllowance,v=>service.Settings.ShowAllowance=v);
  BindCheck("AnimateCheck",()=>service.Settings.Animate,v=>{service.Settings.Animate=v;Settle();});
  BindCheck("MotionCheck",()=>service.Settings.ReduceMotion,v=>{service.Settings.ReduceMotion=v;Settle();});
  Find<TextBlock>("DataPath").Text=service.DataFolder;
  BuildProviders();BuildThemes();BuildFonts();BuildWidgetSettings();ApplyTheme();
  refresh.Tick+=(_,_)=>Render();
  motion.Tick+=(_,_)=>{double t=Math.Clamp(clock.Elapsed.TotalMilliseconds/300,0,1);shown=from+(long)(((decimal)target-from)*(decimal)(1-Math.Pow(1-t,3)));Paint(shown);if(t>=1)motion.Stop();};
  Opened+=(_,_)=>{SetLayout(service.Settings.Layout);Render();if(Program.Arguments.Contains("--smoke"))DispatcherTimer.RunOnce(()=>FinishSmoke(),TimeSpan.FromSeconds(4));};
  KeyDown+=(_,e)=>{if((e.KeyModifiers&(KeyModifiers.Control|KeyModifiers.Meta))==0)return;if(e.Key==Key.T){SetLayout("Tiny");e.Handled=true;}if(e.Key==Key.Escape){SetLayout("Dashboard");e.Handled=true;}};
  SizeChanged+=(_,_)=>Find<UniformGrid>("ThemeGallery").Columns=Bounds.Width<900?2:3;
  initializing=false;refresh.Start();
 }
 void BindCheck(string name,Func<bool> get,Action<bool> set){var box=Find<CheckBox>(name);box.IsChecked=get();box.IsCheckedChanged+=(_,_)=>{if(initializing)return;set(box.IsChecked==true);service.Save();Render();};}
 void Navigate(string name){
  page=name;foreach(string p in new[]{"Overview","Providers","Appearance","Settings"}){Find<StackPanel>(p+"Page").IsVisible=p==name;Find<Button>(p+"Nav").Background=p==name?Brush.Parse("#2A3543"):Brushes.Transparent;}
  Find<TextBlock>("PageTitle").Text=name=="Overview"?"Usage overview":name;
  Find<TextBlock>("PageSubtitle").Text=name switch{"Overview"=>"Your selected tools, in one counter.","Providers"=>"Count the tools you use. Keep the rest off.","Appearance"=>"Build a look that fits your desktop.",_=>"Small windows. Local data. Your choices."};Render();
 }
 void BuildProviders(){
  var panel=Find<StackPanel>("ProviderRows");
  foreach(var provider in service.Providers){
   var body=new StackPanel{Spacing=8};var checkbox=new CheckBox{Content=provider.Name,FontSize=17,FontWeight=FontWeight.Medium,IsChecked=service.Settings.Enabled.GetValueOrDefault(provider.Name)};body.Children.Add(checkbox);
   body.Children.Add(Label(provider.Description,12,"#A9B6C6"));
   var status=Label(provider.Detected?"Detected on this computer":"Not detected · choose a folder or launch the tool",12,"#76D8E5");providerStatus[provider.Name]=status;body.Children.Add(status);
   var folder=new TextBlock{Text=provider.Root,FontSize=11,TextWrapping=TextWrapping.Wrap,Foreground=Brush.Parse("#A9B6C6")};body.Children.Add(folder);
   var actions=new StackPanel{Orientation=Avalonia.Layout.Orientation.Horizontal,Spacing=8};var choose=new Button{Content="Choose folder"};var check=new Button{Content="Check for reports"};actions.Children.Add(choose);actions.Children.Add(check);body.Children.Add(actions);
   checkbox.IsCheckedChanged+=(_,_)=>{service.Select(provider.Name,checkbox.IsChecked==true);Render();};
   check.Click+=(_,_)=>{service.Refresh();Render();};
   choose.Click+=async(_,_)=>{var folders=await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions{Title="Select "+provider.Name+" home folder",AllowMultiple=false});if(folders.Count>0){string? path=folders[0].TryGetLocalPath();if(path!=null){try{service.SetRoot(provider.Name,path);folder.Text=path;Find<TextBlock>("Feedback").Text=service.Note;}catch(IOException){Find<TextBlock>("Feedback").Text="The previous collector is still finishing a read. Try again after it stops.";}}}};
   panel.Children.Add(new Border{Child=body,Background=Brush.Parse("#20262F"),CornerRadius=new CornerRadius(10),Padding=new Thickness(20)});
  }
 }
 sealed record FontOption(string Name){public FontFamily Family=>new(Name);public override string ToString()=>Name;}
 List<FontOption> fonts=new();
 void BuildFonts(){
  var preferred=new[]{"Inter","Segoe UI","SF Pro Text","Helvetica Neue","Aptos","Arial","Bahnschrift","Calibri","Cambria","Candara","Century Gothic","Consolas","Corbel","Courier New","DIN Alternate","Georgia","Gill Sans","Lucida Console","Menlo","Monaco","Palatino","Rockwell","Sitka Text","Tahoma","Times New Roman","Trebuchet MS","Verdana"};
  var installed=FontManager.Current.SystemFonts.Select(x=>x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
  fonts=preferred.Where(x=>x=="Inter"||installed.Contains(x)).Concat(installed.OrderBy(x=>x)).Distinct(StringComparer.OrdinalIgnoreCase).Select(x=>new FontOption(x)).ToList();
  foreach(string name in new[]{"InterfaceFont","CounterFont"}){
   var box=Find<ComboBox>(name);box.ItemsSource=fonts;box.MaxDropDownHeight=330;
   box.ItemTemplate=new FuncDataTemplate<FontOption>((font,_)=>new TextBlock{Text=font!.Name,FontFamily=font.Family,FontSize=16,Margin=new Thickness(2)});
  }
  SyncFonts();
  Find<ComboBox>("InterfaceFont").SelectionChanged+=(_,_)=>{if(initializing||Find<ComboBox>("InterfaceFont").SelectedItem is not FontOption font)return;service.Settings.Font=font.Name;service.Save();ApplyFonts();};
  Find<ComboBox>("CounterFont").SelectionChanged+=(_,_)=>{if(initializing||Find<ComboBox>("CounterFont").SelectedItem is not FontOption font)return;service.Settings.NumberFont=font.Name;service.Save();ApplyFonts();};
 }
 void SyncFonts(){Find<ComboBox>("InterfaceFont").SelectedItem=fonts.FirstOrDefault(x=>x.Name==service.Settings.Font)??fonts[0];Find<ComboBox>("CounterFont").SelectedItem=fonts.FirstOrDefault(x=>x.Name==service.Settings.NumberFont)??fonts[0];}
 void ApplyFonts(){FontFamily=new FontFamily(service.Settings.Font);foreach(string name in new[]{"Total","WidgetNumber","NumberPreview"})Find<TextBlock>(name).FontFamily=new FontFamily(service.Settings.NumberFont);Find<TextBlock>("FontNote").Text=service.Settings.Font+" · "+service.Settings.NumberFont+" · "+fonts.Count+" installed font choices. Unavailable fonts fall back safely.";}
 void BuildThemes(){
  foreach(var theme in Themes.All){var body=new StackPanel{Spacing=7};body.Children.Add(new TextBlock{Text=theme.Name,FontFamily=new FontFamily(theme.Font),FontSize=17,FontWeight=FontWeight.Medium,Foreground=Brush.Parse(theme.Text)});body.Children.Add(new TextBlock{Text="12,345,678",FontFamily=new FontFamily(theme.Numbers),FontSize=24,Foreground=Brush.Parse(theme.Accent)});var button=new Button{Content=body,Background=Brush.Parse(theme.Surface),Margin=new Thickness(0,0,12,12),Padding=new Thickness(16,12),HorizontalAlignment=Avalonia.Layout.HorizontalAlignment.Stretch,VerticalAlignment=Avalonia.Layout.VerticalAlignment.Stretch,MinHeight=106};button.Click+=(_,_)=>{service.Settings.Theme=theme.Name;service.Settings.Font=ResolveFont(theme.Font);service.Settings.NumberFont=ResolveFont(theme.Numbers);service.Save();bool was=initializing;initializing=true;SyncFonts();initializing=was;ApplyTheme();};Find<UniformGrid>("ThemeGallery").Children.Add(button);}
 }
 string ResolveFont(string name)=>fonts.Any(f=>f.Name==name)?name:"Inter";
 void ApplyTheme(){
  var theme=Themes.All.FirstOrDefault(t=>t.Name==service.Settings.Theme)??Themes.All[0];
  Background=service.Settings.Layout=="Dashboard"?Brush.Parse(theme.Canvas):Brushes.Transparent;Foreground=Brush.Parse(theme.Text);
  Find<Grid>("Dashboard").Background=Brush.Parse(theme.Canvas);
  foreach(var pair in new[]{("Canvas",theme.Canvas),("Surface",theme.Surface),("Text",theme.Text),("Muted",theme.Muted),("Accent",theme.Accent),("Signal",theme.Signal),("Stroke",theme.Stroke)})Resources[pair.Item1]=Brush.Parse(pair.Item2);
  foreach(string name in new[]{"Total","WidgetNumber","NumberPreview"})Find<TextBlock>(name).Foreground=Brush.Parse(theme.Accent);
  var color=Color.Parse(theme.Surface);if(service.Settings.Transparent)color=new Color(204,color.R,color.G,color.B);Find<Border>("Widget").Background=new SolidColorBrush(color);
  RequestedThemeVariant=theme.Light?ThemeVariant.Light:ThemeVariant.Dark;Topmost=service.Settings.Topmost;ApplyFonts();
  foreach(var border in Find<StackPanel>("ProviderRows").Children.OfType<Border>()){border.Background=Brush.Parse(theme.Surface);foreach(var label in border.GetVisualDescendants().OfType<TextBlock>())label.Foreground=Brush.Parse(theme.Muted);}
  foreach(var status in providerStatus.Values)status.Foreground=Brush.Parse(theme.Signal);
  foreach(var button in Find<UniformGrid>("ThemeGallery").Children.OfType<Button>()){button.BorderThickness=new Thickness(2);button.BorderBrush=button.Content is StackPanel body&&body.Children[0] is TextBlock title&&title.Text==theme.Name?Brush.Parse(theme.Accent):Brushes.Transparent;}
  foreach(string name in new[]{"Overview","Providers","Appearance","Settings"})Find<Button>(name+"Nav").Background=name==page?Brush.Parse(theme.Surface):Brushes.Transparent;
 }
 public void SetLayout(string requested){
  bool wasSmall=service.Settings.Layout!="Dashboard";string layout=Layouts.Contains(requested)?requested:"Dashboard";Settle();service.Settings.Layout=layout;bool full=layout=="Dashboard",tiny=layout=="Tiny";
  Find<Grid>("Dashboard").IsVisible=full;Find<Border>("Widget").IsVisible=!full;Find<Grid>("WidgetHeader").IsVisible=!tiny;Find<StackPanel>("WidgetDetails").IsVisible=!tiny;Find<StackPanel>("TinyActions").IsVisible=tiny;
  SystemDecorations=full?SystemDecorations.Full:SystemDecorations.None;CanResize=full;
  MinWidth=full?760:tiny?220:320;MinHeight=full?540:tiny?52:140;
  Width=full?1080:tiny?300:layout=="Mini"?360:440;Height=full?780:tiny?68:layout=="Mini"?210:280;
  if(full){
   var screen=Screens.ScreenFromWindow(this)??Screens.Primary;
   if(screen!=null){var area=screen.WorkingArea;double scale=screen.Scaling;Width=Math.Max(760,Math.Min(Width,area.Width/scale-24));Height=Math.Max(540,Math.Min(Height,area.Height/scale-24));if(wasSmall)Position=new PixelPoint(area.X+Math.Max(0,(int)((area.Width-Width*scale)/2)),area.Y+Math.Max(0,(int)((area.Height-Height*scale)/2)));}
  }
  Find<Border>("Widget").Padding=tiny?new Thickness(10,6):new Thickness(18,14);
  Find<TextBlock>("WidgetNumber").Margin=tiny?new Thickness(0,0,40,0):new Thickness(0);
  bool was=initializing;initializing=true;Find<ComboBox>("LayoutChoice").SelectedItem=layout;initializing=was;
  service.Save();numberContext="";ApplyTheme();Render();Place();
 }
 void Place(){if(service.Settings.Layout=="Dashboard"||service.Settings.Corner=="Free")return;var screen=Screens.ScreenFromWindow(this);if(screen==null)return;var area=screen.WorkingArea;double scale=screen.Scaling;bool right=service.Settings.Corner.EndsWith("right"),bottom=service.Settings.Corner.StartsWith("Bottom");Position=new PixelPoint(right?area.Right-(int)(Width*scale)-12:area.X+12,bottom?area.Bottom-(int)(Height*scale)-12:area.Y+12);}
 public void Reveal(){Show();WindowState=WindowState.Normal;Activate();Render();}
 public void RequestExit(){exiting=true;(Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.Shutdown();}
 Tokens Current(Snapshot snapshot)=>Convenience.Total(snapshot,service.Settings.Scope);
 void Render(){
  if(!IsVisible||WindowState==WindowState.Minimized)return;
  var snapshot=service.Combined();var total=Current(snapshot);bool selected=service.Settings.Enabled.Values.Any(v=>v);bool has=snapshot.Recorded.Total>0;
  string context=service.Settings.Scope+"|"+service.Settings.Layout+"|"+string.Join(",",service.Settings.Enabled.Where(x=>x.Value).Select(x=>x.Key));
  bool animate=has&&hasNumber&&context==numberContext&&total.Total>target&&service.Settings.Animate&&!service.Settings.ReduceMotion;
  if(context!=numberContext||total.Total!=target){motion.Stop();if(animate){from=shown;target=total.Total;clock.Restart();motion.Start();}else{shown=target=total.Total;Paint(total.Total,selected&&has);}numberContext=context;hasNumber=has;}
  Find<TextBlock>("Status").Text=snapshot.Status;
  Find<TextBlock>("ScopeLabel").Text=service.Settings.Scope.ToUpperInvariant()+" TOKENS";
  Find<TextBlock>("InputValue").Text=Short(total.Input);Find<TextBlock>("OutputValue").Text=Short(total.Output);Find<TextBlock>("CacheValue").Text=Short(total.Cached);
  Find<TextBlock>("SourceSummary").Text=selected?string.Join("  +  ",service.Settings.Enabled.Where(x=>x.Value).Select(x=>x.Key)):"Choose your tools in Providers";
  Find<TextBlock>("WidgetScope").Text=service.Settings.Scope+" · "+(service.Settings.Enabled.Count(x=>x.Value))+" sources";
  Find<TextBlock>("WidgetBreakdown").Text="Input "+Short(total.Input)+"  ·  Output "+Short(total.Output);
  string allowance=FormatAllowance(snapshot.Quota);Find<TextBlock>("AllowanceText").Text=allowance;RenderWidget(snapshot,total);
  Find<Border>("AllowancePanel").IsVisible=service.Settings.Enabled.GetValueOrDefault("Codex");
  if(snapshot.Quota?.Windows.Count>0){var q=snapshot.Quota.Windows.OrderByDescending(x=>x.Minutes).First();Find<ProgressBar>("Allowance").Value=Expired(q)?0:100-q.Used;}
  foreach(var provider in service.Providers)providerStatus[provider.Name].Text=(service.Settings.Enabled.GetValueOrDefault(provider.Name)?provider.Snapshot.Status:"Not selected")+" · "+provider.Snapshot.Recorded.Total.ToString("N0")+" recorded tokens";
  if(page=="Overview"&&service.Settings.Layout=="Dashboard"){
   string key=service.Settings.Scope+"|"+total.Total+"|"+context;
   if(key!=lastModels){lastModels=key;BuildRows(snapshot,total);}
  }
  if(service.Note.Length>0)Find<TextBlock>("Feedback").Text=service.Note;
 }
 void BuildRows(Snapshot snapshot,Tokens total){
  var models=Find<StackPanel>("ModelRows");models.Children.Clear();
  foreach(var model in snapshot.Models.Select(m=>(model:m,value:service.Settings.Scope=="Today"?m.Today:service.Settings.Scope=="7 days"?m.Week:m.Recorded)).Where(x=>x.value.Total>0).OrderByDescending(x=>x.value.Total).Take(6)){
   var row=new StackPanel{Spacing=5};row.Children.Add(new TextBlock{Text=model.model.Label,TextTrimming=TextTrimming.CharacterEllipsis,FontSize=11,Foreground=Foreground});row.Children.Add(new ProgressBar{Minimum=0,Maximum=100,Value=total.Total==0?0:100.0*model.value.Total/total.Total,Height=4,Foreground=Brush.Parse("#76D8E5")});row.Children.Add(Label(model.value.Total.ToString("N0")+" tokens",11,"#A9B6C6"));models.Children.Add(row);
  }
  if(models.Children.Count==0)models.Children.Add(Label("Model usage appears after local reports arrive.",12,"#A9B6C6"));
  var days=Find<StackPanel>("DayRows");days.Children.Clear();
  for(int i=6;i>=0;i--){var date=DateTime.Today.AddDays(-i);snapshot.Days.TryGetValue(date.ToString("yyyy-MM-dd"),out var amount);var row=new Grid{ColumnDefinitions=new ColumnDefinitions("*,Auto")};row.Children.Add(Label(i==0?"Today":date.ToString("ddd, MMM d"),12,"#A9B6C6"));var value=Label(Short(amount?.Total??0),12);Grid.SetColumn(value,1);row.Children.Add(value);days.Children.Add(row);}
 }
 static bool Expired(QuotaWindow q)=>q.Reset>0&&q.Reset<DateTimeOffset.UtcNow.ToUnixTimeSeconds();
 static string FormatAllowance(Quota? quota){
  if(quota==null||quota.Windows.Count==0)return "Not reported locally yet";
  return string.Join(" · ",quota.Windows.Select(q=>(q.Minutes==300?"5-hour":q.Minutes==10080?"Weekly":q.Minutes+" min")+": "+(Expired(q)?"awaiting update":(100-q.Used).ToString("0.#")+"% remaining")))+((DateTime.UtcNow-quota.At.ToUniversalTime()).TotalMinutes>15?" · older snapshot":"");
 }
 void Paint(long value,bool has=true){string text=has?value.ToString("N0"):"—";Find<TextBlock>("Total").Text=text;Find<TextBlock>("WidgetNumber").Text=text;}
 void Settle(){motion.Stop();shown=target;Paint(target,hasNumber);}
 async Task Copy(){if(Clipboard!=null){await Clipboard.SetTextAsync(Current(service.Combined()).Total.ToString("N0"));Find<TextBlock>("Feedback").Text="Copied the selected total";}}
 async Task Export(){var file=await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions{Title="Export aggregate usage",SuggestedFileName="PlayDo-tokens-"+DateTime.Today.ToString("yyyy-MM-dd")+".csv",FileTypeChoices=new[]{new FilePickerFileType("CSV"){Patterns=new[]{"*.csv"}}}});if(file==null)return;try{await using var stream=await file.OpenWriteAsync();using var writer=new StreamWriter(stream,new UTF8Encoding(true));await writer.WriteAsync(Convenience.Csv(service.Combined(),service.Settings.Scope));Find<TextBlock>("Feedback").Text="CSV saved";}catch(IOException){Find<TextBlock>("Feedback").Text="Could not save CSV. Choose another folder.";}}
 static string Short(long n)=>n>=1000000000?(n/1e9).ToString("0.0")+"B":n>=1000000?(n/1e6).ToString("0.0")+"M":n>=1000?(n/1e3).ToString("0.0")+"K":n.ToString("N0");
 async void FinishSmoke(){
  try{
   foreach(string layout in Layouts){
    SetLayout(layout);await Task.Delay(160);UpdateLayout();
    if(Find<Grid>("Dashboard").IsVisible!=(layout=="Dashboard"))throw new Exception("Layout visibility failed");
    if(layout=="Tiny"&&(Math.Abs(Bounds.Width-300)>2||Math.Abs(Bounds.Height-68)>2))throw new Exception("Tiny bounds failed");
    if(Find<TextBlock>(layout=="Dashboard"?"Total":"WidgetNumber").Text!=Current(service.Combined()).Total.ToString("N0"))throw new Exception("Visible counter does not match selected total");
   }
   foreach(var font in fonts.Take(20)){var preview=Find<ComboBox>("CounterFont").ItemTemplate!.Build(font) as TextBlock;if(preview==null||preview.FontFamily.Name!=font.Name)throw new Exception("Font preview did not use its own family");}
   SetLayout("Dashboard");await Task.Delay(250);UpdateLayout();
   string? capture=TrackerService.Option(Program.Arguments,"--capture");
   if(capture!=null){
    foreach(string route in new[]{"Overview","Providers","Appearance","Settings"}){Navigate(route);await Task.Delay(180);UpdateLayout();using var bitmap=new RenderTargetBitmap(new PixelSize((int)Bounds.Width,(int)Bounds.Height),new Vector(96,96));bitmap.Render(this);bitmap.Save(Path.Combine(Path.GetDirectoryName(capture)!,Path.GetFileNameWithoutExtension(capture)+"-"+route.ToLowerInvariant()+".png"));}
    Navigate("Appearance");UpdateLayout();using var finalImage=new RenderTargetBitmap(new PixelSize((int)Bounds.Width,(int)Bounds.Height),new Vector(96,96));finalImage.Render(this);finalImage.Save(capture);
    SetLayout("Tiny");await Task.Delay(180);UpdateLayout();using var tiny=new RenderTargetBitmap(new PixelSize((int)Bounds.Width,(int)Bounds.Height),new Vector(96,96));tiny.Render(this);tiny.Save(Path.Combine(Path.GetDirectoryName(capture)!,"portable-tiny.png"));
   }
   string? report=TrackerService.Option(Program.Arguments,"--report");if(report!=null)File.WriteAllText(report,System.Text.Json.JsonSerializer.Serialize(new{Version="0.6.0",WindowReady=true,Layouts=Layouts,Fonts=fonts.Count,Total=service.Combined().Recorded.Total,Providers=service.Providers.Count}));
  }catch(Exception e){Console.Error.WriteLine(e);Environment.ExitCode=1;}
  finally{exiting=true;(Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.Shutdown(Environment.ExitCode);}
 }
}
