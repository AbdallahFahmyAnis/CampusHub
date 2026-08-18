using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CampusHub.Gateway.Pages;

[AllowAnonymous]
public class DeniedModel : PageModel
{
    public void OnGet()
    {
    }
}
