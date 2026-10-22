using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Interop;
namespace PlayDo.Odometer {
 public class Settings {
  public bool Topmost,ReduceMotion;
  public string Theme="Instrument",Font="Segoe UI",NumberFont="Consolas",Counter="Mechanical",View="Dashboard";
  public void Normalize(){
   Theme=Appearance.Choice(Theme,Appearance.Themes);
   Font=Appearance.FontName(Font);NumberFont=NumberFont==null?"Consolas":Appearance.FontName(NumberFont);
   Counter=Appearance.Choice(Counter,Appearance.Counters);
   if(Array.IndexOf(new[]{"Dashboard","Compact","Mini"},View)<0)View="Dashboard";
  }
  public static Settings Load(string path){try{var prefs=File.Exists(path)?new JavaScriptSerializer().Deserialize<Settings>(File.ReadAllText(path)):new Settings();if(prefs==null)prefs=new Settings();prefs.Normalize();return prefs;}catch{return new Settings();}}
  public void Save(string path){Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,new JavaScriptSerializer().Serialize(this));}
 }
 public static class Shell {
  public const string AppId="PlayDo.TokenOdometer";
  [StructLayout(LayoutKind.Sequential)] struct PropertyKey { public Guid Format; public uint Id; }
  [StructLayout(LayoutKind.Sequential)] struct PropVariant { public ushort Type,Reserved1,Reserved2,Reserved3; public IntPtr Value,Padding; }
  [ComImport,Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
  interface IPropertyStore {
   [PreserveSig] int GetCount(out uint count);
   [PreserveSig] int GetAt(uint index,out PropertyKey key);
   [PreserveSig] int GetValue(ref PropertyKey key,IntPtr value);
   [PreserveSig] int SetValue(ref PropertyKey key,ref PropVariant value);
   [PreserveSig] int Commit();
  }
  [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr hwnd,int attribute,ref int value,int size);
  [DllImport("shell32.dll",CharSet=CharSet.Unicode)] static extern int SetCurrentProcessExplicitAppUserModelID(string appId);
  public static void SetIdentity(){try{SetCurrentProcessExplicitAppUserModelID(AppId);}catch{}}
  public static void InstallShortcuts(string root){
   Shortcut(Path.Combine(root,"PlayDo's Token Odometer.lnk"),"");
   Shortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),"PlayDo's Token Odometer.lnk"),"");
  }
  static void SetShortcutIdentity(string path){
   object native=Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("00021401-0000-0000-C000-000000000046")));
   var value=new PropVariant {Type=31,Value=Marshal.StringToCoTaskMemUni(AppId)};
   try {
    var file=(System.Runtime.InteropServices.ComTypes.IPersistFile)native;file.Load(path,2);
    var store=(IPropertyStore)native;var key=new PropertyKey {Format=new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"),Id=5};
    Marshal.ThrowExceptionForHR(store.SetValue(ref key,ref value));Marshal.ThrowExceptionForHR(store.Commit());file.Save(path,true);
   }finally{Marshal.FreeCoTaskMem(value.Value);Marshal.FinalReleaseComObject(native);}
  }
  public static void DarkTitleBar(Window w){TitleBar(w,true);}
  public static void TitleBar(Window w,bool dark){try{int value=dark?1:0;DwmSetWindowAttribute(new WindowInteropHelper(w).Handle,20,ref value,4);}catch{}}
  static string StartupPath(){return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup),"PlayDo's Token Odometer.lnk");}
  public static bool StartupEnabled(){return File.Exists(StartupPath());}
  public static void SetStartup(bool on){string p=StartupPath();if(on)Shortcut(p,"--tray");else if(File.Exists(p))File.Delete(p);}
  public static void Shortcut(string path,string args) {
   object shell=null,link=null;
   try {
    var type=Type.GetTypeFromProgID("WScript.Shell");shell=Activator.CreateInstance(type);
    link=type.InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{path});var lt=link.GetType();
    string exe=Assembly.GetExecutingAssembly().Location;
    lt.InvokeMember("TargetPath",BindingFlags.SetProperty,null,link,new object[]{exe});
    lt.InvokeMember("Arguments",BindingFlags.SetProperty,null,link,new object[]{args});
    lt.InvokeMember("WorkingDirectory",BindingFlags.SetProperty,null,link,new object[]{Path.GetDirectoryName(exe)});
    lt.InvokeMember("IconLocation",BindingFlags.SetProperty,null,link,new object[]{exe+",0"});
    lt.InvokeMember("Description",BindingFlags.SetProperty,null,link,new object[]{"PlayDo's Token Odometer — local Codex usage"});
    lt.InvokeMember("Save",BindingFlags.InvokeMethod,null,link,null);
   }finally{if(link!=null)Marshal.FinalReleaseComObject(link);if(shell!=null)Marshal.FinalReleaseComObject(shell);}
   SetShortcutIdentity(path);
  }
 }
}
