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
            INSERT INTO Loans (Borrower, Description, Principal, InterestRate, AmountRepaid, LoanDate, ExpectedRepaymentDate, IsSettled, CreatedAt, UpdatedAt)
            OUTPUT INSERTED.Id
            VALUES (@Borrower, @Description, @Principal, @InterestRate, @AmountRepaid, @LoanDate, @ExpectedRepaymentDate, @IsSettled, GETUTCDATE(), GETUTCDATE())
            """;
        return await conn.ExecuteScalarAsync<int>(sql, loan);
    }

    public async Task UpdateAsync(Loan loan)
    {
        using var conn = Connect();
        var sql = """
            UPDATE Loans SET
                Borrower = @Borrower, Description = @Description, Principal = @Principal,
                InterestRate = @InterestRate, AmountRepaid = @AmountRepaid,
                LoanDate = @LoanDate, ExpectedRepaymentDate = @ExpectedRepaymentDate,
                IsSettled = @IsSettled, UpdatedAt = GETUTCDATE()
            WHERE Id = @Id
            """;
        await conn.ExecuteAsync(sql, loan);
    }

    public async Task DeleteAsync(int id, int userId)
    {
        using var conn = Connect();
        await conn.ExecuteAsync("DELETE FROM Loans WHERE Id = @Id AND UserId = @UserId", new { Id = id, UserId = userId });
    }
}
