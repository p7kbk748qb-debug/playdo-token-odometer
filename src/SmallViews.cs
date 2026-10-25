using System;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PlayDo.Odometer {
 public sealed class SmallViewOptions {
  public bool ShowPrimary=true,ShowWeekly=true,ShowReset,ShowBreakdown,ShowModel,ShowStatus=true;
 }

 // Account allowance is independent of the selected token-count scope.
 public sealed class AllowanceDisplay {
  public string Text,Reset,Detail;public double Used;public bool Available;
  public static AllowanceDisplay Create(Quota quota,long minutes,DateTime now) {
   string label=minutes==300?"5-hour":"Weekly";
   var result=new AllowanceDisplay {Text=label+" · not reported",Reset="Reset time unavailable",Detail="Codex has not reported this allowance in local records."};
   var window=quota==null||quota.Windows==null?null:quota.Windows.FirstOrDefault(x=>x!=null&&x.Minutes==minutes);
   if(window==null)return result;
   DateTime reset=DateTime.MinValue;
   try{if(window.Reset>0)reset=new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).AddSeconds(window.Reset);}catch(ArgumentOutOfRangeException){}
   bool expired=reset!=DateTime.MinValue&&now>=reset;
   bool stale=(now-quota.At.ToUniversalTime()).TotalMinutes>15;
   bool valid=!double.IsNaN(window.Used)&&!double.IsInfinity(window.Used);
   result.Available=!expired&&valid;
   result.Used=result.Available?Math.Max(0,Math.Min(100,window.Used)):0;
   result.Text=label+" · "+(expired?"awaiting update":!valid?"unavailable":result.Used.ToString("0.#")+"% used"+(stale?" · older":""));
   if(reset!=DateTime.MinValue)result.Reset=(expired?"Previous reset ":"Resets ")+reset.ToLocalTime().ToString("ddd h:mm tt");
   result.Detail=result.Text+". Last reported "+quota.At.ToLocalTime().ToString("g")+". "+result.Reset+". Account allowance; separate from token totals.";
   return result;
  }
 }

 public sealed partial class Dashboard {
  Window smallSettings;
  SmallViewOptions SmallOptions {get{return view=="Mini"?settings.Mini:settings.Compact;}}
  void SmallVisibility(string name,bool show){Find<FrameworkElement>(name).Visibility=show?Visibility.Visible:Visibility.Collapsed;}
  void ApplySmallLayout() {
   if(view=="Dashboard")return;var p=SmallOptions;bool mini=view=="Mini";
   SmallVisibility("SmallPrimary",p.ShowPrimary);SmallVisibility("SmallWeekly",p.ShowWeekly);
   SmallVisibility("SmallPrimaryReset",p.ShowReset);SmallVisibility("SmallWeeklyReset",p.ShowReset);
   SmallVisibility("SmallBreakdown",p.ShowBreakdown);SmallVisibility("SmallModel",p.ShowModel);SmallVisibility("CompactStatus",p.ShowStatus);
   double extra=(p.ShowPrimary?40+(p.ShowReset?20:0):0)+(p.ShowWeekly?40+(p.ShowReset?20:0):0)+(p.ShowBreakdown?26:0)+(p.ShowModel?26:0);
   Window.MinHeight=(mini?164:220)+extra-(p.ShowStatus?0:18);
   Window.Height=(mini?174:260)+extra-(p.ShowStatus?0:18);
  }
  void RenderSmall(Snapshot snapshot,Tokens totals) {
   var p=SmallOptions;
   if(p.ShowBreakdown){Text("SmallBreakdown","In "+Short(totals.Input)+"   ·   Out "+Short(totals.Output));Find<TextBlock>("SmallBreakdown").ToolTip="Input "+Count(totals.Input)+" · Output "+Count(totals.Output)+" · "+scope;}
   if(p.ShowModel){var top=snapshot.Models.OrderByDescending(x=>Select(x).Total).FirstOrDefault(x=>Select(x).Total>0);string value=top==null?"Top model · no usage in "+scope.ToLowerInvariant():"Top model · "+(top.Label??top.Id)+" · "+Short(Select(top).Total);Text("SmallModel",value);Find<TextBlock>("SmallModel").ToolTip=value;}
   if(p.ShowPrimary)RenderSmallAllowance("SmallPrimary",AllowanceDisplay.Create(snapshot.Quota,300,DateTime.UtcNow));
   if(p.ShowWeekly)RenderSmallAllowance("SmallWeekly",AllowanceDisplay.Create(snapshot.Quota,10080,DateTime.UtcNow));
  }
  void RenderSmallAllowance(string prefix,AllowanceDisplay value) {
   Text(prefix+"Text",value.Text);Text(prefix+"Reset",value.Reset);var bar=Find<ProgressBar>(prefix+"Bar");bar.Value=value.Used;bar.Opacity=value.Available?1:.35;
   AutomationProperties.SetName(bar,value.Detail);Find<StackPanel>(prefix).ToolTip=value.Detail;
  }
  public void OpenSmallSettings() {
   if(smallSettings!=null){smallSettings.Activate();return;}
   var dialog=new Window{Title="Mini & Compact settings",Owner=Window,Width=470,Height=Math.Min(575,SystemParameters.WorkArea.Height),MinWidth=400,MinHeight=350,WindowStartupLocation=WindowStartupLocation.CenterOwner,ShowInTaskbar=false,Resources=Window.Resources,Foreground=(Brush)Window.Resources["Text"],FontFamily=Appearance.Font(settings.Font),FontSize=13,Topmost=Window.Topmost};
   dialog.SetResourceReference(Window.BackgroundProperty,"Surface");smallSettings=dialog;
   var body=new StackPanel{Margin=new Thickness(24)};
   body.Children.Add(Label("Small window settings",23,"Text"));
   var help=Label("Choose what each window shows. Changes save as you go.",12,"Muted");help.TextWrapping=TextWrapping.Wrap;help.Margin=new Thickness(0,6,0,18);body.Children.Add(help);
   var selector=new ComboBox{ItemsSource=new[]{"Mini","Compact"},SelectedItem=view=="Mini"?"Mini":"Compact",Margin=new Thickness(0,0,0,16)};AutomationProperties.SetName(selector,"Window to configure");body.Children.Add(selector);
   var options=new StackPanel();body.Children.Add(options);
   var note=Label("Allowance bars show the last Codex report. Hover for its time. Token scope does not change account allowance.",12,"Muted");note.TextWrapping=TextWrapping.Wrap;note.Margin=new Thickness(0,10,0,18);body.Children.Add(note);
   var buttons=new WrapPanel();body.Children.Add(buttons);
   var preview=new Button{Content="Open this view",Margin=new Thickness(0,0,8,8)};buttons.Children.Add(preview);
   var style=new Button{Content="Appearance",Margin=new Thickness(0,0,8,8)};buttons.Children.Add(style);
   var done=new Button{Content="Done",IsDefault=true,IsCancel=true,Margin=new Thickness(0,0,0,8)};buttons.Children.Add(done);
   TextBlock saveNote=Label("",12,"Muted");saveNote.TextWrapping=TextWrapping.Wrap;body.Children.Add(saveNote);
   Action populate=delegate {
    options.Children.Clear();var prefs=Convert.ToString(selector.SelectedItem)=="Mini"?settings.Mini:settings.Compact;
    Action<string,bool,Action<bool>> toggle=delegate(string title,bool current,Action<bool> change){var box=new CheckBox{Content=title,IsChecked=current,Margin=new Thickness(0,0,0,12),MinHeight=24};AutomationProperties.SetName(box,Convert.ToString(selector.SelectedItem)+" "+title);box.Click+=delegate{change(box.IsChecked==true);try{settings.Save(settingsPath);saveNote.Text="Saved";}catch{saveNote.Text="Could not save. Check app folder permissions.";}ApplySmallLayout();Render();};options.Children.Add(box);};
    toggle("5-hour usage bar",prefs.ShowPrimary,delegate(bool v){prefs.ShowPrimary=v;});
    toggle("Weekly usage bar",prefs.ShowWeekly,delegate(bool v){prefs.ShowWeekly=v;});
    toggle("Reset times below bars",prefs.ShowReset,delegate(bool v){prefs.ShowReset=v;});
    toggle("Input and output totals",prefs.ShowBreakdown,delegate(bool v){prefs.ShowBreakdown=v;});
    toggle("Most-used model",prefs.ShowModel,delegate(bool v){prefs.ShowModel=v;});
    toggle("Activity status",prefs.ShowStatus,delegate(bool v){prefs.ShowStatus=v;});
   };
   selector.SelectionChanged+=delegate{populate();};populate();
   preview.Click+=delegate{SetView(Convert.ToString(selector.SelectedItem));};
   bool appearance=false;style.Click+=delegate{appearance=true;dialog.Close();};done.Click+=delegate{dialog.Close();};
   dialog.Content=new ScrollViewer{Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};
   dialog.SourceInitialized+=delegate{Shell.TitleBar(dialog,Appearance.Get(settings.Theme).Dark);};
   dialog.ContentRendered+=delegate{selector.Focus();};dialog.Closed+=delegate{smallSettings=null;};dialog.ShowDialog();if(appearance)OpenAppearance();
  }
 }
}
