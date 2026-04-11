using MoneyWeb.Data.Models;

namespace MoneyWeb.Data.Repositories.Interfaces;

public interface IIncomeRepository
{
    Task<IEnumerable<Income>> GetAllAsync(int userId, bool activeOnly = true);
    Task<Income?> GetByIdAsync(int id, int userId);
    Task<int> CreateAsync(Income income);
    Task UpdateAsync(Income income);
    Task DeleteAsync(int id, int userId);
    Task AdvanceNextPaymentAsync(int id, int userId);
    Task<IEnumerable<Income>> GetSharedWithMeAsync(int userId);
}
