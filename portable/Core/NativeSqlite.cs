using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace PlayDo.Odometer {
 public static class NativeSqlite {
  [ModuleInitializer] public static void InstallResolver()=>NativeLibrary.SetDllImportResolver(typeof(ReplayDatabase).Assembly,(name,assembly,path)=>{
   if(name!="winsqlite3.dll")return IntPtr.Zero;
   return NativeLibrary.Load(OperatingSystem.IsWindows()?"winsqlite3.dll":OperatingSystem.IsMacOS()?"/usr/lib/libsqlite3.dylib":"libsqlite3.so.0");
  });
  [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_open_v2(byte[] path,out IntPtr db,int flags,IntPtr vfs);
  [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_close(IntPtr db);
  [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_prepare_v2(IntPtr db,byte[] sql,int bytes,out IntPtr statement,IntPtr tail);
  [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_step(IntPtr statement);
  [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_finalize(IntPtr statement);
  [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)] static extern IntPtr sqlite3_column_text(IntPtr statement,int column);
  [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)] static extern IntPtr sqlite3_backup_init(IntPtr target,byte[] targetName,IntPtr source,byte[] sourceName);
  [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_backup_step(IntPtr backup,int pages);
  [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_backup_finish(IntPtr backup);
  public static void Backup(string source,string target){
   if(File.Exists(target))throw new IOException("Target cache already exists");
   IntPtr from=IntPtr.Zero,to=IntPtr.Zero,backup=IntPtr.Zero;
   try{
    int code=sqlite3_open_v2(Encoding.UTF8.GetBytes(source+"\0"),out from,1,IntPtr.Zero);if(code!=0)throw new DatabaseException(code);
    code=sqlite3_open_v2(Encoding.UTF8.GetBytes(target+"\0"),out to,6,IntPtr.Zero);if(code!=0)throw new DatabaseException(code);
    byte[] main=Encoding.UTF8.GetBytes("main\0");backup=sqlite3_backup_init(to,main,from,main);
    if(backup==IntPtr.Zero)throw new IOException("Could not start history backup");
    for(int retry=0;;retry++){code=sqlite3_backup_step(backup,-1);if(code==101)break;if((code!=5&&code!=6)||retry>=5)throw new DatabaseException(code);Thread.Sleep(150);}
    code=sqlite3_backup_finish(backup);backup=IntPtr.Zero;if(code!=0)throw new DatabaseException(code);
   }catch{if(backup!=IntPtr.Zero){sqlite3_backup_finish(backup);backup=IntPtr.Zero;}if(to!=IntPtr.Zero){sqlite3_close(to);to=IntPtr.Zero;}if(File.Exists(target))File.Delete(target);throw;}
   finally{if(backup!=IntPtr.Zero)sqlite3_backup_finish(backup);if(from!=IntPtr.Zero)sqlite3_close(from);if(to!=IntPtr.Zero)sqlite3_close(to);}
  }
  public static IEnumerable<string[]> ReadRows(string path,string sql){
   IntPtr db=IntPtr.Zero,statement=IntPtr.Zero;
   try{
    int code=sqlite3_open_v2(Encoding.UTF8.GetBytes(path+"\0"),out db,1,IntPtr.Zero);if(code!=0)throw new DatabaseException(code);
    code=sqlite3_prepare_v2(db,Encoding.UTF8.GetBytes(sql+"\0"),-1,out statement,IntPtr.Zero);if(code!=0)throw new DatabaseException(code);
    while((code=sqlite3_step(statement))==100){var row=new string[5];for(int i=0;i<row.Length;i++)row[i]=Marshal.PtrToStringUTF8(sqlite3_column_text(statement,i));yield return row;}
    if(code!=101)throw new DatabaseException(code);
   }finally{if(statement!=IntPtr.Zero)sqlite3_finalize(statement);if(db!=IntPtr.Zero)sqlite3_close(db);}
  }
 }
}
