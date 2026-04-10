using Dapper;
using Microsoft.Data.SqlClient;
using MoneyWeb.Data.Models;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Data.Repositories;

public class BillRepository(string connectionString) : IBillRepository
{
    private SqlConnection Connect() => new(connectionString);

    public async Task<IEnumerable<Bill>> GetAllAsync(bool activeOnly = true)
    {
        using var conn = Connect();
        var sql = activeOnly
            ? "SELECT * FROM Bills WHERE IsActive = 1 ORDER BY Name"
            : "SELECT * FROM Bills ORDER BY Name";
        return await conn.QueryAsync<Bill>(sql);
    }

    public async Task<Bill?> GetByIdAsync(int id)
    {
        using var conn = Connect();
        return await conn.QuerySingleOrDefaultAsync<Bill>(
            "SELECT * FROM Bills WHERE Id = @Id", new { Id = id });
    }

    public async Task<int> CreateAsync(Bill bill)
    {
        using var conn = Connect();
        var sql = """
            INSERT INTO Bills (Name, Amount, Frequency, DayDue, IsActive, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES (@Name, @Amount, @Frequency, @DayDue, @IsActive, GETUTCDATE(), GETUTCDATE())
            """;
        return await conn.ExecuteScalarAsync<int>(sql, bill);
    }

    public async Task UpdateAsync(Bill bill)
    {
        using var conn = Connect();
        var sql = """
            UPDATE Bills SET
                Name = @Name, Amount = @Amount, Frequency = @Frequency,
                DayDue = @DayDue, IsActive = @IsActive, UpdatedAt = GETUTCDATE()
            WHERE Id = @Id
            """;
        await conn.ExecuteAsync(sql, bill);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = Connect();
        await conn.ExecuteAsync("DELETE FROM Bills WHERE Id = @Id", new { Id = id });
    }
}
