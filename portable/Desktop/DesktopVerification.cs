using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace PlayDo.Desktop;
public partial class MainWindow {
 void CaptureSurface(string path){
  StopEffects();UpdateLayout();var root=Find<Grid>("Root");
  using var bitmap=new RenderTargetBitmap(new PixelSize((int)root.Bounds.Width,(int)root.Bounds.Height),new Vector(96,96));bitmap.Render(root);bitmap.Save(path);
 }
 async Task VerifyDashboard(string? capture){
  Navigate("Overview");Render();
  if(Find<Grid>("DayChart").Children.Count!=7||Find<Avalonia.Controls.Primitives.UniformGrid>("ProviderSummary").Children.Count!=4)throw new Exception("Dashboard chart or provider summary missing");
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
