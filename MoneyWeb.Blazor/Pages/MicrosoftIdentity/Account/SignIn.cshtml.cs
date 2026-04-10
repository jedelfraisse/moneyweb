using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MoneyWeb.Blazor.Pages.MicrosoftIdentity.Account;

public class SignInModel : PageModel
{
    public IActionResult OnGet(string? redirectUri = "/")
    {
        var props = new AuthenticationProperties { RedirectUri = redirectUri };
        // Always show the Microsoft account picker so the user can switch accounts.
        props.Parameters["prompt"] = "select_account";
        return Challenge(props, OpenIdConnectDefaults.AuthenticationScheme);
    }
}
