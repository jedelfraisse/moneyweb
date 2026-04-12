using Dapper;
using Microsoft.Data.SqlClient;
using MoneyWeb.Data.Models;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Data.Repositories;

public class KidRepository(string connectionString) : IKidRepository
{
    private SqlConnection Connect() => new(connectionString);

    // ── Kids ──────────────────────────────────────────────────────────────

    public async Task<IEnumerable<Kid>> GetByUserAsync(int userId)
    {
        using var conn = Connect();
        return await conn.QueryAsync<Kid>(
            "SELECT * FROM Kids WHERE UserId = @UserId ORDER BY Name", new { UserId = userId });
    }

    public async Task<Kid?> GetByIdAsync(int id, int userId)
    {
        using var conn = Connect();
        return await conn.QuerySingleOrDefaultAsync<Kid>(
            "SELECT * FROM Kids WHERE Id = @Id AND UserId = @UserId", new { Id = id, UserId = userId });
    }

    public async Task<int> CreateKidAsync(Kid kid)
    {
        using var conn = Connect();
        return await conn.ExecuteScalarAsync<int>("""
            INSERT INTO Kids (UserId, Name, ColorHex, CreatedAt, UpdatedAt)
            VALUES (@UserId, @Name, @ColorHex, GETUTCDATE(), GETUTCDATE());
            SELECT SCOPE_IDENTITY();
            """, kid);
    }

    public async Task UpdateKidAsync(Kid kid)
    {
        using var conn = Connect();
        await conn.ExecuteAsync("""
            UPDATE Kids SET Name = @Name, ColorHex = @ColorHex, UpdatedAt = GETUTCDATE()
            WHERE Id = @Id AND UserId = @UserId
            """, kid);
    }

    public async Task DeleteKidAsync(int id, int userId)
    {
        using var conn = Connect();
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();
        try
        {
            await conn.ExecuteAsync("DELETE FROM KidTransactions WHERE KidId = @Id AND UserId = @UserId", new { Id = id, UserId = userId }, tx);
            await conn.ExecuteAsync("DELETE FROM ChoreCompletions WHERE KidId = @Id AND UserId = @UserId", new { Id = id, UserId = userId }, tx);
            await conn.ExecuteAsync("DELETE FROM Chores WHERE KidId = @Id AND UserId = @UserId", new { Id = id, UserId = userId }, tx);
            await conn.ExecuteAsync("DELETE FROM Kids WHERE Id = @Id AND UserId = @UserId", new { Id = id, UserId = userId }, tx);
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ── Chores ────────────────────────────────────────────────────────────

    public async Task<IEnumerable<Chore>> GetChoresForKidAsync(int kidId, int userId)
    {
        using var conn = Connect();
        return await conn.QueryAsync<Chore>(
            "SELECT * FROM Chores WHERE KidId = @KidId AND UserId = @UserId ORDER BY Name",
            new { KidId = kidId, UserId = userId });
    }

    public async Task<int> CreateChoreAsync(Chore chore)
    {
        using var conn = Connect();
        return await conn.ExecuteScalarAsync<int>("""
            INSERT INTO Chores (KidId, UserId, Name, Description, RewardAmount, Frequency, IsActive, CreatedAt, UpdatedAt)
            VALUES (@KidId, @UserId, @Name, @Description, @RewardAmount, @Frequency, @IsActive, GETUTCDATE(), GETUTCDATE());
            SELECT SCOPE_IDENTITY();
            """, chore);
    }

    public async Task UpdateChoreAsync(Chore chore)
    {
        using var conn = Connect();
        await conn.ExecuteAsync("""
            UPDATE Chores SET Name = @Name, Description = @Description,
                RewardAmount = @RewardAmount, Frequency = @Frequency,
                IsActive = @IsActive, UpdatedAt = GETUTCDATE()
            WHERE Id = @Id AND UserId = @UserId
            """, chore);
    }

    public async Task DeleteChoreAsync(int id, int userId)
    {
        using var conn = Connect();
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();
        try
        {
            await conn.ExecuteAsync("DELETE FROM ChoreCompletions WHERE ChoreId = @Id AND UserId = @UserId", new { Id = id, UserId = userId }, tx);
            await conn.ExecuteAsync("DELETE FROM Chores WHERE Id = @Id AND UserId = @UserId", new { Id = id, UserId = userId }, tx);
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ── Transactions ──────────────────────────────────────────────────────

    public async Task<IEnumerable<KidTransaction>> GetTransactionsAsync(int kidId, int userId)
    {
        using var conn = Connect();
        return await conn.QueryAsync<KidTransaction>(
            "SELECT * FROM KidTransactions WHERE KidId = @KidId AND UserId = @UserId ORDER BY TransactionDate, Id",
            new { KidId = kidId, UserId = userId });
    }

    public async Task<int> AddTransactionAsync(KidTransaction tx)
    {
        using var conn = Connect();
        return await conn.ExecuteScalarAsync<int>("""
            INSERT INTO KidTransactions (KidId, UserId, TransactionDate, Amount, Description, ContributorName, Source, ChoreCompletionId, CreatedAt)
            VALUES (@KidId, @UserId, @TransactionDate, @Amount, @Description, @ContributorName, @Source, @ChoreCompletionId, GETUTCDATE());
            SELECT SCOPE_IDENTITY();
            """, tx);
    }

    public async Task DeleteTransactionAsync(int id, int userId)
    {
        using var conn = Connect();
        await conn.ExecuteAsync(
            "DELETE FROM KidTransactions WHERE Id = @Id AND UserId = @UserId", new { Id = id, UserId = userId });
    }

    // ── Chore completion (atomic) ──────────────────────────────────────────

    public async Task<int> CompleteChoreAsync(ChoreCompletion completion)
    {
        using var conn = Connect();
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();
        try
        {
            var chore = await conn.QuerySingleOrDefaultAsync<Chore>(
                "SELECT * FROM Chores WHERE Id = @ChoreId AND UserId = @UserId",
                new { completion.ChoreId, completion.UserId }, tx)
                ?? throw new InvalidOperationException("Chore not found.");

            var completionId = await conn.ExecuteScalarAsync<int>("""
                INSERT INTO ChoreCompletions (ChoreId, KidId, UserId, CompletedDate, Amount, Notes, CreatedAt)
                VALUES (@ChoreId, @KidId, @UserId, @CompletedDate, @Amount, @Notes, GETUTCDATE());
                SELECT SCOPE_IDENTITY();
                """, completion, tx);

            await conn.ExecuteAsync("""
                INSERT INTO KidTransactions (KidId, UserId, TransactionDate, Amount, Description, Source, ChoreCompletionId, CreatedAt)
                VALUES (@KidId, @UserId, @CompletedDate, @Amount, @Description, @Source, @CompletionId, GETUTCDATE());
                """,
                new
                {
                    completion.KidId,
                    completion.UserId,
                    completion.CompletedDate,
                    completion.Amount,
                    Description = $"Chore: {chore.Name}",
                    Source = (int)KidTransactionSource.ChoreReward,
                    CompletionId = completionId
                }, tx);

            await tx.CommitAsync();
            return completionId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ── Balance ───────────────────────────────────────────────────────────

    public async Task<decimal> GetBalanceAsync(int kidId, int userId)
    {
        using var conn = Connect();
        return await conn.ExecuteScalarAsync<decimal>(
            "SELECT ISNULL(SUM(Amount), 0) FROM KidTransactions WHERE KidId = @KidId AND UserId = @UserId",
            new { KidId = kidId, UserId = userId });
    }
}
