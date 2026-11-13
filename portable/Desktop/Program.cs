using Avalonia;

namespace PlayDo.Desktop;
static class Program {
 public static string[] Arguments=Array.Empty<string>();
 [STAThread] public static int Main(string[] args){
  Arguments=args;
  if(args.Contains("--core-smoke"))return Smoke.Run(args);
  using var instance=args.Contains("--smoke")?null:new InstanceGate(TrackerService.DataPath(args));
  if(instance!=null&&!instance.IsPrimary){instance.RequestOpen();return 0;}
  return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
 }
 public static AppBuilder BuildAvaloniaApp()=>AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
}
