using System.Security.Claims;
using MoneyWeb.Data.Models;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Blazor.Services;

/// <summary>
/// Resolves the current authenticated user from the database by the user-id claim on the
/// signed-in cookie principal. Scoped per-circuit — safe to cache within a single connection.
/// User provisioning/first-login setup happens once, at login time, in PasswordlessAuthService —
/// by the time this runs, the User row already exists.
/// </summary>
public class CurrentUserService(IUserRepository userRepo, IHttpContextAccessor httpContextAccessor)
{
    private User? _cached;

    public async Task<User?> GetCurrentUserAsync()
    {
        if (_cached is not null) return _cached;

        var principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true) return null;

        var userIdClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId)) return null;

        _cached = await userRepo.GetByIdAsync(userId);
        return _cached;
    }

    public void Invalidate() => _cached = null;
}
