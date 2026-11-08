using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace PlayDo.Odometer {
 public partial class UsageLedger {
  // Claude can repeat the same assistant response across content blocks, resumes and forks.
  // Store numeric high-water marks, keyed by a hash of the provider response ID.
  public Dictionary<string,Tokens> ClaudeResponses=new Dictionary<string,Tokens>();

  public bool AcceptClaude(string session,string line) {
   if(line.IndexOf("\"usage\"",StringComparison.Ordinal)<0)return false;
   try {
    var record=Obj(new JavaScriptSerializer {MaxJsonLength=1048576,RecursionLimit=128}.DeserializeObject(line));
    if(!"assistant".Equals(Get(record,"type")))return false;
    var message=Obj(Get(record,"message"));
    string id=Text(Get(message,"id"),256),model=Text(Get(message,"model"),128);
    var usage=Obj(Get(message,"usage"));DateTime at;
    if(id==null||model=="<synthetic>"||usage==null||!Timestamp(Get(record,"timestamp"),out at))return false;
    long read=Num(usage,"cache_read_input_tokens");
    var current=new Tokens {Input=Num(usage,"input_tokens")+read+Num(usage,"cache_creation_input_tokens"),Cached=read,Output=Num(usage,"output_tokens")};
    if(current.Total==0)return false;
    string key;
    using(var sha=SHA256.Create())key=Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(id)));
    Tokens prior;
    if(!ClaudeResponses.TryGetValue(key,out prior))prior=new Tokens();
    var delta=Delta(current,prior);if(delta.Total==0)return false;
    delta.Cached=Math.Min(delta.Input,delta.Cached);
    ClaudeResponses[key]=new Tokens {Input=Math.Max(prior.Input,current.Input),Cached=Math.Max(prior.Cached,current.Cached),Output=Math.Max(prior.Output,current.Output)};
    string day=at.ToLocalTime().ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
    AddDay(Days,day,delta);
    AddBreakdown(Models,model??UnknownId,model??"Unknown Claude model",day,delta);
    SessionUsage entry;
    if(!Sessions.TryGetValue(session,out entry)){entry=new SessionUsage();Sessions[session]=entry;}
    entry.Observed.Add(delta);if(at>entry.LastAt)entry.LastAt=at;
    Events++;
    return true;
   }catch(ArgumentException){return false;}catch(InvalidOperationException){return false;}catch(FormatException){return false;}
  }
 }
}
