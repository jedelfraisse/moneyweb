using System.Security.Claims;
using MoneyWeb.Data.Models;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Blazor.Services;

/// <summary>
/// Resolves the current authenticated user from the database,
/// provisioning a new (unapproved) record on first login.
/// Scoped per-circuit — safe to cache within a single connection.
/// </summary>
public class CurrentUserService(IUserRepository userRepo, IUserGroupRepository userGroupRepo, IHttpContextAccessor httpContextAccessor)
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

        var user = await userRepo.GetByEntraObjectIdAsync(oid);
        if (user is null)
        {
            // First login — provision with IsApproved = false
            var email = principal.FindFirstValue("preferred_username")
                     ?? principal.FindFirstValue(ClaimTypes.Email)
                     ?? string.Empty;
            var name = principal.FindFirstValue("name")
                    ?? principal.FindFirstValue(ClaimTypes.Name)
                    ?? email;

            user = new User
            {
                EntraObjectId = oid,
                Email = email,
                DisplayName = name,
                IsApproved = false,
                IsAdmin = false
            };
            user.Id = await userRepo.CreateAsync(user);
            // Auto-join any pending group invites for this email
            await userGroupRepo.CheckAndJoinPendingInvitesAsync(user.Id, email);
            // Re-fetch so IsApproved reflects any auto-approval from group invite
            user = await userRepo.GetByIdAsync(user.Id) ?? user;
        }

        _cached = user;
        return _cached;
    }

    public void Invalidate() => _cached = null;
}
