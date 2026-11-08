using System;
using System.Linq;

namespace PlayDo.Odometer {
 public static class SourceTotals {
  public const string Both="Codex + Claude Code",Codex="Codex",Claude="Claude Code";
  public static string[] Choices {get{return new[]{Both,Codex,Claude};}}
  public static Snapshot Select(string source,Snapshot codex,Snapshot claude) {
   if(source==Claude)return claude??new Snapshot {Status="No local Claude Code sessions found"};
   if(source!=Both||claude==null)return codex;
   var s=new Snapshot {
    Today=Sum(codex.Today,claude.Today),Week=Sum(codex.Week,claude.Week),Recorded=Sum(codex.Recorded,claude.Recorded),
    Quota=codex.Quota,Files=codex.Files+claude.Files,Processed=codex.Processed+claude.Processed,Compressed=codex.Compressed+claude.Compressed,
    Bytes=codex.Bytes+claude.Bytes,Events=codex.Events+claude.Events,Indexing=codex.Indexing||claude.Indexing,Paused=codex.Paused&&claude.Paused,
    LastEvent=codex.LastEvent>claude.LastEvent?codex.LastEvent:claude.LastEvent,Started=codex.Started<claude.Started?codex.Started:claude.Started
   };
   foreach(var d in codex.Days)s.Days[d.Key]=d.Value.Copy();
   foreach(var d in claude.Days){Tokens t;s.Days[d.Key]=s.Days.TryGetValue(d.Key,out t)?Sum(t,d.Value):d.Value.Copy();}
   s.Models=codex.Models.Select(x=>Tag(x,Codex)).Concat(claude.Models.Select(x=>Tag(x,Claude))).ToList();
   s.Sessions=codex.Sessions.Concat(claude.Sessions).OrderByDescending(x=>x.At).Take(8).ToList();
   s.Status=s.Paused?"Paused":s.Indexing?"Importing local usage":s.Files==0?"No local sessions found":s.LastEvent==DateTime.MinValue?"Waiting for local token reports":"Watching local usage";
   return s;
  }
  static Tokens Sum(Tokens a,Tokens b){var t=a.Copy();t.Add(b);return t;}
  static BreakdownView Tag(BreakdownView x,string provider){return new BreakdownView {Id=provider+":"+x.Id,Label=provider+" · "+x.Label,Today=x.Today,Week=x.Week,Recorded=x.Recorded};}
 }
}
