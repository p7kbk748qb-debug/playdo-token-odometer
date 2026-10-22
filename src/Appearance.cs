using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace PlayDo.Odometer {
 public sealed class ThemePreset {
  public readonly string Name,Description,Font,NumberFont,Counter,Material;
  public readonly double Radius;
  public readonly bool Dark;
  public readonly string[] Colors;
  public ThemePreset(string name,string description,string font,string numberFont,string counter,string material,double radius,bool dark,string[] colors){Name=name;Description=description;Font=font;NumberFont=numberFont;Counter=counter;Material=material;Radius=radius;Dark=dark;Colors=colors;}
 }
 public static class Appearance {
  public static readonly string[] Themes={"Instrument","Graphite","Arctic","Terminal","Winter","Hazy","Aurora","Blueprint","Sunset","Paper"};
  public static readonly string[] Fonts={"Segoe UI","Segoe UI Light","Bahnschrift","Consolas","Cascadia Mono","Lucida Console","Georgia","Trebuchet MS","Cambria"};
  public static readonly string[] Counters={"Mechanical","Clean","Ledger","Pixel","Frosted","Airy","Split","Outline","Ribbon","Serif"};
  public static readonly string[] Views={"Dashboard","Compact","Mini"};
  static readonly HashSet<string> InstalledFonts=new HashSet<string>(System.Windows.Media.Fonts.SystemFontFamilies.Select(x=>x.Source),StringComparer.OrdinalIgnoreCase);
  static readonly string[] Keys={"Canvas","Surface","SurfaceAlt","Control","Hover","Pressed","Stroke","Text","Muted","Accent","Signal","Selected","OnAccent","Digit","ChartBar"};
  public static readonly ThemePreset[] Presets={
   new ThemePreset("Instrument","Precision meter · amber on ink","Bahnschrift","Consolas","Mechanical","meter",16,true,new[]{"#101B2B","#17273B","#152235","#20334A","#304A65","#405773","#405773","#F1F5FB","#B8C6D9","#FFBD69","#76D8E5","#493A29","#101B2B","#0B1422","#799FBE"}),
   new ThemePreset("Graphite","Brushed charcoal · silver display","Segoe UI","Bahnschrift","Clean","brushed",5,true,new[]{"#17191D","#22252B","#1D2026","#2D3139","#3D4551","#4B5665","#596473","#F3F5F8","#BFC7D1","#BCD9FF","#A5DEE5","#344A66","#151A21","#101216","#8BA9D0"}),
   new ThemePreset("Arctic","Light steel · crisp outlined digits","Bahnschrift","Consolas","Outline","ice",4,false,new[]{"#EBF0F6","#FFFFFF","#F4F7FB","#E0E8F2","#CFDDED","#BED1E8","#96A8BD","#172B43","#42566E","#195DB5","#096A76","#D6E6FC","#FFFFFF","#F0F5FB","#527BA7"}),
   new ThemePreset("Terminal","Phosphor rules · console typography","Consolas","Lucida Console","Pixel","scan",0,true,new[]{"#080E0A","#101B13","#0D1710","#1A2C21","#294634","#345940","#486B52","#E6F4E9","#ACCAB3","#8DE9A4","#B9E18C","#294A33","#0B160F","#080F0A","#6AA87C"}),
   new ThemePreset("Winter","Frosted midnight · quiet crystal lines","Segoe UI Light","Bahnschrift","Frosted","crystal",22,true,new[]{"#111F35","#1D314A","#182B43","#2B405B","#364E6A","#415E7C","#607E9F","#F5FAFF","#C3D7E9","#D2EEFF","#98DCEC","#334D65","#12253D","#243C56","#93BAD6"}),
   new ThemePreset("Hazy","Smoked lavender · soft editorial type","Georgia","Trebuchet MS","Airy","haze",28,true,new[]{"#252233","#343044","#2E2A3E","#403A50","#51465F","#62516F","#796983","#FBF4FC","#D6C6DB","#F1D1C3","#D8C3F7","#584252","#302632","#302B40","#B7A0C6"}),
   new ThemePreset("Aurora","Prismatic ribbons · split instrument","Bahnschrift","Segoe UI","Split","ribbon",12,true,new[]{"#0E232B","#17343C","#142D38","#21424C","#2D5260","#3A6370","#46727D","#F0FCFA","#B8DBDB","#A9F0DA","#E1BAF2","#28574F","#102C2C","#102A33","#90C6CE"}),
   new ThemePreset("Blueprint","Drafting grid · technical outlines","Consolas","Bahnschrift","Outline","grid",0,true,new[]{"#0E2743","#153657","#12304F","#1C4266","#2E5477","#3B6588","#577FA3","#F1F8FF","#BDD9F1","#BCEEFF","#A8DAD5","#28516E","#122D49","#102C49","#92C3E4"}),
   new ThemePreset("Sunset","Apricot glow · broad rounded plates","Trebuchet MS","Georgia","Ribbon","sunset",25,true,new[]{"#2A2021","#3B2C2A","#332725","#493630","#5B443B","#6A5146","#8D6D5B","#FFF6ED","#E1C7B8","#FFD0A1","#F8BFA9","#634535","#352321","#322423","#D4AA8C"}),
   new ThemePreset("Paper","Warm parchment · a ruled token ledger","Georgia","Consolas","Ledger","paper",2,false,new[]{"#F2EEE4","#FFFCF4","#F7F2E7","#E8E1D4","#DCD3C2","#CDC2AF","#A59B87","#302C25","#625746","#76502D","#3E6860","#E5D7BD","#FFF9EC","#F5EDDD","#8C7357"})
  };
  public static string Choice(string value,string[] choices){return choices.Contains(value)?value:choices[0];}
  public static ThemePreset Get(string name){return Presets[Array.IndexOf(Themes,Choice(name,Themes))];}
  public static void ChoosePreset(Settings settings,string name){settings.Theme=Choice(name,Themes);RestorePreset(settings);}
  public static void RestorePreset(Settings settings){var preset=Get(settings.Theme);settings.Font=preset.Font;settings.NumberFont=preset.NumberFont;settings.Counter=preset.Counter;}
  public static string FontName(string requested){return Choice(requested,Fonts);}
  public static FontFamily Font(string requested){string chosen=FontName(requested);return new FontFamily(InstalledFonts.Contains(chosen)?chosen:"Segoe UI");}
  public static SolidColorBrush Brush(string color){var brush=(SolidColorBrush)new BrushConverter().ConvertFromString(color);brush.Freeze();return brush;}
  static Color ColorOf(string color){return Brush(color).Color;}
  static Color Tint(Color color,double amount){return Color.FromRgb((byte)(color.R+(255-color.R)*amount),(byte)(color.G+(255-color.G)*amount),(byte)(color.B+(255-color.B)*amount));}
  public static Brush Material(ThemePreset preset){
   var main=ColorOf(preset.Colors[1]);var light=Tint(main,preset.Dark?.045:0);var brush=new LinearGradientBrush(light,main,new Point(0,0),new Point(1,1));
   if(preset.Material=="haze")brush.GradientStops.Insert(1,new GradientStop(ColorOf("#44384B"),.55));
   if(preset.Material=="sunset")brush.GradientStops.Insert(1,new GradientStop(ColorOf("#50352D"),.15));
   if(preset.Material=="ribbon")brush.GradientStops.Insert(1,new GradientStop(ColorOf("#27384D"),.5));
   brush.Freeze();return brush;
  }
  public static Brush Detail(ThemePreset preset){
   if(preset.Material=="haze"||preset.Material=="sunset"){
    var glow=ColorOf(preset.Colors[9]);glow.A=10;var clear=glow;clear.A=0;
    var mist=new RadialGradientBrush(glow,clear){Center=new Point(.82,.18),GradientOrigin=new Point(.82,.18),RadiusX=.85,RadiusY=.9};mist.Freeze();return mist;
   }
   var drawing=new DrawingGroup();var accent=ColorOf(preset.Colors[10]);accent.A=(byte)(preset.Dark?24:20);var pen=new Pen(new SolidColorBrush(accent),1);pen.Freeze();
   double size=preset.Material=="grid"?28:preset.Material=="paper"?32:preset.Material=="scan"?5:preset.Material=="brushed"?6:140;
   Action<double,double,double,double> line=(x,y,a,b)=>drawing.Children.Add(new GeometryDrawing(null,pen,new LineGeometry(new Point(x,y),new Point(a,b))));
   if(preset.Material=="grid"){line(0,0,size,0);line(0,0,0,size);}
   else if(preset.Material=="paper"||preset.Material=="scan"||preset.Material=="brushed")line(0,size-1,size,size-1);
   else if(preset.Material=="crystal"){line(98,15,98,75);line(72,30,124,60);line(124,30,72,60);line(98,26,86,18);line(98,26,110,18);line(78,33,78,46);line(118,55,118,68);}
   else if(preset.Material=="ice"){line(0,40,140,0);line(50,140,140,50);}
   else if(preset.Material=="ribbon"){line(0,135,140,15);line(0,115,140,-5);}
   else {line(15,138,15,131);line(29,138,29,134);line(43,138,43,131);}
   drawing.Freeze();var result=new DrawingBrush(drawing){TileMode=TileMode.Tile,Viewport=new Rect(0,0,size,size),ViewportUnits=BrushMappingMode.Absolute,Stretch=Stretch.None};result.Freeze();return result;
  }
  public static string Apply(Window window,string theme,string font){return Apply(window,new Settings{Theme=theme,Font=font});}
  public static string Apply(Window window,Settings settings){
   var preset=Get(settings.Theme);for(int i=0;i<Keys.Length;i++)window.Resources[Keys[i]]=Brush(preset.Colors[i]);
   window.Resources["SurfaceMaterial"]=Material(preset);window.Resources["DetailBrush"]=Detail(preset);window.Resources["PanelRadius"]=new CornerRadius(preset.Radius);window.Resources["ButtonRadius"]=new CornerRadius(Math.Min(preset.Radius,9));
   window.Resources["UiFont"]=Font(settings.Font);window.Resources["NumberFont"]=Font(settings.NumberFont);window.FontFamily=Font(settings.Font);Shell.TitleBar(window,preset.Dark);return preset.Name;
  }
  public static FrameworkElement Counter(Settings settings,string value,bool preview){
   var preset=Get(settings.Theme);string style=Choice(settings.Counter,Counters);double size=preview?29:62,height=preview?43:84;
   var row=new StackPanel{Orientation=Orientation.Horizontal};var accent=Brush(preset.Colors[9]);var stroke=Brush(preset.Colors[6]);var digit=Brush(preset.Colors[13]);
   Func<string,double,TextBlock> text=(content,fontSize)=>new TextBlock{Text=content,FontFamily=Font(settings.NumberFont),FontSize=fontSize,Foreground=accent,VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center};
   if(style=="Mechanical"||style=="Pixel"||style=="Frosted"||style=="Outline"||style=="Split")foreach(char ch in value){
    bool number=Char.IsDigit(ch);var label=text(ch.ToString(),size);label.FontWeight=style=="Frosted"?FontWeights.Light:style=="Pixel"?FontWeights.Bold:FontWeights.Normal;
    var content=new Grid();content.Children.Add(label);var tile=new Border{Child=content,Width=number?(preview?24:51):(preview?9:22),Height=height,Margin=new Thickness(0,0,preview?3:5,0),CornerRadius=new CornerRadius(style=="Frosted"?12:style=="Mechanical"?6:style=="Split"?3:0),BorderThickness=new Thickness(number?(style=="Outline"?1.5:1):0),BorderBrush=style=="Frosted"?accent:stroke,Background=number?digit:Brush(preset.Colors[1])};
    if(style=="Frosted"&&number)tile.Background=Material(preset);
    if(style=="Split"&&number)content.Children.Add(new Border{Height=1,Background=stroke,VerticalAlignment=VerticalAlignment.Center,IsHitTestVisible=false});
    if(style=="Pixel"&&number)tile.BorderThickness=new Thickness(0,0,0,3);
    if(style=="Outline")tile.Background=Brush(preset.Colors[1]);row.Children.Add(tile);
   }else {
    var label=text(value,style=="Clean"?size+8:style=="Airy"?size+3:size);label.FontWeight=style=="Clean"?FontWeights.SemiBold:FontWeights.Normal;if(style=="Serif")label.FontStyle=FontStyles.Italic;
    var plate=new Border{Child=label,Padding=new Thickness(preview?6:16,2,preview?6:16,preview?3:7),CornerRadius=new CornerRadius(style=="Ribbon"?23:style=="Airy"?18:0)};
    if(style=="Ledger"){plate.Background=digit;plate.BorderBrush=accent;plate.BorderThickness=new Thickness(0,1,0,2);}
    else if(style=="Ribbon"){plate.Background=digit;plate.BorderBrush=stroke;plate.BorderThickness=new Thickness(1);}
    else if(style=="Airy"){plate.Background=Material(preset);plate.Padding=new Thickness(preview?10:20,0,preview?10:20,4);}
    row.Children.Add(plate);
   }
   return row;
  }
 }
}
