using MoneyWeb.Data.Models;

namespace MoneyWeb.Data.Repositories.Interfaces;

public interface IBankAccountRepository
{
    Task<IEnumerable<BankAccount>> GetAllAsync(int userId);
    Task<BankAccount?> GetByIdAsync(int id, int userId);
    Task<int> CreateAsync(BankAccount account);
    Task UpdateAsync(BankAccount account);
    Task DeleteAsync(int id, int userId);
}
