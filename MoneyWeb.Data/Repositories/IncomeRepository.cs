using Dapper;
using Microsoft.Data.SqlClient;
using MoneyWeb.Data.Models;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Data.Repositories;

public class IncomeRepository(string connectionString) : IIncomeRepository
{
    private SqlConnection Connect() => new(connectionString);

    public async Task<IEnumerable<Income>> GetAllAsync(int userId, bool activeOnly = true)
    {
        using var conn = Connect();
        var where = activeOnly ? "AND i.IsActive = 1" : string.Empty;
        var sql = $"""
            SELECT i.*, b.Id, b.Name, b.AccountType, b.CurrentBalance, b.Notes
            FROM Income i
            LEFT JOIN BankAccounts b ON b.Id = i.BankAccountId AND b.UserId = i.UserId
            WHERE i.UserId = @UserId {where}
            ORDER BY i.Name
            """;
        var results = await conn.QueryAsync<Income, BankAccount, Income>(
            sql,
            (income, bank) => { income.BankAccount = bank; return income; },
            new { UserId = userId },
            splitOn: "Id");
        return results;
    }

    public async Task<Income?> GetByIdAsync(int id, int userId)
    {
        using var conn = Connect();
        return await conn.QuerySingleOrDefaultAsync<Income>(
            "SELECT * FROM Income WHERE Id = @Id AND UserId = @UserId",
            new { Id = id, UserId = userId });
    }

    public async Task<int> CreateAsync(Income income)
    {
        using var conn = Connect();
        var sql = """
            INSERT INTO Income (UserId, BankAccountId, Name, Description, IncomeType, IsVariable,
                                EstimatedAmount, Frequency, NextPaymentDate, IsActive, Notes, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES (@UserId, @BankAccountId, @Name, @Description, @IncomeType, @IsVariable,
                    @EstimatedAmount, @Frequency, @NextPaymentDate, @IsActive, @Notes, GETUTCDATE(), GETUTCDATE())
            """;
        return await conn.ExecuteScalarAsync<int>(sql, income);
    }

    public async Task UpdateAsync(Income income)
    {
        using var conn = Connect();
        var sql = """
            UPDATE Income SET
                BankAccountId = @BankAccountId, Name = @Name, Description = @Description,
                IncomeType = @IncomeType, IsVariable = @IsVariable, EstimatedAmount = @EstimatedAmount,
                Frequency = @Frequency, NextPaymentDate = @NextPaymentDate,
                IsActive = @IsActive, Notes = @Notes, UpdatedAt = GETUTCDATE()
            WHERE Id = @Id AND UserId = @UserId
            """;
        await conn.ExecuteAsync(sql, income);
    }

    public async Task DeleteAsync(int id, int userId)
    {
        using var conn = Connect();
        await conn.ExecuteAsync("DELETE FROM Income WHERE Id = @Id AND UserId = @UserId",
            new { Id = id, UserId = userId });
    }

    public async Task AdvanceNextPaymentAsync(int id, int userId)
    {
        var income = await GetByIdAsync(id, userId);
        if (income is null) return;
        income.NextPaymentDate = income.AdvancePaymentDate();
        await UpdateAsync(income);
    }

    public async Task<IEnumerable<Income>> GetSharedWithMeAsync(int userId)
    {
        using var conn = Connect();
        var sql = """
            SELECT DISTINCT i.*, u.DisplayName AS OwnerDisplayName
            FROM Income i
            INNER JOIN Users u ON u.Id = i.UserId
            WHERE i.UserId != @UserId
              AND i.IsActive = 1
              AND EXISTS (
                SELECT 1 FROM SharePermissions sp
                WHERE sp.EntityType = 1
                  AND sp.EntityId = i.Id
                  AND (
                    sp.SharedWithUserId = @UserId
                    OR (sp.SharedWithGroupId IS NOT NULL AND EXISTS (
                        SELECT 1 FROM UserGroupMembers m
                        WHERE m.GroupId = sp.SharedWithGroupId
                          AND m.UserId = @UserId AND m.Status = 1
                    ))
                  )
              )
            ORDER BY i.Name
            """;
        return await conn.QueryAsync<Income>(sql, new { UserId = userId });
    }
}
