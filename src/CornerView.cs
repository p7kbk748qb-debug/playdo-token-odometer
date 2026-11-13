using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Forms=System.Windows.Forms;

namespace PlayDo.Odometer {
 public sealed partial class Dashboard {
  static readonly string[] Corners={"Free","Top left","Top right","Bottom left","Bottom right"};
  bool positioning;
  bool IncludesCodex {get{return settings.Source!=SourceTotals.Claude;}}
  public Snapshot CurrentSnapshot(){return SourceTotals.Select(settings.Source,collector.GetSnapshot(),claudeCollector==null?null:claudeCollector.GetSnapshot());}
  void InitializeSources(){
   if(Array.IndexOf(SourceTotals.Choices,settings.Source)<0)settings.Source=SourceTotals.Codex;
   Choice("UsageSourceChoice",SourceTotals.Choices,settings.Source);
   Find<ComboBox>("UsageSourceChoice").SelectionChanged+=delegate{ChangeSource(Find<ComboBox>("UsageSourceChoice").SelectedItem as string);};
   ApplySourcePause();
  }
  void ApplySourcePause(){
   collector.Pause(paused||!IncludesCodex);
   if(claudeCollector!=null)claudeCollector.Pause(paused||settings.Source==SourceTotals.Codex);
  }
  void ChangeSource(string source){
   if(source==null||source==settings.Source)return;
   settings.Source=source;ApplySourcePause();
   Find<ComboBox>("UsageSourceChoice").SelectedItem=source;
   actionMessage="";SaveSettings();ApplySmallLayout();Render();
  }
  void InitializeCorner(){
   if(Array.IndexOf(Corners,settings.Corner)<0)settings.Corner=Corners[0];
   Find<FrameworkElement>("DashboardDrag").MouseLeftButtonDown+=delegate(object sender,MouseButtonEventArgs e){
    if(e.ClickCount==2)ToggleMaximize();else if(Window.WindowState==WindowState.Normal)Window.DragMove();
   };
   Find<FrameworkElement>("SmallDrag").MouseLeftButtonDown+=delegate(object sender,MouseButtonEventArgs e){
    if(e.ClickCount==2){SetView("Dashboard");return;}
    var before=new Point(Window.Left,Window.Top);
    Window.DragMove();
    if(before!=new Point(Window.Left,Window.Top)&&settings.Corner!=Corners[0]){settings.Corner=Corners[0];SaveSettings();}
   };
   Window.SourceInitialized+=delegate{((HwndSource)PresentationSource.FromVisual(Window)).AddHook(WorkAreaHook);};
   Click("MinimizeButton",delegate{Window.WindowState=WindowState.Minimized;});
   Click("MaximizeButton",delegate{ToggleMaximize();});
   Click("HideButton",delegate{Window.Hide();});
   Click("HideSmallButton",delegate{Window.Hide();});
   Window.SizeChanged+=delegate{PlaceCorner();};
   Window.DpiChanged+=delegate{Window.Dispatcher.BeginInvoke(new Action(PlaceCorner),DispatcherPriority.Loaded);};
   Window.IsVisibleChanged+=delegate{if(Window.IsVisible)Window.Dispatcher.BeginInvoke(new Action(PlaceCorner),DispatcherPriority.Loaded);};
  }
  void ToggleMaximize(){Window.WindowState=Window.WindowState==WindowState.Maximized?WindowState.Normal:WindowState.Maximized;}
  void ApplyWindowSurface(){
   bool small=view!="Dashboard";
   SmallVisibility("DashboardChrome",!small);
   Window.ResizeMode=small?ResizeMode.CanMinimize:ResizeMode.CanResizeWithGrip;
   var surface=Find<Border>("CompactView");
   if(small&&settings.Transparent){
    var color=((SolidColorBrush)Window.Resources["Surface"]).Color;
    color.A=204;var brush=new SolidColorBrush(color);brush.Freeze();surface.Background=brush;Find<Border>("TinyView").Background=brush;
    SmallVisibility("SmallTexture",false);
   }else{
    Resource(surface,Border.BackgroundProperty,"SurfaceMaterial");Resource(Find<Border>("TinyView"),Border.BackgroundProperty,"SurfaceMaterial");SmallVisibility("SmallTexture",true);
   }
   PlaceCorner();
  }
  [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
  [StructLayout(LayoutKind.Sequential)] struct PixelPoint {public int X,Y;}
  [StructLayout(LayoutKind.Sequential)] struct MinMaxInfo {public PixelPoint Reserved,MaxSize,MaxPosition,MinTrack,MaxTrack;}
  IntPtr WorkAreaHook(IntPtr handle,int message,IntPtr wParam,IntPtr lParam,ref bool handled){
   if(message==0x24){
    var screen=Forms.Screen.FromHandle(handle);var area=screen.WorkingArea;var monitor=screen.Bounds;
    var info=(MinMaxInfo)Marshal.PtrToStructure(lParam,typeof(MinMaxInfo));
    info.MaxPosition=new PixelPoint{X=area.Left-monitor.Left,Y=area.Top-monitor.Top};
    info.MaxSize=new PixelPoint{X=area.Width,Y=area.Height};
    Marshal.StructureToPtr(info,lParam,false);handled=true;
   }
   return IntPtr.Zero;
  }
  void CenterDashboard(){
   if(view!="Dashboard"||!Window.IsVisible||Window.WindowState!=WindowState.Normal)return;
   var handle=new WindowInteropHelper(Window).Handle;var source=HwndSource.FromHwnd(handle);
   if(source==null||source.CompositionTarget==null)return;
   var scale=source.CompositionTarget.TransformToDevice;var area=Forms.Screen.FromHandle(handle).WorkingArea;
   int x=area.Left+Math.Max(0,(int)((area.Width-Window.Width*scale.M11)/2));
   int y=area.Top+Math.Max(0,(int)((area.Height-Window.Height*scale.M22)/2));
   SetWindowPos(handle,IntPtr.Zero,x,y,0,0,0x15);
  }
  void PlaceCorner(){
   if(positioning||view=="Dashboard"||settings.Corner=="Free"||!Window.IsVisible||Window.WindowState!=WindowState.Normal)return;
   var handle=new WindowInteropHelper(Window).Handle;var source=HwndSource.FromHwnd(handle);
   if(source==null||source.CompositionTarget==null)return;
   var scale=source.CompositionTarget.TransformToDevice;
   var area=Forms.Screen.FromHandle(handle).WorkingArea;
   var bounds=CornerBounds(new Rect(area.X,area.Y,area.Width,area.Height),new Size(Window.Width*scale.M11,Window.Height*scale.M22),settings.Corner,12*scale.M11);
   positioning=true;
   try{SetWindowPos(handle,IntPtr.Zero,(int)Math.Round(bounds.X),(int)Math.Round(bounds.Y),0,0,0x15);}
   finally{positioning=false;}
  }
  public static Point CornerBounds(Rect area,Size size,string corner,double margin){
   double x=corner.EndsWith("right",StringComparison.Ordinal)?area.Right-size.Width-margin:area.Left+margin;
   double y=corner.StartsWith("Bottom",StringComparison.Ordinal)?area.Bottom-size.Height-margin:area.Top+margin;
   return new Point(Math.Max(area.Left,Math.Min(area.Right-size.Width,x)),Math.Max(area.Top,Math.Min(area.Bottom-size.Height,y)));
  }
 }
}
