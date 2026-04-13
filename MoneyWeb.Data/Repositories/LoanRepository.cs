using Dapper;
using Microsoft.Data.SqlClient;
using MoneyWeb.Data.Models;
using MoneyWeb.Data.Repositories.Interfaces;

namespace MoneyWeb.Data.Repositories;

public class LoanRepository(string connectionString) : ILoanRepository
{
    private SqlConnection Connect() => new(connectionString);

    public async Task<IEnumerable<Loan>> GetAllAsync(int userId, bool unsettledOnly = false)
    {
        using var conn = Connect();
        var sql = unsettledOnly
            ? "SELECT * FROM Loans WHERE UserId = @UserId AND IsSettled = 0 ORDER BY LoanDate DESC"
            : "SELECT * FROM Loans WHERE UserId = @UserId ORDER BY LoanDate DESC";
        return await conn.QueryAsync<Loan>(sql, new { UserId = userId });
    }

    public async Task<Loan?> GetByIdAsync(int id, int userId)
    {
        using var conn = Connect();
        return await conn.QuerySingleOrDefaultAsync<Loan>(
            "SELECT * FROM Loans WHERE Id = @Id AND UserId = @UserId", new { Id = id, UserId = userId });
    }

    public async Task<int> CreateAsync(Loan loan)
    {
        using var conn = Connect();
        var sql = """
            INSERT INTO Loans (UserId, Borrower, Email, Phone, Description, Principal, OriginalPrincipal, InterestRate, AmountRepaid, LoanDate, ExpectedRepaymentDate, IsSettled, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES (@UserId, @Borrower, @Email, @Phone, @Description, @Principal, @OriginalPrincipal, @InterestRate, @AmountRepaid, @LoanDate, @ExpectedRepaymentDate, @IsSettled, GETUTCDATE(), GETUTCDATE())
            """;
        return await conn.ExecuteScalarAsync<int>(sql, loan);
    }

    public async Task UpdateAsync(Loan loan)
    {
        using var conn = Connect();
        var sql = """
            UPDATE Loans SET
                Borrower = @Borrower, Email = @Email, Phone = @Phone, Description = @Description,
                Principal = @Principal, OriginalPrincipal = @OriginalPrincipal,
                InterestRate = @InterestRate, AmountRepaid = @AmountRepaid,
                LoanDate = @LoanDate, ExpectedRepaymentDate = @ExpectedRepaymentDate,
                IsSettled = @IsSettled, UpdatedAt = GETUTCDATE()
            WHERE Id = @Id AND UserId = @UserId
            """;
        await conn.ExecuteAsync(sql, loan);
    }

    public async Task DeleteAsync(int id, int userId)
    {
        using var conn = Connect();
        await conn.ExecuteAsync("DELETE FROM Loans WHERE Id = @Id AND UserId = @UserId", new { Id = id, UserId = userId });
    }

    public async Task<IEnumerable<LoanTransaction>> GetTransactionsAsync(int loanId, int userId)
    {
        using var conn = Connect();
        return await conn.QueryAsync<LoanTransaction>(
            "SELECT * FROM LoanTransactions WHERE LoanId = @LoanId AND UserId = @UserId ORDER BY TransactionDate DESC, Id DESC",
            new { LoanId = loanId, UserId = userId });
    }

    public async Task<int> AddTransactionAsync(LoanTransaction tx)
    {
        using var conn = Connect();
        var sql = """
            INSERT INTO LoanTransactions (LoanId, UserId, TransactionDate, Type, Amount, Notes, CreatedAt)
            OUTPUT INSERTED.Id
            VALUES (@LoanId, @UserId, @TransactionDate, @Type, @Amount, @Notes, GETUTCDATE())
            """;
        return await conn.ExecuteScalarAsync<int>(sql, tx);
    }

    public async Task DeleteTransactionAsync(int id, int userId)
    {
        using var conn = Connect();
        await conn.ExecuteAsync("DELETE FROM LoanTransactions WHERE Id = @Id AND UserId = @UserId", new { Id = id, UserId = userId });
    }

    public async Task<IEnumerable<Loan>> GetAllActiveWithInterestAsync()
    {
        using var conn = Connect();
        return await conn.QueryAsync<Loan>(
            "SELECT * FROM Loans WHERE IsSettled = 0 AND InterestRate > 0");
    }

    public async Task<IEnumerable<Loan>> GetSharedWithMeAsync(int userId)
    {
        using var conn = Connect();
        var sql = """
            SELECT DISTINCT l.*, u.DisplayName AS OwnerDisplayName
            FROM Loans l
            INNER JOIN Users u ON u.Id = l.UserId
            WHERE l.UserId != @UserId
              AND l.IsSettled = 0
              AND EXISTS (
                SELECT 1 FROM SharePermissions sp
                WHERE sp.EntityType = 4
                  AND sp.EntityId = l.Id
                  AND (
                    sp.SharedWithUserId = @UserId
                    OR (sp.SharedWithGroupId IS NOT NULL AND EXISTS (
                        SELECT 1 FROM UserGroupMembers m
                        WHERE m.GroupId = sp.SharedWithGroupId
                          AND m.UserId = @UserId AND m.Status = 1
                    ))
                  )
              )
            ORDER BY l.Borrower
            """;
        return await conn.QueryAsync<Loan>(sql, new { UserId = userId });
    }
}

