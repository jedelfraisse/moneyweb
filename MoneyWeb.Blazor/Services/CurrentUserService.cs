using System.Security.Claims;
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

        var oid = principal.FindFirstValue("oid")
               ?? principal.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier");
        if (string.IsNullOrEmpty(oid)) return null;

        var email = principal.FindFirstValue("preferred_username")
                 ?? principal.FindFirstValue(ClaimTypes.Email)
                 ?? string.Empty;

        // CIAM sends given_name + family_name; workforce AAD sends a single "name" claim
        var givenName  = principal.FindFirstValue("given_name")  ?? string.Empty;
        var familyName = principal.FindFirstValue("family_name") ?? string.Empty;
        var name = !string.IsNullOrWhiteSpace(givenName) || !string.IsNullOrWhiteSpace(familyName)
            ? $"{givenName} {familyName}".Trim()
            : principal.FindFirstValue("name")
           ?? principal.FindFirstValue(ClaimTypes.Name)
           ?? email;

        var user = await userRepo.GetByEntraObjectIdAsync(oid);
        if (user is null)
        {
            // First login — provision with IsApproved = false
            user = new User
            {
                EntraObjectId = oid,
                Email = email,
                DisplayName = name,
                IsApproved = false,
                IsAdmin = false
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
            // Keep email and display name fresh on every login
            bool changed = false;
            if (!string.IsNullOrWhiteSpace(email) && user.Email != email)
            { user.Email = email; changed = true; }
            if (!string.IsNullOrWhiteSpace(name) && user.DisplayName != name)
            { user.DisplayName = name; changed = true; }
            if (changed) await userRepo.UpdateAsync(user);
        }

        _cached = user;
        return _cached;
    }

    public void Invalidate() => _cached = null;
}
