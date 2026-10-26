using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
namespace StreetSignal.Dashboard.Security;
public sealed class LoginAbuseGuard : IDisposable
{
 private readonly PartitionedRateLimiter<string> accounts=PartitionedRateLimiter.Create<string,string>(email=>RateLimitPartition.GetFixedWindowLimiter(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant()))),_=>new FixedWindowRateLimiterOptions{PermitLimit=5,Window=TimeSpan.FromMinutes(5),QueueLimit=0,AutoReplenishment=true}));
 public RateLimitLease Attempt(string email)=>accounts.AttemptAcquire(email);
 public void Dispose()=>accounts.Dispose();
}
