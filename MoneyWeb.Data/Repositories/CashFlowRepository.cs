using Dapper;
using Microsoft.Data.SqlClient;
using MoneyWeb.Data.Models;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Data.Repositories;

public class CashFlowRepository(string connectionString) : ICashFlowRepository
{
    private SqlConnection Connect() => new(connectionString);

    public async Task<IEnumerable<CashFlowTransaction>> GetForAccountAsync(
        int bankAccountId, int userId, DateOnly from, DateOnly to)
    {
        using var conn = Connect();
        return await conn.QueryAsync<CashFlowTransaction>("""
            SELECT * FROM CashFlowTransactions
            WHERE BankAccountId = @BankAccountId
              AND UserId = @UserId
              AND TransactionDate >= @From
              AND TransactionDate <= @To
            ORDER BY TransactionDate, Id
            """, new { BankAccountId = bankAccountId, UserId = userId, From = from, To = to });
    }

    public async Task DeleteProjectedForSourceAsync(int referenceId, int userId, TransactionCategory category)
    {
        using var conn = Connect();
        await conn.ExecuteAsync("""
            DELETE FROM CashFlowTransactions
            WHERE ReferenceId = @ReferenceId AND UserId = @UserId
              AND Category = @Category AND IsProjected = 1 AND IsManualOverride = 0
            """, new { ReferenceId = referenceId, UserId = userId, Category = (int)category });
    }

    public async Task<int> CountManualOverridesAsync(int debtGroupId, int userId)
    {
        using var conn = Connect();
        return await conn.ExecuteScalarAsync<int>("""
            SELECT COUNT(*) FROM CashFlowTransactions
            WHERE DebtGroupId = @GroupId AND UserId = @UserId
              AND IsProjected = 1 AND IsManualOverride = 1
            """, new { GroupId = debtGroupId, UserId = userId });
    }

    public async Task DeleteProjectedForGroupAsync(int debtGroupId, int userId, bool includeManualOverrides)
    {
        using var conn = Connect();
        var overrideFilter = includeManualOverrides ? "" : "AND IsManualOverride = 0";
        await conn.ExecuteAsync($"""
            DELETE FROM CashFlowTransactions
            WHERE DebtGroupId = @GroupId AND UserId = @UserId
              AND IsProjected = 1 {overrideFilter}
            """, new { GroupId = debtGroupId, UserId = userId });
    }

    public async Task BulkInsertProjectedAsync(IEnumerable<CashFlowTransaction> transactions)
    {
        using var conn = Connect();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();
        const string sql = """
            INSERT INTO CashFlowTransactions
                (UserId, BankAccountId, TransactionDate, Description, Amount, Category,
                 ReferenceId, DebtGroupId, IsProjected, IsManualOverride, IsAutoDraft, GeneratedByStrategy, CreatedAt, UpdatedAt)
            VALUES
                (@UserId, @BankAccountId, @TransactionDate, @Description, @Amount, @Category,
                 @ReferenceId, @DebtGroupId, 1, 0, @IsAutoDraft, @GeneratedByStrategy, GETUTCDATE(), GETUTCDATE())
            """;
        foreach (var t in transactions)
            await conn.ExecuteAsync(sql, new
            {
                t.UserId, t.BankAccountId, t.TransactionDate, t.Description,
                t.Amount, Category = (int)t.Category, t.ReferenceId, t.DebtGroupId,
                t.IsAutoDraft,
                GeneratedByStrategy = t.GeneratedByStrategy.HasValue ? (int?)t.GeneratedByStrategy.Value : null
            }, tx);
        tx.Commit();
    }

    public async Task UpdateManualOverrideDateAsync(int id, int userId, DateOnly newDate)
    {
        using var conn = Connect();
        await conn.ExecuteAsync("""
            UPDATE CashFlowTransactions
            SET TransactionDate = @Date, IsManualOverride = 1, UpdatedAt = GETUTCDATE()
            WHERE Id = @Id AND UserId = @UserId
            """, new { Id = id, UserId = userId, Date = newDate });
    }

    public async Task MarkAsSubmittedAsync(int id, int userId, DateOnly submittedDate)
    {
        using var conn = Connect();
        await conn.ExecuteAsync("""
            UPDATE CashFlowTransactions
            SET IsSubmitted = 1, TransactionDate = @Date, UpdatedAt = GETUTCDATE()
            WHERE Id = @Id AND UserId = @UserId
            """, new { Id = id, UserId = userId, Date = submittedDate });
    }

    public async Task MarkAsProcessedAsync(int id, int userId)
    {
        using var conn = Connect();
        await conn.ExecuteAsync("""
            UPDATE CashFlowTransactions
            SET IsProjected = 0, UpdatedAt = GETUTCDATE()
            WHERE Id = @Id AND UserId = @UserId
            """, new { Id = id, UserId = userId });
    }

    public async Task InsertManualAsync(CashFlowTransaction t)
    {
        using var conn = Connect();
        await conn.ExecuteAsync("""
            INSERT INTO CashFlowTransactions
                (UserId, BankAccountId, TransactionDate, Description, Amount, Category,
                 ReferenceId, DebtGroupId, IsProjected, IsManualOverride, IsAutoDraft, IsSubmitted,
                 GeneratedByStrategy, CreatedAt, UpdatedAt)
            VALUES
                (@UserId, @BankAccountId, @TransactionDate, @Description, @Amount, @Category,
                 NULL, NULL, 1, 0, 0, 0,
                 NULL, GETUTCDATE(), GETUTCDATE())
            """, new
        {
            t.UserId, t.BankAccountId, t.TransactionDate, t.Description,
            t.Amount, Category = (int)t.Category
        });
    }

    public async Task DeleteAsync(int id, int userId)
    {
        using var conn = Connect();
        await conn.ExecuteAsync(
            "DELETE FROM CashFlowTransactions WHERE Id = @Id AND UserId = @UserId",
            new { Id = id, UserId = userId });
    }

    public async Task<IEnumerable<int>> GetProjectedSourceIdsAsync(int userId, TransactionCategory category)
    {
        using var conn = Connect();
        return await conn.QueryAsync<int>("""
            SELECT DISTINCT ReferenceId FROM CashFlowTransactions
            WHERE UserId = @UserId AND Category = @Category
              AND IsProjected = 1 AND ReferenceId IS NOT NULL
            """, new { UserId = userId, Category = (int)category });
    }

    public async Task<IEnumerable<int>> GetProjectedGroupIdsAsync(int userId)
    {
        using var conn = Connect();
        return await conn.QueryAsync<int>("""
            SELECT DISTINCT DebtGroupId FROM CashFlowTransactions
            WHERE UserId = @UserId AND IsProjected = 1 AND DebtGroupId IS NOT NULL
            """, new { UserId = userId });
    }
}
