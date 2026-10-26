using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StreetSignal.Dashboard.Security;
using StreetSignal.Dashboard.Services;
namespace StreetSignal.Dashboard.Pages;
[Authorize(Roles="official,admin")]
public class PatternsModel(InsightsRepository repository,ILogger<PatternsModel> logger):PageModel
{
 [BindProperty(SupportsGet=true)]public string Area{get;set;}="";
 public string[] Areas=>OfficialAccess.Areas(User);
 public PatternSummary? Data{get;set;}
 public string? Error{get;set;}
 public async Task<IActionResult> OnGetAsync(){if(Areas.Length==0)return string.IsNullOrEmpty(Area)?Page():Forbid();if(string.IsNullOrEmpty(Area))Area=Areas.First();if(!Areas.Contains(Area))return Forbid();try{Data=await repository.Patterns(User,Area,HttpContext.RequestAborted);}catch(Exception ex){logger.LogError(ex,"Patterns query failed");Error="Could not load patterns. Refresh or check the Firebase connection and reports index.";}return Page();}
 public static string Duration(double? minutes)=>minutes is {} m?$"{(int)m/60}h {m%60:0.#}m":"No restored reports yet";
}
