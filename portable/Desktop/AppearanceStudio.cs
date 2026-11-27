using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace PlayDo.Desktop;

public partial class MainWindow {
 const string ThemeInk="Theme default";
 static readonly Dictionary<string,(string Dark,string Light)> inks=new(){
  ["Amber"]=("#FFCA79","#774900"),["Cyan"]=("#79DFED","#06596B"),["Mint"]=("#8DEBB9","#186548"),
  ["Violet"]=("#D5BCFF","#64418B"),["Rose"]=("#FFB3CA","#873555")
 };
 readonly Dictionary<string,TextBlock> galleryMarkers=new();

 static string EffectiveInk(ThemeSpec theme,string? choice)=>
  choice!=null && inks.TryGetValue(choice,out var ink)?(theme.Light?ink.Light:ink.Dark):theme.Accent;

 void ApplyFonts(){
  FontFamily=TypefaceFamily(ResolveFont(service.Settings.Font));
  foreach(string name in new[]{"Total","WidgetNumber","NumberPreview"})
   Find<TextBlock>(name).FontFamily=TypefaceFamily(ResolveFont(service.Settings.NumberFont));
  Find<TextBlock>("FontNote").Text=$"{ResolveFont(service.Settings.Font)} interface  ·  {ResolveFont(service.Settings.NumberFont)} numerals";
 }

 void BuildThemes(){
  var gallery=Find<UniformGrid>("ThemeGallery");
  foreach(var theme in Themes.All){
   var body=new StackPanel{Spacing=6};
   var title=new TextBlock{Text=theme.Name,FontFamily=TypefaceFamily(ResolveFont(theme.Font)),FontSize=15,FontWeight=FontWeight.SemiBold,Foreground=Brush.Parse(theme.Text)};
   var caption=new Grid{ColumnDefinitions=new ColumnDefinitions("*,Auto")};caption.Children.Add(title);
   var marker=new TextBlock{Text="✓",Foreground=Brush.Parse(theme.Text),IsVisible=false};
   Grid.SetColumn(marker,1);caption.Children.Add(marker);body.Children.Add(caption);galleryMarkers.Add(theme.Name,marker);
   body.Children.Add(new TextBlock{Text="12,345,678",FontSize=22,FontFamily=TypefaceFamily(ResolveFont(theme.Numbers)),Foreground=Brush.Parse(theme.Accent)});
   var colors=new StackPanel{Orientation=Orientation.Horizontal,Spacing=3};
   foreach(string color in new[]{theme.Accent,theme.Signal,theme.Muted,theme.Stroke})
    colors.Children.Add(new Border{Background=Brush.Parse(color),Width=21,Height=4,CornerRadius=new CornerRadius(2)});
   body.Children.Add(colors);
   // Keep each specimen on its own palette when Fluent paints pointer-over and focus states.
   var face=new MeterFace{Opacity=.70};
   face.SetDetail(PresetDetail(theme.Name),Color.Parse(theme.Signal));
   body.Margin=new Thickness(12);
   var specimen=new Grid();specimen.Children.Add(face);specimen.Children.Add(body);
   var tile=new Border{Background=Brush.Parse(theme.Surface),Child=specimen,ClipToBounds=true,CornerRadius=new CornerRadius(6)};
   var button=new Button{Tag=theme.Name,Content=tile,Padding=new Thickness(2),Margin=new Thickness(0,0,8,8),
    Background=Brush.Parse(theme.Surface),HorizontalAlignment=HorizontalAlignment.Stretch,HorizontalContentAlignment=HorizontalAlignment.Stretch};
   AutomationProperties.SetName(button,theme.Name+" preset");
   button.Click+=(_,_)=>ApplyLookValues(new AppearanceLook(theme.Name,theme.Name,ResolveFont(theme.Font),ResolveFont(theme.Numbers),null));
   gallery.Children.Add(button);
  }
  var inkBox=Find<ComboBox>("NumberInk");inkBox.ItemsSource=new[]{ThemeInk}.Concat(inks.Keys).ToArray();SyncInk();
  inkBox.SelectionChanged+=(_,_)=>{
   if(initializing || inkBox.SelectedItem is not string choice)return;
   service.Settings.NumberInk=choice==ThemeInk?null:choice;ApplyTheme();service.Save();
  };
  BuildFaceOptions();
  Find<TextBox>("LookName").TextChanging+=(_,_)=>UpdateSaveButton();
  Find<Button>("SaveLook").Click+=(_,_)=>SaveCurrentLook();
  Find<Button>("ApplyLook").Click+=(_,_)=>{
   if(SelectedLook() is not {} look)return;
   LookFeedback(ApplyLookValues(look)?$"Applied {look.Name}.":$"Applied {look.Name} for this session. {service.Note}");
  };
  Find<Button>("RemoveLook").Click+=(_,_)=>{
   if(SelectedLook() is not {} look)return;
   var looks=service.Settings.SavedLooks!;
   int index=looks.IndexOf(look);
   looks.RemoveAt(index);
   if(!service.Save()){
    looks.Insert(index,look);
    LookFeedback($"Could not remove {look.Name}. {service.Note}");
    return;
   }
   RefreshSavedLooks();UpdateSaveButton();
   LookFeedback($"Removed {look.Name}.");
  };
  RefreshSavedLooks();UpdateSaveButton();
 }

 bool? studioWide;
 void ArrangeStudio(){
  bool wide=Bounds.Width>930;
  if(studioWide==wide)return;
  studioWide=wide;
  foreach(var pair in new[]{("StudioEditor","StudioControls"),("SaveLookRow","SaveLookForm")}){
   var grid=Find<Grid>(pair.Item1);var second=Find<Grid>(pair.Item2);
   grid.ColumnDefinitions=new ColumnDefinitions(wide?(pair.Item1=="StudioEditor"?"*,16,*":"Auto,16,*"):"*");
   grid.RowDefinitions=new RowDefinitions(wide?"Auto":pair.Item1=="StudioEditor"?"Auto,16,Auto":"Auto,8,Auto");
   Grid.SetColumn(second,wide?2:0);Grid.SetRow(second,wide?0:2);
  }
 }

 void LookFeedback(string message){
  var status=Find<TextBlock>("LookStatus");
  status.Text=message;status.IsVisible=message.Length>0;
 }

 void UpdateGallery(ThemeSpec theme){
  foreach(var tile in Find<UniformGrid>("ThemeGallery").Children.OfType<Button>()){
   bool active=Equals(tile.Tag,theme.Name);
   galleryMarkers[(string)tile.Tag!].IsVisible=active;
   tile.BorderBrush=Brush.Parse(active?theme.Accent:theme.Stroke);tile.BorderThickness=new Thickness(active?2:1);
   AutomationProperties.SetHelpText(tile,active?"Current palette":"Apply this palette and font pair");
  }
 }

 void SyncInk(){
  string? ink=service.Settings.NumberInk;
  Find<ComboBox>("NumberInk").SelectedItem=ink!=null && inks.ContainsKey(ink)?ink:ThemeInk;
 }

 bool ApplyLookValues(AppearanceLook look){
  var prefs=service.Settings;
  prefs.Theme=look.Theme;prefs.Font=look.Font;prefs.NumberFont=look.NumberFont;prefs.NumberInk=look.NumberInk;prefs.FaceDetail=look.FaceDetail;
  bool previous=initializing;initializing=true;
  try{SyncFonts();SyncInk();SyncFaceOptions();}finally{initializing=previous;}
  ApplyTheme();return service.Save();
 }

 AppearanceLook? SelectedLook()=>service.Settings.SavedLooks?.FirstOrDefault(x=>x!=null && x.Name==Find<ComboBox>("SavedLooks").SelectedItem as string);

 void UpdateSaveButton(){
  var name=Find<TextBox>("LookName").Text?.Trim();
  bool duplicate=service.Settings.SavedLooks?.Any(x=>string.Equals(x?.Name,name,StringComparison.OrdinalIgnoreCase))==true;
  Find<Button>("SaveLook").IsEnabled=!string.IsNullOrEmpty(name) && !duplicate;
  LookFeedback(duplicate?"Choose a different name.":string.Empty);
 }

 void SaveCurrentLook(){
  string? name=Find<TextBox>("LookName").Text?.Trim();
  if(string.IsNullOrEmpty(name) || !Find<Button>("SaveLook").IsEnabled)return;
  var prefs=service.Settings;
  var previous=prefs.SavedLooks;
  var updated=previous==null?new List<AppearanceLook>():new List<AppearanceLook>(previous);
  updated.Add(new(name,prefs.Theme,prefs.Font,prefs.NumberFont,prefs.NumberInk,prefs.FaceDetail));
  prefs.SavedLooks=updated;
  if(!service.Save()){
   prefs.SavedLooks=previous;
   LookFeedback($"Could not save {name}. {service.Note}");
   return;
  }
  RefreshSavedLooks(name);Find<TextBox>("LookName").Text="";
  LookFeedback($"Saved {name}.");
 }

 void RefreshSavedLooks(string? select=null){
  var box=Find<ComboBox>("SavedLooks");
  var names=service.Settings.SavedLooks?.Where(x=>!string.IsNullOrWhiteSpace(x?.Name)).Select(x=>x.Name).ToArray()??Array.Empty<string>();
  box.ItemsSource=names;box.SelectedItem=select??names.FirstOrDefault();
  Find<Grid>("LookActions").IsVisible=names.Length>0;
 }
}
