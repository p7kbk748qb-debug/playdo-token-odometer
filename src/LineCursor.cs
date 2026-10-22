using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
namespace PlayDo.Odometer {
 public class LineCursor {
  public long Offset; public long ReadBytes;
  public long LastWriteTicks;public string Boundary;
  static string Fingerprint(byte[] bytes){using(var sha=SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(bytes));}
  public bool Read(string path, Action<string> accept) {
   using(var file=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete,65536)) {
    long modified=File.GetLastWriteTimeUtc(path).Ticks;
    if(file.Length<Offset||(file.Length==Offset&&modified!=LastWriteTicks))Offset=0;
    if(Offset>0&&Boundary!=null){file.Seek(Math.Max(0,Offset-64),SeekOrigin.Begin);byte[] end=new byte[(int)Math.Min(64,Offset)];file.Read(end,0,end.Length);if(Fingerprint(end)!=Boundary)Offset=0;}
    if(file.Length==Offset)return false;
    file.Seek(Offset,SeekOrigin.Begin);long position=Offset;bool changed=false,oversize=false;
    byte[] chunk=new byte[65536];using(var line=new MemoryStream()) {
     int n;while((n=file.Read(chunk,0,chunk.Length))>0) {
      ReadBytes+=n;
      for(int i=0;i<n;i++) {
       position++;byte b=chunk[i];
       if(b==10) {
        if(!oversize&&line.Length>0)accept(Encoding.UTF8.GetString(line.GetBuffer(),0,(int)line.Length).TrimEnd('\r'));
        Offset=position;line.SetLength(0);oversize=false;changed=true;
       }else if(!oversize) {if(line.Length<262144)line.WriteByte(b);else {oversize=true;line.SetLength(0);}}
      }
     }
    }LastWriteTicks=modified;if(Offset>0){file.Seek(Math.Max(0,Offset-64),SeekOrigin.Begin);byte[] end=new byte[(int)Math.Min(64,Offset)];file.Read(end,0,end.Length);Boundary=Fingerprint(end);}return changed;
   }
  }
 }
}
