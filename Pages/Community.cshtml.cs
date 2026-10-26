using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StreetSignal.Dashboard.Security;
using StreetSignal.Dashboard.Services;
namespace StreetSignal.Dashboard.Pages;
[Authorize(Roles="official,admin")]
public class CommunityModel(InsightsRepository repository,ILogger<CommunityModel> logger):PageModel
{
 [BindProperty(SupportsGet=true)]public string Area{get;set;}="";
 public string[] Areas=>OfficialAccess.Areas(User);
 public CommunitySummary? Data{get;set;}
 public ReadinessSummary? Readiness{get;set;}
 public string? Error{get;set;}
 public string? ReadinessError{get;set;}
 public async Task<IActionResult> OnGetAsync(){if(Areas.Length==0)return string.IsNullOrEmpty(Area)?Page():Forbid();if(string.IsNullOrEmpty(Area))Area=Areas.First();if(!Areas.Contains(Area))return Forbid();try{Data=await repository.Community(User,Area,HttpContext.RequestAborted);}catch(Exception ex){logger.LogError(ex,"Community posts query failed");Error="Community posts could not be loaded. Refresh or check the Firebase connection.";}try{Readiness=await repository.Readiness(User,Area,HttpContext.RequestAborted);}catch(Exception ex){logger.LogError(ex,"Readiness aggregation failed");ReadinessError="Emergency readiness totals are temporarily unavailable.";}return Page();}
 public static string NeedLabel(string kind)=>kind switch{"power_equipment"=>"Power for equipment","mobility"=>"Mobility support","water"=>"Water support","young_or_old"=>"Young or older residents",_=>"Other support"};
}
