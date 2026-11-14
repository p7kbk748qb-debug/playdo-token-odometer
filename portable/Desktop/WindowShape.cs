using System.Runtime.InteropServices;
using Avalonia.Platform;

namespace PlayDo.Desktop;
public partial class MainWindow {
 [DllImport("gdi32.dll")] static extern IntPtr CreateRoundRectRgn(int left,int top,int right,int bottom,int ellipseWidth,int ellipseHeight);
 [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr region);
 [DllImport("gdi32.dll")] static extern bool PtInRegion(IntPtr region,int x,int y);
 [DllImport("user32.dll")] static extern int SetWindowRgn(IntPtr window,IntPtr region,bool redraw);
 [DllImport("user32.dll")] static extern int GetWindowRgn(IntPtr window,IntPtr region);
 string windowShape="";
 void UpdateWindowShape(){
  if(!OperatingSystem.IsWindows())return;
  var handle=this.TryGetPlatformHandle()?.Handle??IntPtr.Zero;if(handle==IntPtr.Zero)return;
  bool full=service.Settings.Layout=="Dashboard";
  int width=(int)Math.Ceiling(ClientSize.Width*RenderScaling),height=(int)Math.Ceiling(ClientSize.Height*RenderScaling),diameter=(int)Math.Round(28*RenderScaling);
  string key=handle+":"+(full?"dashboard":width+":"+height+":"+diameter);if(windowShape==key)return;windowShape=key;
  if(full){SetWindowRgn(handle,IntPtr.Zero,true);return;}
  // Clip the native window too: compositor fallback surfaces otherwise keep square corners.
  var region=CreateRoundRectRgn(0,0,width+1,height+1,diameter,diameter);
  if(region==IntPtr.Zero){windowShape="";return;}
  if(SetWindowRgn(handle,region,true)==0){DeleteObject(region);windowShape="";}
  // Windows owns the region after a successful SetWindowRgn.
 }
 void VerifyWindowShape(string layout){
  if(!OperatingSystem.IsWindows())return;
  var handle=this.TryGetPlatformHandle()!.Handle;var region=CreateRoundRectRgn(0,0,1,1,1,1);
  try{
   int kind=GetWindowRgn(handle,region);
   if(layout=="Dashboard"){if(kind!=0)throw new Exception("Dashboard retained widget clipping");}
   else if(kind==0||PtInRegion(region,0,0)||!PtInRegion(region,(int)(ClientSize.Width*RenderScaling/2),(int)(ClientSize.Height*RenderScaling/2)))throw new Exception("Widget native corners are not rounded");
  }finally{DeleteObject(region);}
 }
}
