using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;

namespace PlayDo.Desktop;
// One writer per private data directory, with a local same-user restore signal.
public sealed class InstanceGate:IDisposable {
 [DllImport("libc",SetLastError=true)] static extern int flock(int descriptor,int operation);
 readonly FileStream? lockFile;readonly string name;readonly CancellationTokenSource stop=new();Task? listener;
 public bool IsPrimary {get;}
 public static Action? OpenRequested;
 public InstanceGate(string data){
  Directory.CreateDirectory(data);string path=Path.Combine(data,"desktop.instance");
  string identity=Path.TrimEndingDirectorySeparator(Path.GetFullPath(data));if(OperatingSystem.IsWindows())identity=identity.ToUpperInvariant();
  name="PlayDo.Odometer."+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..24];
  try{
   lockFile=new FileStream(path,FileMode.OpenOrCreate,FileAccess.ReadWrite,OperatingSystem.IsWindows()?FileShare.Read:FileShare.ReadWrite);
   // Unix sharing flags do not enforce the same writer exclusion as Windows.
   if(!OperatingSystem.IsWindows()&&flock(lockFile.SafeFileHandle.DangerousGetHandle().ToInt32(),6)!=0)throw new IOException("Data directory already has a writer");
   IsPrimary=true;
   lockFile.SetLength(0);var bytes=Encoding.UTF8.GetBytes(name);lockFile.Write(bytes);lockFile.Flush();listener=Listen();
  }catch(IOException){
   lockFile?.Dispose();lockFile=null;
   IsPrimary=false;
   for(int retry=0;retry<3;retry++){try{using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite);using var reader=new StreamReader(stream);string existing=reader.ReadToEnd();if(existing.Length>0){name=existing;break;}}catch(IOException){}Thread.Sleep(50);}
  }
 }
 async Task Listen(){
  while(!stop.IsCancellationRequested){
   try{
    using var pipe=new NamedPipeServerStream(name,PipeDirection.In,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);
    await pipe.WaitForConnectionAsync(stop.Token);
    using var timeout=CancellationTokenSource.CreateLinkedTokenSource(stop.Token);timeout.CancelAfter(2000);
    var data=new byte[1];if(await pipe.ReadAsync(data.AsMemory(),timeout.Token)==1&&data[0]==1)OpenRequested?.Invoke();
   }catch(OperationCanceledException){}catch(IOException){if(!stop.IsCancellationRequested)await Task.Delay(250);}
  }
 }
 public void RequestOpen(){try{using var pipe=new NamedPipeClientStream(".",name,PipeDirection.Out,PipeOptions.CurrentUserOnly);pipe.Connect(1000);pipe.WriteByte(1);pipe.Flush();}catch(IOException){}catch(TimeoutException){}}
 public void Dispose(){stop.Cancel();lockFile?.Dispose();}
}
