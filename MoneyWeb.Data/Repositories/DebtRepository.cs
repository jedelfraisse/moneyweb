using Dapper;
using Microsoft.Data.SqlClient;
using MoneyWeb.Data.Models;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Data.Repositories;

public class DebtRepository(string connectionString) : IDebtRepository
{
    private SqlConnection Connect() => new(connectionString);

    public async Task<IEnumerable<Debt>> GetAllAsync(bool activeOnly = true)
    {
        using var conn = Connect();
        var sql = activeOnly
            ? "SELECT * FROM Debts WHERE IsActive = 1 ORDER BY InterestRate DESC"
            : "SELECT * FROM Debts ORDER BY InterestRate DESC";
        return await conn.QueryAsync<Debt>(sql);
    }

    public async Task<Debt?> GetByIdAsync(int id)
    {
        using var conn = Connect();
        return await conn.QuerySingleOrDefaultAsync<Debt>(
            "SELECT * FROM Debts WHERE Id = @Id", new { Id = id });
    }

    public async Task<int> CreateAsync(Debt debt)
    {
        using var conn = Connect();
        var sql = """
            INSERT INTO Debts (Name, Lender, Balance, InterestRate, MinimumPayment, PayoffDate, IsActive, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES (@Name, @Lender, @Balance, @InterestRate, @MinimumPayment, @PayoffDate, @IsActive, GETUTCDATE(), GETUTCDATE())
            """;
        return await conn.ExecuteScalarAsync<int>(sql, debt);
    }

    public async Task UpdateAsync(Debt debt)
    {
        using var conn = Connect();
        var sql = """
            UPDATE Debts SET
                Name = @Name, Lender = @Lender, Balance = @Balance,
                InterestRate = @InterestRate, MinimumPayment = @MinimumPayment,
                PayoffDate = @PayoffDate, IsActive = @IsActive, UpdatedAt = GETUTCDATE()
            WHERE Id = @Id
            """;
        await conn.ExecuteAsync(sql, debt);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = Connect();
        await conn.ExecuteAsync("DELETE FROM Debts WHERE Id = @Id", new { Id = id });
    }
}
