using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
namespace PlayDo.Odometer {
 public class Tokens {
  public long Input, Cached, Output, Reasoning;
  public long Total { get { return Input + Output; } }
  public void Add(Tokens t) { Input+=t.Input;Cached+=t.Cached;Output+=t.Output;Reasoning+=t.Reasoning; }
  public Tokens Copy() { return new Tokens {Input=Input,Cached=Cached,Output=Output,Reasoning=Reasoning}; }
 }
 public class SessionUsage {
  public Tokens Last = new Tokens(); public Tokens Observed = new Tokens(); public DateTime LastAt;
  public string ModelId=UsageLedger.UnknownId;
  public DateTime ModelAt;
 }
 public class BreakdownUsage {public string Id,Label;public Dictionary<string,Tokens> Days=new Dictionary<string,Tokens>();}
 public class QuotaWindow { public double Used; public long Minutes, Reset; }
 public class Quota { public DateTime At; public List<QuotaWindow> Windows = new List<QuotaWindow>(); }
 public class UsageLedger {
  public const string UnknownId="unknown";
  public Dictionary<string,Tokens> Days = new Dictionary<string,Tokens>();
  public Dictionary<string,BreakdownUsage> Models=new Dictionary<string,BreakdownUsage>();
  // A v2 cache may include records no longer present in readable local history.
  // Keep that per-day baseline and replace its unattributed shortfall as logs return.
  public Dictionary<string,Tokens> LegacyDays=new Dictionary<string,Tokens>(),LegacyRecoveredDays=new Dictionary<string,Tokens>(),PreservedDays=new Dictionary<string,Tokens>();
  public Dictionary<string,SessionUsage> Sessions = new Dictionary<string,SessionUsage>();
  public Dictionary<string,bool> Seen = new Dictionary<string,bool>();
  public Quota LatestQuota; public long Events; public DateTime Started = DateTime.UtcNow;
  [ScriptIgnore] public ReplayDatabase Replay {get;set;}
  [ScriptIgnore] public ReplayDatabase LegacyReplay {get;set;}
  private static Dictionary<string,object> Obj(object v) { return v as Dictionary<string,object>; }
  private static object Get(Dictionary<string,object> d,string k) { object v;return d!=null&&d.TryGetValue(k,out v)?v:null; }
  private static long Num(Dictionary<string,object> d,string k) { long n;return long.TryParse(Convert.ToString(Get(d,k),CultureInfo.InvariantCulture),out n)&&n>=0&&n<1000000000000000L?n:0; }
  private static Tokens Parse(Dictionary<string,object> d) { long i=Num(d,"input_tokens"),o=Num(d,"output_tokens");return new Tokens {Input=i,Output=o,Cached=Math.Min(i,Num(d,"cached_input_tokens")),Reasoning=Math.Min(o,Num(d,"reasoning_output_tokens"))}; }
  private static Tokens Delta(Tokens a,Tokens b) {return new Tokens {Input=Math.Max(0,a.Input-b.Input),Output=Math.Max(0,a.Output-b.Output),Cached=Math.Max(0,a.Cached-b.Cached),Reasoning=Math.Max(0,a.Reasoning-b.Reasoning)};}
  static bool Timestamp(object value,out DateTime at){return DateTime.TryParse(Convert.ToString(value),CultureInfo.InvariantCulture,DateTimeStyles.AdjustToUniversal|DateTimeStyles.AssumeUniversal,out at);}
  static string Text(object value,int limit){var text=value as string;if(String.IsNullOrWhiteSpace(text)||text.Length>limit)return null;foreach(char c in text)if(Char.IsControl(c))return null;return text.Trim();}
  bool Metadata(string session,Dictionary<string,object> root,Dictionary<string,object> p){
   DateTime at;if(!Timestamp(Get(root,"timestamp"),out at)&&!Timestamp(Get(p,"timestamp"),out at))return false;
   SessionUsage s;if(!Sessions.TryGetValue(session,out s)){s=new SessionUsage();Sessions[session]=s;}
   if(at<s.ModelAt)return false;
   string model=Text(Get(p,"model"),128);s.ModelId=model==null?UnknownId:model.ToLowerInvariant();s.ModelAt=at;return true;
  }
  static void AddDay(Dictionary<string,Tokens> days,string day,Tokens delta){Tokens value;if(!days.TryGetValue(day,out value)){value=new Tokens();days[day]=value;}value.Add(delta);}
  static void AddBreakdown(Dictionary<string,BreakdownUsage> groups,string id,string label,string day,Tokens delta){BreakdownUsage group;if(!groups.TryGetValue(id,out group)){group=new BreakdownUsage {Id=id,Label=label};groups[id]=group;}AddDay(group.Days,day,delta);}
  static void Subtract(Tokens value,Tokens minus){value.Input-=minus.Input;value.Output-=minus.Output;value.Cached-=minus.Cached;value.Reasoning-=minus.Reasoning;}
  public void ReconcileLegacy(){
   foreach(var old in PreservedDays){Subtract(Days[old.Key],old.Value);Subtract(Models[UnknownId].Days[old.Key],old.Value);}
   PreservedDays.Clear();
   foreach(var old in LegacyDays){Tokens current;if(!LegacyRecoveredDays.TryGetValue(old.Key,out current))current=new Tokens();var missing=Delta(old.Value,current);missing.Cached=Math.Min(missing.Input,missing.Cached);missing.Reasoning=Math.Min(missing.Output,missing.Reasoning);if(missing.Total==0)continue;PreservedDays[old.Key]=missing;AddDay(Days,old.Key,missing);AddBreakdown(Models,UnknownId,"Unknown model",old.Key,missing);}
  }
  public bool Accept(string session, string line) {
   if(line.IndexOf("token_count",StringComparison.Ordinal)<0&&line.IndexOf("turn_context",StringComparison.Ordinal)<0)return false;
   try {
    var root=Obj(new JavaScriptSerializer {MaxJsonLength=262144}.DeserializeObject(line));
    var p=Obj(Get(root,"payload"));
    string kind=Convert.ToString(Get(root,"type"));if(kind=="turn_context")return Metadata(session,root,p);
    if(Convert.ToString(Get(root,"type"))!="event_msg"||Convert.ToString(Get(p,"type"))!="token_count")return false;
    DateTime at;if(!DateTime.TryParse(Convert.ToString(Get(root,"timestamp")),CultureInfo.InvariantCulture,DateTimeStyles.AdjustToUniversal|DateTimeStyles.AssumeUniversal,out at))return false;
    bool changed=false;var q=Obj(Get(p,"rate_limits"));
    if(q!=null&&(Get(q,"limit_id")==null||Convert.ToString(Get(q,"limit_id"))=="codex")&&(LatestQuota==null||at>LatestQuota.At)) {
     var fresh=new Quota {At=at};
     foreach(var k in new[]{"primary","secondary"}) {var w=Obj(Get(q,k));double used;if(w!=null&&Get(w,"used_percent")!=null&&double.TryParse(Convert.ToString(Get(w,"used_percent"),CultureInfo.InvariantCulture),NumberStyles.Float,CultureInfo.InvariantCulture,out used)&&!double.IsNaN(used)&&!double.IsInfinity(used))fresh.Windows.Add(new QuotaWindow {Used=Math.Max(0,Math.Min(100,used)),Minutes=Num(w,"window_minutes"),Reset=Num(w,"resets_at")});}
     LatestQuota=fresh;changed=true;
    }
    var info=Obj(Get(p,"info"));var total=Obj(Get(info,"total_token_usage"));if(total==null)return changed;
    Tokens now=Parse(total);SessionUsage s;bool known=Sessions.TryGetValue(session,out s);
    if(!known)s=new SessionUsage();bool exists=known&&s.LastAt!=DateTime.MinValue;
    // A copied archive or replay must never rewind the baseline used for live deltas.
    if(exists&&at<s.LastAt)return changed;
    bool reset=exists&&(now.Input<s.Last.Input||now.Output<s.Last.Output);
    var last=Obj(Get(info,"last_token_usage"));
    Tokens delta=(!exists||reset)?(last==null?new Tokens():Parse(last)):Delta(now,s.Last);
    if(!exists&&last!=null&&now.Input==delta.Input&&now.Output==delta.Output)delta=now.Copy();
    delta.Cached=Math.Min(delta.Input,delta.Cached);delta.Reasoning=Math.Min(delta.Output,delta.Reasoning);
    string key;
    using(var sha=SHA256.Create())key=Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(at.ToString("O")+"|"+now.Input+"|"+now.Output+"|"+now.Cached)));
    bool seen=Seen.ContainsKey(key)||(Replay!=null&&Replay.Contains(key));
    bool legacy=!seen&&delta.Total>0&&LegacyReplay!=null&&LegacyReplay.Contains(key);
    if(seen&&exists&&at==s.LastAt&&(now.Input<s.Last.Input||now.Output<s.Last.Output))return changed;
    if(!exists)Sessions[session]=s;s.Last=now;s.LastAt=at;
    if(seen||delta.Total==0)return changed;
    Seen[key]=true;s.Observed.Add(delta);Events++;
    string day=at.ToLocalTime().ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
    if(legacy)AddDay(LegacyRecoveredDays,day,delta);
    AddDay(Days,day,delta);AddBreakdown(Models,s.ModelId,s.ModelId==UnknownId?"Unknown model":s.ModelId,day,delta);return true;
   }catch(ArgumentException){return false;}catch(InvalidOperationException){return false;}catch(FormatException){return false;}
  }
  public static Tokens TotalForDays(Dictionary<string,Tokens> days,DateTime start) {var result=new Tokens();string cutoff=start.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);foreach(var d in days)if(String.CompareOrdinal(d.Key,cutoff)>=0)result.Add(d.Value);return result;}
  public Tokens TotalFor(DateTime start) {return TotalForDays(Days,start);}
 }
}
