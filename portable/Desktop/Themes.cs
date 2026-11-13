namespace PlayDo.Desktop;
public sealed record ThemeSpec(string Name,string Canvas,string Surface,string Text,string Muted,string Accent,string Signal,string Stroke,string Font,string Numbers,bool Light=false);
public static class Themes {
 public static readonly ThemeSpec[] All={
  new("Graphite","#171C23","#222A34","#F0F3F8","#A9B6C6","#FFBD69","#76D8E5","#475363","Inter","Inter"),
  new("Instrument","#101B2B","#17273B","#F1F5FB","#B8C6D9","#FFBD69","#76D8E5","#405773","Bahnschrift","Consolas"),
  new("Arctic","#EDF2F5","#FFFFFF","#172B3E","#435B6C","#175479","#176B71","#8AA3B5","Inter","Menlo",true),
  new("Terminal","#111A16","#17291E","#E5F4E9","#A1C4AC","#94E8A2","#6ECFC2","#3C6550","Consolas","Consolas"),
  new("Winter","#101D30","#1D3049","#EDF4FF","#BCCDE5","#B7D8FF","#80D9EC","#4A6685","Inter","Inter"),
  new("Hazy","#272431","#3D354B","#FFF0E8","#CCBDD4","#F1CCB7","#C6B3E8","#756781","Georgia","Inter"),
  new("Aurora","#102427","#18353B","#E3FBF4","#9FC7C2","#8DEBB9","#81CDEB","#416D72","Inter","Inter"),
  new("Blueprint","#102D4C","#173C65","#EBF5FF","#ADCBE7","#C0E0FF","#80D9EC","#507DA8","Inter","Consolas"),
  new("Sunset","#2C2025","#422D36","#FFF0E4","#D8B8B7","#FFD09A","#F4A6AB","#815861","Inter","Cambria"),
  new("Paper","#F6F1E7","#FFFCF4","#302C27","#685E51","#80511A","#426855","#B1A28D","Georgia","Georgia",true)
 };
}
