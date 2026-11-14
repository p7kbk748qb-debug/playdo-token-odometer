using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using PlayDo.Odometer;

namespace PlayDo.Desktop;
public partial class MainWindow {
 bool allModels;string lastQuota="";
 readonly DispatcherTimer effects=new(){Interval=TimeSpan.FromMilliseconds(16)};
 readonly System.Diagnostics.Stopwatch effectClock=new();
 readonly List<ScaleTransform> chartScales=new();bool fading,chartAnimating;
 IBrush ThemeBrush(string key)=>Resources.TryGetResource(key,ActualThemeVariant,out var brush)?(IBrush)brush!:Foreground!;
 IBrush SurfaceBrush()=>ThemeBrush("Surface");
 void ArrangeAnalytics(){
  if(!IsInitialized)return;
  bool wide=Bounds.Width>=1000&&service.Settings.Enabled.GetValueOrDefault("Codex");
  foreach(string name in new[]{"Overview","Providers","Appearance","Settings"})Find<StackPanel>(name+"Page").MaxWidth=Math.Max(0,Bounds.Width-244);
  Find<Grid>("AnalyticsGrid").ColumnDefinitions=new ColumnDefinitions(wide?"1.4*,20,*":"*");
  var quota=Find<Border>("AllowancePanel");Grid.SetColumn(quota,wide?2:0);Grid.SetRow(quota,wide?0:1);quota.Margin=wide?default:new Thickness(0,20,0,0);
  Find<UniformGrid>("ProviderSummary").Columns=Bounds.Width>=940?4:2;
 }
 void RenderDashboardRows(Snapshot snapshot,Tokens total){
  var providers=Find<UniformGrid>("ProviderSummary");providers.Children.Clear();
  foreach(var provider in service.Providers){
   bool on=service.Settings.Enabled.GetValueOrDefault(provider.Name);var data=provider.Snapshot;var value=Convenience.Total(data,service.Settings.Scope);
   var text=new StackPanel{Spacing=6};text.Children.Add(Label(provider.Name,12,"#A9B6C6"));
   text.Children.Add(new TextBlock{Text=on?value.Total.ToString("N0"):"Off",FontSize=18,FontWeight=FontWeight.Medium,Foreground=on?Foreground:ThemeBrush("Muted"),TextTrimming=TextTrimming.CharacterEllipsis});
   text.Children.Add(Label(on?(data.Indexing?"Importing":data.Recorded.Total>0?"tokens":"Awaiting reports"):"Not counted",10,"#A9B6C6"));
   var button=new Button{Content=text,HorizontalAlignment=HorizontalAlignment.Stretch,HorizontalContentAlignment=HorizontalAlignment.Left,Padding=new Thickness(12),Margin=new Thickness(0,0,8,8),Background=SurfaceBrush(),BorderThickness=new Thickness(0)};
   ToolTip.SetTip(button,provider.Name+" · "+(on?value.Total.ToString("N0")+" tokens · "+data.Status:"Not selected")+". Open provider settings.");
   button.Click+=(_,_)=>Navigate("Providers");providers.Children.Add(button);
  }
  var models=Find<StackPanel>("ModelRows");models.Children.Clear();
  var ranked=snapshot.Models.Select(m=>(model:m,value:service.Settings.Scope=="Today"?m.Today:service.Settings.Scope=="7 days"?m.Week:m.Recorded)).Where(x=>x.value.Total>0).OrderByDescending(x=>x.value.Total).ToArray();
  Find<TextBlock>("ModelCount").Text=ranked.Length+" reported";
  Find<Button>("MoreModels").IsVisible=ranked.Length>5;Find<Button>("MoreModels").Content=allModels?"Show fewer models":"Show all "+ranked.Length+" models";
  foreach(var entry in ranked.Take(allModels?ranked.Length:5)){
   double share=total.Total>0?100.0*entry.value.Total/total.Total:0;
   var row=new Grid{ColumnDefinitions=new ColumnDefinitions("*,16,Auto"),RowDefinitions=new RowDefinitions("Auto,Auto")};
   var name=Label(entry.model.Label,12);name.TextTrimming=TextTrimming.CharacterEllipsis;ToolTip.SetTip(name,entry.model.Label);row.Children.Add(name);
   var amount=Label(entry.value.Total.ToString("N0"),13);Grid.SetColumn(amount,2);row.Children.Add(amount);
   var bar=new ProgressBar{Minimum=0,Maximum=100,Value=share,Height=3,Foreground=ThemeBrush("Signal"),Margin=new Thickness(0,9,0,0)};Grid.SetRow(bar,1);row.Children.Add(bar);
   var ratio=Label(share>0&&share<.1?"<0.1%":share.ToString("0.#")+"%",11,"#A9B6C6");Grid.SetColumn(ratio,2);Grid.SetRow(ratio,1);ratio.Margin=new Thickness(0,5,0,0);ratio.HorizontalAlignment=HorizontalAlignment.Right;row.Children.Add(ratio);
   ToolTip.SetTip(row,"Input "+entry.value.Input.ToString("N0")+" · Output "+entry.value.Output.ToString("N0")+" · Cached "+entry.value.Cached.ToString("N0")+" (included in input)");models.Children.Add(row);
  }
  if(ranked.Length==0)models.Children.Add(Label("No model reports in this period. Try Recorded or check Providers.",12,"#A9B6C6"));
  RenderChart(snapshot);ArrangeAnalytics();
 }
 void RenderChart(Snapshot snapshot){
  var chart=Find<Grid>("DayChart");chart.Children.Clear();chart.ColumnDefinitions=new ColumnDefinitions("*,*,*,*,*,*,*");chartScales.Clear();
  var days=Enumerable.Range(0,7).Select(i=>{var date=DateTime.Today.AddDays(i-6);snapshot.Days.TryGetValue(date.ToString("yyyy-MM-dd"),out var tokens);return(date,tokens);}).ToArray();
  long max=days.Max(x=>x.tokens?.Total??0),sum=days.Sum(x=>x.tokens?.Total??0);int active=days.Count(x=>(x.tokens?.Total??0)>0);
  Find<TextBlock>("ChartTotal").Text=Short(sum)+" tokens";
  Find<TextBlock>("ChartNote").Text=active==0?"No local reports in the last seven days.":active+" active "+(active==1?"day":"days")+" · daily average "+Short(sum/7)+" tokens";
  for(int i=0;i<days.Length;i++){
   var day=days[i];long amount=day.tokens?.Total??0;var column=new Grid{RowDefinitions=new RowDefinitions("Auto,*,Auto"),Margin=new Thickness(3,0)};
   var value=Label(amount>0?Short(amount):"—",10,"#A9B6C6");value.HorizontalAlignment=HorizontalAlignment.Center;column.Children.Add(value);
   var scale=new ScaleTransform(1,1);chartScales.Add(scale);
   var track=new Border{Background=ThemeBrush("Stroke"),Opacity=.22,CornerRadius=new CornerRadius(4),Margin=new Thickness(9,8,9,8)};Grid.SetRow(track,1);column.Children.Add(track);
   var bar=new Border{Height=amount>0?Math.Max(3,116.0*amount/Math.Max(1,max)):0,Background=ThemeBrush(i==6?"Accent":"Signal"),CornerRadius=new CornerRadius(4),VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(9,8,9,8),RenderTransform=scale,RenderTransformOrigin=new RelativePoint(.5,1,RelativeUnit.Relative)};Grid.SetRow(bar,1);column.Children.Add(bar);
   var label=Label(i==6?"Today":day.date.ToString("ddd"),10,"#A9B6C6");label.HorizontalAlignment=HorizontalAlignment.Center;Grid.SetRow(label,2);column.Children.Add(label);
   ToolTip.SetTip(column,day.date.ToString("D")+" · "+(day.tokens==null?"No local report":amount.ToString("N0")+" observed tokens"));
   Avalonia.Automation.AutomationProperties.SetName(column,day.date.ToString("D")+", "+amount.ToString("N0")+" observed tokens");
   Grid.SetColumn(column,i);chart.Children.Add(column);
  }
  StartEffects(false);
 }
 void RenderAllowance(Quota? quota){
  bool old=quota!=null&&(DateTime.UtcNow-quota.At.ToUniversalTime()).TotalMinutes>15;
  string key=quota==null?"none":quota.At.Ticks+"|"+DateTime.Now.ToString("yyyyMMddHHmm")+"|"+string.Join(";",quota.Windows.Select(q=>$"{q.Minutes}:{q.Used}:{q.Reset}:{Expired(q)}"));
  if(key==lastQuota)return;lastQuota=key;
  var rows=Find<StackPanel>("AllowanceRows");rows.Children.Clear();
  var windows=quota?.Windows.Count>0?quota.Windows:new List<QuotaWindow>{new(){Minutes=300},new(){Minutes=10080}};
  foreach(var q in windows){
   bool known=quota?.Windows.Contains(q)==true,expired=known&&Expired(q);double used=Math.Clamp(q.Used,0,100);
   var row=new StackPanel{Spacing=6};var title=new Grid{ColumnDefinitions=new ColumnDefinitions("*,Auto")};title.Children.Add(Label(q.Minutes==300?"5-hour window":q.Minutes==10080?"Weekly window":q.Minutes+" min window",12));
   var value=Label(!known?"Not reported":expired?"Awaiting update":used.ToString("0.#")+"% used",12,"#A9B6C6");Grid.SetColumn(value,1);title.Children.Add(value);row.Children.Add(title);
   row.Children.Add(new ProgressBar{Maximum=100,Value=known&&!expired?used:0,Height=5,Foreground=ThemeBrush("Signal"),Opacity=(old||expired) ? .5 : 1});
   string reset="Reset time unavailable";try{if(q.Reset>0)reset=(expired?"Previous reset ":"Resets ")+DateTimeOffset.FromUnixTimeSeconds(q.Reset).LocalDateTime.ToString("ddd h:mm tt");}catch(ArgumentOutOfRangeException){}
   row.Children.Add(Label(reset,11,"#A9B6C6"));rows.Children.Add(row);
  }
  Find<TextBlock>("AllowanceText").Text=quota==null?"Not reported locally yet":(old?"Older snapshot · ":"Reported · ")+quota.At.ToLocalTime().ToString("MMM d, h:mm tt");
 }
 void StartEffects(bool fade){
  StopEffects();
  if(!service.Settings.Effects||service.Settings.ReduceMotion||!IsVisible||service.Settings.Layout!="Dashboard"){StopEffects();return;}
  fading=fade;chartAnimating=page=="Overview";effectClock.Restart();if(fade)Find<StackPanel>("PageContent").Opacity=.88;if(chartAnimating)foreach(var scale in chartScales)scale.ScaleY=.08;effects.Start();
 }
 void StepEffects(){
  double p=Math.Clamp(effectClock.Elapsed.TotalMilliseconds/220,0,1),eased=1-Math.Pow(1-p,3);
  if(fading)Find<StackPanel>("PageContent").Opacity=.88+.12*eased;if(chartAnimating)foreach(var scale in chartScales)scale.ScaleY=.08+.92*eased;
  if(p>=1)StopEffects();
 }
 void StopEffects(){effects.Stop();Find<StackPanel>("PageContent").Opacity=1;foreach(var scale in chartScales)scale.ScaleY=1;}
}
