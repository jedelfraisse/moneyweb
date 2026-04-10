using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MoneyWeb.Blazor.Pages.MicrosoftIdentity.Account;

public class SignOutFederatedModel : PageModel
{
    public IActionResult OnGet()
    {
        // Full federated sign-out — clears local cookie AND redirects to Azure logout.
        return SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            OpenIdConnectDefaults.AuthenticationScheme,
            CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
