using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Web.Script.Serialization {
 [AttributeUsage(AttributeTargets.Property|AttributeTargets.Field)] public sealed class ScriptIgnoreAttribute:Attribute {}
 // Keep the existing parser and checkpoint types shared with the Windows application.
 public sealed class JavaScriptSerializer {
  public int MaxJsonLength {get;set;}=64*1024*1024;
  public int RecursionLimit {get;set;}=128;
  JsonSerializerOptions Options(){
   var resolver=new DefaultJsonTypeInfoResolver();
   resolver.Modifiers.Add(info=>{for(int i=info.Properties.Count-1;i>=0;i--)if(info.Properties[i].AttributeProvider?.IsDefined(typeof(ScriptIgnoreAttribute),true)==true)info.Properties.RemoveAt(i);});
   var options=new JsonSerializerOptions{IncludeFields=true,MaxDepth=RecursionLimit,TypeInfoResolver=resolver};
   options.Converters.Add(new CheckpointDate());return options;
  }
  public string Serialize(object value)=>JsonSerializer.Serialize(value,Options());
  public T Deserialize<T>(string json){try{if(json.Length>MaxJsonLength)throw new ArgumentException("JSON exceeds limit");return JsonSerializer.Deserialize<T>(json,Options());}catch(JsonException e){throw new ArgumentException("Invalid JSON",e);}}
  public object DeserializeObject(string json){
   try{if(json.Length>MaxJsonLength)throw new ArgumentException("JSON exceeds limit");using var doc=JsonDocument.Parse(json,new JsonDocumentOptions{MaxDepth=RecursionLimit});return ConvertValue(doc.RootElement);}
   catch(JsonException e){throw new ArgumentException("Invalid JSON",e);}
  }
  static object ConvertValue(JsonElement value)=>value.ValueKind switch{
   JsonValueKind.Object=>value.EnumerateObject().ToDictionary(x=>x.Name,x=>ConvertValue(x.Value)),
   JsonValueKind.Array=>value.EnumerateArray().Select(ConvertValue).ToArray(),
   JsonValueKind.String=>value.GetString(),
   JsonValueKind.Number=>value.TryGetInt64(out long n)?(object)n:value.GetDouble(),
   JsonValueKind.True=>true,JsonValueKind.False=>false,_=>null
  };
  sealed class CheckpointDate:JsonConverter<DateTime>{
   public override DateTime Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options){
    string value=reader.GetString();
    if(value!=null&&value.StartsWith("/Date(")&&value.EndsWith(")/")&&long.TryParse(value.AsSpan(6,value.Length-8),out long ms))return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
    return DateTime.Parse(value,System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.RoundtripKind);
   }
   public override void Write(Utf8JsonWriter writer,DateTime value,JsonSerializerOptions options)=>writer.WriteStringValue(value.ToString("O"));
  }
 }
}
