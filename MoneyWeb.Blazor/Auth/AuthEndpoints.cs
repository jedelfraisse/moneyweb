using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MoneyWeb.Blazor.Services;
using Sqids;

namespace MoneyWeb.Blazor.Auth;

/// <summary>
/// Passwordless login/logout endpoints. These are plain minimal APIs — not Blazor components —
/// because setting/clearing the auth cookie requires a normal HTTP request/response, which an
/// interactive Blazor Server circuit doesn't have after the initial render.
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        // Namespaced under /auth/* (not /login/*) so these don't collide with the Blazor
        // page routes at /login and /login/verify — Blazor Server registers its own endpoint
        // for a @page route, and a minimal API at the identical path+verb is an ambiguous match.
        app.MapPost("/auth/login", async (HttpContext http, PasswordlessAuthService auth, SqidsEncoder<int> sqids) =>
        {
            var form = await http.Request.ReadFormAsync();
            var email = form["email"].ToString();
            var returnUrl = form["returnUrl"].ToString();

            var result = await auth.StartLoginAsync(email, GetBaseUrl(http.Request));
            if (!result.Success)
                return Results.LocalRedirect($"/login{BuildQuery(("error", result.ErrorCode), ("returnUrl", returnUrl))}");

            var rid = sqids.Encode(result.TokenId!.Value);
            return Results.LocalRedirect($"/login/verify{BuildQuery(("rid", rid), ("returnUrl", returnUrl))}");
        });

        app.MapGet("/auth/magic", async (HttpContext http, PasswordlessAuthService auth, string token, string? returnUrl) =>
        {
            var result = await auth.VerifyMagicLinkAsync(token);
            if (!result.Success)
                return Results.LocalRedirect($"/login{BuildQuery(("error", result.ErrorCode))}");

            await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                PasswordlessAuthService.BuildPrincipal(result.User!),
                new AuthenticationProperties { IsPersistent = true });

            return Results.LocalRedirect(IsLocalSafe(returnUrl) ? returnUrl! : "/");
        });

        app.MapPost("/auth/verify", async (HttpContext http, PasswordlessAuthService auth, SqidsEncoder<int> sqids) =>
        {
            var form = await http.Request.ReadFormAsync();
            var rid = form["rid"].ToString();
            var code = form["code"].ToString();
            var returnUrl = form["returnUrl"].ToString();

            var id = sqids.Decode(rid).FirstOrDefault();
            var result = await auth.VerifyCodeAsync(id, code);
            if (!result.Success)
                return Results.LocalRedirect($"/login/verify{BuildQuery(("rid", rid), ("error", result.ErrorCode), ("returnUrl", returnUrl))}");

            await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                PasswordlessAuthService.BuildPrincipal(result.User!),
                new AuthenticationProperties { IsPersistent = true });

            return Results.LocalRedirect(IsLocalSafe(returnUrl) ? returnUrl! : "/");
        });

        app.MapGet("/auth/logout", async (HttpContext http) =>
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.LocalRedirect("/signed-out");
        });
    }

    private static string GetBaseUrl(HttpRequest req) => $"{req.Scheme}://{req.Host}";

    /// <summary>Open-redirect guard — only ever redirect back to a relative path we were given.</summary>
    private static bool IsLocalSafe(string? url) =>
        !string.IsNullOrWhiteSpace(url) && Uri.IsWellFormedUriString(url, UriKind.Relative) && !url.StartsWith("//");

    private static string BuildQuery(params (string Key, string? Value)[] parts)
    {
        var pairs = parts.Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!)}");
        var joined = string.Join("&", pairs);
        return joined.Length == 0 ? "" : $"?{joined}";
    }
}
