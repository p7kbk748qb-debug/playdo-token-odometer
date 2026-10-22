using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
class BrandAssets {
 static Color C(string hex){return ColorTranslator.FromHtml(hex);}
 static void Round(Graphics g,Brush b,float x,float y,float w,float h,float r){using(var p=new GraphicsPath()){p.AddArc(x,y,r*2,r*2,180,90);p.AddArc(x+w-r*2,y,r*2,r*2,270,90);p.AddArc(x+w-r*2,y+h-r*2,r*2,r*2,0,90);p.AddArc(x,y+h-r*2,r*2,r*2,90,90);p.CloseFigure();g.FillPath(b,p);}}
 static Bitmap Icon(int size) {
  var bmp=new Bitmap(size,size,PixelFormat.Format32bppArgb);using(var g=Graphics.FromImage(bmp)){g.SmoothingMode=SmoothingMode.AntiAlias;g.ScaleTransform(size/512f,size/512f);using(var bg=new SolidBrush(C("#17273B")))Round(g,bg,12,12,488,488,110);
   using(var pen=new Pen(C("#76D8E5"),22)){pen.StartCap=LineCap.Round;pen.EndCap=LineCap.Round;g.DrawArc(pen,97,75,318,318,203,134);}
   using(var amber=new SolidBrush(C("#FFBD69")))Round(g,amber,88,222,336,180,28);
   using(var dark=new SolidBrush(C("#101B2B"))) {for(int i=0;i<3;i++)Round(g,dark,110+i*104,244,84,136,13);}
   using(var f=new Font("Consolas",76,FontStyle.Bold,GraphicsUnit.Pixel))using(var b=new SolidBrush(C("#FFBD69"))){var fmt=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center};g.DrawString("0",f,b,new RectangleF(110,244,84,136),fmt);g.DrawString("0",f,b,new RectangleF(214,244,84,136),fmt);g.DrawString("1",f,b,new RectangleF(318,244,84,136),fmt);}
  }return bmp;
 }
 static void Main(string[] args){string dir=args[0];Directory.CreateDirectory(dir);using(var icon=Icon(512))icon.Save(Path.Combine(dir,"app-icon.png"),ImageFormat.Png);
  int[] sizes={16,24,32,48,64,128,256};var png=new byte[sizes.Length][];for(int i=0;i<sizes.Length;i++)using(var b=Icon(sizes[i]))using(var ms=new MemoryStream()){b.Save(ms,ImageFormat.Png);png[i]=ms.ToArray();}
  using(var file=File.Create(Path.Combine(dir,"app.ico")))using(var w=new BinaryWriter(file)){w.Write((ushort)0);w.Write((ushort)1);w.Write((ushort)sizes.Length);int offset=6+16*sizes.Length;for(int i=0;i<sizes.Length;i++){w.Write((byte)(sizes[i]==256?0:sizes[i]));w.Write((byte)(sizes[i]==256?0:sizes[i]));w.Write((byte)0);w.Write((byte)0);w.Write((ushort)1);w.Write((ushort)32);w.Write(png[i].Length);w.Write(offset);offset+=png[i].Length;}foreach(var bytes in png)w.Write(bytes);}
  using(var cover=new Bitmap(1600,900))using(var g=Graphics.FromImage(cover)){g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(C("#101B2B"));using(var pen=new Pen(C("#23364E"),1)){for(int x=0;x<1600;x+=50)g.DrawLine(pen,x,0,x,900);for(int y=0;y<900;y+=50)g.DrawLine(pen,0,y,1600,y);}using(var panel=new SolidBrush(C("#17273B")))Round(g,panel,120,140,1360,620,44);
   using(var icon=Icon(320))g.DrawImage(icon,1050,290,320,320);
   using(var b=new SolidBrush(C("#FFBD69")))using(var f=new Font("Segoe UI",34,FontStyle.Bold))g.DrawString("PlayDo's",f,b,200,235);
   using(var b=new SolidBrush(C("#F1F5FB")))using(var f=new Font("Segoe UI",58,FontStyle.Bold)){g.DrawString("Token",f,b,195,305);g.DrawString("Odometer",f,b,195,400);}
   using(var b=new SolidBrush(C("#B8C6D9")))using(var f=new Font("Segoe UI",23))g.DrawString("Your Codex usage, in view.",f,b,201,555);
   using(var b=new SolidBrush(C("#76D8E5")))using(var f=new Font("Segoe UI",17))g.DrawString("Local records. Quiet in the background.",f,b,201,620);
   cover.Save(Path.Combine(dir,"cover.png"),ImageFormat.Png);
  }
 }
}
