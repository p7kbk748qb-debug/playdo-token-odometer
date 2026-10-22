using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using PlayDo.Odometer;

public static class ConvenienceTests {
 static int passed,failed;
 static void Check(string name,Action action){try{action();passed++;Console.WriteLine("PASS "+name);}catch(Exception e){failed++;Console.WriteLine("FAIL "+name+": "+e.Message);}}
 static void Eq<T>(T actual,T expected){if(!Object.Equals(actual,expected))throw new Exception("expected "+expected+", got "+actual);}
 static void True(bool value,string reason){if(!value)throw new Exception(reason);}
 static Tokens T(long input,long cached,long output){return new Tokens {Input=input,Cached=cached,Output=output};}
 static Snapshot Fixture(){return new Snapshot {Today=T(30,20,4),Week=T(300,200,40),Recorded=T(3000,2000,400),Models=new List<BreakdownView>{
  new BreakdownView {Id="private-id-alpha",Label="Alpha",Today=T(10,8,1),Week=T(100,80,10),Recorded=T(1000,800,100)},
  new BreakdownView {Id="private-id-beta",Label="Beta",Today=T(15,10,2),Week=T(150,100,20),Recorded=T(1500,1000,200)},
  new BreakdownView {Id="unknown",Label="Unknown model",Today=T(5,2,1),Week=T(50,20,10),Recorded=T(500,200,100)}
 }};}
 static List<string[]> Rows(string csv){
  var rows=new List<string[]>();var cells=new List<string>();var field=new System.Text.StringBuilder();bool quoted=false;
  for(int i=0;i<csv.Length;i++){char c=csv[i];if(c=='"'){if(quoted&&i+1<csv.Length&&csv[i+1]=='"'){field.Append('"');i++;}else quoted=!quoted;}else if(c==','&&!quoted){cells.Add(field.ToString());field.Length=0;}else if(c=='\r'&&!quoted&&i+1<csv.Length&&csv[i+1]=='\n'){cells.Add(field.ToString());field.Length=0;rows.Add(cells.ToArray());cells.Clear();i++;}else field.Append(c);}
  if(quoted)throw new Exception("unterminated CSV quote");if(field.Length>0||cells.Count>0){cells.Add(field.ToString());rows.Add(cells.ToArray());}return rows;
 }
 static void Row(string[] actual,params string[] expected){Eq(actual.Length,expected.Length);for(int i=0;i<actual.Length;i++)Eq(actual[i],expected[i]);}
 public static int Main(){
  Check("scope totals select Today, 7 days and Recorded",()=>{var s=Fixture();Eq(Convenience.Total(s,"Today").Total,34L);Eq(Convenience.Total(s,"7 days").Total,340L);Eq(Convenience.Total(s,"Recorded").Total,3400L);});
  Check("invalid scopes fall back to Today and copied totals cannot alter snapshot",()=>{var s=Fixture();var total=Convenience.Total(s,"=cmd");Eq(total.Total,34L);total.Input=0;Eq(s.Today.Input,30L);Eq(Convenience.Total(s,null).Total,34L);Eq(Convenience.Total(s,"recorded").Total,34L);});
  Check("copy text honors current culture without changing 64 bit value",()=>{var before=Thread.CurrentThread.CurrentCulture;try{Thread.CurrentThread.CurrentCulture=CultureInfo.GetCultureInfo("de-DE");var s=new Snapshot {Today=T(4572891094L,0,5)};Eq(Convenience.CountText(s,"Today"),"4.572.891.099");}finally{Thread.CurrentThread.CurrentCulture=before;}});
  Check("CSV exports authoritative total and cached input remains a subset",()=>{var rows=Rows(Convenience.Csv(Fixture(),"Recorded"));Row(rows[0],"Scope","Model","Input","Cached input","Output","Total");Row(rows[1],"Recorded","All models","3000","2000","400","3400");Eq(rows.Count,5);});
  Check("CSV model rows are sorted by selected scope with unknown retained",()=>{var s=Fixture();s.Models[0].Today=T(100,0,1);s.Models[0].Recorded=T(1,0,1);var rows=Rows(Convenience.Csv(s,"Today"));Eq(rows[2][1],"Alpha");Eq(rows[3][1],"Beta");Eq(rows[4][1],"Unknown model");rows=Rows(Convenience.Csv(s,"Recorded"));Eq(rows[2][1],"Beta");Eq(rows[3][1],"Unknown model");Eq(rows[4][1],"Alpha");});
  Check("CSV skips models with zero usage only in requested scope",()=>{var s=Fixture();s.Models.Add(new BreakdownView {Label="Yesterday only",Week=T(50,30,5),Recorded=T(50,30,5)});Eq(Rows(Convenience.Csv(s,"Today")).Count,5);Eq(Rows(Convenience.Csv(s,"7 days")).Count,6);});
  Check("CSV uses invariant exact integer fields across cultures",()=>{var before=Thread.CurrentThread.CurrentCulture;try{Thread.CurrentThread.CurrentCulture=CultureInfo.GetCultureInfo("fr-FR");var s=new Snapshot {Recorded=T(4572891094L,4200000000L,15844929L)};var rows=Rows(Convenience.Csv(s,"Recorded"));Row(rows[1],"Recorded","All models","4572891094","4200000000","15844929","4588736023");}finally{Thread.CurrentThread.CurrentCulture=before;}});
  Check("CSV escapes commas quotes and embedded newlines with CRLF records",()=>{var s=Fixture();s.Models[1].Label="A, \"quoted\"\r\nmodel";string csv=Convenience.Csv(s,"Today");var rows=Rows(csv);Eq(rows[2][1],s.Models[1].Label);True(csv.Contains("\"A, \"\"quoted\"\"\r\nmodel\""),"RFC escaping missing");True(csv.EndsWith("\r\n",StringComparison.Ordinal),"CRLF end missing");});
  Check("CSV neutralizes formula prefixes after leading whitespace",()=>{foreach(string label in new[]{"=1+1","+SUM(1,2)","-1","@run","  =1","\t=1"," \r\n+1","\nmodel"}){var s=new Snapshot {Models=new List<BreakdownView>{new BreakdownView {Label=label,Today=T(1,0,1)}}};Eq(Rows(Convenience.Csv(s,"=invalid"))[2][1],"'"+label);Eq(Rows(Convenience.Csv(s,"=invalid"))[2][0],"Today");}});
  Check("empty snapshot exports a zero total and no private identifiers",()=>{var s=Fixture();s.Models.Clear();var rows=Rows(Convenience.Csv(null,"Today"));Eq(rows.Count,2);Row(rows[1],"Today","All models","0","0","0","0");Eq(Convenience.CountText(null,"Recorded"),"0");string csv=Convenience.Csv(Fixture(),"Today");True(!csv.Contains("private-id"),"private identifiers included");});
  Check("missing labels and null model totals recover as unknown or zero",()=>{var s=new Snapshot {Today=null,Models=new List<BreakdownView>{null,new BreakdownView {Label=null,Today=T(2,1,1)},new BreakdownView {Label="No totals",Today=null}}};Eq(Convenience.Total(s,"Today").Total,0L);var rows=Rows(Convenience.Csv(s,"Today"));Eq(rows.Count,3);Eq(rows[2][1],"Unknown model");});
  Check("equal model counts use deterministic label ordering",()=>{var s=new Snapshot {Models=new List<BreakdownView>{new BreakdownView {Label="zeta",Today=T(1,0,1)},new BreakdownView {Label="Alpha",Today=T(1,0,1)},new BreakdownView {Label="beta",Today=T(1,0,1)}}};var rows=Rows(Convenience.Csv(s,"Today"));Eq(rows[2][1],"Alpha");Eq(rows[3][1],"beta");Eq(rows[4][1],"zeta");});
  Console.WriteLine(passed+" passed, "+failed+" failed");return failed==0?0:1;
 }
}
