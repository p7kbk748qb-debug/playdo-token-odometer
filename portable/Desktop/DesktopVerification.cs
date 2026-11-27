using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Interactivity;
using System.Text.Json;

namespace PlayDo.Desktop;
public partial class MainWindow {
 void CaptureSurface(string path){
  StopEffects();UpdateLayout();var root=Find<Grid>("Root");
  using var bitmap=new RenderTargetBitmap(new PixelSize((int)root.Bounds.Width,(int)root.Bounds.Height),new Vector(96,96));bitmap.Render(root);bitmap.Save(path);
 }
 async Task VerifyAppearanceStudio(string? capture){
  Navigate("Appearance");
  var prefs=service.Settings;
  var ink=Find<ComboBox>("NumberInk");
  var detail=Find<ComboBox>("FaceDetail");
  var looks=Find<ComboBox>("SavedLooks");
  var name=Find<TextBox>("LookName");
  var counter=Find<ComboBox>("CounterFont");
  var face=Find<ComboBox>("InterfaceFont");
  const string example="Night shift";
  var originalDetail=detail.SelectedItem;
  string? originalInk=ink.SelectedItem as string;
  var originalFace=face.SelectedItem;
  var originalCounter=counter.SelectedItem;
  string originalTheme=prefs.Theme;
  var gallery=Find<Avalonia.Controls.Primitives.UniformGrid>("ThemeGallery");
  var originalPreset=Array.FindIndex(Themes.All,t=>t.Name==originalTheme);
  static void Click(Button button){
   if(!button.IsEffectivelyEnabled)throw new Exception("Studio button disabled: "+button.Name);
   button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
  }
  void SelectPreset(int i)=>Click((Button)gallery.Children[i]);
  Color NumberColor(string control)=>((ISolidColorBrush)Find<TextBlock>(control).Foreground!).Color;
  static string OtherSettings(Preferences p)=>JsonSerializer.Serialize(new{
   p.Layout,p.Scope,p.Corner,p.Topmost,p.Transparent,p.WidgetBackground,p.Animate,p.Effects,
   p.ReduceMotion,p.Paused,p.ShowAllowance,p.Enabled,p.Roots,p.Mini,p.Compact
  });
  Preferences ReadSaved()=>JsonSerializer.Deserialize<Preferences>(File.ReadAllText(Path.Combine(service.DataFolder,"preferences.json")))!;
  async Task Screenshot(string file,Control control){
   if(capture==null)return;
   control.BringIntoView();
   await Task.Delay(160);
   CaptureSurface(Path.Combine(Path.GetDirectoryName(capture)!,file));
  }
  if(Program.Arguments.Contains("--verify-legacy-settings")&&
     (originalTheme!="Instrument"||originalInk!="Theme default"||!Equals(originalDetail,"Preset")||prefs.FaceDetail!=null||looks.ItemCount!=0||
      prefs.WidgetBackground!=null||!prefs.Transparent||!prefs.ReduceMotion||prefs.Mini.ShowPrimary||
      !prefs.Compact.ShowReset||prefs.ShowAllowance||prefs.Effects))
   throw new Exception("Legacy preferences changed when loading Appearance Studio");
  string unrelated=OtherSettings(prefs);
  SelectPreset(0);
  ink.SelectedItem="Theme default";
  var defaultColor=NumberColor("NumberPreview");
  string defaultDetail=Find<MeterFace>("StudioFace").Detail;
  ink.SelectedItem="Violet";
  var violet=NumberColor("NumberPreview");
  if(defaultColor==violet||NumberColor("Total")!=violet||NumberColor("WidgetNumber")!=violet)
   throw new Exception("Custom number ink did not reach all three counters");
  detail.SelectedItem="Dots";
  if(Find<MeterFace>("StudioFace").Detail!="Dots"||Find<MeterFace>("DashboardFace").Detail!="Dots")
   throw new Exception("Counter face detail did not reach the studio and dashboard");
  face.SelectedIndex=Math.Min(1,fonts.Count-1);
  counter.SelectedIndex=Math.Min(2,fonts.Count-1);
  var selectedFace=face.SelectedItem;
  var selectedCounter=counter.SelectedItem;
  string faceName=prefs.Font,counterName=prefs.NumberFont,themeName=prefs.Theme;
  name.Text=example;
  await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(()=>{},Avalonia.Threading.DispatcherPriority.Background);
  if(Program.Arguments.Contains("--verify-legacy-settings")){
   // Block the atomic temp file in this run's fresh synthetic data directory.
   string blocker=Path.Combine(service.DataFolder,"preferences.json.tmp");
   if(Path.Exists(blocker))throw new Exception("Unexpected preferences temp file before failure check");
   Directory.CreateDirectory(blocker);
   try{
    Click(Find<Button>("SaveLook"));
    if(!Find<TextBlock>("LookStatus").IsVisible||Find<TextBlock>("LookStatus").Text?.Contains(service.Note)!=true||service.Note.Length==0||
       name.Text!=example||prefs.SavedLooks?.Any(look=>look.Name==example)==true||
       looks.Items.Cast<object>().Contains(example)||!Find<Button>("SaveLook").IsEffectivelyEnabled)
     throw new Exception("Failed save lost the typed name, added an unsaved look, or disabled retry");
   }finally{Directory.Delete(blocker);}
  }
  Click(Find<Button>("SaveLook"));
  looks.SelectedItem=example;
  if(!Equals(looks.SelectedItem,example))throw new Exception("Saved look missing from picker");
  var saved=ReadSaved().SavedLooks?.SingleOrDefault(look=>look.Name==example);
  if(saved==null||saved.Theme!=themeName||saved.Font!=faceName||saved.NumberFont!=counterName||saved.NumberInk!="Violet"||saved.FaceDetail!="Dots")
   throw new Exception("Saved look failed its disk round trip");
  await Screenshot("appearance-studio-look.png",looks);
  SelectPreset(1);
  face.SelectedIndex=0;counter.SelectedIndex=0;
  ink.SelectedItem="Cyan";detail.SelectedItem="Plain";
  looks.SelectedItem=example;
  Click(Find<Button>("ApplyLook"));
  var applied=ReadSaved();
  if(applied.Theme!=themeName||applied.Font!=faceName||applied.NumberFont!=counterName||applied.NumberInk!="Violet"||applied.FaceDetail!="Dots"||!Equals(detail.SelectedItem,"Dots")||
     !Equals(face.SelectedItem,selectedFace)||!Equals(counter.SelectedItem,selectedCounter)||
     !Equals(ink.SelectedItem,"Violet")||NumberColor("NumberPreview")!=violet)
   throw new Exception("Applying a saved look did not restore the appearance controls and preferences");
  await Screenshot("appearance-studio-ink.png",Find<TextBlock>("NumberPreview"));
  ink.SelectedItem="Theme default";
  detail.SelectedItem="Preset";
  if(ReadSaved().FaceDetail!=null||Find<MeterFace>("StudioFace").Detail!=defaultDetail||Find<MeterFace>("DashboardFace").Detail!=defaultDetail)
   throw new Exception("Reset did not resolve the preset counter face detail");
  if(ReadSaved().NumberInk!=null||NumberColor("NumberPreview")!=defaultColor||NumberColor("WidgetNumber")!=defaultColor||NumberColor("Total")!=defaultColor)
   throw new Exception("Theme default did not restore counter ink");
  looks.SelectedItem=example;
  if(Program.Arguments.Contains("--verify-legacy-settings")){
   string blocker=Path.Combine(service.DataFolder,"preferences.json.tmp");
   if(Path.Exists(blocker))throw new Exception("Unexpected preferences temp file before removal check");
   Directory.CreateDirectory(blocker);
   try{
    Click(Find<Button>("RemoveLook"));
    if(!Find<TextBlock>("LookStatus").IsVisible||Find<TextBlock>("LookStatus").Text?.Contains(service.Note)!=true||service.Note.Length==0||
       !Equals(looks.SelectedItem,example)||prefs.SavedLooks?.Any(look=>look.Name==example)!=true||
       !Find<Button>("RemoveLook").IsEffectivelyEnabled)
     throw new Exception("Failed removal discarded the saved look or disabled retry");
   }finally{Directory.Delete(blocker);}
  }
  Click(Find<Button>("RemoveLook"));
  if(looks.Items.Cast<object>().Contains(example)||ReadSaved().SavedLooks?.Any(look=>look.Name==example)==true)
   throw new Exception("Removed look remains in settings or picker");
  if(OtherSettings(prefs)!=unrelated||OtherSettings(ReadSaved())!=unrelated)
   throw new Exception("Appearance controls changed unrelated settings");
  if(originalPreset>=0)SelectPreset(originalPreset);
  face.SelectedItem=originalFace;counter.SelectedItem=originalCounter;ink.SelectedItem=originalInk;detail.SelectedItem=originalDetail;
  name.Text="";
 }
 async Task VerifyDashboard(string? capture){
  await VerifyAppearanceStudio(capture);
  bool transparent=service.Settings.Transparent;
  string? backgroundMode=service.Settings.WidgetBackground;service.Settings.WidgetBackground=null;
  service.Settings.Transparent=true;
  foreach(string layout in new[]{"Mini","Compact","Tiny"}){
   SetLayout(layout);await Task.Delay(80);UpdateLayout();
   if(Background is not ISolidColorBrush windowBackground||windowBackground.Color.A!=0)throw new Exception("Transparent "+layout+" retained the window's own background layer: "+Background);
  }
  if(Find<Border>("Widget").Background is not ISolidColorBrush clear||clear.Color.A!=0||Find<Border>("Widget").BorderThickness!=default)throw new Exception("Transparent widget retained a painted background or border");
  if(capture!=null){
   CaptureSurface(Path.Combine(Path.GetDirectoryName(capture)!,"widget-transparent.png"));
   // Include the native control template, not only Root: it can paint behind a clear panel.
   using var windowImage=new RenderTargetBitmap(new PixelSize((int)Bounds.Width,(int)Bounds.Height),new Vector(96,96));windowImage.Render(this);windowImage.Save(Path.Combine(Path.GetDirectoryName(capture)!,"window-transparent.png"));
  }
  SetWidgetBackground("Glass");UpdateLayout();
  var savedBackground=System.Text.Json.JsonSerializer.Deserialize<Preferences>(File.ReadAllText(Path.Combine(service.DataFolder,"preferences.json")));
  if(savedBackground?.WidgetBackground!="Glass")throw new Exception("Glass background choice did not persist");
  if(Find<Border>("Widget").Background is not LinearGradientBrush glass||glass.GradientStops.Any(stop=>stop.Color.A==0||stop.Color.A==255)||Find<Border>("Widget").BorderThickness!=new Thickness(1))throw new Exception("Glass widget must have a translucent tint and outline");
  if(Find<TextBlock>("WidgetNumber").Foreground is not ISolidColorBrush numbers||numbers.Color.A!=255)throw new Exception("Glass mode faded the counter text");
  if(capture!=null)CaptureSurface(Path.Combine(Path.GetDirectoryName(capture)!,"widget-glass.png"));
  SetWidgetBackground("Solid");
  if(Find<Border>("Widget").Background is not ISolidColorBrush solid||solid.Color.A!=255)throw new Exception("Solid widget did not restore opaque background");
  service.Settings.Transparent=transparent;service.Settings.WidgetBackground=backgroundMode;
  bool wasInitializing=initializing;initializing=true;Find<ComboBox>("BackgroundChoice").SelectedItem=WidgetBackgroundMode;initializing=wasInitializing;SetLayout("Dashboard");
  if(Background is not ISolidColorBrush dashboardBackground||dashboardBackground.Color.A!=255)throw new Exception("Dashboard did not restore its opaque window background");
  Navigate("Overview");Render();
  if(Find<Grid>("DayChart").Children.Count!=7||Find<Avalonia.Controls.Primitives.UniformGrid>("ProviderSummary").Children.Count!=4)throw new Exception("Dashboard chart or provider summary missing");
  if(Program.Arguments.Contains("--verify-legacy-settings")){
   var days=service.Combined().Days;
   int activeDays=Enumerable.Range(0,7).Count(i=>days.GetValueOrDefault(DateTime.Today.AddDays(-i).ToString("yyyy-MM-dd"))?.Total>0);
   if(activeDays<5)throw new Exception("Synthetic chart fixture covers fewer than five days");
  }
  string originalTheme=service.Settings.Theme;
  foreach(var theme in Themes.All){service.Settings.Theme=theme.Name;ApplyTheme();Render();await Task.Delay(40);UpdateLayout();}
  service.Settings.Theme=originalTheme;ApplyTheme();Render();
  bool reduce=service.Settings.ReduceMotion;service.Settings.ReduceMotion=true;StartEffects(true);
  if(effects.IsEnabled||Find<StackPanel>("PageContent").Opacity!=1)throw new Exception("Reduced motion did not settle interface effects");
  service.Settings.ReduceMotion=reduce;
  Hide();if(effects.IsEnabled||refresh.IsEnabled||motion.IsEnabled)throw new Exception("Hidden UI left animation or refresh running");Reveal();
  Width=760;Height=740;await Task.Delay(180);ArrangeAnalytics();UpdateLayout();
  if(Find<StackPanel>("OverviewPage").Bounds.Width>Find<StackPanel>("PageContent").Bounds.Width+1)throw new Exception("Narrow dashboard overflows horizontally");
  if(Grid.GetRow(Find<Border>("AllowancePanel"))!=1)throw new Exception("Narrow analytics layout failed");
  if(capture!=null)CaptureSurface(Path.Combine(Path.GetDirectoryName(capture)!,"dashboard-narrow.png"));
  service.Settings.Theme="Paper";ApplyTheme();Render();if(capture!=null)CaptureSurface(Path.Combine(Path.GetDirectoryName(capture)!,"dashboard-light.png"));
  service.Settings.Theme=originalTheme;ApplyTheme();Width=1080;Height=780;await Task.Delay(180);ArrangeAnalytics();Render();
  bool codex=service.Settings.Enabled.GetValueOrDefault("Codex");service.Settings.Enabled["Codex"]=false;lastModels="";Render();UpdateLayout();
  if(Find<Grid>("AnalyticsGrid").ColumnDefinitions.Count!=1||Find<Border>("AllowancePanel").IsVisible)throw new Exception("Codex-off chart should use the full width without quota");
  if(capture!=null)CaptureSurface(Path.Combine(Path.GetDirectoryName(capture)!,"dashboard-codex-off.png"));
  service.Settings.Enabled["Codex"]=codex;lastModels="";Render();
 }
}
