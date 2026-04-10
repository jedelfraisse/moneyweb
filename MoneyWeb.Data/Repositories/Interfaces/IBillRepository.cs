using MoneyWeb.Data.Models;

namespace MoneyWeb.Data.Repositories.Interfaces;

public interface IBillRepository
{
    Task<IEnumerable<Bill>> GetAllAsync(int userId, bool activeOnly = true);
    Task<Bill?> GetByIdAsync(int id, int userId);
    Task<int> CreateAsync(Bill bill);
    Task UpdateAsync(Bill bill);
    Task DeleteAsync(int id, int userId);
}
