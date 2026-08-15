namespace MoneyWeb.Data.Models;

/// <summary>
/// A single passwordless login attempt: one magic-link token and one 6-digit code,
/// sharing the same expiry — consuming either one invalidates both.
/// Only hashes are ever stored; the raw token/code exist solely in the outgoing email.
/// </summary>
public class LoginToken
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
