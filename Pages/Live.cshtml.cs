using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StreetSignal.Dashboard.Security;
using StreetSignal.Dashboard.Services;
namespace StreetSignal.Dashboard.Pages;
[Authorize(Roles="official,admin")]
public class LiveModel(FirestoreDashboard data,ILogger<LiveModel> logger):PageModel
{
 [BindProperty(SupportsGet=true)]public string Area{get;set;}="";
 public string[] Areas=>OfficialAccess.Areas(User);
 public LiveIncident[] Incidents{get;set;}=[];
 public string? Error{get;set;}
 public string? IndexSetupUrl{get;set;}
 [TempData]public string? Notice{get;set;}
 [BindProperty]public AlertInput Input{get;set;}=new();
 public async Task<IActionResult> OnGetAsync(){if(Areas.Length==0)return string.IsNullOrEmpty(Area)?Page():Forbid();if(string.IsNullOrEmpty(Area))Area=Areas.First();if(!Areas.Contains(Area))return Forbid();await Load();return Page();}
 async Task Load(){
  try{Incidents=await data.Read(User,Area,HttpContext.RequestAborted);}
  catch(Grpc.Core.RpcException ex) when(ex.StatusCode==Grpc.Core.StatusCode.FailedPrecondition && ex.Status.Detail.Contains("requires an index",StringComparison.OrdinalIgnoreCase)){
   logger.LogWarning("Report query requires a Firestore index");
   Error="Reports cannot load until the Firebase reports index is enabled. A Firebase project owner must create the index, wait for its status to become Enabled, then refresh this page.";
   var match=System.Text.RegularExpressions.Regex.Match(ex.Status.Detail,@"https://console\.firebase\.google\.com/[^\s""<>]+");
   if(match.Success && Uri.TryCreate(match.Value,UriKind.Absolute,out var url) && url.Scheme=="https" && url.Host=="console.firebase.google.com")IndexSetupUrl=url.AbsoluteUri;
  }
  catch(Exception ex){logger.LogError(ex,"Firestore read failed for authorized area");Error="Could not load reports. Check the Firebase connection, then refresh.";}
 }
 public async Task<IActionResult> OnPostIncidentAsync(string id,string state){if(!Areas.Contains(Area))return Forbid();try{await data.Update(User,Area,id,state,HttpContext.RequestAborted);Notice="Incident updated.";}catch(UnauthorizedAccessException){return Forbid();}catch(Exception ex){logger.LogError(ex,"Incident update failed");Notice="Incident could not be updated. Refresh and try again.";}return RedirectToPage(new{Area});}
 public async Task<IActionResult> OnPostAlertAsync(){if(!Areas.Contains(Area)||!Areas.Contains(Input.Area))return Forbid();if(!ModelState.IsValid){await Load();return Page();}try{await data.Send(User,Input.Area,Input.Service,Input.Title,Input.Body,HttpContext.RequestAborted);Notice="Official alert published to the selected area.";}catch(Exception ex){logger.LogError(ex,"Alert publish failed");Notice="Alert was not confirmed as published. Check before retrying.";}return RedirectToPage(new{Area});}
 public sealed class AlertInput{
 [Required]public string Area{get;set;}="";
 [Required,RegularExpression("^(water|electricity)$")]public string Service{get;set;}="water";
 [Required,StringLength(120)]public string Title{get;set;}="";
 [Required,StringLength(1000)]public string Body{get;set;}="";
 }
}
