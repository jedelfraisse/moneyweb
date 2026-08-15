using MoneyWeb.Data.Models;

namespace MoneyWeb.Data.Repositories.Interfaces;

public interface ILoginTokenRepository
{
    Task<int> CreateAsync(LoginToken token);

    /// <summary>Not yet consumed and not yet expired.</summary>
    Task<LoginToken?> GetActiveByIdAsync(int id);

    /// <summary>Not yet consumed and not yet expired.</summary>
    Task<LoginToken?> GetActiveByTokenHashAsync(string tokenHash);

    /// <summary>Most recently created token for this email, active or not (used for resend throttling).</summary>
    Task<LoginToken?> GetLatestForEmailAsync(string email);

    Task IncrementAttemptCountAsync(int id);
    Task MarkConsumedAsync(int id);
}
