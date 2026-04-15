using Dapper;
using Microsoft.Data.SqlClient;
using MoneyWeb.Data.Models;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Data.Repositories;

public class UserRepository(string connectionString) : IUserRepository
{
    private SqlConnection Connect() => new(connectionString);

    public async Task<User?> GetByEntraObjectIdAsync(string entraObjectId)
    {
        using var conn = Connect();
        return await conn.QuerySingleOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE EntraObjectId = @EntraObjectId",
            new { EntraObjectId = entraObjectId });
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        using var conn = Connect();
        return await conn.QuerySingleOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        using var conn = Connect();
        return await conn.QueryAsync<User>("SELECT * FROM Users ORDER BY DisplayName");
    }

    public async Task<int> CreateAsync(User user)
    {
        using var conn = Connect();
        // MERGE guards against the race condition where multiple concurrent requests
        // (HTTP pipeline + Blazor circuit) both attempt to provision the same user.
        var sql = """
            MERGE Users WITH (HOLDLOCK) AS target
            USING (SELECT @EntraObjectId AS EntraObjectId) AS source
                ON target.EntraObjectId = source.EntraObjectId
            WHEN NOT MATCHED THEN
                INSERT (EntraObjectId, Email, DisplayName, PostalCode, IsApproved, IsAdmin, CreatedAt)
                VALUES (@EntraObjectId, @Email, @DisplayName, @PostalCode, @IsApproved, @IsAdmin, GETUTCDATE());

            SELECT Id FROM Users WHERE EntraObjectId = @EntraObjectId;
            """;
        return await conn.ExecuteScalarAsync<int>(sql, user);
    }

    public async Task UpdateAsync(User user)
    {
        using var conn = Connect();
        await conn.ExecuteAsync("""
            UPDATE Users SET
                Email = @Email, DisplayName = @DisplayName, PostalCode = @PostalCode,
                IsApproved = @IsApproved, IsAdmin = @IsAdmin,
                CashFlowHorizonMonths = @CashFlowHorizonMonths,
                ShowSinkingFunds = @ShowSinkingFunds,
                ShowLoans = @ShowLoans,
                ShowFamily = @ShowFamily
            WHERE Id = @Id
            """, user);
    }

    public async Task DeleteUserDataAsync(int userId)
    {
        using var conn = Connect();
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();
        try
        {
            await conn.ExecuteAsync("DELETE FROM CashFlowTransactions WHERE UserId = @UserId", new { UserId = userId }, tx);
            await conn.ExecuteAsync("DELETE FROM Debts WHERE UserId = @UserId", new { UserId = userId }, tx);
            await conn.ExecuteAsync("DELETE FROM Bills WHERE UserId = @UserId", new { UserId = userId }, tx);
            await conn.ExecuteAsync("DELETE FROM Loans WHERE UserId = @UserId", new { UserId = userId }, tx);
            await conn.ExecuteAsync("DELETE FROM Users WHERE Id = @UserId", new { UserId = userId }, tx);
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
