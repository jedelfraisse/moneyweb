using Dapper;
using Microsoft.Data.SqlClient;
using MoneyWeb.Data.Models;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Data.Repositories;

public class LoginTokenRepository(string connectionString) : ILoginTokenRepository
{
    private SqlConnection Connect() => new(connectionString);

    public async Task<int> CreateAsync(LoginToken token)
    {
        using var conn = Connect();
        var sql = """
            INSERT INTO LoginTokens (Email, TokenHash, CodeHash, ExpiresAtUtc)
            VALUES (@Email, @TokenHash, @CodeHash, @ExpiresAtUtc);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;
        return await conn.ExecuteScalarAsync<int>(sql, token);
    }

    public async Task<LoginToken?> GetActiveByIdAsync(int id)
    {
        using var conn = Connect();
        return await conn.QuerySingleOrDefaultAsync<LoginToken>("""
            SELECT * FROM LoginTokens
            WHERE Id = @Id AND ConsumedAtUtc IS NULL AND ExpiresAtUtc > SYSUTCDATETIME()
            """, new { Id = id });
    }

    public async Task<LoginToken?> GetActiveByTokenHashAsync(string tokenHash)
    {
        using var conn = Connect();
        return await conn.QuerySingleOrDefaultAsync<LoginToken>("""
            SELECT * FROM LoginTokens
            WHERE TokenHash = @TokenHash AND ConsumedAtUtc IS NULL AND ExpiresAtUtc > SYSUTCDATETIME()
            """, new { TokenHash = tokenHash });
    }

    public async Task<LoginToken?> GetLatestForEmailAsync(string email)
    {
        using var conn = Connect();
        return await conn.QuerySingleOrDefaultAsync<LoginToken>("""
            SELECT TOP 1 * FROM LoginTokens WHERE Email = @Email ORDER BY CreatedAtUtc DESC
            """, new { Email = email });
    }

    public async Task IncrementAttemptCountAsync(int id)
    {
        using var conn = Connect();
        await conn.ExecuteAsync(
            "UPDATE LoginTokens SET AttemptCount = AttemptCount + 1 WHERE Id = @Id",
            new { Id = id });
    }

    public async Task MarkConsumedAsync(int id)
    {
        using var conn = Connect();
        await conn.ExecuteAsync(
            "UPDATE LoginTokens SET ConsumedAtUtc = SYSUTCDATETIME() WHERE Id = @Id",
            new { Id = id });
    }
}
