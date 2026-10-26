using System.Security.Claims;
using Google.Cloud.Firestore;
using StreetSignal.Dashboard.Security;
namespace StreetSignal.Dashboard.Services;

public record ForecastSummary(double? Accuracy,int Answered);
public sealed class OverviewRepository(FirestoreDb db)
{
 public async Task<OverviewData> Read(ClaimsPrincipal user,string area,CancellationToken ct)
 {
  OfficialAccess.Demand(user,area);
  var now=DateTimeOffset.UtcNow;
  var snapshot=await db.Collection("reports").WhereEqualTo("areaId",area).WhereGreaterThanOrEqualTo("startedAt",Timestamp.FromDateTimeOffset(now.AddDays(-56))).GetSnapshotAsync(ct);
  var reports=snapshot.Documents.Select(d=>new AnalyticsReport(Text(d,"householdId"),Text(d,"service"),Text(d,"status"),d.TryGetValue<long>("people",out var p)?(int)Math.Clamp(p,0,10000):0,d.GetValue<Timestamp>("startedAt").ToDateTimeOffset(),d.TryGetValue<Timestamp?>("restoredAt",out var restored)&&restored!=null,d.TryGetValue<bool>("pilot",out var pilot)&&pilot));
  return ReportAnalytics.Calculate(reports,now);
 }
 public async Task<ForecastSummary> Forecast(ClaimsPrincipal user,string area,CancellationToken ct)
 {
  OfficialAccess.Demand(user,area);
  // Keep private feedback on the server and only use households that consented.
  var households=await db.Collection("households").WhereEqualTo("areaId",area).Select("consent.data").GetSnapshotAsync(ct);
  var since=Timestamp.FromDateTime(DateTime.UtcNow.AddDays(-56));
  var scores=new List<double>();
  foreach(var home in households.Documents.Where(d=>d.TryGetValue<bool>("consent.data",out var consent)&&consent))
  {
   var predictions=await home.Reference.Collection("predictions").WhereGreaterThanOrEqualTo("issuedAt",since).Select("areaId","feedback").GetSnapshotAsync(ct);
   foreach(var prediction in predictions.Documents)
   {
    if(Text(prediction,"areaId")!=area)continue;
    switch(Text(prediction,"feedback")){case "yes":scores.Add(1);break;case "partly":scores.Add(.5);break;case "no":scores.Add(0);break;}
   }
  }
  return new(scores.Count==0?null:scores.Average()*100,scores.Count);
 }
 static string Text(DocumentSnapshot d,string key)=>d.TryGetValue<string>(key,out var value)?value:"";
}
