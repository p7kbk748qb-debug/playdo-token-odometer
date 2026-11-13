using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;

namespace PlayDo.Desktop;
public partial class App:Application {
 TrackerService? tracker;
 public override void Initialize()=>AvaloniaXamlLoader.Load(this);
 public override void OnFrameworkInitializationCompleted(){
  if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop){
   tracker=new TrackerService(Program.Arguments);
   string? legacy=TrackerService.Option(Program.Arguments,"--import-legacy");if(legacy!=null)tracker.ImportLegacy(legacy);
   var window=new MainWindow(tracker);desktop.MainWindow=window;desktop.ShutdownMode=ShutdownMode.OnExplicitShutdown;
   var menu=new NativeMenu();
   var open=new NativeMenuItem("Open odometer");open.Click+=(_,_)=>window.Reveal();menu.Add(open);
   foreach(string layout in new[]{"Dashboard","Compact","Mini","Tiny"}){var item=new NativeMenuItem(layout);item.Click+=(_,_)=>{window.SetLayout(layout);window.Reveal();};menu.Add(item);}
   menu.Add(new NativeMenuItemSeparator());var exit=new NativeMenuItem("Exit");exit.Click+=(_,_)=>window.RequestExit();menu.Add(exit);
   var tray=new TrayIcon{Icon=new WindowIcon(AssetLoader.Open(new Uri("avares://PlayDoTokenOdometer/Assets/app.ico"))),ToolTipText="PlayDo’s Token Odometer",Menu=menu,IsVisible=true};
   tray.Clicked+=(_,_)=>window.Reveal();TrayIcon.SetIcons(this,new TrayIcons{tray});
   desktop.Exit+=(_,_)=>{tray.Dispose();tracker.Dispose();};
   tracker.Start();
  }
  base.OnFrameworkInitializationCompleted();
 }
}
