using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PlayDo.Odometer {
 // Export only the already aggregated snapshot. Never include session IDs or paths.
 public static class Convenience {
  static string Scope(string scope){return scope=="7 days"||scope=="Recorded"?scope:"Today";}
  static Tokens Select(Tokens today,Tokens week,Tokens recorded,string scope){return (scope=="Recorded"?recorded:scope=="7 days"?week:today)??new Tokens();}
  static Tokens ModelTotal(BreakdownView model,string scope){return Select(model.Today,model.Week,model.Recorded,scope);}
  static string Label(BreakdownView model){return String.IsNullOrWhiteSpace(model.Label)?"Unknown model":model.Label;}

  public static Tokens Total(Snapshot snapshot,string scope){
   if(snapshot==null)return new Tokens();
   return Select(snapshot.Today,snapshot.Week,snapshot.Recorded,Scope(scope)).Copy();
  }
  public static string CountText(Snapshot snapshot,string scope){return Total(snapshot,scope).Total.ToString("N0",CultureInfo.CurrentCulture);}

  static string Cell(string text){
   text=text??"";int start=0;bool leadingControl=false;
   while(start<text.Length&&Char.IsWhiteSpace(text[start])){if(text[start]=='\t'||text[start]=='\r'||text[start]=='\n')leadingControl=true;start++;}
   if(leadingControl||(start<text.Length&&"=+-@".IndexOf(text[start])>=0))text="'"+text;
   return text.IndexOfAny(new[]{',','"','\r','\n'})>=0?"\""+text.Replace("\"","\"\"")+"\"":text;
  }
  static void Row(StringBuilder csv,string scope,string label,Tokens tokens){
   csv.Append(Cell(scope)).Append(',').Append(Cell(label)).Append(',')
    .Append(tokens.Input.ToString(CultureInfo.InvariantCulture)).Append(',')
    .Append(tokens.Cached.ToString(CultureInfo.InvariantCulture)).Append(',')
    .Append(tokens.Output.ToString(CultureInfo.InvariantCulture)).Append(',')
    .Append(tokens.Total.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
  }
  public static string Csv(Snapshot snapshot,string scope){
   scope=Scope(scope);var csv=new StringBuilder("Scope,Model,Input,Cached input,Output,Total\r\n");
   Row(csv,scope,"All models",Total(snapshot,scope));
   if(snapshot!=null&&snapshot.Models!=null){
    foreach(var model in snapshot.Models.Where(m=>m!=null&&ModelTotal(m,scope).Total>0)
     .OrderByDescending(m=>ModelTotal(m,scope).Total)
     .ThenBy(m=>Label(m),StringComparer.OrdinalIgnoreCase).ThenBy(m=>Label(m),StringComparer.Ordinal))
     Row(csv,scope,Label(model),ModelTotal(model,scope));
   }
   return csv.ToString();
  }
 }
}
