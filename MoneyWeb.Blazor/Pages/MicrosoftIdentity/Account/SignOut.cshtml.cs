using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MoneyWeb.Blazor.Pages.MicrosoftIdentity.Account;

public class SignOutModel : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        // Sign out of the local cookie only — no Azure redirect.
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return LocalRedirect("/");
    }
}
