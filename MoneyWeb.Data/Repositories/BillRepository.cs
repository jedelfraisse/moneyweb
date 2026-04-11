using Dapper;
using Microsoft.Data.SqlClient;
using MoneyWeb.Data.Models;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Data.Repositories;

public class BillRepository(string connectionString) : IBillRepository
{
    private SqlConnection Connect() => new(connectionString);

    public async Task<IEnumerable<Bill>> GetAllAsync(int userId, bool activeOnly = true)
    {
        using var conn = Connect();
        var sql = $"""
            SELECT b.*, ba.Name AS BankAccountName
            FROM Bills b
            LEFT JOIN BankAccounts ba ON ba.Id = b.BankAccountId
            WHERE b.UserId = @UserId{(activeOnly ? " AND b.IsActive = 1" : "")}
            ORDER BY b.Name
            """;
        return await conn.QueryAsync<Bill>(sql, new { UserId = userId });
    }

    public async Task<Bill?> GetByIdAsync(int id, int userId)
    {
        using var conn = Connect();
        return await conn.QuerySingleOrDefaultAsync<Bill>(
            """
            SELECT b.*, ba.Name AS BankAccountName
            FROM Bills b
            LEFT JOIN BankAccounts ba ON ba.Id = b.BankAccountId
            WHERE b.Id = @Id AND b.UserId = @UserId
            """, new { Id = id, UserId = userId });
    }

    public async Task<int> CreateAsync(Bill bill)
    {
        using var conn = Connect();
        return await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO Bills (UserId, Name, Amount, Frequency, DayDue, AnnualMonth, Category,
                               PaymentMethod, BankAccountId, Notes, IsActive, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES (@UserId, @Name, @Amount, @Frequency, @DayDue, @AnnualMonth, @Category,
                    @PaymentMethod, @BankAccountId, @Notes, @IsActive, GETUTCDATE(), GETUTCDATE())
            """, bill);
    }

    public async Task UpdateAsync(Bill bill)
    {
        using var conn = Connect();
        await conn.ExecuteAsync(
            """
            UPDATE Bills SET
                Name = @Name, Amount = @Amount, Frequency = @Frequency, DayDue = @DayDue,
                AnnualMonth = @AnnualMonth, Category = @Category, PaymentMethod = @PaymentMethod,
                BankAccountId = @BankAccountId, Notes = @Notes, IsActive = @IsActive,
                UpdatedAt = GETUTCDATE()
            WHERE Id = @Id AND UserId = @UserId
            """, bill);
    }

    public async Task DeleteAsync(int id, int userId)
    {
        using var conn = Connect();
        await conn.ExecuteAsync(
            "DELETE FROM BillOccurrences WHERE BillId = @Id AND UserId = @UserId; DELETE FROM Bills WHERE Id = @Id AND UserId = @UserId",
            new { Id = id, UserId = userId });
    }

    // ── Occurrences ───────────────────────────────────────────────────────────

    public async Task<IEnumerable<BillOccurrence>> GetOccurrencesAsync(int billId, int userId)
    {
        using var conn = Connect();
        return await conn.QueryAsync<BillOccurrence>(
            """
            SELECT o.*, b.Name AS BillName
            FROM BillOccurrences o
            INNER JOIN Bills b ON b.Id = o.BillId
            WHERE o.BillId = @BillId AND o.UserId = @UserId
            ORDER BY o.DueDate DESC
            """, new { BillId = billId, UserId = userId });
    }

    public async Task<IEnumerable<BillOccurrence>> GetUpcomingOccurrencesAsync(int userId, DateOnly from, DateOnly to)
    {
        using var conn = Connect();
        return await conn.QueryAsync<BillOccurrence>(
            """
            SELECT o.*, b.Name AS BillName
            FROM BillOccurrences o
            INNER JOIN Bills b ON b.Id = o.BillId
            WHERE o.UserId = @UserId AND o.DueDate >= @From AND o.DueDate <= @To
            ORDER BY o.DueDate
            """, new { UserId = userId, From = from, To = to });
    }

    public async Task<int> CreateOccurrenceAsync(BillOccurrence occ)
    {
        using var conn = Connect();
        return await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO BillOccurrences
                (BillId, UserId, DueDate, EstimatedAmount, ActualAmount, Status, SubmittedDate, Notes, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES (@BillId, @UserId, @DueDate, @EstimatedAmount, @ActualAmount, @Status, @SubmittedDate, @Notes, GETUTCDATE(), GETUTCDATE())
            """, occ);
    }

    public async Task UpdateOccurrenceAsync(BillOccurrence occ)
    {
        using var conn = Connect();
        await conn.ExecuteAsync(
            """
            UPDATE BillOccurrences SET
                DueDate = @DueDate, EstimatedAmount = @EstimatedAmount, ActualAmount = @ActualAmount,
                Status = @Status, SubmittedDate = @SubmittedDate, Notes = @Notes, UpdatedAt = GETUTCDATE()
            WHERE Id = @Id AND UserId = @UserId
            """, occ);
    }

    public async Task DeleteOccurrenceAsync(int id, int userId)
    {
        using var conn = Connect();
        await conn.ExecuteAsync(
            "DELETE FROM BillOccurrences WHERE Id = @Id AND UserId = @UserId",
            new { Id = id, UserId = userId });
    }
}
