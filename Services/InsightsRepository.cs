using Google.Cloud.Firestore;
using System.Security.Claims;
using StreetSignal.Dashboard.Security;
namespace StreetSignal.Dashboard.Services;
public sealed class InsightsRepository(FirestoreDb db)
{
 public async Task<PatternSummary> Patterns(ClaimsPrincipal user,string area,CancellationToken ct)
 {
  OfficialAccess.Demand(user,area);var now=DateTimeOffset.UtcNow;
  var snap=await db.Collection("reports").WhereEqualTo("areaId",area).WhereGreaterThanOrEqualTo("startedAt",Timestamp.FromDateTimeOffset(now.AddDays(-56))).GetSnapshotAsync(ct);
  return PatternAnalytics.Calculate(snap.Documents.Where(d=>d.GetValue<Timestamp>("startedAt").ToDateTimeOffset()<=now).Select(d=>{
   var local=d.GetValue<Timestamp>("startedAt").ToDateTimeOffset().ToOffset(TimeSpan.FromHours(2));
   return new PatternReport(Text(d,"service"),Text(d,"status"),(int)(Number(d,"weekday")??(((int)local.DayOfWeek+6)%7+1)),(int)(Number(d,"hour")??local.Hour),d.TryGetValue<Timestamp?>("restoredAt",out var restored)&&restored!=null&&Number(d,"durationMinutes") is {} minutes&&minutes>=0?(int?)Math.Min(minutes,int.MaxValue):null,Flag(d,"pilot"));
  }));
 }
 public async Task<CommunitySummary> Community(ClaimsPrincipal user,string area,CancellationToken ct)
 {
  OfficialAccess.Demand(user,area);
  // Exclude contact and poster IDs. Member IDs are reduced to a count before returning data.
  var snap=await db.Collection("posts").WhereEqualTo("areaId",area).Select("sector","topic","kind","title","place","quality","members","target","open","expiresAt","createdAt","pilot").GetSnapshotAsync(ct);
  return CommunityAnalytics.Calculate(snap.Documents.Select(d=>new CommunityPost(Text(d,"sector"),Text(d,"topic"),Text(d,"kind"),Text(d,"title"),Text(d,"place"),Text(d,"quality"),d.TryGetValue<string[]>("members",out var members)?members?.Length??0:0,Number(d,"target") is {} target&&target>0?(int?)Math.Min(target,int.MaxValue):null,Flag(d,"open"),Time(d,"expiresAt"),Time(d,"createdAt")??DateTimeOffset.MinValue,Flag(d,"pilot"))),DateTimeOffset.UtcNow);
 }
 public async Task<ReadinessSummary> Readiness(ClaimsPrincipal user,string area,CancellationToken ct)
 {
  OfficialAccess.Demand(user,area);
  var homes=await db.Collection("households").WhereEqualTo("areaId",area).Select("consent.data").GetSnapshotAsync(ct);
  var needs=new Dictionary<string,int>();var issued=0;var acted=0;var now=DateTimeOffset.UtcNow;
  foreach(var home in homes.Documents.Where(d=>Flag(d,"consent.data")))
  {
   var records=await home.Reference.Collection("needs").Select("areaId","kind").GetSnapshotAsync(ct);
   foreach(var kind in records.Documents.Where(d=>Text(d,"areaId")==area).Select(d=>Text(d,"kind")).Where(k=>!string.IsNullOrWhiteSpace(k)).Distinct())needs[kind]=needs.GetValueOrDefault(kind)+1;
   var predictions=await home.Reference.Collection("predictions").WhereGreaterThanOrEqualTo("issuedAt",Timestamp.FromDateTimeOffset(now.AddDays(-56))).Select("areaId","issuedAt","acted").GetSnapshotAsync(ct);
   foreach(var prediction in predictions.Documents.Where(d=>Text(d,"areaId")==area&&Time(d,"issuedAt")<=now)){issued++;if(Flag(prediction,"acted"))acted++;}
  }
  return new(needs,issued,acted);
 }
 static string Text(DocumentSnapshot d,string key)=>d.TryGetValue<string>(key,out var value)?value??"":"";
 static long? Number(DocumentSnapshot d,string key)=>d.TryGetValue<long?>(key,out var value)?value:null;
 static bool Flag(DocumentSnapshot d,string key)=>d.TryGetValue<bool>(key,out var value)&&value;
 static DateTimeOffset? Time(DocumentSnapshot d,string key)=>d.TryGetValue<Timestamp?>(key,out var value)?value?.ToDateTimeOffset():null;
}


