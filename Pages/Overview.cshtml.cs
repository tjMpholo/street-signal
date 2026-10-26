using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StreetSignal.Dashboard.Security;
using StreetSignal.Dashboard.Services;
namespace StreetSignal.Dashboard.Pages;
[Authorize(Roles="official,admin")]
public class OverviewModel(OverviewRepository repository,ILogger<OverviewModel> logger):PageModel
{
 [BindProperty(SupportsGet=true)]public string Area{get;set;}="";
 public string[] Areas=>OfficialAccess.Areas(User);
 public OverviewData? Data{get;set;}
 public ForecastSummary? Forecast{get;set;}
 public string? Error{get;set;}
 public string? ForecastError{get;set;}
 public async Task<IActionResult> OnGetAsync()
 {
  if(Areas.Length==0)return string.IsNullOrEmpty(Area)?Page():Forbid();
  if(string.IsNullOrEmpty(Area))Area=Areas.First();
  if(!Areas.Contains(Area))return Forbid();
  try{Data=await repository.Read(User,Area,HttpContext.RequestAborted);}catch(Exception ex){logger.LogError(ex,"Overview report aggregation failed");Error="Could not load overview reports. Refresh or check the Firebase connection and reports index.";}
  try{Forecast=await repository.Forecast(User,Area,HttpContext.RequestAborted);}catch(Exception ex){logger.LogError(ex,"Forecast aggregation failed");ForecastError="Forecast feedback is currently unavailable.";}
  return Page();
 }
}
