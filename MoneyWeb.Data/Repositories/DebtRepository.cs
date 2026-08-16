using Dapper;
using Microsoft.Data.SqlClient;
using MoneyWeb.Data.Models;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Data.Repositories;

public class DebtRepository(string connectionString) : IDebtRepository
{
    private SqlConnection Connect() => new(connectionString);

    public async Task<IEnumerable<Debt>> GetAllAsync(int userId, bool activeOnly = true)
    {
        using var conn = Connect();
        var sql = activeOnly
            ? "SELECT * FROM Debts WHERE UserId = @UserId AND IsActive = 1 ORDER BY InterestRate DESC"
            : "SELECT * FROM Debts WHERE UserId = @UserId ORDER BY InterestRate DESC";
        var debts = (await conn.QueryAsync<Debt>(sql, new { UserId = userId })).ToList();
        await AttachFeesAsync(conn, debts, userId);
        await AttachBankAccountsAsync(conn, debts);
        return debts;
    }

    public async Task<Debt?> GetByIdAsync(int id, int userId)
    {
        using var conn = Connect();
        var debt = await conn.QuerySingleOrDefaultAsync<Debt>(
            "SELECT * FROM Debts WHERE Id = @Id AND UserId = @UserId", new { Id = id, UserId = userId });
        if (debt is not null)
            await AttachFeesAsync(conn, [debt], userId);
        return debt;
    }

    public async Task<int> CreateAsync(Debt debt)
    {
        using var conn = Connect();
        var sql = """
            INSERT INTO Debts (UserId, GroupId, GroupSortOrder, BankAccountId, Name, Lender, DebtType, Balance, CreditLimit, InterestRate, MinimumPayment,
                               IsFixedPayment, PaymentDayOfMonth, LastPaymentDate, PaymentMethod, PayoffDate, IsActive,
                               PromoInterestRate, PromoExpirationDate, PromoStartDate, PromoOriginalBalance, PromoExpirationBehavior,
                               CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES (@UserId, @GroupId, @GroupSortOrder, @BankAccountId, @Name, @Lender, @DebtType, @Balance, @CreditLimit, @InterestRate, @MinimumPayment,
                    @IsFixedPayment, @PaymentDayOfMonth, @LastPaymentDate, @PaymentMethod, @PayoffDate, @IsActive,
                    @PromoInterestRate, @PromoExpirationDate, @PromoStartDate, @PromoOriginalBalance, @PromoExpirationBehavior,
                    GETUTCDATE(), GETUTCDATE())
            """;
        return await conn.ExecuteScalarAsync<int>(sql, debt);
    }

    public async Task UpdateAsync(Debt debt)
    {
        using var conn = Connect();
        var sql = """
            UPDATE Debts SET
                GroupId = @GroupId, GroupSortOrder = @GroupSortOrder, BankAccountId = @BankAccountId,
                Name = @Name, Lender = @Lender, DebtType = @DebtType, Balance = @Balance, CreditLimit = @CreditLimit,
                InterestRate = @InterestRate, MinimumPayment = @MinimumPayment,
                IsFixedPayment = @IsFixedPayment,
                PaymentDayOfMonth = @PaymentDayOfMonth, LastPaymentDate = @LastPaymentDate,
                PaymentMethod = @PaymentMethod,
                PayoffDate = @PayoffDate, IsActive = @IsActive,
                PromoInterestRate = @PromoInterestRate, PromoExpirationDate = @PromoExpirationDate,
                PromoStartDate = @PromoStartDate, PromoOriginalBalance = @PromoOriginalBalance,
                PromoExpirationBehavior = @PromoExpirationBehavior,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @Id AND UserId = @UserId
            """;
        await conn.ExecuteAsync(sql, debt);
    }

    public async Task DeleteAsync(int id, int userId)
    {
        using var conn = Connect();
        await conn.ExecuteAsync("DELETE FROM Debts WHERE Id = @Id AND UserId = @UserId", new { Id = id, UserId = userId });
    }

    public async Task UpdateLastPaymentAsync(int id, int userId, DateOnly paymentDate)
    {
        using var conn = Connect();
        await conn.ExecuteAsync(
            "UPDATE Debts SET LastPaymentDate = @PaymentDate, UpdatedAt = GETUTCDATE() WHERE Id = @Id AND UserId = @UserId",
            new { Id = id, UserId = userId, PaymentDate = paymentDate });
    }

    public async Task<IEnumerable<Debt>> GetSharedWithMeAsync(int userId)
    {
        using var conn = Connect();
        var sql = """
            SELECT DISTINCT d.*, u.DisplayName AS OwnerDisplayName
            FROM Debts d
            INNER JOIN Users u ON u.Id = d.UserId
            WHERE d.UserId != @UserId
              AND d.IsActive = 1
              AND EXISTS (
                SELECT 1 FROM SharePermissions sp
                WHERE sp.EntityType = 0
                  AND sp.EntityId = d.Id
                  AND (
                    sp.SharedWithUserId = @UserId
                    OR (sp.SharedWithGroupId IS NOT NULL AND EXISTS (
                        SELECT 1 FROM UserGroupMembers m
                        WHERE m.GroupId = sp.SharedWithGroupId
                          AND m.UserId = @UserId AND m.Status = 1
                    ))
                  )
              )
            ORDER BY d.Name
            """;
        return await conn.QueryAsync<Debt>(sql, new { UserId = userId });
    }

    private static async Task AttachFeesAsync(SqlConnection conn, List<Debt> debts, int userId)
    {
        if (debts.Count == 0) return;
        var debtIds = debts.Select(d => d.Id).ToList();
        var fees = (await conn.QueryAsync<DebtFee>(
            "SELECT * FROM DebtFees WHERE DebtId IN @Ids AND UserId = @UserId ORDER BY Id",
            new { Ids = debtIds, UserId = userId })).ToList();
        var feesByDebt = fees.GroupBy(f => f.DebtId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var debt in debts)
            if (feesByDebt.TryGetValue(debt.Id, out var df))
                debt.Fees = df;
    }

    private static async Task AttachBankAccountsAsync(SqlConnection conn, List<Debt> debts)
    {
        if (debts.Count == 0) return;
        var ids = debts.Where(d => d.BankAccountId.HasValue).Select(d => d.BankAccountId!.Value).Distinct().ToList();
        if (ids.Count == 0) return;
        var accounts = (await conn.QueryAsync<BankAccount>(
            "SELECT * FROM BankAccounts WHERE Id IN @Ids", new { Ids = ids }))
            .ToDictionary(a => a.Id);
        foreach (var debt in debts)
            if (debt.BankAccountId.HasValue && accounts.TryGetValue(debt.BankAccountId.Value, out var acct))
                debt.BankAccount = acct;
    }

    public async Task<IEnumerable<DebtFee>> GetFeesAsync(int debtId, int userId)
    {
        using var conn = Connect();
        return await conn.QueryAsync<DebtFee>(
            "SELECT * FROM DebtFees WHERE DebtId = @DebtId AND UserId = @UserId ORDER BY Id",
            new { DebtId = debtId, UserId = userId });
    }

    public async Task<int> CreateFeeAsync(DebtFee fee)
    {
        using var conn = Connect();
        return await conn.ExecuteScalarAsync<int>("""
            INSERT INTO DebtFees (DebtId, UserId, Name, Amount, Category, IsActive, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES (@DebtId, @UserId, @Name, @Amount, @Category, @IsActive, GETUTCDATE(), GETUTCDATE())
            """, fee);
    }

    public async Task UpdateFeeAsync(DebtFee fee)
    {
        using var conn = Connect();
        await conn.ExecuteAsync("""
            UPDATE DebtFees SET Name = @Name, Amount = @Amount, Category = @Category,
                IsActive = @IsActive, UpdatedAt = GETUTCDATE()
            WHERE Id = @Id AND UserId = @UserId
            """, fee);
    }

    public async Task DeleteFeeAsync(int id, int userId)
    {
        using var conn = Connect();
        await conn.ExecuteAsync(
            "DELETE FROM DebtFees WHERE Id = @Id AND UserId = @UserId",
            new { Id = id, UserId = userId });
    }
}
