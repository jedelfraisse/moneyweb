using Dapper;
using Microsoft.Data.SqlClient;
using MoneyWeb.Data.Models;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Data.Repositories;

public class SinkingFundRepository(string connectionString) : ISinkingFundRepository
{
    private SqlConnection Connect() => new(connectionString);

    public async Task<IEnumerable<SinkingFund>> GetAllAsync(int userId, bool activeOnly = true)
    {
        using var conn = Connect();
        var where = activeOnly ? "AND sf.IsActive = 1" : string.Empty;
        var sql = $"""
            SELECT sf.*
            FROM SinkingFunds sf
            WHERE sf.UserId = @UserId {where}
            ORDER BY sf.NextDueDate
            """;

        var funds = (await conn.QueryAsync<SinkingFund>(sql, new { UserId = userId })).ToList();

        var accountIds = funds
            .SelectMany(f => new[] { f.BankAccountId, f.FundingAccountId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        if (accountIds.Count > 0)
        {
            var accounts = (await conn.QueryAsync<BankAccount>(
                "SELECT * FROM BankAccounts WHERE Id IN @Ids", new { Ids = accountIds }))
                .ToDictionary(a => a.Id);

            foreach (var f in funds)
            {
                if (f.BankAccountId.HasValue && accounts.TryGetValue(f.BankAccountId.Value, out var acct))
                    f.BankAccount = acct;
                if (f.FundingAccountId.HasValue && accounts.TryGetValue(f.FundingAccountId.Value, out var src))
                    f.FundingAccount = src;
            }
        }

        return funds;
    }

    public async Task<int> CreateAsync(SinkingFund fund)
    {
        using var conn = Connect();
        var sql = """
            INSERT INTO SinkingFunds
                (UserId, Name, Description, TargetAmount, Frequency, NextDueDate,
                 MonthlyContribution, CurrentSaved, BankAccountId, FundingAccountId, IsActive, CreatedAt, UpdatedAt)
            VALUES
                (@UserId, @Name, @Description, @TargetAmount, @Frequency, @NextDueDate,
                 @MonthlyContribution, @CurrentSaved, @BankAccountId, @FundingAccountId, @IsActive, GETUTCDATE(), GETUTCDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT)
            """;
        return await conn.ExecuteScalarAsync<int>(sql, fund);
    }

    public async Task UpdateAsync(SinkingFund fund)
    {
        using var conn = Connect();
        var sql = """
            UPDATE SinkingFunds SET
                Name = @Name,
                Description = @Description,
                TargetAmount = @TargetAmount,
                Frequency = @Frequency,
                NextDueDate = @NextDueDate,
                MonthlyContribution = @MonthlyContribution,
                BankAccountId = @BankAccountId,
                FundingAccountId = @FundingAccountId,
                IsActive = @IsActive,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @Id AND UserId = @UserId
            """;
        await conn.ExecuteAsync(sql, fund);
    }

    public async Task DeleteAsync(int id, int userId)
    {
        using var conn = Connect();
        await conn.ExecuteAsync(
            "DELETE FROM SinkingFunds WHERE Id = @Id AND UserId = @UserId",
            new { Id = id, UserId = userId });
    }

    public async Task AdjustSavedAsync(int id, int userId, decimal delta)
    {
        using var conn = Connect();
        var sql = """
            UPDATE SinkingFunds
            SET CurrentSaved = CASE WHEN CurrentSaved + @Delta < 0 THEN 0 ELSE CurrentSaved + @Delta END,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @Id AND UserId = @UserId
            """;
        await conn.ExecuteAsync(sql, new { Id = id, UserId = userId, Delta = delta });
    }

    public async Task MarkPaidAndResetAsync(int id, int userId)
    {
        using var conn = Connect();
        var fund = await conn.QuerySingleOrDefaultAsync<SinkingFund>(
            "SELECT * FROM SinkingFunds WHERE Id = @Id AND UserId = @UserId",
            new { Id = id, UserId = userId });

        if (fund is null) return;

        var next = fund.NextDueDate.AddMonths(fund.PeriodMonths);
        await conn.ExecuteAsync(
            "UPDATE SinkingFunds SET CurrentSaved = 0, NextDueDate = @Next, UpdatedAt = GETUTCDATE() WHERE Id = @Id AND UserId = @UserId",
            new { Id = id, UserId = userId, Next = next });
    }
}
