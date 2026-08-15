using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using MoneyWeb.Blazor.Services.Email;
using MoneyWeb.Data.Models;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Blazor.Services;

public record LoginRequestResult(bool Success, int? TokenId, string? ErrorCode);
public record VerifyResult(bool Success, User? User, string? ErrorCode);

/// <summary>
/// Owns the passwordless login flow: generating/verifying the magic-link token and
/// one-time code, and resolving (or first-time provisioning) the User row on success.
/// This is where user provisioning now happens — CurrentUserService just reads the
/// already-resolved user off the signed-in cookie.
/// </summary>
public class PasswordlessAuthService(
    ILoginTokenRepository tokenRepo,
    IUserRepository userRepo,
    IUserGroupRepository userGroupRepo,
    ISharingContactRepository contactRepo,
    IEmailSender emailSender)
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ResendThrottle = TimeSpan.FromSeconds(30);

    public async Task<LoginRequestResult> StartLoginAsync(string rawEmail, string baseUrl)
    {
        var email = Normalize(rawEmail);
        if (!IsValidEmail(email)) return new LoginRequestResult(false, null, "invalid_email");

        // Resend-spam guard only — not a security control. Each token is independently
        // protected by its own hash/expiry/attempt-count, so letting an older still-active
        // token coexist with a new one for its remaining lifetime is harmless.
        var existing = await tokenRepo.GetLatestForEmailAsync(email);
        if (existing is not null
            && existing.ConsumedAtUtc is null
            && existing.ExpiresAtUtc > DateTime.UtcNow
            && DateTime.UtcNow - existing.CreatedAtUtc < ResendThrottle)
        {
            return new LoginRequestResult(true, existing.Id, null);
        }

        var (rawToken, tokenHash) = GenerateToken();
        var (code, codeHash) = GenerateCode();
        var token = new LoginToken
        {
            Email = email,
            TokenHash = tokenHash,
            CodeHash = codeHash,
            ExpiresAtUtc = DateTime.UtcNow.Add(TokenLifetime)
        };
        token.Id = await tokenRepo.CreateAsync(token);

        var magicLink = $"{baseUrl}/auth/magic?token={rawToken}";
        await emailSender.SendLoginEmailAsync(email, magicLink, code);

        return new LoginRequestResult(true, token.Id, null);
    }

    public async Task<VerifyResult> VerifyCodeAsync(int tokenId, string submittedCode)
    {
        var token = await tokenRepo.GetActiveByIdAsync(tokenId);
        if (token is null) return new VerifyResult(false, null, "expired");
        if (token.AttemptCount >= MaxAttempts) return new VerifyResult(false, null, "too_many_attempts");

        var submittedHash = HashHex(Encoding.UTF8.GetBytes(submittedCode.Trim()));
        if (!ConstantTimeEquals(submittedHash, token.CodeHash))
        {
            await tokenRepo.IncrementAttemptCountAsync(tokenId);
            return new VerifyResult(false, null, "invalid_code");
        }

        // Resolve the user before marking consumed — if provisioning throws (e.g. a transient
        // DB error), the token stays valid for retry instead of being burned for nothing.
        var user = await ResolveOrCreateUserAsync(token.Email, "code");
        await tokenRepo.MarkConsumedAsync(tokenId);
        return new VerifyResult(true, user, null);
    }

    public async Task<VerifyResult> VerifyMagicLinkAsync(string rawToken)
    {
        string tokenHash;
        try
        {
            tokenHash = HashHex(Convert.FromHexString(rawToken));
        }
        catch (FormatException)
        {
            return new VerifyResult(false, null, "expired");
        }

        var token = await tokenRepo.GetActiveByTokenHashAsync(tokenHash);
        if (token is null) return new VerifyResult(false, null, "expired");

        var user = await ResolveOrCreateUserAsync(token.Email, "magicLink");
        await tokenRepo.MarkConsumedAsync(token.Id);
        return new VerifyResult(true, user, null);
    }

    public Task<LoginToken?> GetActiveTokenAsync(int tokenId) => tokenRepo.GetActiveByIdAsync(tokenId);

    public static ClaimsPrincipal BuildPrincipal(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName),
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    private async Task<User> ResolveOrCreateUserAsync(string email, string loginMethod)
    {
        var user = await userRepo.GetByEmailAsync(email);
        if (user is null)
        {
            user = new User
            {
                Email = email,
                DisplayName = email,
                IsApproved = false,
                IsAdmin = false,
            };
            user.Id = await userRepo.CreateAsync(user);
            // Auto-join any pending group invites and link sharing contacts for this email —
            // same provisioning steps CurrentUserService used to run on first CIAM login.
            await userGroupRepo.CheckAndJoinPendingInvitesAsync(user.Id, email);
            await contactRepo.LinkUserAsync(email, user.Id);
            user = await userRepo.GetByIdAsync(user.Id) ?? user;
        }

        user.LastLoginAtUtc = DateTime.UtcNow;
        user.LastLoginClaimsJson = $$"""{"loginMethod":"{{loginMethod}}"}""";
        await userRepo.UpdateAsync(user);
        return user;
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();

    private static bool IsValidEmail(string email) =>
        !string.IsNullOrWhiteSpace(email) && email.Contains('@') && email.Length <= 256;

    private static (string Raw, string Hash) GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return (Convert.ToHexString(bytes), HashHex(bytes));
    }

    private static (string Code, string Hash) GenerateCode()
    {
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        return (code, HashHex(Encoding.UTF8.GetBytes(code)));
    }

    private static string HashHex(byte[] data) => Convert.ToHexString(SHA256.HashData(data));

    private static bool ConstantTimeEquals(string aHex, string bHex) =>
        CryptographicOperations.FixedTimeEquals(Convert.FromHexString(aHex), Convert.FromHexString(bHex));
}
