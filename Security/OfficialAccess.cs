using System.Security.Claims;
using System.Text.Json;
namespace StreetSignal.Dashboard.Security;
public static class OfficialAccess
{
 public static ClaimsPrincipal Principal(string uid, IReadOnlyDictionary<string,object> claims)
 {
  if(!claims.TryGetValue("role",out var role)||role?.ToString() is not ("official" or "admin"))throw new UnauthorizedAccessException();
  var allAreas=role.ToString()=="admin" && claims.TryGetValue("allAreas",out var all) && all is true;
  if(!claims.TryGetValue("areaIds",out var value)) { if(!allAreas)throw new UnauthorizedAccessException(); value=Array.Empty<string>(); }
  var areas=JsonSerializer.Deserialize<string[]>(JsonSerializer.Serialize(value))??[];
  if((areas.Length==0&&!allAreas)||(!allAreas&&areas.Length>100)||areas.Any(a=>string.IsNullOrWhiteSpace(a)||a.Contains('/')))throw new UnauthorizedAccessException();
  var list=new List<Claim>{new(ClaimTypes.NameIdentifier,uid),new(ClaimTypes.Role,role.ToString()!)};
  list.AddRange(areas.Distinct().Select(a=>new Claim("areaId",a)));
  return new(new ClaimsIdentity(list,"Firebase"));
 }
 public static string[] Areas(ClaimsPrincipal user)=>user.FindAll("areaId").Select(c=>c.Value).Distinct().ToArray();
 public static void Demand(ClaimsPrincipal user,string area){if(!user.Identity!.IsAuthenticated||!Areas(user).Contains(area))throw new UnauthorizedAccessException();}
}
