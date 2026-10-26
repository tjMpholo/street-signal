using Google.Cloud.Firestore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using StreetSignal.Dashboard.Security;
namespace StreetSignal.Dashboard.Services;
public record LiveIncident(string Id,string Area,string Service,string Problem,int Homes,int ReportCount,int People,DateTime First,string State,bool Pilot);
public sealed class FirestoreDashboard(FirestoreDb db)
{
 public async Task<LiveIncident[]> Read(ClaimsPrincipal user,string area,CancellationToken ct)
 {
  OfficialAccess.Demand(user,area);
  var since=Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-56));
  var snap=await db.Collection("reports").WhereEqualTo("areaId",area).WhereGreaterThanOrEqualTo("startedAt",since).GetSnapshotAsync(ct);
  var result=new List<LiveIncident>();
  foreach(var group in snap.Documents.Where(d=>Text(d,"status")!="back_on").GroupBy(d=>(Service:Text(d,"service"),Problem:Text(d,"status"))))
  {
   var ordered=group.OrderBy(d=>d.GetValue<Timestamp>("startedAt")).ThenBy(d=>d.Id).ToArray();
   for(int start=0;start<ordered.Length;)
   {
    var first=ordered[start].GetValue<Timestamp>("startedAt").ToDateTime();
    var batch=ordered.Skip(start).TakeWhile(d=>d.GetValue<Timestamp>("startedAt").ToDateTime()<first.AddHours(3)).ToArray();start+=batch.Length;
    var id=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(area+"|"+group.Key.Service+"|"+group.Key.Problem+"|"+batch[0].Id))).ToLowerInvariant();
    var saved=await db.Collection("incidents").Document(id).GetSnapshotAsync(ct);
    var state=batch.All(d=>d.TryGetValue<Timestamp?>("restoredAt",out var t)&&t!=null)?"resolved":"suspected";
    if(saved.Exists&&Text(saved,"areaId")==area&&Text(saved,"status") is "confirmed" or "resolved")state=Text(saved,"status");
    var households=batch.Where(d=>!string.IsNullOrEmpty(Text(d,"householdId")) || !string.IsNullOrEmpty(Text(d,"uid"))).GroupBy(d=>string.IsNullOrEmpty(Text(d,"householdId"))?Text(d,"uid"):Text(d,"householdId"));
    result.Add(new(id,area,group.Key.Service,group.Key.Problem,households.Count(),batch.Length,households.Sum(g=>g.Max(d=>d.TryGetValue<long>("people",out var p)?(int)Math.Clamp(p,0,10000):0)),first,state,batch.Any(d=>d.TryGetValue<bool>("pilot",out var p)&&p)));
   }
  }
  return result.OrderBy(x=>x.State=="resolved").ThenByDescending(x=>x.First).ToArray();
 }
 public async Task Update(ClaimsPrincipal user,string area,string id,string state,CancellationToken ct)
 {
  OfficialAccess.Demand(user,area);
  if(state is not ("confirmed" or "resolved"))throw new ArgumentException("Invalid state");
  var item=(await Read(user,area,ct)).SingleOrDefault(x=>x.Id==id)??throw new UnauthorizedAccessException();
  if(item.State=="resolved")throw new InvalidOperationException("Incident already resolved.");
  await db.Collection("incidents").Document(id).SetAsync(new Dictionary<string,object?>{
   ["areaId"]=area,["service"]=item.Service,["status"]=state,["reportCount"]=item.ReportCount,["peopleAffected"]=item.People,["firstReportAt"]=Timestamp.FromDateTime(item.First),["confirmedBy"]=user.FindFirstValue(ClaimTypes.NameIdentifier),["updatedAt"]=FieldValue.ServerTimestamp,["resolvedAt"]=state=="resolved"?FieldValue.ServerTimestamp:null
  },SetOptions.MergeAll,ct);
 }
 public async Task Send(ClaimsPrincipal user,string area,string service,string title,string body,CancellationToken ct)
 {
  OfficialAccess.Demand(user,area);
  if(service is not ("water" or "electricity")||string.IsNullOrWhiteSpace(title)||title.Length>120||string.IsNullOrWhiteSpace(body)||body.Length>1000)throw new ArgumentException("Invalid alert");
  await db.Collection("alerts").AddAsync(new Dictionary<string,object>{["areaIds"]=new[]{area},["service"]=service,["title"]=title.Trim(),["body"]=new Dictionary<string,string>{{"en",body.Trim()}},["source"]="official",["sentAt"]=FieldValue.ServerTimestamp},ct);
 }
 static string Text(DocumentSnapshot d,string key)=>d.TryGetValue<string>(key,out var value)?value:"";
}

