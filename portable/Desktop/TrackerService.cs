using System.Text.Json;
using PlayDo.Odometer;

namespace PlayDo.Desktop;
public sealed class WidgetOptions {
 public bool ShowPrimary {get;set;}=true;
 public bool ShowWeekly {get;set;}=true;
 public bool ShowReset {get;set;}
 public bool ShowBreakdown {get;set;}
 public bool ShowModel {get;set;}
 public bool ShowStatus {get;set;}=true;
}
public sealed class Preferences {
 public WidgetOptions Mini {get;set;}=new();
 public WidgetOptions Compact {get;set;}=new(){ShowReset=true,ShowBreakdown=true,ShowModel=true};
 public string Theme {get;set;}="Graphite";
 public string Font {get;set;}="Inter";
 public string NumberFont {get;set;}="Inter";
 public string Layout {get;set;}="Dashboard";
 public string Scope {get;set;}="Today";
 public string Corner {get;set;}="Free";
 public bool Topmost {get;set;}
 public bool Transparent {get;set;}
 public bool Animate {get;set;}
 public bool ReduceMotion {get;set;}
 public bool Paused {get;set;}
 public bool ShowAllowance {get;set;}=true;
 public Dictionary<string,bool> Enabled {get;set;}=new();
 public Dictionary<string,string> Roots {get;set;}=new();
}
public sealed class Provider {
 public string Name {get;init;}="";
 public string Description {get;init;}="";
 public string Root {get;set;}="";
 public bool Detected {get;init;}
 public Collector? Builtin;public ExternalCollector? External;
 public Snapshot Snapshot=>Builtin?.GetSnapshot()??External?.GetSnapshot()??new Snapshot();
}
public sealed class TrackerService:IDisposable {
 public string DataFolder {get;}
 public Preferences Settings {get;private set;}
 public List<Provider> Providers {get;}=new();
 public string Note {get;private set;}="";
 readonly string settingsPath;bool started;
 public static string DataPath(string[] args){
  string home=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
  return Option(args,"--data")??(OperatingSystem.IsMacOS()?Path.Combine(home,"Library","Application Support","PlayDo Token Odometer"):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PlayDo","TokenOdometer","Desktop"));
 }
 public TrackerService(string[] args){
  string home=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
  DataFolder=DataPath(args);
  Directory.CreateDirectory(DataFolder);settingsPath=Path.Combine(DataFolder,"preferences.json");
  bool existing=File.Exists(settingsPath);
  try{Settings=existing?JsonSerializer.Deserialize<Preferences>(File.ReadAllText(settingsPath))??new():new();}catch{Settings=new();Note="Preferences could not be read; defaults are active. The original file was kept.";}
  Settings.Enabled??=new();Settings.Roots??=new();
  Settings.Mini??=new();Settings.Compact??=new(){ShowReset=true,ShowBreakdown=true,ShowModel=true};
  var defaults=new Dictionary<string,string>{
   ["Codex"]=Option(args,"--codex-home")??Environment.GetEnvironmentVariable("CODEX_HOME")??Path.Combine(home,".codex"),
   ["Claude Code"]=Option(args,"--claude-home")??Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR")??Path.Combine(home,".claude"),
   ["Gemini CLI"]=Option(args,"--gemini-home")??Path.Combine(home,".gemini"),
   ["OpenCode"]=Option(args,"--opencode-home")??Path.Combine(Environment.GetEnvironmentVariable("XDG_DATA_HOME")??Path.Combine(home,".local","share"),"opencode")
  };
  foreach(var entry in defaults){
   string root=Settings.Roots.TryGetValue(entry.Key,out var custom)?custom:entry.Value;
   bool found=entry.Key switch{"Codex"=>Directory.Exists(Path.Combine(root,"sessions"))||Directory.Exists(Path.Combine(root,"archived_sessions")),"Claude Code"=>Directory.Exists(Path.Combine(root,"projects")),"Gemini CLI"=>Directory.Exists(Path.Combine(root,"tmp")),_=>File.Exists(Path.Combine(root,"opencode.db"))};
   if(!Settings.Enabled.ContainsKey(entry.Key))Settings.Enabled[entry.Key]=!existing&&found;
   var p=new Provider{Name=entry.Key,Root=root,Detected=found,Description=entry.Key switch{"Codex"=>"Local token reports + allowance snapshots","Claude Code"=>"Assistant responses, cache reads and writes","Gemini CLI"=>"Saved session reports, including thought tokens",_=>"Local SQLite assistant usage; opened read-only"}};
   if(entry.Key=="Codex"||entry.Key=="Claude Code")p.Builtin=new Collector(root,DataFolder,entry.Key=="Claude Code");
   else p.External=new ExternalCollector(entry.Key,root,DataFolder);
   Providers.Add(p);
  }
 }
 public void Start(){if(started)return;started=true;foreach(var p in Providers){bool on=Settings.Enabled.GetValueOrDefault(p.Name)&&!Settings.Paused;p.Builtin?.Pause(!on);p.External?.Enable(on);p.Builtin?.Start();p.External?.Start();}}
 public void Select(string provider,bool enabled){Settings.Enabled[provider]=enabled;var p=Providers.First(x=>x.Name==provider);p.Builtin?.Pause(!enabled||Settings.Paused);p.External?.Enable(enabled&&!Settings.Paused);Save();}
 public void Pause(bool paused){Settings.Paused=paused;foreach(var p in Providers){bool on=!paused&&Settings.Enabled.GetValueOrDefault(p.Name);p.Builtin?.Pause(!on);p.External?.Enable(on);}Save();}
 public void Refresh(){foreach(var p in Providers){p.Builtin?.Refresh();p.External?.Refresh();}}
 public void SetRoot(string provider,string root){
  var p=Providers.First(x=>x.Name==provider);
  p.Builtin?.Dispose();p.External?.Dispose();Settings.Roots[provider]=root;Save();p.Root=root;
  if(provider=="Codex"||provider=="Claude Code"){p.Builtin=new Collector(root,DataFolder,provider=="Claude Code");p.Builtin.Pause(!Settings.Enabled.GetValueOrDefault(provider)||Settings.Paused);if(started)p.Builtin.Start();}
  else{p.External=new ExternalCollector(provider,root,DataFolder);p.External.Enable(Settings.Enabled.GetValueOrDefault(provider)&&!Settings.Paused);if(started)p.External.Start();}
  Note="Folder saved. New reports will be added to the existing provider history.";
 }
 public void Save(){
  string temporary=settingsPath+".tmp";
  try{File.WriteAllText(temporary,JsonSerializer.Serialize(Settings,new JsonSerializerOptions{WriteIndented=true}));File.Move(temporary,settingsPath,true);Note="";}
  catch(IOException){Note="Could not save preferences. Check the data folder permissions.";}catch(UnauthorizedAccessException){Note="Could not save preferences. Check the data folder permissions.";}
 }
 public Snapshot Combined(){
  var s=new Snapshot{Status="No providers selected"};int count=0;
  foreach(var p in Providers.Where(x=>Settings.Enabled.GetValueOrDefault(x.Name))){
   var data=p.Snapshot;count++;s.Today.Add(data.Today);s.Week.Add(data.Week);s.Recorded.Add(data.Recorded);s.Events+=data.Events;s.Files+=data.Files;s.Indexing|=data.Indexing;
   if(data.LastEvent>s.LastEvent)s.LastEvent=data.LastEvent;
   if(p.Name=="Codex")s.Quota=data.Quota;
   foreach(var d in data.Days){if(!s.Days.TryGetValue(d.Key,out var total)){total=new();s.Days[d.Key]=total;}total.Add(d.Value);}
   foreach(var m in data.Models)s.Models.Add(new BreakdownView{Id=p.Name+":"+m.Id,Label=p.Name+" · "+m.Label,Today=m.Today,Week=m.Week,Recorded=m.Recorded});
  }
  s.Paused=Settings.Paused;s.Status=Settings.Paused?"Paused · resume to catch up":count==0?"No providers selected":s.Indexing?"Importing local history":s.Recorded.Total==0?"Waiting for local usage reports":"Tracking "+count+" selected "+(count==1?"provider":"providers");
  return s;
 }
 public void ImportLegacy(string folder){
  if(started)throw new InvalidOperationException("Import history before collection starts");
  int imported=0,skipped=0;
  foreach(string name in new[]{"usage-v3.db","claude-usage-v1.db","usage-v2.db"}){
   string source=Path.Combine(folder,name),target=Path.Combine(DataFolder,name);
   if(File.Exists(source)){if(File.Exists(target)){skipped++;continue;}NativeSqlite.Backup(source,target);imported++;}
  }
  string old=Path.Combine(folder,"settings.json");
  if(File.Exists(old)&&!File.Exists(settingsPath)){
   using var doc=JsonDocument.Parse(File.ReadAllText(old));var json=doc.RootElement;
   foreach(string field in new[]{"Theme","Font","NumberFont","Corner"})if(json.TryGetProperty(field,out var value)&&value.ValueKind==JsonValueKind.String)typeof(Preferences).GetProperty(field)!.SetValue(Settings,value.GetString());
   foreach(string field in new[]{"Mini","Compact"})if(json.TryGetProperty(field,out var value)&&value.ValueKind==JsonValueKind.Object)typeof(Preferences).GetProperty(field)!.SetValue(Settings,JsonSerializer.Deserialize<WidgetOptions>(value.GetRawText())??new());
   foreach(string field in new[]{"Topmost","Transparent","ReduceMotion"})if(json.TryGetProperty(field,out var value)&&(value.ValueKind==JsonValueKind.True||value.ValueKind==JsonValueKind.False))typeof(Preferences).GetProperty(field)!.SetValue(Settings,value.GetBoolean());
   if(json.TryGetProperty("View",out var view))Settings.Layout=view.GetString()??"Dashboard";
   if(json.TryGetProperty("AnimateCount",out var motion))Settings.Animate=motion.GetBoolean();
   if(json.TryGetProperty("Source",out var selection)){string? source=selection.GetString();Settings.Enabled["Codex"]=source!="Claude Code";Settings.Enabled["Claude Code"]=source!="Codex";}
   Save();
  }
  Note=imported>0?"Imported "+imported+" separate history cache(s). The original was not changed."+(skipped>0?" Existing target caches were kept.":""):skipped>0?"Existing provider caches were not replaced. Use a new --data folder to import legacy history.":"No legacy history caches were found in that folder.";
 }
 public void Dispose(){foreach(var p in Providers){p.Builtin?.Dispose();p.External?.Dispose();}}
 public static string? Option(string[] args,string key){int index=Array.IndexOf(args,key);return index>=0&&index+1<args.Length?args[index+1]:null;}
}
