using System.Threading.RateLimiting;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using StreetSignal.Dashboard.Security;
using StreetSignal.Dashboard.Services;
var builder=WebApplication.CreateBuilder(args);
if(builder.Environment.IsDevelopment())builder.Configuration.AddJsonFile(".local/firebase.json",optional:true,reloadOnChange:false);
builder.Configuration.AddEnvironmentVariables().AddCommandLine(args);
builder.WebHost.ConfigureKestrel(options=>{options.AddServerHeader=false;options.Limits.MaxRequestBodySize=32*1024;});
builder.Services.AddRazorPages(options=>options.Conventions.AuthorizeFolder("/"));
builder.Services.AddHttpClient("FirebaseLogin",client=>client.Timeout=TimeSpan.FromSeconds(15));
builder.Services.AddSingleton<LoginAbuseGuard>();
// Nginx connects locally and overwrites these headers. Remote callers are not trusted.
builder.Services.Configure<ForwardedHeadersOptions>(options=>{
 options.ForwardedHeaders=ForwardedHeaders.XForwardedFor|ForwardedHeaders.XForwardedProto;
 options.ForwardLimit=1;
 options.KnownProxies.Add(System.Net.IPAddress.Loopback);
 options.KnownProxies.Add(System.Net.IPAddress.IPv6Loopback);
});
builder.Services.AddDataProtection().SetApplicationName("StreetSignal.Dashboard");
var keyPath=builder.Configuration["Security:DataProtectionPath"];
if(!string.IsNullOrWhiteSpace(keyPath))builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keyPath));
builder.Services.AddAntiforgery(options=>{options.Cookie.HttpOnly=true;options.Cookie.SameSite=SameSiteMode.Strict;options.Cookie.SecurePolicy=builder.Environment.IsDevelopment()?CookieSecurePolicy.SameAsRequest:CookieSecurePolicy.Always;});
builder.Services.AddRateLimiter(options=>{
 options.RejectionStatusCode=429;
 options.OnRejected=async(context,ct)=>{context.HttpContext.Response.Headers.RetryAfter="60";await context.HttpContext.Response.WriteAsync("Too many sign-in attempts. Please wait and try again.",ct);};
 options.AddPolicy("login",context=>RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString()??"unknown",_=>new FixedWindowRateLimiterOptions{PermitLimit=10,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options=>{
 options.LoginPath="/Login";options.AccessDeniedPath="/Login";options.Cookie.Name=builder.Environment.IsDevelopment()?"StreetSignal.Session":"__Host-StreetSignal.Session";
 options.Cookie.Path="/";options.Cookie.HttpOnly=true;options.Cookie.SameSite=SameSiteMode.Strict;options.Cookie.SecurePolicy=builder.Environment.IsDevelopment()?CookieSecurePolicy.SameAsRequest:CookieSecurePolicy.Always;
 options.SlidingExpiration=false;options.ExpireTimeSpan=TimeSpan.FromMinutes(30);
 options.Events.OnValidatePrincipal=async context=>{
  try{
   var token=context.Properties.GetTokenValue("firebase_token")??throw new UnauthorizedAccessException();
   var verified=await context.HttpContext.RequestServices.GetRequiredService<FirebaseAuth>().VerifyIdTokenAsync(token,true);
   context.ReplacePrincipal(await context.HttpContext.RequestServices.GetRequiredService<FirebaseAccess>().Resolve(verified,context.HttpContext.RequestAborted));
  }catch{context.RejectPrincipal();await context.HttpContext.SignOutAsync();}
 };
});
builder.Services.AddAuthorization();
var project=builder.Configuration["Firebase:ProjectId"]??throw new InvalidOperationException("Firebase project is required.");
if(string.IsNullOrWhiteSpace(builder.Configuration["Firebase:ApiKey"]))throw new InvalidOperationException("Configure Firebase:ApiKey before starting the dashboard.");
var credentialPath=builder.Configuration["Firebase:CredentialsPath"];
var credential=string.IsNullOrWhiteSpace(credentialPath)?GoogleCredential.GetApplicationDefault():CredentialFactory.FromFile<ServiceAccountCredential>(credentialPath).ToGoogleCredential();
var firebase=FirebaseApp.Create(new AppOptions{ProjectId=project,Credential=credential});
builder.Services.AddSingleton(FirebaseAuth.GetAuth(firebase));
builder.Services.AddSingleton(new FirestoreDbBuilder{ProjectId=project,Credential=credential}.Build());
builder.Services.AddScoped<FirestoreDashboard>();builder.Services.AddScoped<OverviewRepository>();builder.Services.AddScoped<InsightsRepository>();builder.Services.AddScoped<FirebaseAccess>();
var app=builder.Build();
app.UseForwardedHeaders();
// Managed TLS hosts such as Cloud Run redirect HTTP at their edge. Do not trust
// arbitrary forwarded IP headers: the per-account limit works independently of IP.
if(!app.Environment.IsDevelopment()){app.UseExceptionHandler("/Error");app.UseHsts();}
if(!builder.Configuration.GetValue<bool>("Hosting:ManagedTls"))app.UseHttpsRedirection();
app.Use(async(context,next)=>{
 context.Response.Headers.XContentTypeOptions="nosniff";
 context.Response.Headers.XFrameOptions="DENY";
 context.Response.Headers["Referrer-Policy"]="no-referrer";
 context.Response.Headers["Content-Security-Policy"]="default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; object-src 'none'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
 context.Response.Headers.CacheControl="no-store";
 context.Response.Headers["Permissions-Policy"]="camera=(), microphone=(), geolocation=()";
 await next();
});
app.UseRouting();app.UseRateLimiter();app.UseAuthentication();app.UseAuthorization();
app.MapGet("/health",()=>Results.Ok(new{status="ok"})).AllowAnonymous();
app.MapStaticAssets();app.MapRazorPages().WithStaticAssets();app.Run();
