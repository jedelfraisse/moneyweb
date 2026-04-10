using Dapper;
using Microsoft.Data.SqlClient;
using MoneyWeb.Data.Models;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Data.Repositories;

public class BankAccountRepository(string connectionString) : IBankAccountRepository
{
    private SqlConnection Connect() => new(connectionString);

    public async Task<IEnumerable<BankAccount>> GetAllAsync(int userId)
    {
        using var conn = Connect();
        return await conn.QueryAsync<BankAccount>(
            "SELECT * FROM BankAccounts WHERE UserId = @UserId ORDER BY Name",
            new { UserId = userId });
    }

    public async Task<BankAccount?> GetByIdAsync(int id, int userId)
    {
        using var conn = Connect();
        return await conn.QuerySingleOrDefaultAsync<BankAccount>(
            "SELECT * FROM BankAccounts WHERE Id = @Id AND UserId = @UserId",
            new { Id = id, UserId = userId });
    }

    public async Task<int> CreateAsync(BankAccount account)
    {
        using var conn = Connect();
        return await conn.ExecuteScalarAsync<int>("""
            INSERT INTO BankAccounts (UserId, Name, AccountType, CurrentBalance, Notes, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES (@UserId, @Name, @AccountType, @CurrentBalance, @Notes, GETUTCDATE(), GETUTCDATE())
            """, account);
    }

    public async Task UpdateAsync(BankAccount account)
    {
        using var conn = Connect();
        await conn.ExecuteAsync("""
            UPDATE BankAccounts SET
                Name = @Name, AccountType = @AccountType,
                CurrentBalance = @CurrentBalance, Notes = @Notes,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @Id AND UserId = @UserId
            """, account);
    }

    public async Task DeleteAsync(int id, int userId)
    {
        using var conn = Connect();
        await conn.ExecuteAsync(
            "DELETE FROM BankAccounts WHERE Id = @Id AND UserId = @UserId",
            new { Id = id, UserId = userId });
    }
}
