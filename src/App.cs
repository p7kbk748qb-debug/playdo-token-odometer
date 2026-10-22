using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Forms=System.Windows.Forms;
namespace PlayDo.Odometer {
 static class Program {
  static string Option(string[] a,string key){int i=Array.IndexOf(a,key);return i>=0&&i+1<a.Length?a[i+1]:null;}
  [STAThread] static int Main(string[] args) {
   string baseDir=Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
   var parent=Directory.GetParent(baseDir);
   bool checkout=parent!=null&&String.Equals(new DirectoryInfo(baseDir).Name,"dist",StringComparison.OrdinalIgnoreCase)
    &&File.Exists(Path.Combine(parent.FullName,"build.ps1"))&&File.Exists(Path.Combine(parent.FullName,"src","MainWindow.xaml"));
   string root=checkout?parent.FullName:baseDir;
   if(args.Contains("--install-shortcuts")||args.Contains("--shortcut")){
    try{if(args.Contains("--install-shortcuts"))Shell.InstallShortcuts(root);else Shell.Shortcut(Path.Combine(root,"PlayDo's Token Odometer.lnk"),"");return 0;}
    catch(Exception e){MessageBox.Show("Could not create the launcher shortcut: "+e.Message,"PlayDo's Token Odometer",MessageBoxButton.OK,MessageBoxImage.Error);return 1;}
   }
   string data=Option(args,"--data")??Path.Combine(root,"data","live");
   string codex=Option(args,"--codex-home")??Environment.GetEnvironmentVariable("CODEX_HOME")??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".codex");
   string isolation=args.Contains("--isolated")?"."+Math.Abs(data.GetHashCode()).ToString():"";
   bool created;using(var mutex=new Mutex(true,"Local\\PlayDo.TokenOdometer"+isolation,out created))
   using(var openEvent=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\PlayDo.TokenOdometer.Open"+isolation))
   using(var exitEvent=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\PlayDo.TokenOdometer.Exit"+isolation)) {
    if(args.Contains("--exit")){if(!created)exitEvent.Set();return 0;}
    if(!created){openEvent.Set();return 0;}
    try {
     Directory.CreateDirectory(data);Shell.SetIdentity();
     var app=new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown};
     using(var collector=new Collector(codex,data))
     using(var tray=new Forms.NotifyIcon()) {
      string prefsPath=Path.Combine(data,"settings.json");var settings=Settings.Load(prefsPath);var dashboard=new Dashboard(collector,root,settings,prefsPath);app.MainWindow=dashboard.Window;
      tray.Icon=System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);tray.Text="PlayDo's Token Odometer";tray.Visible=true;
      var menu=new Forms.ContextMenuStrip();menu.Items.Add("Open odometer",null,(s,e)=>dashboard.Show());
      var scopeMenu=new Forms.ToolStripMenuItem("Token period");foreach(string period in new[]{"Today","7 days","Recorded"}){string selected=period;scopeMenu.DropDownItems.Add(selected,null,(s,e)=>dashboard.SetScope(selected));}menu.Items.Add(scopeMenu);
      menu.Items.Add("Copy token total",null,(s,e)=>dashboard.CopyCurrentTotal());menu.Items.Add("Appearance & settings",null,(s,e)=>dashboard.OpenAppearance());
      menu.Items.Add("Compact / full view",null,(s,e)=>{dashboard.Show();dashboard.ToggleCompact();});menu.Items.Add("Mini view",null,(s,e)=>{dashboard.SetView("Mini");dashboard.Show();});
      menu.Items.Add("Pause / resume tracking",null,(s,e)=>dashboard.TogglePause());menu.Items.Add(new Forms.ToolStripSeparator());menu.Items.Add("Exit",null,(s,e)=>app.Shutdown());tray.ContextMenuStrip=menu;tray.DoubleClick+=(s,e)=>dashboard.Show();
      dashboard.ExitRequested+=()=>app.Shutdown();bool exiting=false;
      dashboard.Window.Closing+=(s,e)=>{if(!exiting){e.Cancel=true;dashboard.Window.Hide();}};
      app.Exit+=(s,e)=>{exiting=true;tray.Visible=false;};
      var openWait=ThreadPool.RegisterWaitForSingleObject(openEvent,(s,t)=>app.Dispatcher.BeginInvoke(new Action(()=>dashboard.Show())),null,-1,false);
      var exitWait=ThreadPool.RegisterWaitForSingleObject(exitEvent,(s,t)=>app.Dispatcher.BeginInvoke(new Action(()=>app.Shutdown())),null,-1,false);
      dashboard.RestoreView();
      string requestedView=Option(args,"--view");if(requestedView!=null)dashboard.SetView(requestedView);
      if(args.Contains("--compact"))dashboard.SetView("Compact");
      if(args.Contains("--mini"))dashboard.SetView("Mini");
      collector.Start();if(!args.Contains("--tray"))dashboard.Show();
      if(args.Contains("--appearance"))dashboard.OpenAppearance();
      string capture=Option(args,"--capture");string report=Option(args,"--report");
      if(capture!=null||report!=null) {
       var timer=new DispatcherTimer {Interval=TimeSpan.FromSeconds(1)};int ticks=0;
       timer.Tick+=delegate{ticks++;var snap=collector.GetSnapshot();if(ticks<3||((snap.Indexing||snap.Status=="Starting collector")&&ticks<180))return;timer.Stop();if(capture!=null){dashboard.Show();dashboard.Capture(capture);}if(report!=null)File.WriteAllText(report,new System.Web.Script.Serialization.JavaScriptSerializer{MaxJsonLength=1048576}.Serialize(snap));if(args.Contains("--exit-after-capture"))app.Shutdown();};timer.Start();
      }
      app.Run();openWait.Unregister(null);exitWait.Unregister(null);menu.Dispose();
     }mutex.ReleaseMutex();return 0;
    }catch(Exception e){try{File.WriteAllText(Path.Combine(data,"startup-error.txt"),e.ToString());}catch{}MessageBox.Show("The odometer could not start. Details are in the app's data/startup-error.txt file.","PlayDo's Token Odometer",MessageBoxButton.OK,MessageBoxImage.Error);return 1;}
   }
  }
 }
}
