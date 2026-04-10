using MoneyWeb.Data.Models;

namespace MoneyWeb.Data.Repositories.Interfaces;

public interface IBillRepository
{
    Task<IEnumerable<Bill>> GetAllAsync(bool activeOnly = true);
    Task<Bill?> GetByIdAsync(int id);
    Task<int> CreateAsync(Bill bill);
    Task UpdateAsync(Bill bill);
    Task DeleteAsync(int id);
}
