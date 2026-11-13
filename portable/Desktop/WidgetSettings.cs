using Avalonia.Controls;
using PlayDo.Odometer;

namespace PlayDo.Desktop;
public partial class MainWindow {
 void BuildWidgetSettings(){
  var choice=Find<ComboBox>("WidgetOptionsChoice");choice.ItemsSource=new[]{"Mini","Compact"};choice.SelectedItem="Mini";
  void Populate(){
   var panel=Find<StackPanel>("WidgetOptionRows");panel.Children.Clear();
   var options=(choice.SelectedItem as string)=="Compact"?service.Settings.Compact:service.Settings.Mini;
   foreach(var pair in new[]{("ShowPrimary","5-hour usage bar"),("ShowWeekly","Weekly usage bar"),("ShowReset","Reset times"),("ShowBreakdown","Input and output"),("ShowModel","Most-used model"),("ShowStatus","Activity status")}){
    var property=typeof(WidgetOptions).GetProperty(pair.Item1)!;var box=new CheckBox{Content=pair.Item2,IsChecked=(bool)property.GetValue(options)!};
    box.IsCheckedChanged+=(_,_)=>{property.SetValue(options,box.IsChecked==true);service.Save();Render();};panel.Children.Add(box);
   }
  }
  choice.SelectionChanged+=(_,_)=>Populate();Populate();
 }
 void RenderWidget(Snapshot snapshot,Tokens total){
  string layout=service.Settings.Layout;
  if(layout=="Dashboard"||layout=="Tiny")return;
  var options=layout=="Mini"?service.Settings.Mini:service.Settings.Compact;
  bool quota=service.Settings.ShowAllowance&&service.Settings.Enabled.GetValueOrDefault("Codex");
  Find<TextBlock>("WidgetBreakdown").IsVisible=options.ShowBreakdown;
  Find<TextBlock>("WidgetModel").IsVisible=options.ShowModel;
  Find<TextBlock>("WidgetActivity").IsVisible=options.ShowStatus;Find<TextBlock>("WidgetActivity").Text=snapshot.Status;
  Find<StackPanel>("WidgetPrimary").IsVisible=quota&&options.ShowPrimary;
  Find<StackPanel>("WidgetWeekly").IsVisible=quota&&options.ShowWeekly;
  var top=snapshot.Models.OrderByDescending(m=>service.Settings.Scope=="Today"?m.Today.Total:service.Settings.Scope=="7 days"?m.Week.Total:m.Recorded.Total).FirstOrDefault();
  Find<TextBlock>("WidgetModel").Text=top==null?"No model usage reported":"Top model · "+top.Label;
  foreach(var pair in new[]{("WidgetPrimary",300L),("WidgetWeekly",10080L)}){
   var window=snapshot.Quota?.Windows.FirstOrDefault(x=>x.Minutes==pair.Item2);
   bool expired=window!=null&&Expired(window);string label=pair.Item2==300?"Codex 5-hour":"Codex weekly";
   Find<TextBlock>(pair.Item1+"Text").Text=label+" · "+(window==null?"not reported":expired?"awaiting update":window.Used.ToString("0.#")+"% used"+(snapshot.Quota!=null&&(DateTime.UtcNow-snapshot.Quota.At.ToUniversalTime()).TotalMinutes>15?" · older":""));
   var bar=Find<ProgressBar>(pair.Item1+"Bar");bar.Value=window==null||expired?0:window.Used;bar.Opacity=window==null||expired ? .4 : 1;
   string reset="Reset time unavailable";
   try{if(window?.Reset>0)reset=(expired?"Previous reset ":"Resets ")+DateTimeOffset.FromUnixTimeSeconds(window.Reset).LocalDateTime.ToString("ddd h:mm tt");}catch(ArgumentOutOfRangeException){}
   Find<TextBlock>(pair.Item1+"Reset").IsVisible=options.ShowReset;Find<TextBlock>(pair.Item1+"Reset").Text=reset;
   ToolTip.SetTip(bar,FormatAllowance(snapshot.Quota)+(snapshot.Quota==null?"":" · reported "+snapshot.Quota.At.ToLocalTime().ToString("g")));
  }
  double height=(layout=="Mini"?130:160)+(options.ShowBreakdown?24:0)+(options.ShowModel?22:0)+(options.ShowStatus?22:0)+(quota&&options.ShowPrimary?40+(options.ShowReset?20:0):0)+(quota&&options.ShowWeekly?40+(options.ShowReset?20:0):0);
  if(Math.Abs(Height-height)>1){Height=height;Place();}
 }
}
