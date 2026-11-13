using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using PlayDo.Odometer;
using PlayDo.Desktop;

class Program {
 static int passed,failed;
 static void Check(string name,Action check){try{check();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e);}}
 static void Assert(bool value,string message){if(!value)throw new Exception(message);}
 static void Wait(Func<bool> predicate){var end=DateTime.UtcNow.AddSeconds(12);while(DateTime.UtcNow<end){if(predicate())return;Thread.Sleep(60);}throw new Exception("Collector timeout");}
 static string Gemini(long output=8)=>JsonSerializer.Serialize(new{id="gemini_fixture",type="gemini",timestamp=DateTime.UtcNow.ToString("O"),model="gemini-fixture",tokens=new{input=99,output,thoughts=2,cached=70,total=101+output}});
 static string OpenCode()=>JsonSerializer.Serialize(new{role="assistant",modelID="opencode-fixture",tokens=new{input=9,output=8,reasoning=2,cache=new{read=70,write=20}}});
 [DllImport("winsqlite3.dll",CharSet=CharSet.Unicode)] static extern int sqlite3_open16(string path,out IntPtr db);
 [DllImport("winsqlite3.dll")] static extern int sqlite3_exec(IntPtr db,byte[] sql,IntPtr callback,IntPtr arg,IntPtr error);
 [DllImport("winsqlite3.dll")] static extern int sqlite3_close(IntPtr db);
 static int Main(){
  NativeLibrary.SetDllImportResolver(typeof(Program).Assembly,(name,_,_)=>name=="winsqlite3.dll"?NativeLibrary.Load(OperatingSystem.IsWindows()?name:OperatingSystem.IsMacOS()?"/usr/lib/libsqlite3.dylib":"libsqlite3.so.0"):IntPtr.Zero);
  string temp=Path.Combine(Path.GetTempPath(),"odometer-providers-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
  try{
   Check("Directory aliases cannot start two desktop cache writers",()=>{
    string folder=Path.Combine(temp,"instance");using var first=new InstanceGate(folder);using var alias=new InstanceGate(folder+Path.DirectorySeparatorChar);Assert(first.IsPrimary&&!alias.IsPrimary,"directory alias bypassed writer lock");
    if(OperatingSystem.IsWindows()){using var casing=new InstanceGate(folder.ToUpperInvariant());Assert(!casing.IsPrimary,"case alias bypassed writer lock");}
   });
   Check("Gemini total includes thoughts and replay adds only growth",()=>{var l=new UsageLedger();l.AcceptGemini("s",Gemini());l.AcceptGemini("copy",Gemini());Assert(l.TotalFor(DateTime.MinValue).Total==109,"wrong total");l.AcceptGemini("s",Gemini(10));Assert(l.TotalFor(DateTime.MinValue).Total==111,"replay growth wrong");Assert(l.TotalFor(DateTime.MinValue).Cached==70,"cache not subset");});
   Check("Gemini ignores user records and missing identities",()=>{var l=new UsageLedger();l.AcceptGemini("s","{\"type\":\"user\",\"tokens\":{\"input\":20}}");l.AcceptGemini("s","{\"type\":\"gemini\",\"timestamp\":\"bad\"}");Assert(l.TotalFor(DateTime.MinValue).Total==0,"invalid usage counted");});
   Check("OpenCode includes reasoning cache read and cache write once",()=>{var l=new UsageLedger();l.AcceptOpenCode("msg_fixture","s",OpenCode(),DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());l.AcceptOpenCode("msg_fixture","copy",OpenCode(),DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());var t=l.TotalFor(DateTime.MinValue);Assert(t.Input==99&&t.Output==10&&t.Total==109&&t.Cached==70,"wrong OpenCode total");});
   Check("Gemini JSONL partial append and restart preserve deduplication",()=>{
    string home=Path.Combine(temp,"gemini"),data=Path.Combine(temp,"gemini-cache"),folder=Path.Combine(home,"tmp","fixture","chats");Directory.CreateDirectory(folder);string path=Path.Combine(folder,"session-test.jsonl");
    File.WriteAllText(path,Gemini()+"\n"+Gemini(10));
    using(var c=new ExternalCollector("Gemini CLI",home,data)){c.Enable(true);c.Start();Wait(()=>c.GetSnapshot().Recorded.Total==109);File.AppendAllText(path,"\n");c.Refresh();Wait(()=>c.GetSnapshot().Recorded.Total==111);}
    using(var c=new ExternalCollector("Gemini CLI",home,data)){c.Enable(true);c.Start();Wait(()=>c.GetSnapshot().Recorded.Total==111);}
   });
   Check("Legacy Gemini JSON session snapshots are accepted",()=>{
    string home=Path.Combine(temp,"gemini-json"),data=Path.Combine(temp,"gemini-json-cache"),folder=Path.Combine(home,"tmp","fixture","chats");Directory.CreateDirectory(folder);
    File.WriteAllText(Path.Combine(folder,"session-test.json"),"{\"sessionId\":\"fixture\",\"messages\":["+Gemini()+"]}");
    using var c=new ExternalCollector("Gemini CLI",home,data);c.Enable(true);c.Start();Wait(()=>c.GetSnapshot().Recorded.Total==109);
   });
   string source=Path.Combine(temp,"legacy");Directory.CreateDirectory(source);
   using(var state=new SavedState()){state.Ledger.AcceptClaude("s",JsonSerializer.Serialize(new{type="assistant",timestamp=DateTime.UtcNow.ToString("O"),message=new{id="msg_old",model="claude-fixture",usage=new{input_tokens=99,output_tokens=8}}}));Store.Save(Path.Combine(source,"claude-usage-v1.db"),state);}
   Check("Consistent SQLite migration leaves original data unchanged",()=>{string original=Path.Combine(source,"claude-usage-v1.db"),copy=Path.Combine(temp,"copy.db");byte[] before=File.ReadAllBytes(original);NativeSqlite.Backup(original,copy);Assert(before.SequenceEqual(File.ReadAllBytes(original)),"source changed");using var state=Store.Load(copy);Assert(state.Ledger.TotalFor(DateTime.MinValue).Total==107,"history changed");});
   Check("Legacy settings import preserves actual fonts and widget view",()=>{
    File.WriteAllText(Path.Combine(source,"settings.json"),"{\"Theme\":\"Hazy\",\"Font\":\"Georgia\",\"NumberFont\":\"Consolas\",\"View\":\"Tiny\",\"Source\":\"Claude Code\",\"ReduceMotion\":true}");
    using var tracker=new TrackerService(new[]{"--data",Path.Combine(temp,"import")});tracker.ImportLegacy(source);
    Assert(tracker.Settings.Layout=="Tiny"&&tracker.Settings.Theme=="Hazy"&&tracker.Settings.NumberFont=="Consolas"&&tracker.Settings.ReduceMotion,"preferences changed");
    Assert(!tracker.Settings.Enabled["Codex"]&&tracker.Settings.Enabled["Claude Code"],"provider choice changed");
   });
   Check("OpenCode is read from a supported SQLite schema",()=>{
    string home=Path.Combine(temp,"opencode");Directory.CreateDirectory(home);string dbPath=Path.Combine(home,"opencode.db");sqlite3_open16(dbPath,out var db);
    try{long now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();string sql="CREATE TABLE session_message(id TEXT,session_id TEXT,type TEXT,data TEXT,time_created INTEGER,time_updated INTEGER);INSERT INTO session_message VALUES('msg_fixture','s','assistant','"+OpenCode().Replace("'","''")+"',"+now+","+now+");";Assert(sqlite3_exec(db,Encoding.UTF8.GetBytes(sql+"\0"),IntPtr.Zero,IntPtr.Zero,IntPtr.Zero)==0,"fixture SQL failed");}finally{sqlite3_close(db);}
    using var c=new ExternalCollector("OpenCode",home,Path.Combine(temp,"open-cache"));c.Enable(true);c.Start();Wait(()=>c.GetSnapshot().Recorded.Total==109);
   });
   Check("Checkpoints retain usage metadata without response bodies",()=>{string json=ReplayDatabase.LoadReadOnly(Path.Combine(temp,"gemini-cache","gemini cli-usage-v1.db"));Assert(!json.Contains("gemini_fixture")&&!json.Contains("messages"),"raw response data persisted");});
  }finally{try{Directory.Delete(temp,true);}catch{}}
  Console.WriteLine(passed+" passed, "+failed+" failed");return failed==0?0:1;
 }
}
