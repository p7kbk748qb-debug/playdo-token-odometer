using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace PlayDo.Odometer {
 public static class NumberMotion {
  public static long At(long from,long to,double progress){
   if(progress<=0)return from;if(progress>=1)return to;
   double eased=1-Math.Pow(1-progress,3);
   return from+(long)(((decimal)to-from)*(decimal)eased);
  }
 }
 public sealed partial class Dashboard {
  DispatcherTimer numberTimer;long displayedNumber,targetNumber;bool numberKnown;
  string numberContext="";readonly System.Diagnostics.Stopwatch numberClock=new System.Diagnostics.Stopwatch();long motionFrom;
  void UpdateNumber(long n,bool has){
   string context=view+"|"+settings.Theme+"|"+settings.Counter+"|"+settings.NumberFont+"|"+scope+"|"+settings.Source;
   string key=context+"|"+(has?Count(n):"—");if(previous==key)return;previous=key;
   bool animate=has&&numberKnown&&context==numberContext&&n>targetNumber&&n>displayedNumber&&settings.AnimateCount&&!settings.ReduceMotion;
   StopNumberTimer();targetNumber=n;numberContext=context;
   if(animate){
    motionFrom=displayedNumber;numberClock.Restart();
    if(numberTimer==null){numberTimer=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(33)};numberTimer.Tick+=delegate{
     double progress=numberClock.Elapsed.TotalMilliseconds/300;
     displayedNumber=NumberMotion.At(motionFrom,targetNumber,progress);
     PaintNumber(displayedNumber,true,progress>=1);
     if(progress>=1)StopNumberTimer();
    };}
    numberTimer.Start();
   }else {displayedNumber=n;PaintNumber(n,has,true);}
   numberKnown=has;
  }
  void StopNumberTimer(){if(numberTimer!=null)numberTimer.Stop();}
  void SettleNumber(){StopNumberTimer();if(numberKnown){displayedNumber=targetNumber;PaintNumber(targetNumber,true,true);}}
  void PaintNumber(long n,bool has,bool final){
   string value=has?Count(n):"—";
   if(view=="Tiny"){Text("TinyNumber",value);AutomationProperties.SetName(Find<TextBlock>("TinyNumber"),has?Count(targetNumber)+" tokens":"No usage recorded yet");return;}
   var row=Find<StackPanel>(view=="Dashboard"?"Digits":"CompactDigits");row.Children.Clear();
   row.Children.Add(Appearance.Counter(settings,value,false));
   AutomationProperties.SetName(row,has?Count(targetNumber)+" tokens":"No usage recorded yet");
  }
 }
}
