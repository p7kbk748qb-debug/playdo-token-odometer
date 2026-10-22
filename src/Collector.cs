using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace PlayDo.Odometer {
 public class SessionView {public string Id;public long Total;public DateTime At;}
 public class BreakdownView {public string Id,Label;public Tokens Today=new Tokens(),Week=new Tokens(),Recorded=new Tokens();}
 public class Snapshot {
  public Tokens Today=new Tokens(),Week=new Tokens(),Recorded=new Tokens();
  public Dictionary<string,Tokens> Days=new Dictionary<string,Tokens>();
  public List<SessionView> Sessions=new List<SessionView>();public Quota Quota;
  public List<BreakdownView> Models=new List<BreakdownView>();
  public int Files,Processed,Compressed;public long Bytes,Events;public bool Indexing,Paused;
  public string Status="Starting collector";public DateTime LastEvent;public DateTime Started;
 }
 public sealed class Collector:IDisposable {
  readonly string root,cachePath,legacyPath;readonly object gate=new object();readonly AutoResetEvent wake=new AutoResetEvent(false);
  readonly HashSet<string> pending=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  readonly List<FileSystemWatcher> watchers=new List<FileSystemWatcher>();
  SavedState state;Thread worker;volatile bool stopping,paused,reconcile=true;volatile Snapshot snapshot=new Snapshot();
  int files,processed,compressed;long bytes;bool indexing;string error="";DateTime saved=DateTime.MinValue;
  public event Action Changed;
  public Collector(string codexRoot,string dataFolder) {root=codexRoot;cachePath=Path.Combine(dataFolder,"usage-v3.db");legacyPath=Path.Combine(dataFolder,"usage-v2.db");}
  public Snapshot GetSnapshot(){return snapshot;}
  public void Start() {worker=new Thread(Run){IsBackground=true,Name="PlayDo usage collector",Priority=ThreadPriority.BelowNormal};worker.Start();}
  public void Pause(bool value){paused=value;wake.Set();}
  public void Refresh(){reconcile=true;wake.Set();}
  void Queue(string p) {if(!p.EndsWith(".jsonl",StringComparison.OrdinalIgnoreCase))return;lock(gate)pending.Add(p);wake.Set();}
  void Watch() {
   foreach(string name in new[]{"sessions","archived_sessions"}) {
    string p=Path.Combine(root,name);if(!Directory.Exists(p)||watchers.Any(existing=>existing.Path==p))continue;
    var w=new FileSystemWatcher(p,"*.jsonl"){IncludeSubdirectories=true,NotifyFilter=NotifyFilters.FileName|NotifyFilters.LastWrite|NotifyFilters.Size,InternalBufferSize=16384};
    w.Changed+=(s,e)=>Queue(e.FullPath);w.Created+=(s,e)=>Queue(e.FullPath);w.Renamed+=(s,e)=>{reconcile=true;Queue(e.FullPath);};w.Error+=(s,e)=>{reconcile=true;wake.Set();};w.EnableRaisingEvents=true;watchers.Add(w);
   }
  }
  List<string> Discover() {
   var list=new List<string>();compressed=0;
   foreach(string name in new[]{"sessions","archived_sessions"}){string p=Path.Combine(root,name);if(!Directory.Exists(p))continue;try{list.AddRange(Directory.GetFiles(p,"*.jsonl",SearchOption.AllDirectories));compressed+=Directory.GetFiles(p,"*.jsonl.zst",SearchOption.AllDirectories).Length;}catch(IOException){error="Some session folders are unavailable; retrying automatically.";}catch(UnauthorizedAccessException){error="Cannot read a Codex folder. Check its permissions.";}}
   return list.OrderByDescending(p=>File.GetLastWriteTimeUtc(p)).ToList();
  }
  void Publish() {
   var ledger=state.Ledger;ledger.ReconcileLegacy();var s=new Snapshot {Today=ledger.TotalFor(DateTime.Today),Week=ledger.TotalFor(DateTime.Today.AddDays(-6)),Recorded=ledger.TotalFor(DateTime.MinValue),Quota=ledger.LatestQuota,Files=files,Processed=processed,Compressed=compressed,Bytes=bytes,Events=ledger.Events,Indexing=indexing,Paused=paused,Started=ledger.Started,Models=Breakdowns(ledger.Models)};
   foreach(var d in ledger.Days.Where(d=>String.CompareOrdinal(d.Key,DateTime.Today.AddDays(-6).ToString("yyyy-MM-dd"))>=0))s.Days[d.Key]=d.Value.Copy();
   foreach(var x in ledger.Sessions.OrderByDescending(x=>x.Value.LastAt).Take(8))s.Sessions.Add(new SessionView {Id=x.Key,Total=x.Value.Observed.Total,At=x.Value.LastAt});
   s.LastEvent=ledger.Sessions.Count==0?DateTime.MinValue:ledger.Sessions.Max(x=>x.Value.LastAt);
   s.Status=paused?"Paused · resume to catch up":indexing?"Importing local history · "+processed+" / "+files+" files":error.Length>0?error:files==0?"No local Codex sessions found": "Watching local Codex usage";
   snapshot=s;var changed=Changed;if(changed!=null)changed();
  }
  static List<BreakdownView> Breakdowns(Dictionary<string,BreakdownUsage> groups){return groups.Values.Select(g=>new BreakdownView {Id=g.Id,Label=g.Label,Today=UsageLedger.TotalForDays(g.Days,DateTime.Today),Week=UsageLedger.TotalForDays(g.Days,DateTime.Today.AddDays(-6)),Recorded=UsageLedger.TotalForDays(g.Days,DateTime.MinValue)}).Where(g=>g.Recorded.Total>0).OrderByDescending(g=>g.Recorded.Total).ThenBy(g=>g.Label,StringComparer.OrdinalIgnoreCase).ToList();}
  void Process(string p) {
   if(stopping||paused||!File.Exists(p))return;
   try {
    LineCursor cursor;if(!state.Cursors.TryGetValue(p,out cursor)){cursor=new LineCursor();state.Cursors[p]=cursor;}
    if(new FileInfo(p).Length==cursor.Offset&&File.GetLastWriteTimeUtc(p).Ticks==cursor.LastWriteTicks)return;
    var match=Regex.Match(Path.GetFileName(p),"[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}");
    string id=match.Success?match.Value:Path.GetFileNameWithoutExtension(p);
    long before=cursor.ReadBytes;cursor.Read(p,line=>state.Ledger.Accept(id,line));bytes+=cursor.ReadBytes-before;
   }catch(DatabaseException){error="Local usage cache is busy. This record will be retried.";lock(gate)pending.Add(p);}catch(IOException){error="A session file is busy; retrying automatically.";}catch(UnauthorizedAccessException){error="A session file could not be read.";}
  }
  void Checkpoint(bool force) {if(!force&&(DateTime.UtcNow-saved).TotalSeconds<10)return;try{Store.Save(cachePath,state);saved=DateTime.UtcNow;}catch(Exception e){if(e is IOException||e is UnauthorizedAccessException||e is InvalidOperationException||e is DatabaseException)error="Cannot save local cache. Check free disk space and folder access.";else throw;}}
  void Run() {
   try {
    while(state==null&&!stopping){try{state=Store.Load(cachePath,legacyPath);}catch{snapshot=new Snapshot {Status="Cannot open local cache. Check folder access; retrying in 5 seconds."};var changed=Changed;if(changed!=null)changed();wake.WaitOne(5000);}}
    if(stopping)return;if(Store.Recovered)error="Rebuilding an unreadable cache from local records.";Watch();
    DateTime next=DateTime.MinValue;long priorBytes=-1;
    while(!stopping) {
     if(paused){Publish();wake.WaitOne(1000);continue;}
     if(reconcile||DateTime.UtcNow>=next) {
      reconcile=false;Watch();var paths=Discover();files=paths.Count;processed=0;indexing=true;Publish();
      foreach(string p in paths){if(stopping||paused){reconcile=true;break;}Process(p);processed++;Publish();Thread.Sleep(2);}
      indexing=false;next=DateTime.UtcNow.AddSeconds(60);Publish();
     }
     string[] batch;lock(gate){batch=pending.ToArray();pending.Clear();}
     foreach(string p in batch)Process(p);
     if(batch.Length>0)Publish();
     if(bytes!=priorBytes){Checkpoint(false);if((DateTime.UtcNow-saved).TotalSeconds<1)priorBytes=bytes;}
     wake.WaitOne(650);
    }
    Checkpoint(true);
   }catch(Exception){error="Collector stopped unexpectedly. Reopen the app to retry.";if(state!=null)Publish();}
   finally{if(state!=null)state.Dispose();}
  }
  public void Dispose(){stopping=true;wake.Set();foreach(var w in watchers)w.Dispose();if(worker!=null)worker.Join(3000);}
 }
}
