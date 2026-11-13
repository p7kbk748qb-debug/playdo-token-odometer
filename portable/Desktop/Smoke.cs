using System.Text.Json;
using PlayDo.Odometer;

namespace PlayDo.Desktop;
public static class Smoke {
 public static int Run(string[] args){
  try{
   string? legacy=TrackerService.Option(args,"--audit-legacy");
   if(legacy!=null){
    foreach(string name in new[]{"usage-v3.db","claude-usage-v1.db"}){
     string path=Path.Combine(legacy,name);if(!File.Exists(path))continue;
     string json=ReplayDatabase.LoadReadOnly(path);using var document=JsonDocument.Parse(json);
     long expected=0;foreach(var day in document.RootElement.GetProperty("Ledger").GetProperty("Days").EnumerateObject())expected+=day.Value.GetProperty("Input").GetInt64()+day.Value.GetProperty("Output").GetInt64();
     using var state=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<SavedState>(json);
     if(state.Ledger.TotalFor(DateTime.MinValue).Total!=expected)throw new Exception("Legacy token total mismatch");
    }
    Console.WriteLine("PASS read-only legacy checkpoints deserialize with identical totals");
   }
   string data=TrackerService.Option(args,"--data")??Path.Combine(Path.GetTempPath(),"odometer-smoke-"+Guid.NewGuid().ToString("N"));
   Directory.CreateDirectory(data);
   using(var state=new SavedState()){state.Ledger.AcceptClaude("fixture",JsonSerializer.Serialize(new{type="assistant",timestamp=DateTime.UtcNow.ToString("O"),message=new{id="msg_smoke",model="claude-smoke",usage=new{input_tokens=9,cache_read_input_tokens=70,cache_creation_input_tokens=20,output_tokens=8}}}));Store.Save(Path.Combine(data,"smoke.db"),state);}
   using(var state=Store.Load(Path.Combine(data,"smoke.db"))){if(state.Ledger.TotalFor(DateTime.MinValue).Total!=107)throw new Exception("Portable SQLite accounting failed");}
   Console.WriteLine("PASS portable SQLite, checkpoint JSON and native library resolution");
   return 0;
  }catch(Exception e){Console.Error.WriteLine(e);return 1;}
 }
}
