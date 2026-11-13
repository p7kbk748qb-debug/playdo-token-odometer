using System.Text.Json;
using PlayDo.Odometer;

namespace PlayDo.Desktop;
public static class Smoke {
 public static int Run(string[] args){
  try{
   string data=TrackerService.Option(args,"--data")??Path.Combine(Path.GetTempPath(),"odometer-smoke-"+Guid.NewGuid().ToString("N"));
   Directory.CreateDirectory(data);
   using(var state=new SavedState()){state.Ledger.AcceptClaude("fixture",JsonSerializer.Serialize(new{type="assistant",timestamp=DateTime.UtcNow.ToString("O"),message=new{id="msg_smoke",model="claude-smoke",usage=new{input_tokens=9,cache_read_input_tokens=70,cache_creation_input_tokens=20,output_tokens=8}}}));Store.Save(Path.Combine(data,"smoke.db"),state);}
   using(var state=Store.Load(Path.Combine(data,"smoke.db"))){if(state.Ledger.TotalFor(DateTime.MinValue).Total!=107)throw new Exception("Portable SQLite accounting failed");}
   Console.WriteLine("PASS portable SQLite, checkpoint JSON and native library resolution");
   return 0;
  }catch(Exception e){Console.Error.WriteLine(e);return 1;}
 }
}
