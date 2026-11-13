using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web.Script.Serialization;

namespace PlayDo.Odometer {
 public partial class UsageLedger {
  public bool AcceptGemini(string session,string line){
   try{
    var record=Obj(new JavaScriptSerializer{MaxJsonLength=1048576}.DeserializeObject(line));
    if(!"gemini".Equals(Get(record,"type")))return false;
    var tokens=Obj(Get(record,"tokens"));string id=Text(Get(record,"id"),256);DateTime at;
    if(tokens==null||id==null||!Timestamp(Get(record,"timestamp"),out at))return false;
    long input=Num(tokens,"input"),total=Num(tokens,"total"),output=Num(tokens,"output")+Num(tokens,"thoughts");
    if(total>=input&&total>0)output=total-input;
    return ObserveResponse("gemini:"+id,session,Text(Get(record,"model"),128)??"Unknown Gemini model",at,new Tokens{Input=input,Output=output,Cached=Math.Min(input,Num(tokens,"cached")),Reasoning=Math.Min(output,Num(tokens,"thoughts"))});
   }catch(ArgumentException){return false;}catch(InvalidOperationException){return false;}catch(FormatException){return false;}
  }
  public bool AcceptOpenCode(string id,string session,string json,long created){
   try{
    var record=Obj(new JavaScriptSerializer{MaxJsonLength=1048576}.DeserializeObject(json));
    var tokens=Obj(Get(record,"tokens"));if(tokens==null)return false;
    var cache=Obj(Get(tokens,"cache"));var model=Obj(Get(record,"model"));
    string label=Text(Get(record,"modelID"),128)??Text(Get(model,"id"),128)??"Unknown OpenCode model";
    return ObserveResponse("opencode:"+id,session,label,DateTimeOffset.FromUnixTimeMilliseconds(created).UtcDateTime,new Tokens{
     Input=Num(tokens,"input")+Num(cache,"read")+Num(cache,"write"),Cached=Num(cache,"read"),Output=Num(tokens,"output")+Num(tokens,"reasoning"),Reasoning=Num(tokens,"reasoning")
    });
   }catch(ArgumentException){return false;}catch(InvalidOperationException){return false;}catch(FormatException){return false;}
  }
  bool ObserveResponse(string id,string session,string model,DateTime at,Tokens value){
   if(value.Total<=0)return false;
   string key=Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(id)));
   Tokens before;if(!ClaudeResponses.TryGetValue(key,out before))before=new Tokens();
   var delta=Delta(value,before);if(delta.Total==0)return false;
   delta.Cached=Math.Min(delta.Input,delta.Cached);delta.Reasoning=Math.Min(delta.Output,delta.Reasoning);
   ClaudeResponses[key]=new Tokens{Input=Math.Max(value.Input,before.Input),Output=Math.Max(value.Output,before.Output),Cached=Math.Max(value.Cached,before.Cached),Reasoning=Math.Max(value.Reasoning,before.Reasoning)};
   string day=at.ToLocalTime().ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
   AddDay(Days,day,delta);AddBreakdown(Models,model,model,day,delta);Events++;
   SessionUsage s;if(!Sessions.TryGetValue(session,out s)){s=new SessionUsage();Sessions[session]=s;}s.Observed.Add(delta);if(at>s.LastAt)s.LastAt=at;return true;
  }
 }

 public sealed class ExternalCollector:IDisposable {
  readonly string provider,root,path;readonly AutoResetEvent wake=new(false);readonly object gate=new();
  Thread worker;volatile bool stopping,enabled;volatile Snapshot snapshot=new();SavedState state;
  readonly Dictionary<string,long> modified=new();long watermark;int files;long bytes;DateTime lastDiscovery;string databaseStamp="";
  string[] discovered=Array.Empty<string>();string note="";
  public string Root=>root;
  public ExternalCollector(string provider,string root,string data){this.provider=provider;this.root=root;path=Path.Combine(data,provider.ToLowerInvariant()+"-usage-v1.db");}
  public Snapshot GetSnapshot()=>snapshot;
  public void Start(){worker=new Thread(Run){IsBackground=true,Priority=ThreadPriority.BelowNormal,Name=provider+" usage"};worker.Start();}
  public void Enable(bool value){enabled=value;wake.Set();}
  public void Refresh(){lastDiscovery=DateTime.MinValue;wake.Set();}
  void Run(){
   try{
    state=Store.Load(path);
    while(!stopping){
     if(!enabled){Publish("Not selected");wake.WaitOne();continue;}
     try{long before=bytes;if(provider=="Gemini CLI")ScanGemini();else ScanOpenCode();if(bytes!=before)Store.Save(path,state);Publish(note);}
     catch(Exception e)when(e is IOException||e is UnauthorizedAccessException||e is DatabaseException||e is JsonException||e is InvalidOperationException){Publish("Source unavailable or unsupported. Check the folder and app version.");}
     wake.WaitOne(2500);
    }
    if(state!=null)Store.Save(path,state);
   }catch{Publish("Cannot open local cache. Check folder permissions.");}
   finally{state?.Dispose();}
  }
  void ScanGemini(){
   string folder=Path.Combine(root,"tmp");
   if(!Directory.Exists(folder)){files=0;note="No Gemini CLI session folder found";return;}
   if((DateTime.UtcNow-lastDiscovery).TotalSeconds>=30){discovered=Directory.GetFiles(folder,"session-*.json*",SearchOption.AllDirectories).Where(p=>p.EndsWith(".json")||p.EndsWith(".jsonl")).ToArray();lastDiscovery=DateTime.UtcNow;}
   files=discovered.Length;note=files==0?"No Gemini CLI session reports found":"Watching local Gemini CLI reports";
   foreach(string file in discovered){
    if(stopping||!enabled)break;
    var info=new FileInfo(file);if(!info.Exists)continue;
    long stamp=info.LastWriteTimeUtc.Ticks;
    if(modified.TryGetValue(file,out long old)&&old==stamp)continue;
    if(file.EndsWith(".jsonl")){LineCursor cursor;if(!state.Cursors.TryGetValue(file,out cursor)){cursor=new();state.Cursors[file]=cursor;}long before=cursor.ReadBytes;cursor.Read(file,line=>state.Ledger.AcceptGemini(Path.GetFileName(file),line),1048576,()=>stopping||!enabled);bytes+=cursor.ReadBytes-before;}
    else if(info.Length<=32*1024*1024){
     using var stream=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);using var document=JsonDocument.Parse(stream,new JsonDocumentOptions{MaxDepth=128});
     if(document.RootElement.TryGetProperty("messages",out var messages))foreach(var message in messages.EnumerateArray()){
      if(message.TryGetProperty("type",out var type)&&type.GetString()=="gemini"&&message.TryGetProperty("tokens",out var tokens)){
       var minimal=new Dictionary<string,object>();foreach(string name in new[]{"id","timestamp","model","type"})if(message.TryGetProperty(name,out var v))minimal[name]=v.GetString();minimal["tokens"]=JsonSerializer.Deserialize<Dictionary<string,long>>(tokens.GetRawText());state.Ledger.AcceptGemini(Path.GetFileName(file),new JavaScriptSerializer().Serialize(minimal));
      }
     }
     bytes+=info.Length;
    }else note="A large session report was skipped";
    modified[file]=stamp;
   }
  }
  void ScanOpenCode(){
   string db=Path.Combine(root,"opencode.db");if(!File.Exists(db)){files=0;note="No OpenCode database found";return;}
   string stamp=new FileInfo(db).LastWriteTimeUtc.Ticks+"|"+(File.Exists(db+"-wal")?new FileInfo(db+"-wal").LastWriteTimeUtc.Ticks+"|"+new FileInfo(db+"-wal").Length:"");
   if(stamp==databaseStamp)return;
   string table;
   var tables=NativeSqlite.ReadRows(db,"SELECT name,'','','','' FROM sqlite_master WHERE type='table' AND name IN ('message','session_message')").ToArray();
   if(tables.Any(x=>x[0]=="session_message"))table="session_message";else if(tables.Any(x=>x[0]=="message"))table="message";else {files=0;note="OpenCode database schema is not supported";return;}
   files=1;
   string filter=table=="session_message"?" AND type='assistant'":" AND json_extract(data,'$.role')='assistant'";
   var rows=NativeSqlite.ReadRows(db,"SELECT id,session_id,data,time_created,time_updated FROM "+table+" WHERE time_updated>="+Math.Max(0,watermark).ToString(CultureInfo.InvariantCulture)+filter+" ORDER BY time_updated");
   foreach(var row in rows){if(stopping||!enabled)break;if(long.TryParse(row[3],out long created)){state.Ledger.AcceptOpenCode(row[0],row[1],row[2],created);if(long.TryParse(row[4],out long updated))watermark=Math.Max(watermark,updated);}bytes+=row[2]?.Length??0;}
   databaseStamp=stamp;note="Watching local OpenCode reports";
  }
  void Publish(string status){
   if(state==null){snapshot=new Snapshot{Status=status};return;}var ledger=state.Ledger;
   var s=new Snapshot{Today=ledger.TotalFor(DateTime.Today),Week=ledger.TotalFor(DateTime.Today.AddDays(-6)),Recorded=ledger.TotalFor(DateTime.MinValue),Events=ledger.Events,Files=files,Bytes=bytes,Status=status,Started=ledger.Started,Paused=!enabled};
   foreach(var day in ledger.Days)s.Days[day.Key]=day.Value.Copy();
   s.Models=ledger.Models.Values.Select(m=>new BreakdownView{Id=m.Id,Label=m.Label,Today=UsageLedger.TotalForDays(m.Days,DateTime.Today),Week=UsageLedger.TotalForDays(m.Days,DateTime.Today.AddDays(-6)),Recorded=UsageLedger.TotalForDays(m.Days,DateTime.MinValue)}).ToList();
   if(ledger.Sessions.Count>0)s.LastEvent=ledger.Sessions.Values.Max(x=>x.LastAt);snapshot=s;
  }
  public void Dispose(){stopping=true;wake.Set();worker?.Join(5000);}
 }
}
