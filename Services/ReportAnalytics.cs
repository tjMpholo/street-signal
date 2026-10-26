namespace StreetSignal.Dashboard.Services;

public record AnalyticsReport(string HouseholdId,string Service,string Status,int People,DateTimeOffset Started,bool Restored,bool Pilot);
public record WeekCount(DateOnly Starting,int Count);
public record OverviewData(int OpenWater,int OpenElectricity,int Households,int PeopleThisWeek,int PilotReports,int ReportCount,int WaterReports,int ElectricityReports,WeekCount[] Weeks);

public static class ReportAnalytics
{
 public static OverviewData Calculate(IEnumerable<AnalyticsReport> source,DateTimeOffset now)
 {
  var reports=source.Where(r=>r.Started>=now.AddDays(-56)&&r.Started<=now).ToArray();
  static DateOnly Monday(DateTimeOffset t){var d=DateOnly.FromDateTime(t.ToOffset(TimeSpan.FromHours(2)).DateTime);return d.AddDays(-(((int)d.DayOfWeek+6)%7));}
  var currentWeek=Monday(now);
  var incidents=new List<(DateTimeOffset First,int People)>();
  foreach(var group in reports.Where(r=>r.Status!="back_on").GroupBy(r=>(r.Service,r.Status)))
  {
   var sorted=group.OrderBy(r=>r.Started).ToArray();
   for(var i=0;i<sorted.Length;){var first=sorted[i].Started;var batch=sorted.Skip(i).TakeWhile(r=>r.Started<first.AddHours(3)).ToArray();i+=batch.Length;
    var people=batch.Where(r=>!string.IsNullOrWhiteSpace(r.HouseholdId)).GroupBy(r=>r.HouseholdId).Sum(g=>g.Max(r=>Math.Max(0,r.People)));
    incidents.Add((first,people));
   }
  }
  var weeks=new List<WeekCount>();
  for(var day=Monday(now.AddDays(-56));day<=currentWeek;day=day.AddDays(7))weeks.Add(new(day,incidents.Count(i=>Monday(i.First)==day)));
  var open=reports.Where(r=>!r.Restored&&r.Status!="back_on"&&r.Started>=now.AddHours(-12)).ToArray();
  return new(open.Count(r=>r.Service=="water"),open.Count(r=>r.Service=="electricity"),reports.Where(r=>!string.IsNullOrWhiteSpace(r.HouseholdId)).Select(r=>r.HouseholdId).Distinct().Count(),incidents.Where(i=>Monday(i.First)==currentWeek).Sum(i=>i.People),reports.Count(r=>r.Pilot),reports.Length,reports.Count(r=>r.Service=="water"),reports.Count(r=>r.Service=="electricity"),weeks.ToArray());
 }
}
