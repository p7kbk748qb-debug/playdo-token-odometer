using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
namespace PlayDo.Odometer {
 public sealed partial class Dashboard {
  void InitializeTiny(){
   Click("TinyButton",delegate{SetView("Tiny");});
   Click("TinyExpand",delegate{SetView("Dashboard");});
   Click("TinyHide",delegate{Window.Hide();});
   Find<Border>("TinyView").ContextMenu=Find<Border>("CompactView").ContextMenu;
   Find<Border>("TinyView").MouseLeftButtonDown+=delegate(object sender,MouseButtonEventArgs e){
    if(!TinyDragTarget(e.OriginalSource as DependencyObject))return;
    if(e.ClickCount==2){SetView("Dashboard");return;}
    var before=new Point(Window.Left,Window.Top);Window.DragMove();
    if(before!=new Point(Window.Left,Window.Top)&&settings.Corner!="Free"){settings.Corner="Free";SaveSettings();}
    e.Handled=true;
   };
  }
  public static bool TinyDragTarget(DependencyObject element){
   while(element!=null){if(element is ButtonBase)return false;if(element is Visual)element=VisualTreeHelper.GetParent(element);else break;}return true;
  }
 }
}
