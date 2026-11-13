using Avalonia;

namespace PlayDo.Desktop;
static class Program {
 public static string[] Arguments=Array.Empty<string>();
 [STAThread] public static int Main(string[] args){
  Arguments=args;
  if(args.Contains("--core-smoke"))return Smoke.Run(args);
  return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
 }
 public static AppBuilder BuildAvaloniaApp()=>AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
}
