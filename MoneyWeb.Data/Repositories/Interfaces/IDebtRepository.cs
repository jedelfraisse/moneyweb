using MoneyWeb.Data.Models;

namespace MoneyWeb.Data.Repositories.Interfaces;

public interface IDebtRepository
{
    Task<IEnumerable<Debt>> GetAllAsync(int userId, bool activeOnly = true);
    Task<Debt?> GetByIdAsync(int id, int userId);
    Task<int> CreateAsync(Debt debt);
    Task UpdateAsync(Debt debt);
    Task DeleteAsync(int id, int userId);
}
