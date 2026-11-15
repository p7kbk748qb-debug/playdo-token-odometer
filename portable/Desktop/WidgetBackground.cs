using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace PlayDo.Desktop;
public partial class MainWindow {
 static readonly string[] WidgetBackgrounds={"Solid","Glass","Clear"};
 // Preserve legacy boolean settings until a background is explicitly chosen.
 string WidgetBackgroundMode=>WidgetBackgrounds.Contains(service.Settings.WidgetBackground)?service.Settings.WidgetBackground!:service.Settings.Transparent?"Clear":"Solid";
 void SetWidgetBackground(string mode){
  if(!WidgetBackgrounds.Contains(mode))return;
  service.Settings.WidgetBackground=mode;service.Settings.Transparent=mode!="Solid";
  bool was=initializing;initializing=true;Find<ComboBox>("BackgroundChoice").SelectedItem=mode;initializing=was;
  ApplyTheme();service.Save();Render();
 }
 void ApplyWidgetBackground(ThemeSpec theme){
  var widget=Find<Border>("Widget");string mode=WidgetBackgroundMode;
  widget.BorderThickness=new Thickness(mode=="Clear"?0:1);
  widget.BorderBrush=mode=="Glass"?new SolidColorBrush(theme.Light?Color.FromArgb(70,40,50,65):Color.FromArgb(70,255,255,255)):Brush.Parse(theme.Stroke);
  if(mode=="Clear"){widget.Background=Brushes.Transparent;return;}
  if(mode=="Solid"){widget.Background=Brush.Parse(theme.Surface);return;}
  var tint=Color.Parse(theme.Surface);
  widget.Background=new LinearGradientBrush{
   StartPoint=new RelativePoint(0,0,RelativeUnit.Relative),EndPoint=new RelativePoint(0,1,RelativeUnit.Relative),
   GradientStops=new GradientStops{new GradientStop(Color.FromArgb(100,tint.R,tint.G,tint.B),0),new GradientStop(Color.FromArgb(55,tint.R,tint.G,tint.B),1)}
  };
 }
}
