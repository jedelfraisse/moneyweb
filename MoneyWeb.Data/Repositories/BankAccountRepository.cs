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
        // Nothing in the schema ties these to the account, so a bare delete used to leave its cash-flow entries
        // (invisible on every Transactions page, but still counted elsewhere) and dangling account links behind.
        using var conn = Connect();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        var p = new { Id = id, UserId = userId };
        if (await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM BankAccounts WHERE Id = @Id AND UserId = @UserId", p, tx) == 0)
            return;
        await conn.ExecuteAsync("""
            DELETE FROM CashFlowTransactions WHERE BankAccountId = @Id AND UserId = @UserId;
            UPDATE Bills        SET BankAccountId = NULL WHERE BankAccountId = @Id AND UserId = @UserId;
            UPDATE Debts        SET BankAccountId = NULL WHERE BankAccountId = @Id AND UserId = @UserId;
            UPDATE DebtGroups   SET BankAccountId = NULL WHERE BankAccountId = @Id AND UserId = @UserId;
            UPDATE Income       SET BankAccountId = NULL WHERE BankAccountId = @Id AND UserId = @UserId;
            UPDATE SinkingFunds SET BankAccountId = NULL WHERE BankAccountId = @Id AND UserId = @UserId;
            UPDATE SinkingFunds SET FundingAccountId = NULL WHERE FundingAccountId = @Id AND UserId = @UserId;
            UPDATE Users        SET DashboardAccountId = NULL WHERE Id = @UserId AND DashboardAccountId = @Id;
            DELETE FROM BankAccounts WHERE Id = @Id AND UserId = @UserId;
            """, p, tx);
        tx.Commit();
    }

    public async Task<IEnumerable<BankAccount>> GetSharedWithMeAsync(int userId)
    {
        using var conn = Connect();
        var sql = """
            SELECT DISTINCT b.*, u.DisplayName AS OwnerDisplayName
            FROM BankAccounts b
            INNER JOIN Users u ON u.Id = b.UserId
            WHERE b.UserId != @UserId
              AND EXISTS (
                SELECT 1 FROM SharePermissions sp
                WHERE sp.EntityType = 2
                  AND sp.EntityId = b.Id
                  AND (
                    sp.SharedWithUserId = @UserId
                    OR (sp.SharedWithGroupId IS NOT NULL AND EXISTS (
                        SELECT 1 FROM UserGroupMembers m
                        WHERE m.GroupId = sp.SharedWithGroupId
                          AND m.UserId = @UserId AND m.Status = 1
                    ))
                  )
              )
            ORDER BY b.Name
            """;
        return await conn.QueryAsync<BankAccount>(sql, new { UserId = userId });
    }
}
