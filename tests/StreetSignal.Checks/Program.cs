using StreetSignal.Dashboard.Services;
using StreetSignal.Dashboard.Security;
using System.Security.Claims;
{
var now=new DateTimeOffset(2026,10,26,12,0,0,TimeSpan.Zero);
AnalyticsReport R(string h,string s,int people,double hours,bool restored=false,string status="off",bool pilot=false)=>new(h,s,status,people,now.AddHours(hours),restored,pilot);
var rows=new[]{R("a","water",3,-6),R("a","water",4,-5,pilot:true),R("b","water",2,-3),R("c","electricity",5,-1,true),R("a","water",4,-.5,true,"back_on"),R("old","water",999,-57*24)};
var data=ReportAnalytics.Calculate(rows,now);
void Equal(int actual,int expected,string label){if(actual!=expected)throw new Exception(label+": "+actual+" expected "+expected);}
Equal(data.Households,3,"Distinct households");Equal(data.OpenWater,3,"Open water reports");Equal(data.OpenElectricity,0,"Restored electricity");Equal(data.PeopleThisWeek,11,"Deduplication and exclusive three-hour boundary");Equal(data.Weeks.Sum(w=>w.Count),3,"Grouped weekly incident counts");Equal(data.ReportCount,5,"56-day window");Equal(data.PilotReports,1,"Pilot flag");
var empty=ReportAnalytics.Calculate([],now);Equal(empty.ReportCount,0,"Empty data");Equal(empty.Weeks.Length,9,"Partial boundary weeks");
var sunday=new DateTimeOffset(2026,10,25,21,59,0,TimeSpan.Zero);
var monday=sunday.AddMinutes(1);
var boundary=ReportAnalytics.Calculate(new[]{new AnalyticsReport("a","water","off",9,sunday,false,false),new AnalyticsReport("b","electricity","off",2,monday,false,false)},now);
Equal(boundary.PeopleThisWeek,2,"SAST Monday boundary");
Console.WriteLine("Analytics checks PASS: household deduplication, three-hour boundary, restored reports, pilot flags, empty data, time window, SAST weeks.");
}
{
void Check(bool pass,string label){if(!pass)throw new Exception(label);}
var patterns=PatternAnalytics.Calculate(new[]{new PatternReport("water","off",1,18,60,false),new PatternReport("water","off",1,18,120,true),new PatternReport("electricity","off",7,0,90,false),new PatternReport("water","back_on",1,18,0,false),new PatternReport("water","dirty",0,24,null,false)});
Check(patterns.Medians["water"]==90,"Even median");Check(patterns.Medians["electricity"]==90,"Odd median");Check(patterns.Heatmap[0,18]==2,"Heatmap excludes restoration reports");Check(patterns.Heatmap.Cast<int>().Sum()==3,"Invalid heat coordinates ignored");Check(patterns.PilotCount==1,"Pilot count");Check(PatternAnalytics.Calculate([]).Medians["water"]==null,"Empty median");
var now=DateTimeOffset.UtcNow;
CommunityPost P(string topic,string kind,bool open=true,DateTimeOffset? expires=null,int members=0,int? target=null)=>new("water",topic,kind,"Test","Near clinic","cloudy",members,target,open,expires,now.AddHours(-1),false);
var posts=new[]{P("water_share","offer"),P("water_share","offer",expires:now),P("water_share","offer",false),P("water_quality","check",false),P("water_group","group",members:7,target:5),P("prepare_offer","offer"),P("access_issue","check")};
var community=CommunityAnalytics.Calculate(posts,now);
Check(community.Sectors["water"]==4,"Closed and expired posts excluded");Check(community.Groups.Single().Members==7,"Group count retained even above target");Check(community.WaterChecks.Length==1,"Closed water checks retained for history");Check(community.Helpers==1&&community.Access.Length==1,"Helpers and access reports");Check(CommunityAnalytics.Calculate([],now).Groups.Length==0,"Empty community");
Console.WriteLine("Insights checks PASS: medians, heatmap, invalid coordinates, pilot flags, active/expired posts, water history, groups, helpers, empty data.");
}
{
void Denied(Action action){try{action();throw new Exception("Expected denial");}catch(UnauthorizedAccessException){}}
var official=OfficialAccess.Principal("u1",new Dictionary<string,object>{{"role","official"},{"areaIds",new[]{"soweto"}}});
OfficialAccess.Demand(official,"soweto");
Denied(()=>OfficialAccess.Demand(official,"alexandra"));
Denied(()=>OfficialAccess.Demand(new ClaimsPrincipal(new ClaimsIdentity()),"soweto"));
Denied(()=>OfficialAccess.Principal("u",new Dictionary<string,object>{{"role","resident"},{"areaIds",new[]{"soweto"}}}));
Denied(()=>OfficialAccess.Principal("u",new Dictionary<string,object>{{"role","admin"},{"areaIds",Array.Empty<string>()}}));
Denied(()=>OfficialAccess.Principal("u",new Dictionary<string,object>{{"role","official"},{"areaIds",new[]{"bad/path"}}}));
Console.WriteLine("Authorized area, cross-area rejection, unauthenticated rejection, resident rejection, empty areas, invalid paths: PASS");
var global=OfficialAccess.Principal("global-admin",new Dictionary<string,object>{{"role","admin"},{"allAreas",true}});
if(OfficialAccess.Areas(global).Length!=0)throw new Exception("Unexpected area access");
Denied(()=>OfficialAccess.Demand(global,"unknown"));
Denied(()=>OfficialAccess.Principal("official",new Dictionary<string,object>{{"role","official"},{"allAreas",true}}));
Denied(()=>OfficialAccess.Principal("resident",new Dictionary<string,object>{{"role","resident"},{"allAreas",true}}));
Console.WriteLine("Empty all-area admin can sign in; unknown areas and non-admin escalation denied: PASS");
}

{
 using var guard=new LoginAbuseGuard();
 for(var i=0;i<5;i++){using var lease=guard.Attempt(" Admin@Example.com ");if(!lease.IsAcquired)throw new Exception("Account limited too early");}
 using var blocked=guard.Attempt("admin@example.com");
 if(blocked.IsAcquired)throw new Exception("Account limiter bypassed by case or whitespace");
 using var separate=guard.Attempt("other@example.com");
 if(!separate.IsAcquired)throw new Exception("Unrelated account incorrectly blocked");
 Console.WriteLine("Login protection checks PASS: five attempts allowed, sixth blocked, normalized account key, separate account unaffected.");
}
