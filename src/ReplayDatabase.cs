using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
namespace PlayDo.Odometer {
 public class DatabaseException:Exception {public readonly int Code;public DatabaseException(int code):base("Local usage database error ("+code+")."){Code=code;}}
 // Windows 10+ ships this SQLite implementation. The index stays on disk; no NuGet/runtime download.
 public sealed class ReplayDatabase:IDisposable {
  IntPtr db,lookup;bool disposed;readonly bool readOnly;
  [DllImport("winsqlite3.dll",CharSet=CharSet.Unicode,CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_open16(string filename,out IntPtr db);
  [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_open_v2(byte[] filename,out IntPtr db,int flags,IntPtr vfs);
  [DllImport("winsqlite3.dll",CharSet=CharSet.Unicode,CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_prepare16_v2(IntPtr db,string sql,int bytes,out IntPtr stmt,IntPtr tail);
  [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_step(IntPtr stmt);
  [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_finalize(IntPtr stmt);
  [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_reset(IntPtr stmt);
  [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_clear_bindings(IntPtr stmt);
  [DllImport("winsqlite3.dll",CharSet=CharSet.Unicode,CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_bind_text16(IntPtr stmt,int index,string text,int bytes,IntPtr destroy);
  [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern IntPtr sqlite3_column_text16(IntPtr stmt,int col);
  [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_close(IntPtr db);
  [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)]static extern int sqlite3_busy_timeout(IntPtr db,int ms);
  static void OK(int code){if(code!=0&&code!=100&&code!=101)throw new DatabaseException(code);}
  IntPtr Prepare(string sql){IntPtr s;OK(sqlite3_prepare16_v2(db,sql,-1,out s,IntPtr.Zero));return s;}
  void Execute(string sql){var s=Prepare(sql);try{OK(sqlite3_step(s));}finally{sqlite3_finalize(s);}}
  static void Bind(IntPtr s,string value){OK(sqlite3_bind_text16(s,1,value,value.Length*2,new IntPtr(-1)));}
  public ReplayDatabase(string path):this(path,false){}
  public ReplayDatabase(string path,bool readOnly){this.readOnly=readOnly;try{if(readOnly)OK(sqlite3_open_v2(Encoding.UTF8.GetBytes(path+"\0"),out db,1,IntPtr.Zero));else OK(sqlite3_open16(path,out db));sqlite3_busy_timeout(db,3000);Execute("PRAGMA cache_size=-2048");if(!readOnly){Execute("PRAGMA journal_mode=WAL");Execute("CREATE TABLE IF NOT EXISTS replay (id TEXT PRIMARY KEY) WITHOUT ROWID");Execute("CREATE TABLE IF NOT EXISTS checkpoint (id INTEGER PRIMARY KEY, json TEXT NOT NULL)");}lookup=Prepare("SELECT 1 FROM replay WHERE id=?1");}catch{Dispose();throw;}}
  public bool Contains(string key){if(disposed)throw new DatabaseException(21);Bind(lookup,key);int code=sqlite3_step(lookup);sqlite3_reset(lookup);sqlite3_clear_bindings(lookup);OK(code);return code==100;}
  public string Load(){var s=Prepare("SELECT json FROM checkpoint WHERE id=1");try{int code=sqlite3_step(s);OK(code);return code==100?Marshal.PtrToStringUni(sqlite3_column_text16(s,0)):null;}finally{sqlite3_finalize(s);}}
  // Read the old aggregate checkpoint without changing its schema, journal mode or rows.
  public static string LoadReadOnly(string path){using(var old=new ReplayDatabase(path,true))return old.Load();}
  public void Save(string json,IEnumerable<string> keys){if(readOnly)throw new DatabaseException(8);Execute("BEGIN IMMEDIATE");try{var insert=Prepare("INSERT OR IGNORE INTO replay(id) VALUES(?1)");try{foreach(string key in keys){Bind(insert,key);OK(sqlite3_step(insert));sqlite3_reset(insert);sqlite3_clear_bindings(insert);}}finally{sqlite3_finalize(insert);}var state=Prepare("INSERT OR REPLACE INTO checkpoint(id,json) VALUES(1,?1)");try{Bind(state,json);OK(sqlite3_step(state));}finally{sqlite3_finalize(state);}Execute("COMMIT");}catch{try{Execute("ROLLBACK");}catch{}throw;}}
  public void Dispose(){if(disposed)return;disposed=true;if(lookup!=IntPtr.Zero)sqlite3_finalize(lookup);if(db!=IntPtr.Zero)sqlite3_close(db);}
 }
}
