using MoneyWeb.Data.Models;

namespace MoneyWeb.Data.Repositories.Interfaces;

public interface ILoanRepository
{
    Task<IEnumerable<Loan>> GetAllAsync(bool unsettledOnly = false);
    Task<Loan?> GetByIdAsync(int id);
    Task<int> CreateAsync(Loan loan);
    Task UpdateAsync(Loan loan);
    Task DeleteAsync(int id);
}
