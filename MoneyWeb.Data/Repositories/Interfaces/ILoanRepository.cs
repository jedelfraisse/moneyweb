using MoneyWeb.Data.Models;

namespace MoneyWeb.Data.Repositories.Interfaces;

public interface ILoanRepository
{
    Task<IEnumerable<Loan>> GetAllAsync(int userId, bool unsettledOnly = false);
    Task<Loan?> GetByIdAsync(int id, int userId);
    Task<int> CreateAsync(Loan loan);
    Task UpdateAsync(Loan loan);
    Task DeleteAsync(int id, int userId);
}
