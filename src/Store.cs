using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
namespace PlayDo.Odometer {
 public class SavedState:IDisposable {
  public int Version=2; public UsageLedger Ledger=new UsageLedger();
  public Dictionary<string,LineCursor> Cursors=new Dictionary<string,LineCursor>();
  public void Dispose(){if(Ledger.Replay!=null){Ledger.Replay.Dispose();Ledger.Replay=null;}if(Ledger.LegacyReplay!=null){Ledger.LegacyReplay.Dispose();Ledger.LegacyReplay=null;}}
 }
 public static class Store {
  public static bool Recovered;
  private static JavaScriptSerializer Serializer() {return new JavaScriptSerializer {MaxJsonLength=64*1024*1024,RecursionLimit=64};}
  public static SavedState Load(string path) {return Load(path,null);}
  public static SavedState Load(string path,string legacyPath) {
   bool fresh;var state=LoadCurrent(path,out fresh);
   try {
    if(fresh&&legacyPath!=null&&File.Exists(legacyPath)){
     string oldJson=ReplayDatabase.LoadReadOnly(legacyPath);var old=oldJson==null?null:Serializer().Deserialize<SavedState>(oldJson);
     if(old!=null){if(old.Ledger==null||old.Ledger.Days==null)throw new InvalidDataException("The previous aggregate checkpoint could not be read.");foreach(var day in old.Ledger.Days)state.Ledger.LegacyDays[day.Key]=day.Value.Copy();state.Ledger.Started=old.Ledger.Started;state.Ledger.LatestQuota=old.Ledger.LatestQuota;state.Ledger.ReconcileLegacy();}
    }
    if(state.Ledger.LegacyDays.Count>0&&legacyPath!=null&&File.Exists(legacyPath))state.Ledger.LegacyReplay=new ReplayDatabase(legacyPath,true);
    return state;
   }catch{state.Dispose();throw;}
  }
  static SavedState LoadCurrent(string path,out bool fresh) {
   fresh=true;
   Recovered=false;Directory.CreateDirectory(Path.GetDirectoryName(path));ReplayDatabase db=null;
   try {db=new ReplayDatabase(path);string json=db.Load();var s=json==null?new SavedState():Serializer().Deserialize<SavedState>(json);if(s==null||s.Version!=2||s.Ledger==null||s.Ledger.Days==null||s.Ledger.Sessions==null||s.Ledger.Seen==null||s.Ledger.Models==null||s.Ledger.LegacyDays==null||s.Ledger.LegacyRecoveredDays==null||s.Ledger.PreservedDays==null||s.Cursors==null)throw new InvalidDataException();s.Ledger.Replay=db;fresh=json==null;return s;}
   catch(DatabaseException e){if(db!=null)db.Dispose();if(e.Code!=11&&e.Code!=26)throw;return Recover(path);}
   catch(ArgumentException){if(db!=null)db.Dispose();return Recover(path);}
   catch(InvalidDataException){if(db!=null)db.Dispose();return Recover(path);}
   catch {if(db!=null)db.Dispose();throw;}
  }
  static SavedState Recover(string path){Recovered=true;string suffix=".unreadable-"+DateTime.UtcNow.Ticks;foreach(var ext in new[]{"","-wal","-shm"})if(File.Exists(path+ext))File.Move(path+ext,path+suffix+ext);var fresh=new SavedState();fresh.Ledger.Replay=new ReplayDatabase(path);return fresh;}
  public static void Save(string path,SavedState state) {
   Directory.CreateDirectory(Path.GetDirectoryName(path));if(state.Ledger.Replay==null)state.Ledger.Replay=new ReplayDatabase(path);
   var pending=state.Ledger.Seen;state.Ledger.Seen=new Dictionary<string,bool>();
   try{state.Ledger.Replay.Save(Serializer().Serialize(state),pending.Keys);}catch{state.Ledger.Seen=pending;throw;}
  }
 }
}
