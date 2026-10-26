namespace StreetSignal.Dashboard.Services;
public record PatternReport(string Service,string Status,int Weekday,int Hour,int? Duration,bool Pilot);
public record PatternSummary(int[,] Heatmap,Dictionary<string,double?> Medians,Dictionary<string,int> Types,int Count,int PilotCount);
public static class PatternAnalytics
{
 public static PatternSummary Calculate(IEnumerable<PatternReport> source)
 {
  var rows=source.ToArray();var heat=new int[7,24];
  foreach(var r in rows.Where(r=>r.Status!="back_on"&&r.Weekday>=1&&r.Weekday<=7&&r.Hour>=0&&r.Hour<24))heat[r.Weekday-1,r.Hour]++;
  var medians=new Dictionary<string,double?>();
  foreach(var service in new[]{"water","electricity"}){var values=rows.Where(r=>r.Service==service&&r.Status!="back_on"&&r.Duration>=0).Select(r=>r.Duration!.Value).Order().ToArray();medians[service]=values.Length==0?null:values.Length%2==1?values[values.Length/2]:((double)values[values.Length/2-1]+values[values.Length/2])/2;}
  return new(heat,medians,rows.GroupBy(r=>r.Status).ToDictionary(g=>g.Key,g=>g.Count()),rows.Length,rows.Count(r=>r.Pilot));
 }
}
public record CommunityPost(string Sector,string Topic,string Kind,string Title,string Place,string Quality,int Members,int? Target,bool Open,DateTimeOffset? Expires,DateTimeOffset Created,bool Pilot);
public record CommunitySummary(Dictionary<string,int> Sectors,CommunityPost[] Groups,CommunityPost[] WaterChecks,CommunityPost[] Access,int Helpers,int PilotCount);
public static class CommunityAnalytics
{
 public static CommunitySummary Calculate(IEnumerable<CommunityPost> source,DateTimeOffset now)
 {
  var posts=source.Where(p=>p.Created<=now).OrderByDescending(p=>p.Created).ToArray();
  var active=posts.Where(p=>p.Open&&(p.Expires==null||p.Expires>now)).ToArray();
  return new(active.GroupBy(p=>p.Sector).ToDictionary(g=>g.Key,g=>g.Count()),active.Where(p=>p.Kind=="group").ToArray(),posts.Where(p=>p.Topic=="water_quality").Take(20).ToArray(),active.Where(p=>p.Topic=="access_issue").Take(20).ToArray(),active.Count(p=>p.Topic=="prepare_offer"),posts.Count(p=>p.Pilot));
 }
}
public record ReadinessSummary(Dictionary<string,int> Needs,int Issued,int Acted);
