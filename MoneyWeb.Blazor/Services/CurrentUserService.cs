using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using MoneyWeb.Data.Models;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Blazor.Services;

/// <summary>
/// Resolves the current authenticated user from the database,
/// provisioning a new (unapproved) record on first login.
/// Scoped per-circuit — safe to cache within a single connection.
/// </summary>
public class CurrentUserService(IUserRepository userRepo, IUserGroupRepository userGroupRepo, ISharingContactRepository contactRepo, IHttpContextAccessor httpContextAccessor)
{
    private User? _cached;

    public async Task<User?> GetCurrentUserAsync()
    {
        if (_cached is not null) return _cached;

        var principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true) return null;

        var loginClaimsJson = SerializeClaims(principal);
        var loginAtUtc = DateTime.UtcNow;

        var oid = principal.FindFirstValue("oid")
               ?? principal.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier");
        if (string.IsNullOrEmpty(oid)) return null;

        var email = GetClaimValue(principal,
            "email",
            ClaimTypes.Email,
            "preferred_username",
            ClaimTypes.Upn)
            ?? string.Empty;

        var givenName = GetClaimValue(principal,
            "givenName",
            "given_name",
            ClaimTypes.GivenName)
            ?? string.Empty;

        var surname = GetClaimValue(principal,
            "surname",
            "family_name",
            ClaimTypes.Surname)
            ?? string.Empty;

        var nameFromParts = (!string.IsNullOrWhiteSpace(givenName) || !string.IsNullOrWhiteSpace(surname))
            ? $"{givenName} {surname}".Trim()
            : null; // null means "no reliable name from token"

        var syncedDisplayName = GetClaimValue(principal,
            "displayName",
            "name",
            ClaimTypes.Name)
            ?? nameFromParts;

        var displayName = syncedDisplayName
            ?? email;

        var postalCode = GetClaimValue(principal,
            "postalCode",
            "postal_code",
            ClaimTypes.PostalCode)
            ?? string.Empty;

        var user = await userRepo.GetByEntraObjectIdAsync(oid);
        if (user is null)
        {
            // First login — provision with IsApproved = false
            user = new User
            {
                EntraObjectId = oid,
                Email = email,
                DisplayName = displayName,
                PostalCode = postalCode,
                IsApproved = false,
                IsAdmin = false,
                LastLoginClaimsJson = loginClaimsJson,
                LastLoginAtUtc = loginAtUtc
            };
            user.Id = await userRepo.CreateAsync(user);
            // Auto-join any pending group invites and link sharing contacts for this email
            await userGroupRepo.CheckAndJoinPendingInvitesAsync(user.Id, email);
            await contactRepo.LinkUserAsync(email, user.Id);
            // Re-fetch so IsApproved reflects any auto-approval from group invite
            user = await userRepo.GetByIdAsync(user.Id) ?? user;
        }
        else
        {
            // Keep profile fields fresh when better claims are available on subsequent sign-ins.
            bool changed = false;
            if (!string.IsNullOrWhiteSpace(email) && user.Email != email)
            { user.Email = email; changed = true; }
            if (!string.IsNullOrWhiteSpace(syncedDisplayName) && user.DisplayName != syncedDisplayName)
            { user.DisplayName = syncedDisplayName; changed = true; }
            if (!string.IsNullOrWhiteSpace(postalCode) && user.PostalCode != postalCode)
            { user.PostalCode = postalCode; changed = true; }
            if (user.LastLoginClaimsJson != loginClaimsJson)
            { user.LastLoginClaimsJson = loginClaimsJson; changed = true; }
            if (user.LastLoginAtUtc != loginAtUtc)
            { user.LastLoginAtUtc = loginAtUtc; changed = true; }
            if (changed) await userRepo.UpdateAsync(user);
        }

        _cached = user;
        return _cached;
    }

    public void Invalidate() => _cached = null;

    private static string? GetClaimValue(ClaimsPrincipal principal, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = principal.FindFirstValue(claimType);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static string SerializeClaims(ClaimsPrincipal principal)
    {
        var claims = principal.Claims
            .GroupBy(claim => claim.Type)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(claim => claim.Value).ToArray(),
                StringComparer.Ordinal);

        return JsonSerializer.Serialize(claims, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
