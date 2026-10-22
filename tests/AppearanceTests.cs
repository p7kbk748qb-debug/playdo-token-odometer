using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;
using PlayDo.Odometer;
class AppearanceTests {
 static int passed,failed;
 static void Check(string name,Action test){try{test();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e.Message);}}
 static void Assert(bool value,string message){if(!value)throw new Exception(message);}
 static object Field(Settings prefs,string name){var f=typeof(Settings).GetField(name);Assert(f!=null,"Missing setting "+name);return f.GetValue(prefs);}
 static void Set(Settings prefs,string name,string value){var f=typeof(Settings).GetField(name);Assert(f!=null,"Missing setting "+name);f.SetValue(prefs,value);}
 static void Pump(){var frame=new DispatcherFrame();var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(120)};timer.Tick+=delegate{timer.Stop();frame.Continue=false;};timer.Start();Dispatcher.PushFrame(frame);}
 static ComboBox Choice(Dashboard d,string name){var c=d.Window.FindName(name) as ComboBox;Assert(c!=null,"Missing appearance control "+name);return c;}
 static string Usage(long input,long output,DateTime at){return new JavaScriptSerializer().Serialize(new{type="event_msg",timestamp=at.ToString("O"),payload=new{type="token_count",info=new{total_token_usage=new{input_tokens=input,output_tokens=output},last_token_usage=new{input_tokens=input,output_tokens=output}}}});}
 static void Session(string path,string cwd,string model,long input,long output,DateTime at){var json=new JavaScriptSerializer();File.WriteAllText(path,json.Serialize(new{type="session_meta",timestamp=at.ToString("O"),payload=new{cwd=cwd}})+"\n"+json.Serialize(new{type="turn_context",timestamp=at.ToString("O"),payload=new{model=model,cwd=cwd}})+"\n"+Usage(input,output,at)+"\n");}
 static double Linear(byte n){double v=n/255.0;return v<=0.04045?v/12.92:Math.Pow((v+0.055)/1.055,2.4);}
 static double Lum(Color c){return .2126*Linear(c.R)+.7152*Linear(c.G)+.0722*Linear(c.B);}
 static double Contrast(Dashboard d,string fg,string bg){double a=Lum(((SolidColorBrush)d.Window.Resources[fg]).Color),b=Lum(((SolidColorBrush)d.Window.Resources[bg]).Color);return (Math.Max(a,b)+.05)/(Math.Min(a,b)+.05);}
 static string Read(Dashboard d,string name){var t=d.Window.FindName(name) as TextBlock;Assert(t!=null,"Missing text "+name);return t.Text;}
 static void Press(Dashboard d,string name){var b=d.Window.FindName(name) as Button;Assert(b!=null,"Missing button "+name);b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump();}
 [STAThread] static int Main(string[] args){
  string root=args[0];AppDomain.CurrentDomain.AssemblyResolve+=delegate(object sender,ResolveEventArgs e){string path=args.Length>2?args[2]:Path.Combine(root,"dist",new AssemblyName(e.Name).Name+".exe");return File.Exists(path)?Assembly.LoadFrom(path):null;};
  return Run(root,args.Length>1&&args[1]=="--settings-only");
 }
 static int Run(string root,bool settingsOnly){
  string dir=Path.Combine(root,"build","appearance-check-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);string prefsPath=Path.Combine(dir,"settings.json");
  Check("appearance defaults are backward compatible",delegate{var p=new Settings();Assert((string)Field(p,"Theme")=="Instrument","Default theme");Assert((string)Field(p,"Font")=="Segoe UI","Default font");Assert((string)Field(p,"Counter")=="Mechanical","Default counter");Assert((string)Field(p,"View")=="Dashboard","Default view");});
  Check("number font has a compatible local default",delegate{Assert((string)Field(new Settings(),"NumberFont")=="Consolas","Expected Consolas number font");});
  Check("ten named style presets are available",delegate{Assert(String.Join("|",Appearance.Themes)=="Instrument|Graphite|Arctic|Terminal|Winter|Hazy|Aurora|Blueprint|Sunset|Paper","Ten distinct named presets are required");});
  Check("preset definitions supply real typography counter and material changes",delegate{Assert(Appearance.Presets.Length==10,"Missing preset definitions");Assert(Appearance.Presets.Select(x=>x.Material).Distinct().Count()==10,"Materials repeat");Assert(Appearance.Presets.Select(x=>x.Counter).Distinct().Count()>=8,"Counter treatments are too similar");Assert(Appearance.Get("Winter").Font!=Appearance.Get("Hazy").Font&&Appearance.Get("Winter").Counter!=Appearance.Get("Hazy").Counter,"Winter and Hazy need different typography and counters");foreach(var preset in Appearance.Presets){var p=new Settings{Font="Consolas",NumberFont="Georgia",Counter="Clean"};Appearance.ChoosePreset(p,preset.Name);Assert(p.Font==preset.Font&&p.NumberFont==preset.NumberFont&&p.Counter==preset.Counter,"Preset did not apply defaults: "+preset.Name);Assert(Appearance.Material(preset).IsFrozen&&Appearance.Detail(preset).IsFrozen,"Decoration should be frozen");}});
  Check("appearance preferences round trip",delegate{var p=new Settings();Set(p,"Theme","Arctic");Set(p,"Font","Consolas");Set(p,"NumberFont","Georgia");Set(p,"Counter","Clean");Set(p,"View","Mini");p.Save(prefsPath);var saved=Settings.Load(prefsPath);Assert((string)Field(saved,"Theme")=="Arctic"&&(string)Field(saved,"Font")=="Consolas"&&(string)Field(saved,"NumberFont")=="Georgia"&&(string)Field(saved,"Counter")=="Clean"&&(string)Field(saved,"View")=="Mini","Saved values changed");});
  Check("legacy custom preferences survive loading",delegate{File.WriteAllText(prefsPath,"{\"Theme\":\"Arctic\",\"Font\":\"Consolas\",\"Counter\":\"Ledger\",\"View\":\"Compact\"}");var p=Settings.Load(prefsPath);Assert(p.Theme=="Arctic"&&p.Font=="Consolas"&&p.Counter=="Ledger"&&p.View=="Compact"&&p.NumberFont=="Consolas","Legacy customization changed");});
  Check("null or invalid preferences recover defaults",delegate{File.WriteAllText(prefsPath,"null");Assert(Settings.Load(prefsPath)!=null,"Null settings were returned");File.WriteAllText(prefsPath,"{\"Theme\":\"missing\",\"Font\":null,\"Counter\":\"missing\",\"View\":\"missing\"}");var p=Settings.Load(prefsPath);Assert((string)Field(p,"Theme")=="Instrument"&&(string)Field(p,"Font")=="Segoe UI"&&(string)Field(p,"Counter")=="Mechanical"&&(string)Field(p,"View")=="Dashboard","Invalid settings were not normalized");});
  if(!settingsOnly){
   string home=Path.Combine(dir,"home"),data=Path.Combine(dir,"data");Directory.CreateDirectory(Path.Combine(home,"sessions"));string now=DateTime.UtcNow.ToString("O");
   Session(Path.Combine(home,"sessions","demo.jsonl"),"C:\\Demo\\Odometer","demo-model",12000,350,DateTime.UtcNow);
   Session(Path.Combine(home,"sessions","second.jsonl"),"C:\\Other\\Odometer","other-model",800,25,DateTime.UtcNow.AddSeconds(-1));
   Session(Path.Combine(home,"sessions","older.jsonl"),"C:\\Demo\\Odometer","earlier-model",700,25,DateTime.UtcNow.AddDays(-8));
   using(var collector=new Collector(home,data)){
    collector.Start();var deadline=DateTime.UtcNow.AddSeconds(15);while((collector.GetSnapshot().Recorded.Total==0||collector.GetSnapshot().Indexing)&&DateTime.UtcNow<deadline)Thread.Sleep(50);
    var prefs=new Settings();var dashboard=new Dashboard(collector,root,prefs,prefsPath);dashboard.Show();Pump();
    Check("appearance controls exist and presets change the window",delegate{
     var theme=Choice(dashboard,"ThemeChoice");var font=Choice(dashboard,"FontChoice");Choice(dashboard,"CounterChoice");Choice(dashboard,"ViewChoice");
     string before=dashboard.Window.Background.ToString();theme.SelectedItem="Arctic";Pump();Assert(dashboard.Window.Background.ToString()!=before,"Arctic did not change the background");font.SelectedItem="Consolas";Pump();Assert(dashboard.Window.FontFamily.Source.IndexOf("Consolas",StringComparison.OrdinalIgnoreCase)>=0,"Font did not apply");
    });
    Check("all presets render and save without resetting selection",delegate{
     foreach(string theme in Appearance.Themes){dashboard.SelectPreset(theme);Pump();dashboard.Capture(Path.Combine(root,"build","appearance-"+theme.ToLowerInvariant()+".png"));Assert((string)Field(Settings.Load(prefsPath),"Theme")==theme,"Theme did not persist");foreach(var pair in new[]{new[]{"Text","Canvas"},new[]{"Text","Surface"},new[]{"Text","SurfaceAlt"},new[]{"Text","Control"},new[]{"Text","Hover"},new[]{"Text","Pressed"},new[]{"Muted","Surface"},new[]{"Muted","SurfaceAlt"},new[]{"Accent","Surface"},new[]{"Accent","Digit"},new[]{"Accent","Selected"},new[]{"OnAccent","Accent"}})Assert(Contrast(dashboard,pair[0],pair[1])>=4.5,theme+" contrast too low: "+pair[0]+"/"+pair[1]);}
    });
    Check("gallery has accessible styled previews and preset restore resets overrides",delegate{
     var gallery=dashboard.Window.FindName("ThemeGallery") as System.Windows.Controls.Primitives.UniformGrid;Assert(gallery!=null&&gallery.Children.Count==10,"Ten preview cards required");
     foreach(Button card in gallery.Children){Assert(System.Windows.Automation.AutomationProperties.GetName(card).Contains("style"),"Preset name missing");Assert(card.Content is Border,"Numeric material preview missing");}
     dashboard.SelectPreset("Winter");Choice(dashboard,"FontChoice").SelectedItem="Consolas";Choice(dashboard,"NumberFontChoice").SelectedItem="Georgia";Choice(dashboard,"CounterChoice").SelectedItem="Ledger";Pump();Assert(Settings.Load(prefsPath).NumberFont=="Georgia","Manual number font did not persist");Press(dashboard,"RestorePresetButton");var p=Settings.Load(prefsPath);Assert(p.Font==Appearance.Get("Winter").Font&&p.NumberFont==Appearance.Get("Winter").NumberFont&&p.Counter=="Frosted","Restore preset did not reset all styling");
     dashboard.SelectPreset("Hazy");Assert(dashboard.Window.FontFamily.Source==Appearance.Font("Georgia").Source&&Settings.Load(prefsPath).Counter=="Airy","Preset retained previous overrides");
    });
    Check("scope and window shortcuts stay local and preserve text copy",delegate{
     Assert(dashboard.HandleShortcut(Key.D3,ModifierKeys.Control)&&dashboard.CurrentScope=="Recorded","Recorded shortcut failed");Assert(Read(dashboard,"InputText")==13500L.ToString("N0"),"Shortcut did not update totals");Assert(dashboard.HandleShortcut(Key.D2,ModifierKeys.Control)&&dashboard.CurrentScope=="7 days","Week shortcut failed");Assert(dashboard.HandleShortcut(Key.D1,ModifierKeys.Control)&&dashboard.CurrentScope=="Today","Today shortcut failed");Assert(!dashboard.HandleShortcut(Key.C,ModifierKeys.Control),"Ordinary copy was intercepted");
     Assert(dashboard.HandleShortcut(Key.M,ModifierKeys.Control|ModifierKeys.Shift)&&dashboard.Window.Width<=400,"Mini shortcut failed");dashboard.HandleShortcut(Key.OemComma,ModifierKeys.Control);Assert(dashboard.Window.Width>=760&&((Border)dashboard.Window.FindName("SettingsPanel")).Visibility==Visibility.Visible,"Appearance shortcut failed");Assert(dashboard.HandleShortcut(Key.Escape,ModifierKeys.None)&&dashboard.Window.Width<=400,"Escape did not return to Mini");Assert(!dashboard.HandleShortcut(Key.Escape,ModifierKeys.None),"Escape was intercepted without settings");dashboard.HandleShortcut(Key.M,ModifierKeys.Control);Assert(dashboard.Window.Width>=760,"Compact toggle did not restore dashboard");
     Assert(dashboard.Window.FindName("CopyButton") is Button&&dashboard.Window.FindName("ExportButton") is Button,"Convenience actions missing");var compact=(Border)dashboard.Window.FindName("CompactView");Assert(compact.ContextMenu!=null&&compact.ContextMenu.Items.Count>=7,"Small-view scope and actions menu missing");
    });
    Check("explicit layout choices cancel the temporary Style return view",delegate{
     foreach(bool escape in new[]{false,true}){dashboard.SetView("Mini");dashboard.OpenAppearance();dashboard.ToggleCompact();dashboard.SetView("Dashboard");if(escape)dashboard.HandleShortcut(Key.Escape,ModifierKeys.None);else Press(dashboard,"CloseSettingsButton");Assert(dashboard.Window.Width>=760&&Settings.Load(prefsPath).View=="Dashboard","Done or Escape restored a stale Mini view after an explicit layout choice");}
    });
    Check("scope changes and expired feedback clear the dashboard message",delegate{
     dashboard.SetView("Dashboard");dashboard.SetScope("Today");var feedback=typeof(Dashboard).GetMethod("Feedback",BindingFlags.Instance|BindingFlags.NonPublic);Assert(feedback!=null,"Feedback entry point missing");feedback.Invoke(dashboard,new object[]{"Copied today total"});Assert(Read(dashboard,"ActionFeedback")=="Copied today total","Feedback did not display");dashboard.SetScope("Recorded");Assert(Read(dashboard,"ActionFeedback")=="","Old scope feedback remained visible");
     feedback.Invoke(dashboard,new object[]{"Copied recorded total"});typeof(Dashboard).GetField("actionAt",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(dashboard,DateTime.UtcNow.AddSeconds(-20));dashboard.Render();Assert(Read(dashboard,"ActionFeedback")=="","Expired feedback remained visible");dashboard.SetScope("Today");
    });
    Check("project controls are removed and model ranking follows scope",delegate{
     Assert(dashboard.Window.FindName("ProjectChoice")==null&&dashboard.Window.FindName("ProjectTotal")==null,"Project controls remain visible");
     var modelRows=dashboard.Window.FindName("ModelList") as Panel;Assert(modelRows!=null&&modelRows.Children.Count>=2,"Model ranking not populated");
     Press(dashboard,"AllButton");Assert(modelRows.Children.Count==3,"Recorded model ranking excludes older usage");Assert(Read(dashboard,"InputText")==13500L.ToString("N0"),"Recorded total changed");
     Press(dashboard,"WeekButton");Assert(modelRows.Children.Count==2,"7-day model ranking includes old usage");Press(dashboard,"TodayButton");Assert(Read(dashboard,"InputText")==12800L.ToString("N0"),"Today's total changed");
     var scroll=dashboard.Window.FindName("FullView") as ScrollViewer;var breakdown=dashboard.Window.FindName("BreakdownGrid") as FrameworkElement;breakdown.BringIntoView();Pump();dashboard.Capture(Path.Combine(root,"build","appearance-breakdowns.png"));scroll.ScrollToTop();
    });
    Check("local fonts and narrow appearance controls render",delegate{
     foreach(string font in Appearance.Fonts){Choice(dashboard,"FontChoice").SelectedItem=font;Choice(dashboard,"NumberFontChoice").SelectedItem=font;Pump();Assert((string)Field(Settings.Load(prefsPath),"Font")==font&&Settings.Load(prefsPath).NumberFont==font,"Font preference did not persist");}
     Choice(dashboard,"FontChoice").SelectedItem="Consolas";Choice(dashboard,"ThemeChoice").SelectedItem="Arctic";Press(dashboard,"SettingsButton");dashboard.Window.Width=760;dashboard.Window.Height=640;Pump();dashboard.Capture(Path.Combine(root,"build","appearance-settings-narrow.png"));dashboard.Window.Width=1040;dashboard.Window.Height=800;Pump();dashboard.Capture(Path.Combine(root,"build","appearance-settings.png"));Press(dashboard,"CloseSettingsButton");
    });
    Check("counter and layout controls persist and restore",delegate{
     foreach(string counter in Appearance.Counters){Choice(dashboard,"CounterChoice").SelectedItem=counter;Pump();Assert((string)Field(Settings.Load(prefsPath),"Counter")==counter,"Counter did not persist");}
     foreach(string theme in Appearance.Themes){dashboard.SelectPreset(theme);dashboard.SetView("Mini");Pump();Assert(dashboard.Window.Width<=400&&dashboard.Window.Height<=210,"Mini bounds changed for "+theme);dashboard.Capture(Path.Combine(root,"build","appearance-"+theme.ToLowerInvariant()+"-mini.png"));dashboard.SetView("Compact");Pump();dashboard.Capture(Path.Combine(root,"build","appearance-"+theme.ToLowerInvariant()+"-compact.png"));dashboard.SetView("Dashboard");}
     Choice(dashboard,"ViewChoice").SelectedItem="Mini";Pump();Assert(dashboard.Window.Width<=400&&dashboard.Window.Height<=210,"Mini is not small");dashboard.Capture(Path.Combine(root,"build","appearance-mini.png"));Assert((string)Field(Settings.Load(prefsPath),"View")=="Mini","Mini did not persist");
     Choice(dashboard,"ViewChoice").SelectedItem="Compact";Pump();Assert(dashboard.Window.Width<=500&&dashboard.Window.Height<=300,"Compact size incorrect");dashboard.Capture(Path.Combine(root,"build","appearance-compact.png"));Choice(dashboard,"ViewChoice").SelectedItem="Dashboard";Pump();Assert(dashboard.Window.Width>=760,"Dashboard did not restore");
    });
    Check("small layouts leave maximized state and Style returns to Mini",delegate{
     dashboard.Window.WindowState=WindowState.Maximized;Pump();Choice(dashboard,"ViewChoice").SelectedItem="Mini";Pump();Assert(dashboard.Window.WindowState==WindowState.Normal&&dashboard.Window.Width<=400,"Mini stayed maximized");
     Press(dashboard,"CompactStyleButton");Assert(dashboard.Window.Width>=760,"Style did not expose settings");Press(dashboard,"CloseSettingsButton");Assert(dashboard.Window.Width<=400&&(string)Field(Settings.Load(prefsPath),"View")=="Mini","Done did not return to saved Mini");
    });
    Check("saved appearance restores in a new window",delegate{
     Choice(dashboard,"ThemeChoice").SelectedItem="Arctic";Choice(dashboard,"FontChoice").SelectedItem="Consolas";Choice(dashboard,"NumberFontChoice").SelectedItem="Georgia";Choice(dashboard,"CounterChoice").SelectedItem="Ledger";Choice(dashboard,"ViewChoice").SelectedItem="Mini";Pump();dashboard.Window.Close();
     var restored=new Dashboard(collector,root,Settings.Load(prefsPath),prefsPath);restored.RestoreView();restored.Show();Pump();Assert(restored.Window.Width<=400&&restored.Window.Height<=210,"Saved Mini layout did not restore");Assert(restored.Window.FontFamily.Source.IndexOf("Consolas",StringComparison.OrdinalIgnoreCase)>=0,"Saved font did not restore");Assert((string)Choice(restored,"ThemeChoice").SelectedItem=="Arctic"&&Settings.Load(prefsPath).NumberFont=="Georgia"&&Settings.Load(prefsPath).Counter=="Ledger","Saved customization did not restore");restored.Window.Close();
    });
    dashboard.Window.Close();
   }
   Check("preserved history remains visible without readable usage events",delegate{
    string legacyHome=Path.Combine(dir,"legacy-home"),legacyData=Path.Combine(dir,"legacy-data");Directory.CreateDirectory(legacyHome);Directory.CreateDirectory(legacyData);
    using(var old=new SavedState()){old.Version=1;old.Ledger.Accept("retained",Usage(5000,35,DateTime.UtcNow));Store.Save(Path.Combine(legacyData,"usage-v2.db"),old);}
    using(var collector=new Collector(legacyHome,legacyData)){collector.Start();var deadline=DateTime.UtcNow.AddSeconds(15);while((collector.GetSnapshot().Recorded.Total==0||collector.GetSnapshot().Indexing)&&DateTime.UtcNow<deadline)Thread.Sleep(50);var d=new Dashboard(collector,root,new Settings(),Path.Combine(legacyData,"settings.json"));d.Show();Pump();Assert(Read(d,"InputText")==5000L.ToString("N0"),"Preserved input hidden");Assert(!Read(d,"TotalAccessible").Contains("d ago"),"Missing event timestamp produced a bogus age");d.Window.Close();}
   });
  }
  Console.WriteLine(passed+" passed, "+failed+" failed");return failed==0?0:1;
 }
}
