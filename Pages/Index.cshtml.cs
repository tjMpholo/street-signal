using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace StreetSignal.Dashboard.Pages;
public class IndexModel:PageModel
{
 public IActionResult OnGet()=>RedirectToPage("/Overview");
 public IActionResult OnPost()=>NotFound();
}
