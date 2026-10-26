using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text.Json;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StreetSignal.Dashboard.Security;
namespace StreetSignal.Dashboard.Pages;
[Microsoft.AspNetCore.Authorization.AllowAnonymous]
[ResponseCache(Duration=0,Location=ResponseCacheLocation.None,NoStore=true)]
[Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("login")]
public class LoginModel(IConfiguration config,IHttpClientFactory clients,IServiceProvider services,LoginAbuseGuard guard):PageModel
{
 [BindProperty,Required,EmailAddress,StringLength(254)]public string Email{get;set;}="";
 [BindProperty,Required,StringLength(1024)]public string Password{get;set;}="";

 public void OnGet(){}
 public async Task<IActionResult> OnPostAsync(){
  if(!ModelState.IsValid){Password="";ModelState.Remove(nameof(Password));return Page();}
  using var attempt=guard.Attempt(Email);
  if(!attempt.IsAcquired){Response.StatusCode=429;Response.Headers.RetryAfter="300";ModelState.AddModelError("","Too many sign-in attempts. Wait five minutes and try again.");Password="";ModelState.Remove(nameof(Password));return Page();}
  try{
   using var response=await clients.CreateClient("FirebaseLogin").PostAsJsonAsync("https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key="+Uri.EscapeDataString(config["Firebase:ApiKey"]!),new{email=Email.Trim(),password=Password,returnSecureToken=true});
   if(!response.IsSuccessStatusCode)throw new UnauthorizedAccessException();
   using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
   var token=json.RootElement.GetProperty("idToken").GetString()!;
   var verified=await services.GetRequiredService<FirebaseAuth>().VerifyIdTokenAsync(token,true);
   var principal=await services.GetRequiredService<FirebaseAccess>().Resolve(verified,HttpContext.RequestAborted);
   var properties=new AuthenticationProperties{IsPersistent=false,ExpiresUtc=DateTimeOffset.UtcNow.AddMinutes(30)};
   properties.StoreTokens([new AuthenticationToken{Name="firebase_token",Value=token}]);
   await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,principal,properties);
   return RedirectToPage("/Overview");
  }catch{ModelState.AddModelError("","Sign-in failed. Check your details or contact your administrator.");return Page();}
  finally{Password="";ModelState.Remove(nameof(Password));}
 }
 public async Task<IActionResult> OnPostLogoutAsync(){await HttpContext.SignOutAsync();return RedirectToPage("/Login");}
}


