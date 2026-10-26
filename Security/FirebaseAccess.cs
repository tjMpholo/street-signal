using System.Security.Claims;
using Google.Cloud.Firestore;
using FirebaseAdmin.Auth;

namespace StreetSignal.Dashboard.Security;

public sealed class FirebaseAccess(FirestoreDb db)
{
 public async Task<ClaimsPrincipal> Resolve(FirebaseToken token, CancellationToken ct)
 {
  // Validate the role before querying; all-area access is only accepted for admins.
  var principal=OfficialAccess.Principal(token.Uid,token.Claims);
  if(token.Claims.TryGetValue("role",out var role) && role?.ToString()=="admin"
     && token.Claims.TryGetValue("allAreas",out var all) && all is true)
  {
   var areas=await db.Collection("areas").GetSnapshotAsync(ct);
   var areaIds=areas.Documents.Select(d=>d.Id).ToHashSet(StringComparer.Ordinal);
   // Older mobile records may exist before the area registry is populated.
   // This discovery is restricted to verified all-area admins and projects with
   // an empty registry. Read only the public reports' area IDs, never households.
   if(areaIds.Count==0)
   {
    var reports=await db.Collection("reports").Select("areaId").GetSnapshotAsync(ct);
    foreach(var report in reports.Documents)
     if(report.TryGetValue<string>("areaId",out var id)
        && !string.IsNullOrWhiteSpace(id) && !id.Contains('/'))areaIds.Add(id);
   }
   var identity=(ClaimsIdentity)principal.Identity!;
   foreach(var claim in identity.FindAll("areaId").ToArray())identity.RemoveClaim(claim);
   foreach(var id in areaIds.Order(StringComparer.Ordinal))identity.AddClaim(new Claim("areaId",id));
  }
  return principal;
 }
}
