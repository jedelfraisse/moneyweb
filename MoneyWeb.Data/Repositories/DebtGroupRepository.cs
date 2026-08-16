using Dapper;
using Microsoft.Data.SqlClient;
using MoneyWeb.Data.Models;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Data.Repositories;

public class DebtGroupRepository(string connectionString) : IDebtGroupRepository
{
    private SqlConnection Connect() => new(connectionString);

    public async Task<IEnumerable<DebtGroup>> GetAllAsync(int userId)
    {
        using var conn = Connect();
        return await conn.QueryAsync<DebtGroup>(
            "SELECT * FROM DebtGroups WHERE UserId = @UserId ORDER BY Name",
            new { UserId = userId });
    }

    public async Task<DebtGroup?> GetByIdAsync(int id, int userId)
    {
        using var conn = Connect();
        return await conn.QuerySingleOrDefaultAsync<DebtGroup>(
            "SELECT * FROM DebtGroups WHERE Id = @Id AND UserId = @UserId",
            new { Id = id, UserId = userId });
    }

    public async Task<DebtGroup?> GetWithDebtsAsync(int id, int userId)
    {
        using var conn = Connect();
        var groups = await QueryGroupsWithDebts(conn, userId, groupId: id);
        return groups.FirstOrDefault();
    }

    public async Task<IEnumerable<DebtGroup>> GetAllWithDebtsAsync(int userId)
    {
        using var conn = Connect();
        return await QueryGroupsWithDebts(conn, userId, groupId: null);
    }

    private static async Task<IEnumerable<DebtGroup>> QueryGroupsWithDebts(
        SqlConnection conn, int userId, int? groupId)
    {
        var groupFilter = groupId.HasValue ? "AND g.Id = @GroupId" : string.Empty;
        var sql = $"""
            SELECT g.*, ba.Id, ba.UserId, ba.Name, ba.AccountType, ba.CurrentBalance, ba.Notes, ba.CreatedAt, ba.UpdatedAt,
                   d.Id, d.UserId, d.GroupId, d.GroupSortOrder, d.Name, d.Lender,
                   d.Balance, d.InterestRate, d.MinimumPayment, d.IsFixedPayment,
                   d.PaymentDayOfMonth, d.LastPaymentDate, d.PaymentMethod,
                   d.PayoffDate, d.IsActive,
                   d.PromoInterestRate, d.PromoExpirationDate, d.PromoStartDate, d.PromoOriginalBalance, d.PromoExpirationBehavior,
                   d.CreatedAt, d.UpdatedAt
            FROM DebtGroups g
            LEFT JOIN BankAccounts ba ON ba.Id = g.BankAccountId
            LEFT JOIN Debts d ON d.GroupId = g.Id AND d.IsActive = 1
            WHERE g.UserId = @UserId {groupFilter}
            ORDER BY g.Name, d.GroupSortOrder, d.Balance
            """;

        var groupDict = new Dictionary<int, DebtGroup>();

        await conn.QueryAsync<DebtGroup, BankAccount, Debt, DebtGroup>(
            sql,
            (group, bankAccount, debt) =>
            {
                if (!groupDict.TryGetValue(group.Id, out var existing))
                {
                    existing = group;
                    existing.BankAccount = bankAccount;
                    groupDict[group.Id] = existing;
                }
                if (debt?.Id > 0)
                    existing.Debts.Add(debt);
                return existing;
            },
            new { UserId = userId, GroupId = groupId },
            splitOn: "Id,Id");

        // Load fees and debt-level bank accounts for all debts in the result
        var allDebts = groupDict.Values.SelectMany(g => g.Debts).ToList();
        if (allDebts.Count > 0)
        {
            var debtIds = allDebts.Select(d => d.Id).ToList();
            var fees = (await conn.QueryAsync<DebtFee>(
                "SELECT * FROM DebtFees WHERE DebtId IN @Ids AND UserId = @UserId ORDER BY DebtId, Id",
                new { Ids = debtIds, UserId = userId })).ToList();
            var feesByDebt = fees.GroupBy(f => f.DebtId).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var debt in allDebts)
                if (feesByDebt.TryGetValue(debt.Id, out var df))
                    debt.Fees = df;

            var bankIds = allDebts.Where(d => d.BankAccountId.HasValue)
                                  .Select(d => d.BankAccountId!.Value).Distinct().ToList();
            if (bankIds.Count > 0)
            {
                var accounts = (await conn.QueryAsync<BankAccount>(
                    "SELECT * FROM BankAccounts WHERE Id IN @Ids", new { Ids = bankIds }))
                    .ToDictionary(a => a.Id);
                foreach (var debt in allDebts)
                    if (debt.BankAccountId.HasValue && accounts.TryGetValue(debt.BankAccountId.Value, out var acct))
                        debt.BankAccount = acct;
            }
        }

        return groupDict.Values;
    }

    public async Task<int> CreateAsync(DebtGroup group)
    {
        using var conn = Connect();
        return await conn.ExecuteScalarAsync<int>("""
            INSERT INTO DebtGroups (UserId, Name, BankAccountId, Strategy, MonthlyBudget, Notes, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES (@UserId, @Name, @BankAccountId, @Strategy, @MonthlyBudget, @Notes, GETUTCDATE(), GETUTCDATE())
            """, group);
    }

    public async Task UpdateAsync(DebtGroup group)
    {
        using var conn = Connect();
        await conn.ExecuteAsync("""
            UPDATE DebtGroups SET
                Name = @Name, BankAccountId = @BankAccountId,
                Strategy = @Strategy, MonthlyBudget = @MonthlyBudget, Notes = @Notes,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @Id AND UserId = @UserId
            """, group);
    }

    public async Task DeleteAsync(int id, int userId)
    {
        using var conn = Connect();
        // Unlink debts from the group before deleting
        await conn.ExecuteAsync(
            "UPDATE Debts SET GroupId = NULL, GroupSortOrder = 0 WHERE GroupId = @Id AND UserId = @UserId",
            new { Id = id, UserId = userId });
        await conn.ExecuteAsync(
            "DELETE FROM DebtGroups WHERE Id = @Id AND UserId = @UserId",
            new { Id = id, UserId = userId });
    }
}
